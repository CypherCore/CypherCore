// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.ClientBuild;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Framework.Realm;
using Framework.Web;
using Framework.Web.Rest.Realmlist;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Timers;

public class RealmManager : Singleton<RealmManager>
{
    public static uint HardcodedDevelopmentRealmCategoryId = 1;

    RealmManager() { }

    public void Initialize(int updateInterval)
    {
        _updateTimer = new Timer(TimeSpan.FromSeconds(updateInterval).TotalMilliseconds);
        _updateTimer.Elapsed += UpdateRealms;

        ClientBuildHelper.LoadBuildInfo();

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
                uint realmId = result.Read<uint>(0);
                string name = result.Read<string>(1);
                string externalAddressString = result.Read<string>(2);
                string localAddressString = result.Read<string>(3);

                if (!IPAddress.TryParse(externalAddressString, out IPAddress externalAddress))
                {
                    Log.outError(LogFilter.Realmlist, $"Could not resolve address {externalAddressString} for realm \"{name}\" id {realmId}");
                    continue;
                }

                if (!IPAddress.TryParse(localAddressString, out IPAddress localAddress))
                {
                    Log.outError(LogFilter.Realmlist, $"Could not resolve localAddress {localAddressString} for realm \"{name}\" id {realmId}");
                    continue;
                }

                var realm = new Realm();
                realm.Addresses.Add(externalAddress);
                realm.Addresses.Add(localAddress);
                realm.Port = result.Read<ushort>(4);
                RealmType realmType = (RealmType)result.Read<byte>(5);
                if (realmType == RealmType.FFAPVP)
                    realmType = RealmType.PVP;
                if (realmType >= RealmType.MaxType)
                    realmType = RealmType.Normal;

                realm.Type = (byte)realmType;
                realm.Flags = ConvertLegacyRealmFlags((LegacyRealmFlags)result.Read<byte>(6));
                realm.Timezone = result.Read<byte>(7);
                AccountTypes allowedSecurityLevel = (AccountTypes)result.Read<byte>(8);
                realm.AllowedSecurityLevel = (allowedSecurityLevel <= AccountTypes.Administrator ? allowedSecurityLevel : AccountTypes.Administrator);
                realm.PopulationLevel = ConvertLegacyPopulationState((LegacyRealmFlags)result.Read<byte>(6), result.Read<float>(9));
                realm.Build = result.Read<uint>(10);
                byte region = result.Read<byte>(11);
                byte battlegroup = result.Read<byte>(12);

                realm.Id = new RealmId(region, battlegroup, realmId);

                UpdateRealm(realm);

                var subRegion = new RealmId(region, battlegroup, 0).GetAddressString();
                if (!_subRegions.Contains(subRegion))
                    _subRegions.Add(subRegion);

                if (!existingRealms.ContainsKey(realm.Id))
                    Log.outInfo(LogFilter.Realmlist, $"Added realm \"{realm.Name}\" at {externalAddressString}:{realm.Port}");
                else
                    Log.outDebug(LogFilter.Realmlist, $"Updating realm \"{realm.Name}\" at {externalAddressString}:{realm.Port}");

                existingRealms.Remove(realm.Id);
            }
            while (result.NextRow());
        }

        foreach (var pair in existingRealms)
            Log.outInfo(LogFilter.Realmlist, "Removed realm \"{0}\".", pair.Value);

        _removedRealms = existingRealms;

        if (_currentRealmId.HasValue)
        {
            var realm = _realms.LookupByKey(_currentRealmId.Value);
            if (realm != null)
                _currentRealmId = realm.Id;    // fill other fields of realm id
        }
    }

    public Realm GetRealm(RealmId id)
    {
        return _realms.LookupByKey(id);
    }

    public RealmId GetCurrentRealmId()
    {
        return _currentRealmId.HasValue ? _currentRealmId.Value : new();
    }

    public void SetCurrentRealmId(RealmId id)
    {
        _currentRealmId = id;
    }

    public Realm GetCurrentRealm()
    {
        if (_currentRealmId.HasValue)
            return GetRealm(_currentRealmId.Value);
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

    void FillRealmEntry(Realm realm, uint clientBuild, AccountTypes accountSecurityLevel, RealmEntry realmEntry)
    {
        realmEntry.WowRealmAddress = (int)realm.Id.GetAddress();
        realmEntry.CfgTimezonesID = 1;
        if (accountSecurityLevel >= realm.AllowedSecurityLevel || realm.PopulationLevel == RealmPopulationState.Offline)
            realmEntry.PopulationState = (int)realm.PopulationLevel;
        else
            realmEntry.PopulationState = (int)RealmPopulationState.Locked;

        realmEntry.CfgCategoriesID = realm.Timezone;

        ClientVersion version = new();
        ClientBuildInfo buildInfo = ClientBuildHelper.GetBuildInfo(realm.Build);
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

        RealmFlags flag = realm.Flags;
        if (realm.Build != clientBuild)
            flag |= RealmFlags.VersionMismatch;

        realmEntry.Version = version;

        realmEntry.CfgRealmsID = (int)realm.Id.Index;
        realmEntry.Flags = (int)flag;
        realmEntry.Name = realm.Name;
        realmEntry.CfgConfigsID = (int)realm.GetConfigId();
        realmEntry.CfgLanguagesID = 1;
    }

    public byte[] GetRealmEntryJSON(RealmId id, uint build, AccountTypes accountSecurityLevel)
    {
        byte[] compressed = [];
        Realm realm = GetRealm(id);
        if (realm != null)
        {
            if (realm.PopulationLevel != RealmPopulationState.Offline && realm.Build == build && accountSecurityLevel >= realm.AllowedSecurityLevel)
            {
                RealmEntry realmEntry = new();
                FillRealmEntry(realm, build, accountSecurityLevel, realmEntry);
                var jsonData = Encoding.UTF8.GetBytes("JamJSONRealmEntry:" + JsonSerializer.Serialize(realmEntry) + "\0");
                compressed = BitConverter.GetBytes(jsonData.Length).Combine(ZLib.Compress(jsonData));
            }
        }

        return compressed;
    }

    public byte[] GetRealmList(uint build, AccountTypes accountSecurityLevel, string subRegion)
    {
        var realmList = new RealmListUpdates();
        foreach (var (_, realm) in _realms)
        {
            if (realm.Id.GetSubRegionAddress() != subRegion)
                continue;

            RealmListUpdatePart state = new();
            FillRealmEntry(realm, build, accountSecurityLevel, state.Update);
            state.Deleting = false;
            realmList.Updates.Add(state);
        }

        foreach (var (id, _) in _removedRealms)
        {
            if (id.GetSubRegionAddress() != subRegion)
                continue;

            RealmListUpdatePart state = new();
            state.WoWRealmAddress = (int)id.GetAddress();
            state.Deleting = true;
            realmList.Updates.Add(state);
        }

        var jsonData = Encoding.UTF8.GetBytes("JSONRealmListUpdates:" + JsonSerializer.Serialize(realmList) + "\0");
        return BitConverter.GetBytes(jsonData.Length).Combine(ZLib.Compress(jsonData));
    }

    public BattlenetRpcErrorCode JoinRealm(uint realmAddress, uint build, ClientBuildVariantId buildVariant, IPAddress clientAddress, byte[] clientSecret, Locale locale, string os, TimeSpan timezoneOffset, string accountName, AccountTypes accountSecurityLevel, Bgs.Protocol.GameUtilities.V1.ClientResponse response)
    {
        Realm realm = GetRealm(new RealmId(realmAddress));
        if (realm != null)
        {
            if (realm.PopulationLevel == RealmPopulationState.Offline || realm.Build != build || accountSecurityLevel < realm.AllowedSecurityLevel)
                return BattlenetRpcErrorCode.UserServerNotPermittedOnRealm;

            RealmListServerIPAddresses serverAddresses = new();
            AddressFamily addressFamily = new();
            addressFamily.Id = 1;

            var address = new Address();
            address.Ip = realm.GetAddressForClient(clientAddress).ToString();
            address.Port = realm.Port;
            addressFamily.Addresses.Add(address);
            serverAddresses.Families.Add(addressFamily);

            var jsonData = Encoding.UTF8.GetBytes("JSONRealmListServerIPAddresses:" + JsonSerializer.Serialize(serverAddresses) + "\0");
            byte[] compressed = BitConverter.GetBytes(jsonData.Length).Combine(ZLib.Compress(jsonData));

            byte[] serverSecret = new byte[0].GenerateRandomKey(32);
            byte[] keyData = clientSecret.Combine(serverSecret);

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LOGIN_INFO);
            stmt.AddValue(0, keyData);
            stmt.AddValue(1, clientAddress.ToString());
            stmt.AddValue(2, build);
            stmt.AddValue(3, (byte)locale);
            stmt.AddValue(4, os);
            stmt.AddValue(5, (short)timezoneOffset.TotalMinutes);
            stmt.AddValue(6, accountName);
            DB.Login.DirectExecute(stmt);

            RealmJoinTicket joinTicket = new();
            joinTicket.GameAccount = accountName;
            joinTicket.Platform = buildVariant.Platform;
            joinTicket.ClientArch = buildVariant.Arch;
            joinTicket.Type = buildVariant.Type;

            Bgs.Protocol.Attribute attribute = new();
            attribute.Name = "Param_RealmJoinTicket";
            attribute.Value = new Bgs.Protocol.Variant();
            attribute.Value.BlobValue = Google.Protobuf.ByteString.CopyFromUtf8(JsonSerializer.Serialize(joinTicket));
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

    RealmFlags ConvertLegacyRealmFlags(LegacyRealmFlags legacyRealmFlags)
    {
        RealmFlags realmFlags = RealmFlags.None;
        if (legacyRealmFlags.HasAnyFlag(LegacyRealmFlags.VersionMismatch))
            realmFlags |= RealmFlags.VersionMismatch;
        return realmFlags;
    }

    RealmPopulationState ConvertLegacyPopulationState(LegacyRealmFlags legacyRealmFlags, float population)
    {
        if (legacyRealmFlags.HasAnyFlag(LegacyRealmFlags.Offline))
            return RealmPopulationState.Offline;
        if (legacyRealmFlags.HasAnyFlag(LegacyRealmFlags.Recommended))
            return RealmPopulationState.Recommended;
        if (legacyRealmFlags.HasAnyFlag(LegacyRealmFlags.New))
            return RealmPopulationState.New;
        if (legacyRealmFlags.HasAnyFlag(LegacyRealmFlags.Full) || population > 0.95f)
            return RealmPopulationState.Full;
        if (population > 0.66f)
            return RealmPopulationState.High;
        if (population > 0.33f)
            return RealmPopulationState.Medium;
        return RealmPopulationState.Low;
    }

    public ICollection<Realm> GetRealms() { return _realms.Values; }
    List<string> GetSubRegions() { return _subRegions; }

    ConcurrentDictionary<RealmId, Realm> _realms = new();
    Dictionary<RealmId, string> _removedRealms = new();
    List<string> _subRegions = new();
    Timer _updateTimer;
    RealmId? _currentRealmId;
}