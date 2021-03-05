﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol;
using Bgs.Protocol.GameUtilities.V1;
using Framework.Constants;
using Framework.Database;
using Framework.Serialization;
using Framework.Web;
using Google.Protobuf;
using System;
using System.Collections.Generic;

namespace BNetServer.Networking
{
    public partial class Session
    {
        [Service(OriginalHash.GameUtilitiesService, 1)]
        BattlenetRpcErrorCode HandleProcessClientRequest(ClientRequest request, ClientResponse response)
        {
            if (!authed)
                return BattlenetRpcErrorCode.Denied;

            Bgs.Protocol.Attribute command = null;
            var Params = new Dictionary<string, Variant>();

            for (var i = 0; i < request.Attribute.Count; ++i)
            {
                var attr = request.Attribute[i];
                Params[attr.Name] = attr.Value;
                if (attr.Name.Contains("Command_"))
                    command = attr;
            }

            if (command == null)
            {
                Log.outError(LogFilter.SessionRpc, $"{GetClientInfo()} sent ClientRequest with no command.");
                return BattlenetRpcErrorCode.RpcMalformedRequest;
            }

            return command.Name switch
            {
                "Command_RealmListTicketRequest_v1_b9" => GetRealmListTicket(Params, response),
                "Command_LastCharPlayedRequest_v1_b9" => GetLastCharPlayed(Params, response),
                "Command_RealmListRequest_v1_b9" => GetRealmList(Params, response),
                "Command_RealmJoinRequest_v1_b9" => JoinRealm(Params, response),
                _ => BattlenetRpcErrorCode.RpcNotImplemented
            };
        }

        [Service(OriginalHash.GameUtilitiesService, 10)]
        BattlenetRpcErrorCode HandleGetAllValuesForAttribute(GetAllValuesForAttributeRequest request, GetAllValuesForAttributeResponse response)
        {
            if (!authed)
                return BattlenetRpcErrorCode.Denied;

            if (request.AttributeKey == "Command_RealmListRequest_v1_b9")
            {
                Global.RealmMgr.WriteSubRegions(response);
                return BattlenetRpcErrorCode.Ok;
            }

            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode GetRealmListTicket(Dictionary<string, Variant> Params, ClientResponse response)
        {
            var identity = Params.LookupByKey("Param_Identity");
            if (identity != null)
            {
                var realmListTicketIdentity = Json.CreateObject<RealmListTicketIdentity>(identity.BlobValue.ToStringUtf8(), true);
                var gameAccount = accountInfo.GameAccounts.LookupByKey(realmListTicketIdentity.GameAccountId);
                if (gameAccount != null)
                    gameAccountInfo = gameAccount;
            }

            if (gameAccountInfo == null)
                return BattlenetRpcErrorCode.UtilServerInvalidIdentityArgs;

            if (gameAccountInfo.IsPermanenetlyBanned)
                return BattlenetRpcErrorCode.GameAccountBanned;
            else if (gameAccountInfo.IsBanned)
                return BattlenetRpcErrorCode.GameAccountSuspended;

            var clientInfoOk = false;
            var clientInfo = Params.LookupByKey("Param_ClientInfo");
            if (clientInfo != null)
            {
                var realmListTicketClientInformation = Json.CreateObject<RealmListTicketClientInformation>(clientInfo.BlobValue.ToStringUtf8(), true);
                clientInfoOk = true;
                var i = 0;
                foreach (byte b in realmListTicketClientInformation.Info.Secret)
                    clientSecret[i++] = b;
            }

            if (!clientInfoOk)
                return BattlenetRpcErrorCode.WowServicesDeniedRealmListTicket;

            var stmt = DB.Login.GetPreparedStatement(LoginStatements.UpdBnetLastLoginInfo);
            stmt.AddValue(0, GetRemoteIpEndPoint().ToString());
            stmt.AddValue(1, Enum.Parse(typeof(Locale), locale));
            stmt.AddValue(2, os);
            stmt.AddValue(3, accountInfo.Id);

            DB.Login.Execute(stmt);

            var attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_RealmListTicket";
            attribute.Value = new Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom("AuthRealmListTicket", System.Text.Encoding.UTF8);
            response.Attribute.Add(attribute);

            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode GetLastCharPlayed(Dictionary<string, Variant> Params, ClientResponse response)
        {
            var subRegion = Params.LookupByKey("Command_LastCharPlayedRequest_v1_b9");
            if (subRegion != null)
            {
                var lastPlayerChar = gameAccountInfo.LastPlayedCharacters.LookupByKey(subRegion.StringValue);
                if (lastPlayerChar != null)
                {
                    var compressed = Global.RealmMgr.GetRealmEntryJSON(lastPlayerChar.RealmId, build);
                    if (compressed.Length == 0)
                        return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

                    var attribute = new Bgs.Protocol.Attribute();
                    attribute.Name = "Param_RealmEntry";
                    attribute.Value = new Variant();
                    attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
                    response.Attribute.Add(attribute);

                    attribute = new Bgs.Protocol.Attribute();
                    attribute.Name = "Param_CharacterName";
                    attribute.Value = new Variant();
                    attribute.Value.StringValue = lastPlayerChar.CharacterName;
                    response.Attribute.Add(attribute);

                    attribute = new Bgs.Protocol.Attribute();
                    attribute.Name = "Param_CharacterGUID";
                    attribute.Value = new Variant();
                    attribute.Value.BlobValue = ByteString.CopyFrom(BitConverter.GetBytes(lastPlayerChar.CharacterGUID));
                    response.Attribute.Add(attribute);

                    attribute = new Bgs.Protocol.Attribute();
                    attribute.Name = "Param_LastPlayedTime";
                    attribute.Value = new Variant();
                    attribute.Value.IntValue = (int)lastPlayerChar.LastPlayedTime;
                    response.Attribute.Add(attribute);
                }

                return BattlenetRpcErrorCode.Ok;
            }

            return BattlenetRpcErrorCode.UtilServerUnknownRealm;
        }

        BattlenetRpcErrorCode GetRealmList(Dictionary<string, Variant> Params, ClientResponse response)
        {
            if (gameAccountInfo == null)
                return BattlenetRpcErrorCode.UserServerBadWowAccount;

            var subRegionId = "";
            var subRegion = Params.LookupByKey("Command_RealmListRequest_v1_b9");
            if (subRegion != null)
                subRegionId = subRegion.StringValue;

            var compressed = Global.RealmMgr.GetRealmList(build, subRegionId);
            if (compressed.Length == 0)
                return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

            var attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_RealmList";
            attribute.Value = new Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
            response.Attribute.Add(attribute);

            var realmCharacterCounts = new RealmCharacterCountList();
            foreach (var characterCount in gameAccountInfo.CharacterCounts)
            {
                var countEntry = new RealmCharacterCountEntry();
                countEntry.WowRealmAddress = (int)characterCount.Key;
                countEntry.Count = characterCount.Value;
                realmCharacterCounts.Counts.Add(countEntry);
            }

            compressed = Json.Deflate("JSONRealmCharacterCountList", realmCharacterCounts);

            attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_CharacterCountList";
            attribute.Value = new Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
            response.Attribute.Add(attribute);
            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode JoinRealm(Dictionary<string, Variant> Params, ClientResponse response)
        {
            var realmAddress = Params.LookupByKey("Param_RealmAddress");
            if (realmAddress != null)
                return Global.RealmMgr.JoinRealm((uint)realmAddress.UintValue, build, GetRemoteIpEndPoint().Address, clientSecret, (Locale)Enum.Parse(typeof(Locale), locale), os, gameAccountInfo.Name, response);

            return BattlenetRpcErrorCode.WowServicesInvalidJoinTicket;
        }
    }
}
