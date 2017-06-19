/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

using Bgs.Protocol.GameUtilities.V1;
using Framework.Constants;
using Framework.Rest;
using Framework.Serialization;
using Game.Services;
using Google.Protobuf;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [BnetService(NameHash.GameUtilitiesService, 1)]
        BattlenetRpcErrorCode HandleProcessClientRequest(ClientRequest request)
        {
            Bgs.Protocol.Attribute command = null;
            Dictionary<string, Bgs.Protocol.Variant> Params = new Dictionary<string, Bgs.Protocol.Variant>();

            for (int i = 0; i < request.Attribute.Count; ++i)
            {
                Bgs.Protocol.Attribute attr = request.Attribute[i];
                Params[attr.Name] = attr.Value;
                if (attr.Name.Contains("Command_"))
                    command = attr;
            }

            if (command == null)
            {
                Log.outError(LogFilter.SessionRpc, "{0} sent ClientRequest with no command.", GetPlayerInfo());
                return BattlenetRpcErrorCode.RpcMalformedRequest;
            }
            ClientResponse response = new ClientResponse();

            var status = BattlenetRpcErrorCode.RpcNotImplemented;
            if (command.Name == "Command_RealmListRequest_v1_b9")
                status= HandleRealmListRequest(Params, response);
            else if (command.Name == "Command_RealmJoinRequest_v1_b9")
                status= HandleRealmJoinRequest(Params, response);

            if (status == 0)
                SendBattlenetResponse((uint)NameHash.GameUtilitiesService, 1, response);

            return status;
        }

        [BnetService(NameHash.GameUtilitiesService, 2)]
        BattlenetRpcErrorCode HandlePresenceChannelCreated(PresenceChannelCreatedRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.PresenceChannelCreated: {1}", GetPlayerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        [BnetService(NameHash.GameUtilitiesService, 3)]
        BattlenetRpcErrorCode HandleGetPlayerVariables(GetPlayerVariablesRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.GetPlayerVariables: {1}", GetPlayerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        [BnetService(NameHash.GameUtilitiesService, 6)]
        BattlenetRpcErrorCode HandleProcessServerRequest(ServerRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.ProcessServerRequest: {1}", GetPlayerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        [BnetService(NameHash.GameUtilitiesService, 7)]
        BattlenetRpcErrorCode HandleOnGameAccountOnline(GameAccountOnlineNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.OnGameAccountOnline: {1}", GetPlayerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        [BnetService(NameHash.GameUtilitiesService, 8)]
        BattlenetRpcErrorCode HandleOnGameAccountOffline(GameAccountOfflineNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.OnGameAccountOffline: {1}", GetPlayerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        [BnetService(NameHash.GameUtilitiesService, 9)]
        BattlenetRpcErrorCode HandleGetAchievementsFile(GetAchievementsFileRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.GetAchievementsFile: {1}", GetPlayerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        [BnetService(NameHash.GameUtilitiesService, 10)]
        BattlenetRpcErrorCode HandleGetAllValuesForAttribute(GetAllValuesForAttributeRequest request)
        {
            if (request.AttributeKey == "Command_RealmListRequest_v1_b9")
            {
                GetAllValuesForAttributeResponse response = new GetAllValuesForAttributeResponse();
                Global.RealmMgr.WriteSubRegions(response);
                SendBattlenetResponse((uint)NameHash.GameUtilitiesService, 10, response);
                return BattlenetRpcErrorCode.Ok;
            }

            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleRealmListRequest(Dictionary<string, Bgs.Protocol.Variant> Params, ClientResponse response)
        {
            string subRegionId = "";
            var subRegion = Params.LookupByKey("Command_RealmListRequest_v1_b9");
            if (subRegion != null)
                subRegionId = subRegion.StringValue;

            var compressed = Global.RealmMgr.GetRealmList(Global.WorldMgr.GetRealm().Build, subRegionId);
            if (compressed.Empty())
                return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

            Bgs.Protocol.Attribute attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_RealmList";
            attribute.Value = new Bgs.Protocol.Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
            response.Attribute.Add(attribute);

            var realmCharacterCounts = new RealmCharacterCountList();
            foreach (var characterCount in GetRealmCharacterCounts())
            {
                RealmCharacterCountEntry countEntry = new RealmCharacterCountEntry();
                countEntry.WowRealmAddress = (int)characterCount.Key;
                countEntry.Count = characterCount.Value;
                realmCharacterCounts.Counts.Add(countEntry);
            }
            compressed = Json.Deflate("JSONRealmCharacterCountList", realmCharacterCounts);

            attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_CharacterCountList";
            attribute.Value = new Bgs.Protocol.Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
            response.Attribute.Add(attribute);
            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode HandleRealmJoinRequest(Dictionary<string, Bgs.Protocol.Variant> Params, ClientResponse response)
        {
            var realmAddress = Params.LookupByKey("Param_RealmAddress");
            if (realmAddress != null)
                return Global.RealmMgr.JoinRealm((uint)realmAddress.UintValue, Global.WorldMgr.GetRealm().Build, System.Net.IPAddress.Parse(GetRemoteAddress()), GetRealmListSecret(),
                    GetSessionDbcLocale(), GetOS(), GetAccountName(), response);

            return BattlenetRpcErrorCode.Ok;
        }
    }
}
