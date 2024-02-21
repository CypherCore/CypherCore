// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol;
using Bgs.Protocol.GameUtilities.V1;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Framework.Web;
using Framework.Web.Rest.Realmlist;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

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
            Dictionary<string, Variant> Params = new();
            string removeSuffix(string str)
            {
                var pos = str.LastIndexOf('_');
                if (pos != -1)
                    return str.Substring(0, pos);

                return str;
            }

            for (int i = 0; i < request.Attribute.Count; ++i)
            {
                Bgs.Protocol.Attribute attr = request.Attribute[i];
                if (attr.Name.Contains("Command_"))
                {
                    command = attr;
                    Params[removeSuffix(attr.Name)] = attr.Value;
                }
                else
                    Params[attr.Name] = attr.Value;
            }

            if (command == null)
            {
                Log.outError(LogFilter.SessionRpc, $"{GetClientInfo()} sent ClientRequest with no command.");
                return BattlenetRpcErrorCode.RpcMalformedRequest;
            }

            return removeSuffix(command.Name) switch
            {
                "Command_RealmListTicketRequest_v1" => GetRealmListTicket(Params, response),
                "Command_LastCharPlayedRequest_v1" => GetLastCharPlayed(Params, response),
                "Command_RealmListRequest_v1" => GetRealmList(Params, response),
                "Command_RealmJoinRequest_v1" => JoinRealm(Params, response),
                _ => BattlenetRpcErrorCode.RpcNotImplemented
            };
        }

        [Service(OriginalHash.GameUtilitiesService, 10)]
        BattlenetRpcErrorCode HandleGetAllValuesForAttribute(GetAllValuesForAttributeRequest request, GetAllValuesForAttributeResponse response)
        {
            if (!authed)
                return BattlenetRpcErrorCode.Denied;

            if (request.AttributeKey.Contains("Command_RealmListRequest_v1"))
            {
                Global.RealmMgr.WriteSubRegions(response);
                return BattlenetRpcErrorCode.Ok;
            }

            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode GetRealmListTicket(Dictionary<string, Variant> Params, ClientResponse response)
        {
            Variant identity = Params.LookupByKey("Param_Identity");
            if (identity != null)
            {
                string jsonString = identity.BlobValue.ToStringUtf8().TrimEnd('\0');
                int jsonStart = jsonString.IndexOf(':');
                if (jsonStart != -1)
                {
                    var realmListTicketIdentity = JsonSerializer.Deserialize<RealmListTicketIdentity>(jsonString.Substring(jsonStart + 1));
                    var gameAccount = accountInfo.GameAccounts.LookupByKey(realmListTicketIdentity.GameAccountId);
                    if (gameAccount != null)
                        gameAccountInfo = gameAccount;
                }
            }

            if (gameAccountInfo == null)
                return BattlenetRpcErrorCode.UtilServerInvalidIdentityArgs;

            if (gameAccountInfo.IsPermanenetlyBanned)
                return BattlenetRpcErrorCode.GameAccountBanned;
            else if (gameAccountInfo.IsBanned)
                return BattlenetRpcErrorCode.GameAccountSuspended;

            bool clientInfoOk = false;
            Variant clientInfo = Params.LookupByKey("Param_ClientInfo");
            if (clientInfo != null)
            {
                string jsonString = clientInfo.BlobValue.ToStringUtf8().Trim('\0');
                int jsonStart = jsonString.IndexOf(':');
                if (jsonStart != -1)
                {
                    var realmListTicketClientInformation = JsonSerializer.Deserialize<RealmListTicketClientInformation>(jsonString.Substring(jsonStart + 1));
                    clientInfoOk = true;
                    int i = 0;
                    foreach (byte b in realmListTicketClientInformation.Info.Secret)
                        clientSecret[i++] = b;
                }
            }

            if (!clientInfoOk)
                return BattlenetRpcErrorCode.WowServicesDeniedRealmListTicket;

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_BNET_LAST_LOGIN_INFO);
            stmt.AddValue(0, GetRemoteIpAddress().ToString());
            stmt.AddValue(1, (byte)locale.ToEnum<Locale>());
            stmt.AddValue(2, os);
            stmt.AddValue(3, accountInfo.Id);

            DB.Login.Execute(stmt);

            var attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_RealmListTicket";
            attribute.Value = new Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom("AuthRealmListTicket", Encoding.UTF8);
            response.Attribute.Add(attribute);

            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode GetLastCharPlayed(Dictionary<string, Variant> Params, ClientResponse response)
        {
            Variant subRegion = Params.LookupByKey("Command_LastCharPlayedRequest_v1");
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

            string subRegionId = "";
            Variant subRegion = Params.LookupByKey("Command_RealmListRequest_v1");
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

            var jsonData = Encoding.UTF8.GetBytes("JSONRealmCharacterCountList:" + JsonSerializer.Serialize(realmCharacterCounts) + "\0");
            var compressedData = ZLib.Compress(jsonData);

            compressed = BitConverter.GetBytes(jsonData.Length).Combine(compressedData);

            attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_CharacterCountList";
            attribute.Value = new Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
            response.Attribute.Add(attribute);
            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode JoinRealm(Dictionary<string, Variant> Params, ClientResponse response)
        {
            Variant realmAddress = Params.LookupByKey("Param_RealmAddress");
            if (realmAddress != null)
                return Global.RealmMgr.JoinRealm((uint)realmAddress.UintValue, build, GetRemoteIpAddress(), clientSecret, (Locale)Enum.Parse(typeof(Locale), locale), os, _timezoneOffset, gameAccountInfo.Name, response);

            return BattlenetRpcErrorCode.WowServicesInvalidJoinTicket;
        }
    }
}