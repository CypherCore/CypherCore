// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.Web;
using Framework.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;
using System.Collections.Concurrent;
using Framework.Realm;

public class RealmManager : Singleton<RealmManager>
{
    RealmManager() { }

    public void Initialize(int updateInterval)
    {
        _updateTimer = new Timer(TimeSpan.FromSeconds(updateInterval).TotalMilliseconds);
        _updateTimer.Elapsed += UpdateRealms;

        LoadBuildInfo();

        UpdateRealms(null, null);

        _updateTimer.Start();
    }

    void LoadBuildInfo()
    {
        //                                         0             1             2              3              4      5              6
        SQLResult result = DB.Login.Query("SELECT majorVersion, minorVersion, bugfixVersion, hotfixVersion, build, win64AuthSeed, mac64AuthSeed FROM build_info ORDER BY build ASC");
        if (!result.IsEmpty())
        {
            do
            {
                RealmBuildInfo build = new();
                build.MajorVersion = result.Read<uint>(0);
                build.MinorVersion = result.Read<uint>(1);
                build.BugfixVersion = result.Read<uint>(2);
                string hotfixVersion = result.Read<string>(3);
                if (!hotfixVersion.IsEmpty() && hotfixVersion.Length < build.HotfixVersion.Length)
                    build.HotfixVersion = hotfixVersion.ToCharArray();

                build.Build = result.Read<uint>(4);
                string win64AuthSeedHexStr = result.Read<string>(5);
                if (!win64AuthSeedHexStr.IsEmpty() && win64AuthSeedHexStr.Length == build.Win64AuthSeed.Length * 2)
                    build.Win64AuthSeed = win64AuthSeedHexStr.ToByteArray();

                string mac64AuthSeedHexStr = result.Read<string>(6);
                if (!mac64AuthSeedHexStr.IsEmpty() && mac64AuthSeedHexStr.Length == build.Mac64AuthSeed.Length * 2)
                    build.Mac64AuthSeed = mac64AuthSeedHexStr.ToByteArray();

                _builds.Add(build);

            } while (result.NextRow());
        }
    }

    public void Close()
    {
        _updateTimer.Close();
    }

    void UpdateRealm(Realm realm)
    {
        var oldRealm = _realms.LookupByKey(realm.Id);
        if (oldRealm != null && oldRealm == realm)
                return;

        _realms[realm.Id] = realm;
    }

    void UpdateRealms(object source, ElapsedEventArgs e)
    {
        PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_REALMLIST);
        SQLResult result = DB.Login.Query(stmt);
        Dictionary<RealmId, string> existingRealms = new();
        foreach (var p in _realms)
            existingRealms[p.Key] = p.Value.Name;

        _realms.Clear();

        // Circle through results and add them to the realm map
        if (!result.IsEmpty())
        {
            do
            {
                var realm = new Realm();
                uint realmId = result.Read<uint>(0);
                realm.Name = result.Read<string>(1);
                realm.ExternalAddress = IPAddress.Parse(result.Read<string>(2));
                realm.LocalAddress = IPAddress.Parse(result.Read<string>(3));
                realm.LocalSubnetMask = IPAddress.Parse(result.Read<string>(4));
                realm.Port = result.Read<ushort>(5);
                RealmType realmType = (RealmType)result.Read<byte>(6);
                if (realmType == RealmType.FFAPVP)
                    realmType = RealmType.PVP;
                if (realmType >= RealmType.MaxType)
                    realmType = RealmType.Normal;

                realm.Type = (byte)realmType;
                realm.Flags = (RealmFlags)result.Read<byte>(7);
                realm.Timezone = result.Read<byte>(8);
                AccountTypes allowedSecurityLevel = (AccountTypes)result.Read<byte>(9);
                realm.AllowedSecurityLevel = (allowedSecurityLevel <= AccountTypes.Administrator ? allowedSecurityLevel : AccountTypes.Administrator);
                realm.PopulationLevel = result.Read<float>(10);
                realm.Build = result.Read<uint>(11);
                byte region = result.Read<byte>(12);
                byte battlegroup = result.Read<byte>(13);

                realm.Id = new RealmId(region, battlegroup, realmId);

                UpdateRealm(realm);

                var subRegion = new RealmId(region, battlegroup, 0).GetAddressString();
                if (!_subRegions.Contains(subRegion))
                    _subRegions.Add(subRegion);

                if (!existingRealms.ContainsKey(realm.Id))
                    Log.outInfo(LogFilter.Realmlist, "Added realm \"{0}\" at {1}:{2}", realm.Name, realm.ExternalAddress.ToString(), realm.Port);
                else
                    Log.outDebug(LogFilter.Realmlist, "Updating realm \"{0}\" at {1}:{2}", realm.Name, realm.ExternalAddress.ToString(), realm.Port);

                existingRealms.Remove(realm.Id);
            }
            while (result.NextRow());
        }

        foreach (var pair in existingRealms)
            Log.outInfo(LogFilter.Realmlist, "Removed realm \"{0}\".", pair.Value);
    }

    public Realm GetRealm(RealmId id)
    {
        return _realms.LookupByKey(id);
    }

    public RealmBuildInfo GetBuildInfo(uint build)
    {
        foreach (var clientBuild in _builds)
            if (clientBuild.Build == build)
                return clientBuild;

        return null;
    }

    public uint GetMinorMajorBugfixVersionForBuild(uint build)
    {
        RealmBuildInfo buildInfo = _builds.FirstOrDefault(p => p.Build < build);
        return buildInfo != null ? (buildInfo.MajorVersion * 10000 + buildInfo.MinorVersion * 100 + buildInfo.BugfixVersion) : 0;
    }

    public void WriteSubRegions(Bgs.Protocol.GameUtilities.V1.GetAllValuesForAttributeResponse response)
    {
        foreach (string subRegion in GetSubRegions())
        {
            var variant = new Bgs.Protocol.Variant();
            variant.StringValue = subRegion;
            response.AttributeValue.Add(variant);
        }
    }

    public byte[] GetRealmEntryJSON(RealmId id, uint build)
    {
        byte[] compressed = new byte[0];
        Realm realm = GetRealm(id);
        if (realm != null)
        {
            if (!realm.Flags.HasAnyFlag(RealmFlags.Offline) && realm.Build == build)
            {
                var realmEntry = new RealmEntry();
                realmEntry.WowRealmAddress = (int)realm.Id.GetAddress();
                realmEntry.CfgTimezonesID = 1;
                realmEntry.PopulationState = Math.Max((int)realm.PopulationLevel, 1);
                realmEntry.CfgCategoriesID = realm.Timezone;

                ClientVersion version = new();
                RealmBuildInfo buildInfo = GetBuildInfo(realm.Build);
                if (buildInfo != null)
                {
                    version.Major = (int)buildInfo.MajorVersion;
                    version.Minor = (int)buildInfo.MinorVersion;
                    version.Revision = (int)buildInfo.BugfixVersion;
                    version.Build = (int)buildInfo.Build;
                }
                else
                {
                    version.Major = 6;
                    version.Minor = 2;
                    version.Revision = 4;
                    version.Build = (int)realm.Build;
                }
                realmEntry.Version = version;

                realmEntry.CfgRealmsID = (int)realm.Id.Index;
                realmEntry.Flags = (int)realm.Flags;
                realmEntry.Name = realm.Name;
                realmEntry.CfgConfigsID = (int)realm.GetConfigId();
                realmEntry.CfgLanguagesID = 1;

                compressed = Json.Deflate("JamJSONRealmEntry", realmEntry);
            }
        }

        return compressed;
    }

    public byte[] GetRealmList(uint build, string subRegion)
    {
        var realmList = new RealmListUpdates();
        foreach (var realm in _realms)
        {
            if (realm.Value.Id.GetSubRegionAddress() != subRegion)
                continue;

            RealmFlags flag = realm.Value.Flags;
            if (realm.Value.Build != build)
                flag |= RealmFlags.VersionMismatch;

            RealmListUpdate realmListUpdate = new();
            realmListUpdate.Update.WowRealmAddress = (int)realm.Value.Id.GetAddress();
            realmListUpdate.Update.CfgTimezonesID = 1;
            realmListUpdate.Update.PopulationState = (realm.Value.Flags.HasAnyFlag(RealmFlags.Offline) ? 0 : Math.Max((int)realm.Value.PopulationLevel, 1));
            realmListUpdate.Update.CfgCategoriesID = realm.Value.Timezone;

            RealmBuildInfo buildInfo = GetBuildInfo(realm.Value.Build);
            if (buildInfo != null)
            {
                realmListUpdate.Update.Version.Major = (int)buildInfo.MajorVersion;
                realmListUpdate.Update.Version.Minor = (int)buildInfo.MinorVersion;
                realmListUpdate.Update.Version.Revision = (int)buildInfo.BugfixVersion;
                realmListUpdate.Update.Version.Build = (int)buildInfo.Build;
            }
            else
            {
                realmListUpdate.Update.Version.Major = 7;
                realmListUpdate.Update.Version.Minor = 1;
                realmListUpdate.Update.Version.Revision = 0;
                realmListUpdate.Update.Version.Build = (int)realm.Value.Build;
            }

            realmListUpdate.Update.CfgRealmsID = (int)realm.Value.Id.Index;
            realmListUpdate.Update.Flags = (int)flag;
            realmListUpdate.Update.Name = realm.Value.Name;
            realmListUpdate.Update.CfgConfigsID = (int)realm.Value.GetConfigId();
            realmListUpdate.Update.CfgLanguagesID = 1;

            realmListUpdate.Deleting = false;

            realmList.Updates.Add(realmListUpdate);
        }

        return Json.Deflate("JSONRealmListUpdates", realmList);
    }

    public BattlenetRpcErrorCode JoinRealm(uint realmAddress, uint build, IPAddress clientAddress, byte[] clientSecret, Locale locale, string os, string accountName, Bgs.Protocol.GameUtilities.V1.ClientResponse response)
    {
        Realm realm = GetRealm(new RealmId(realmAddress));
        if (realm != null)
        {
            if (realm.Flags.HasAnyFlag(RealmFlags.Offline) || realm.Build != build)
                return BattlenetRpcErrorCode.UserServerNotPermittedOnRealm;

            RealmListServerIPAddresses serverAddresses = new();
            AddressFamily addressFamily = new();
            addressFamily.Id = 1;

            var address = new Address();
            address.Ip = realm.GetAddressForClient(clientAddress).Address.ToString();
            address.Port = realm.Port;
            addressFamily.Addresses.Add(address);
            serverAddresses.Families.Add(addressFamily);

            byte[] compressed = Json.Deflate("JSONRealmListServerIPAddresses", serverAddresses);

            byte[] serverSecret = new byte[0].GenerateRandomKey(32);
            byte[] keyData = clientSecret.ToArray().Combine(serverSecret);

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LOGIN_INFO);
            stmt.AddValue(0, keyData);
            stmt.AddValue(1, clientAddress.ToString());
            stmt.AddValue(2, (byte)locale);
            stmt.AddValue(3, os);
            stmt.AddValue(4, accountName);
            DB.Login.DirectExecute(stmt);

            Bgs.Protocol.Attribute attribute = new();
            attribute.Name = "Param_RealmJoinTicket";
            attribute.Value = new Bgs.Protocol.Variant();
            attribute.Value.BlobValue = Google.Protobuf.ByteString.CopyFrom(accountName, System.Text.Encoding.UTF8);
            response.Attribute.Add(attribute);

            attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_ServerAddresses";
            attribute.Value = new Bgs.Protocol.Variant();
            attribute.Value.BlobValue = Google.Protobuf.ByteString.CopyFrom(compressed);
            response.Attribute.Add(attribute);

            attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_JoinSecret";
            attribute.Value = new Bgs.Protocol.Variant();
            attribute.Value.BlobValue = Google.Protobuf.ByteString.CopyFrom(serverSecret);
            response.Attribute.Add(attribute);
            return BattlenetRpcErrorCode.Ok;
        }

        return BattlenetRpcErrorCode.UtilServerUnknownRealm;
    }

    public ICollection<Realm> GetRealms() { return _realms.Values; }
    List<string> GetSubRegions() { return _subRegions; }

    List<RealmBuildInfo> _builds = new();
    ConcurrentDictionary<RealmId, Realm> _realms = new();
    List<string> _subRegions = new();
    Timer _updateTimer;
}

public class RealmBuildInfo
{
    public uint Build;
    public uint MajorVersion;
    public uint MinorVersion;
    public uint BugfixVersion;
    public char[] HotfixVersion = new char[4];
    public byte[] Win64AuthSeed = new byte[16];
    public byte[] Mac64AuthSeed = new byte[16];
}
