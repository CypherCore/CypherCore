/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Framework.Cryptography;
using Framework.Database;
using Framework.Rest;
using Framework.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;
using System.Collections.Concurrent;

public class RealmManager : Singleton<RealmManager>
{
    RealmManager() { }

    public void Initialize(int updateInterval)
    {
        _updateTimer = new Timer(TimeSpan.FromSeconds(updateInterval).TotalMilliseconds);
        _updateTimer.Elapsed += UpdateRealms;

        UpdateRealms(null, null);

        _updateTimer.Start();
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
        PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_REALMLIST);
        SQLResult result = DB.Login.Query(stmt);
        Dictionary<RealmHandle, string> existingRealms = new Dictionary<RealmHandle, string>();
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

                realm.Id = new RealmHandle(region, battlegroup, realmId);

                UpdateRealm(realm);

                var subRegion = new RealmHandle(region, battlegroup, 0).GetAddressString();
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

    public Realm GetRealm(RealmHandle id)
    {
        return _realms.LookupByKey(id);
    }

    RealmBuildInfo GetBuildInfo(uint build)
    {
        // List of client builds for verbose version info in realmlist packet
        RealmBuildInfo[] ClientBuilds =
        {
                new RealmBuildInfo(21355, 6, 2, 4, ' '),
                new RealmBuildInfo( 20726, 6, 2, 3, ' '),
                new RealmBuildInfo(20574, 6, 2, 2, 'a'),
                new RealmBuildInfo( 20490, 6, 2, 2, 'a'),
                new RealmBuildInfo( 15595, 4, 3, 4, ' '),
                new RealmBuildInfo( 14545, 4, 2, 2, ' '),
                new RealmBuildInfo( 13623, 4, 0, 6, 'a'),
                new RealmBuildInfo( 13930, 3, 3, 5, 'a'),                                  // 3.3.5a China Mainland build
                new RealmBuildInfo( 12340, 3, 3, 5, 'a'),
                new RealmBuildInfo( 11723, 3, 3, 3, 'a'),
                new RealmBuildInfo( 11403, 3, 3, 2, ' '),
                new RealmBuildInfo( 11159, 3, 3, 0, 'a'),
                new RealmBuildInfo( 10505, 3, 2, 2, 'a'),
                new RealmBuildInfo( 9947,  3, 1, 3, ' '),
                new RealmBuildInfo( 8606,  2, 4, 3, ' '),
                new RealmBuildInfo( 6141,  1, 12, 3, ' '),
                new RealmBuildInfo( 6005,  1, 12, 2, ' '),
                new RealmBuildInfo( 5875,  1, 12, 1, ' '),
        };

        foreach (var clientBuild in ClientBuilds)
            if (clientBuild.Build == build)
                return clientBuild;

        return null;
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

    public byte[] GetRealmEntryJSON(RealmHandle id, uint build)
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

                ClientVersion version = new ClientVersion();
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

                realmEntry.CfgRealmsID = (int)realm.Id.Realm;
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

            RealmListUpdate realmListUpdate = new RealmListUpdate();
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

            realmListUpdate.Update.CfgRealmsID = (int)realm.Value.Id.Realm;
            realmListUpdate.Update.Flags = (int)flag;
            realmListUpdate.Update.Name = realm.Value.Name;
            realmListUpdate.Update.CfgConfigsID = (int)realm.Value.GetConfigId();
            realmListUpdate.Update.CfgLanguagesID = 1;

            realmListUpdate.Deleting = false;

            realmList.Updates.Add(realmListUpdate);
        }

        return Json.Deflate("JSONRealmListUpdates", realmList);
    }

    public BattlenetRpcErrorCode JoinRealm(uint realmAddress, uint build, IPAddress clientAddress, Array<byte> clientSecret, LocaleConstant locale, string os, string accountName, Bgs.Protocol.GameUtilities.V1.ClientResponse response)
    {
        Realm realm = GetRealm(new RealmHandle(realmAddress));
        if (realm != null)
        {
            if (realm.Flags.HasAnyFlag(RealmFlags.Offline) || realm.Build != build)
                return BattlenetRpcErrorCode.UserServerNotPermittedOnRealm;

            RealmListServerIPAddresses serverAddresses = new RealmListServerIPAddresses();
            AddressFamily addressFamily = new AddressFamily();
            addressFamily.Id = 1;

            var address = new Address();
            address.Ip = realm.GetAddressForClient(clientAddress).Address.ToString();
            address.Port = realm.Port;
            addressFamily.Addresses.Add(address);
            serverAddresses.Families.Add(addressFamily);

            byte[] compressed = Json.Deflate("JSONRealmListServerIPAddresses", serverAddresses);

            byte[] serverSecret = new byte[0].GenerateRandomKey(32);
            byte[] keyData = clientSecret.ToArray().Combine(serverSecret);

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LOGIN_INFO);
            stmt.AddValue(0, keyData.ToHexString());
            stmt.AddValue(1, clientAddress.ToString());
            stmt.AddValue(2, locale);
            stmt.AddValue(3, os);
            stmt.AddValue(4, accountName);
            DB.Login.DirectExecute(stmt);

            Bgs.Protocol.Attribute attribute = new Bgs.Protocol.Attribute();
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

    ConcurrentDictionary<RealmHandle, Realm> _realms = new ConcurrentDictionary<RealmHandle, Realm>();
    List<string> _subRegions = new List<string>();
    Timer _updateTimer;
}

class RealmBuildInfo
{
    public RealmBuildInfo(uint build, uint majorVersion, uint minorVersion, uint bugfixVersion, char hotfixVersion = ' ')
    {
        Build = build;
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        BugfixVersion = bugfixVersion;
        HotfixVersion = hotfixVersion;
    }

    public uint Build;
    public uint MajorVersion;
    public uint MinorVersion;
    public uint BugfixVersion;
    public char HotfixVersion;
}
