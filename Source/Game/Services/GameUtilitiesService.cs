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

using Bgs.Protocol.GameUtilities.V1;
using Framework.Constants;
using Framework.Rest;
using Framework.Serialization;
using Game.Services;
using Google.Protobuf;
using System.Collections.Generic;

namespace Game
{
    class GameUtilitiesService : ServiceBase
    {
        public GameUtilitiesService(WorldSession session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        ClientRequest request = new ClientRequest();
                        request.MergeFrom(stream);


                        ClientResponse response = new ClientResponse();
                        BattlenetRpcErrorCode status = HandleProcessClientRequest(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method GameUtilitiesService.ProcessClientRequest(bgs.protocol.game_utilities.v1.ClientRequest: {1}) returned bgs.protocol.game_utilities.v1.ClientResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(1, token, response);
                        else
                            SendResponse(1, token, status);
                        break;
                    }
                case 2:
                    {
                        PresenceChannelCreatedRequest request = new PresenceChannelCreatedRequest();
                        request.MergeFrom(stream);


                        Bgs.Protocol.NoData response = new Bgs.Protocol.NoData();
                        BattlenetRpcErrorCode status = HandlePresenceChannelCreated(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method GameUtilitiesService.PresenceChannelCreated(bgs.protocol.game_utilities.v1.PresenceChannelCreatedRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(2, token, response);
                        else
                            SendResponse(2, token, status);
                        break;
                    }
                case 3:
                    {
                        GetPlayerVariablesRequest request = new GetPlayerVariablesRequest();
                        request.MergeFrom(stream);


                        GetPlayerVariablesResponse response = new GetPlayerVariablesResponse();
                        BattlenetRpcErrorCode status = HandleGetPlayerVariables(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method GameUtilitiesService.GetPlayerVariables(bgs.protocol.game_utilities.v1.GetPlayerVariablesRequest: {1}) returned bgs.protocol.game_utilities.v1.GetPlayerVariablesResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(3, token, response);
                        else
                            SendResponse(3, token, status);
                        break;
                    }
                case 6:
                    {
                        ServerRequest request = new ServerRequest();
                        request.MergeFrom(stream);


                        ServerResponse response = new ServerResponse();
                        BattlenetRpcErrorCode status = HandleProcessServerRequest(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method GameUtilitiesService.ProcessServerRequest(bgs.protocol.game_utilities.v1.ServerRequest: {1}) returned bgs.protocol.game_utilities.v1.ServerResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(6, token, response);
                        else
                            SendResponse(6, token, status);
                        break;
                    }
                case 7:
                    {
                        GameAccountOnlineNotification request = new GameAccountOnlineNotification();
                        request.MergeFrom(stream);


                        BattlenetRpcErrorCode status = HandleOnGameAccountOnline(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method GameUtilitiesService.OnGameAccountOnline(bgs.protocol.game_utilities.v1.GameAccountOnlineNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(7, token, status);
                        break;
                    }
                case 8:
                    {
                        GameAccountOfflineNotification request = new GameAccountOfflineNotification();
                        request.MergeFrom(stream);


                        BattlenetRpcErrorCode status = HandleOnGameAccountOffline(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method GameUtilitiesService.OnGameAccountOffline(bgs.protocol.game_utilities.v1.GameAccountOfflineNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(8, token, status);
                        break;
                    }
                case 9:
                    {
                        GetAchievementsFileRequest request = new GetAchievementsFileRequest();
                        request.MergeFrom(stream);


                        GetAchievementsFileResponse response = new GetAchievementsFileResponse();
                        BattlenetRpcErrorCode status = HandleGetAchievementsFile(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method GameUtilitiesService.GetAchievementsFile(bgs.protocol.game_utilities.v1.GetAchievementsFileRequest: {1}) returned bgs.protocol.game_utilities.v1.GetAchievementsFileResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(9, token, response);
                        else
                            SendResponse(9, token, status);
                        break;
                    }
                case 10:
                    {
                        GetAllValuesForAttributeRequest request = new GetAllValuesForAttributeRequest();
                        request.MergeFrom(stream);


                        GetAllValuesForAttributeResponse response = new GetAllValuesForAttributeResponse();
                        BattlenetRpcErrorCode status = HandleGetAllValuesForAttribute(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method GameUtilitiesService.GetAllValuesForAttribute(bgs.protocol.game_utilities.v1.GetAllValuesForAttributeRequest: {1}) returned bgs.protocol.game_utilities.v1.GetAllValuesForAttributeResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(10, token, response);
                        else
                            SendResponse(10, token, status);
                        break;
                    }
                default:
                    Log.outError(LogFilter.ServiceProtobuf, "Bad method id {0}.", methodId);
                    SendResponse(methodId, token, BattlenetRpcErrorCode.RpcInvalidMethod);
                    break;
            }
        }

        BattlenetRpcErrorCode HandleProcessClientRequest(ClientRequest request, ClientResponse response)
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
                Log.outError(LogFilter.SessionRpc, "{0} sent ClientRequest with no command.", GetCallerInfo());
                return BattlenetRpcErrorCode.RpcMalformedRequest;
            }

            if (command.Name == "Command_RealmListRequest_v1_b9")
                return HandleRealmListRequest(Params, response);
            else if (command.Name == "Command_RealmJoinRequest_v1_b9")
                return HandleRealmJoinRequest(Params, response);

            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandlePresenceChannelCreated(PresenceChannelCreatedRequest request, Bgs.Protocol.NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.PresenceChannelCreated: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGetPlayerVariables(GetPlayerVariablesRequest request, GetPlayerVariablesResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.GetPlayerVariables: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleProcessServerRequest(ServerRequest request, ServerResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.ProcessServerRequest: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnGameAccountOnline(GameAccountOnlineNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.OnGameAccountOnline: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnGameAccountOffline(GameAccountOfflineNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.OnGameAccountOffline: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGetAchievementsFile(GetAchievementsFileRequest request, GetAchievementsFileResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method GameUtilitiesService.GetAchievementsFile: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGetAllValuesForAttribute(GetAllValuesForAttributeRequest request, GetAllValuesForAttributeResponse response)
        {
            if (request.AttributeKey == "Command_RealmListRequest_v1_b9")
            {
                Global.RealmMgr.WriteSubRegions(response);
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
            foreach (var characterCount in _session.GetRealmCharacterCounts())
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
                return Global.RealmMgr.JoinRealm((uint)realmAddress.UintValue, Global.WorldMgr.GetRealm().Build, System.Net.IPAddress.Parse(_session.GetRemoteAddress()), _session.GetRealmListSecret(),
                    _session.GetSessionDbcLocale(), _session.GetOS(), _session.GetAccountName(), response);

            return BattlenetRpcErrorCode.Ok;
        }
    }
}
