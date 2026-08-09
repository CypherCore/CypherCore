// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol.GameUtilities.V2.Client;
using Bgs.Protocol.V2;
using Framework.ClientBuild;
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
        string ParseParamName(string command)
        {
            if (command.StartsWith("Command_"))
            {
                int pos = command.LastIndexOf('_');
                if (pos != -1)
                    command = command.Substring(0, pos);
            }

            return command;
        }

        object FindParamValue(List<(string, object)> Params, string paramName)
        {
            var itr = Params.Find(pair => pair.Item1 == paramName);
            return itr != default ? itr.Item2 : default;
        }

        byte[] CompressJson(string json)
        {
            var jsonData = Encoding.UTF8.GetBytes(json);
            return BitConverter.GetBytes(jsonData.Length).Combine(ZLib.Compress(jsonData));
        }

        BattlenetRpcErrorCode HandleClientRequest(List<(string, object)> Params, List<(string, object)> responseValues)
        {
            var command = Params.Find(pair => pair.Item1.StartsWith("Command_"));
            if (command == default)
            {
                Log.outError(LogFilter.SessionRpc, $"{GetClientInfo()} sent ClientRequest with no command.");
                return BattlenetRpcErrorCode.RpcMalformedRequest;
            }

            switch (command.Item1)
            {
                case "Command_LastCharPlayedRequest_v1":
                    return GetLastCharPlayed(Params, responseValues);
                case "Command_RealmListTicketRequest_v1":
                    return GetRealmListTicket(Params, responseValues);
                case "Command_RealmListRequest_v1":
                    return GetRealmList(Params, responseValues);
                case "Command_RealmJoinRequest_v1":
                    return JoinRealm(Params, responseValues);
                case "Command_FetchBleepProxiesRequest_v1":
                    return GetBleepProxies(Params, responseValues);
                default:
                    Log.outError(LogFilter.SessionRpc, $"{GetClientInfo()} sent ClientRequest with unknown command {command.Item1}.");
                    return BattlenetRpcErrorCode.NotImplemented;

            }
        }

        BattlenetRpcErrorCode HandleGetAllValuesForAttribute(string command, List<object> responseValues)
        {
            if (!IsAuthed())
                return BattlenetRpcErrorCode.Denied;

            switch (command)
            {
                case "Command_RealmListRequest_v1":
                    foreach (string subRegion in Global.RealmMgr.GetSubRegions())
                        responseValues.Add(subRegion);
                    return BattlenetRpcErrorCode.Ok;
                default:
                    break;
            }

            Log.outError(LogFilter.SessionRpc, $"{GetClientInfo()} sent GetAllValuesForAttributeRequest with unknown command {command}.");
            return BattlenetRpcErrorCode.NotImplemented;
        }

        BattlenetRpcErrorCode GetLastCharPlayed(List<(string, object)> Params, List<(string, object)> responseValues)
        {
            object subRegion = FindParamValue(Params, "Command_LastCharPlayedRequest_v1");
            if (subRegion == null || !(subRegion is string))
                return BattlenetRpcErrorCode.UtilServerUnknownRealm;

            LastPlayedCharacterInfo lastPlayerChar = GetLastPlayedCharacter((string)subRegion);
            if (lastPlayerChar != null)
            {
                byte[] realmEntryJson = Global.RealmMgr.GetRealmEntryJSON(lastPlayerChar.RealmId, GetBuild(), GetGameAccountInfo().SecurityLevel);
                if (realmEntryJson.Length == 0)
                    return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

                byte[] guidData = BitConverter.GetBytes(lastPlayerChar.CharacterGUID);

                responseValues.Add(("Param_RealmEntry", realmEntryJson));
                responseValues.Add(("Param_CharacterName", lastPlayerChar.CharacterName));
                responseValues.Add(("Param_CharacterGUID", guidData));
                responseValues.Add(("Param_LastPlayedTime", (long)lastPlayerChar.LastPlayedTime));
            }

            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode GetRealmListTicket(List<(string, object)> Params, List<(string, object)> responseValues)
        {
            GameAccountInfo gameAccountInfo = null;

            object identity = FindParamValue(Params, "Param_Identity");
            if (identity != null && identity is byte[])
            {
                string json = Encoding.UTF8.GetString((byte[])identity);
                int jsonStart = json.IndexOf(':');
                if (jsonStart != -1)
                {
                    RealmListTicketIdentity data = JsonSerializer.Deserialize<RealmListTicketIdentity>(json.Substring(jsonStart + 1));
                    gameAccountInfo = GetGameAccountInfo((uint)data.GameAccountId);
                }
            }

            if (gameAccountInfo == null)
                return BattlenetRpcErrorCode.UtilServerInvalidIdentityArgs;

            if (gameAccountInfo.IsPermanenetlyBanned)
                return BattlenetRpcErrorCode.GameAccountBanned;
            if (gameAccountInfo.IsBanned)
                return BattlenetRpcErrorCode.GameAccountSuspended;

            ClientBuildVariantId clientBuildVariant = default;
            byte[] clientSecret = null;
            object clientInfo = FindParamValue(Params, "Param_ClientInfo");
            if (clientInfo != null)
            {
                string json = Encoding.UTF8.GetString((byte[])clientInfo);
                var jsonStart = json.IndexOf(':');
                if (jsonStart != -1)
                {
                    RealmListTicketClientInformation data = JsonSerializer.Deserialize<RealmListTicketClientInformation>(json.Substring(jsonStart + 1));
                    if (data.Info.Secret.Count == 32 / 4)
                    {
                        clientSecret = new byte[32];
                        Buffer.BlockCopy(data.Info.Secret.ToArray(), 0, clientSecret, 0, 32);
                    }

                    clientBuildVariant = new()
                    {
                        Platform = data.Info.PlatformType,
                        Arch = data.Info.ClientArch,
                        Type = data.Info.Type
                    };
                }
            }

            if (clientSecret == null)
                return BattlenetRpcErrorCode.WowServicesDeniedRealmListTicket;

            SetClientInfo(gameAccountInfo.Id, clientBuildVariant, clientSecret);

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_BNET_LAST_LOGIN_INFO);
            stmt.AddValue(0, GetRemoteIpAddress().ToString());
            stmt.AddValue(1, GetLocale());
            stmt.AddValue(2, GetOS());
            stmt.AddValue(3, GetAccountId());

            DB.Login.Execute(stmt);

            List<byte> realmListTicket = [.. Encoding.UTF8.GetBytes("AuthRealmListTicket\0")];
            responseValues.Add(("Param_RealmListTicket", realmListTicket));

            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode GetRealmList(List<(string, object)> Params, List<(string, object)> responseValues)
        {
            string subRegionId = null;
            object subRegion = FindParamValue(Params, "Command_RealmListRequest_v1");
            if (subRegion != null && subRegion is string)
                subRegionId = (string)subRegion;

            var realmListJson = Global.RealmMgr.GetRealmList(GetBuild(), GetGameAccountInfo().SecurityLevel, subRegionId);

            if (realmListJson.Empty())
                return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

            RealmCharacterCountList realmCharacterCounts = new();
            foreach (var (realmAddress, count) in GetGameAccountInfo().CharacterCounts)
            {
                RealmCharacterCountEntry countEntry = new()
                {
                    WowRealmAddress = (int)realmAddress,
                    Count = count
                };
                realmCharacterCounts.Counts.Add(countEntry);
            }

            var characterCountsJson = CompressJson("JSONRealmCharacterCountList:" + JsonSerializer.Serialize(realmCharacterCounts));

            if (characterCountsJson.Empty())
                return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

            responseValues.Add(("Param_RealmList", realmListJson));
            responseValues.Add(("Param_CharacterCountList", characterCountsJson));

            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode JoinRealm(List<(string, object)> Params, List<(string, object)> responseValues)
        {
            object realmAddress = FindParamValue(Params, "Param_RealmAddress");
            if (realmAddress == null || realmAddress is not ulong)
                return BattlenetRpcErrorCode.UtilServerUnknownRealm;

            RealmJoinResult result = Global.RealmMgr.JoinRealm((uint)realmAddress, GetBuild(), GetBuildVariant(),
                 GetRemoteIpAddress(), GetClientSecret(), GetLocale().ToEnum<Locale>(), GetOS(),
                 GetTimezoneOffset(), GetGameAccountInfo().Name, GetGameAccountInfo().SecurityLevel);

            if (result.Result == BattlenetRpcErrorCode.Ok)
            {
                responseValues.Add(("Param_RealmJoinTicket", result.JoinTicket));
                responseValues.Add(("Param_ServerAddresses", result.ServerAddresses));
                responseValues.Add(("Param_JoinSecret", result.JoinSecret));
            }

            return result.Result;
        }

        BattlenetRpcErrorCode GetBleepProxies(List<(string, object)> Params, List<(string, object)> responseValues)
        {
            var proxyListJson = CompressJson("JSONBleepProxyList:" + JsonSerializer.Serialize(new BleepProxyList()));

            if (proxyListJson.Empty())
                return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

            responseValues.Add(("Param_BleepProxyList", proxyListJson));

            return BattlenetRpcErrorCode.Ok;
        }

        object FromProto(Variant from)
        {
            switch (from.TypeCase)
            {
                case Variant.TypeOneofCase.BoolValue:
                    return from.BoolValue;
                case Variant.TypeOneofCase.IntValue:
                    return from.IntValue;
                case Variant.TypeOneofCase.FloatValue:
                    return from.FloatValue;
                case Variant.TypeOneofCase.StringValue:
                    return from.StringValue;
                case Variant.TypeOneofCase.BlobValue:
                    return from.BlobValue.ToByteArray();
                case Variant.TypeOneofCase.UintValue:
                    return from.UintValue;
                default:
                    break;
            }

            return null;
        }

        Variant ToProto(object from)
        {
            Variant to = new();
            switch (from)
            {
                case bool b:
                    to.BoolValue = b;
                    break;
                case long i:
                    to.IntValue = i;
                    break;
                case double d:
                    to.FloatValue = d;
                    break;
                case string s:
                    to.StringValue = s;
                    break;
                case byte[] blob:
                    to.BlobValue = ByteString.CopyFrom(blob);
                    break;
                case ulong u:
                    to.UintValue = u;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported type: {from.GetType()}");
            }

            return to;
        }

        [Service(OriginalHash.GameUtilitiesService, 1)]
        BattlenetRpcErrorCode HandleProcessTask(ProcessTaskRequest request, ProcessTaskResponse response, Action<BattlenetRpcErrorCode, IMessage> continuation)
        {
            if (!IsAuthed())
                return BattlenetRpcErrorCode.Denied;

            List<(string, object)> Params = [];
            List<(string, object)> responseValues = [];

            foreach (Bgs.Protocol.V2.Attribute attribute in request.Attribute)
            {
                if (!attribute.HasName || attribute == null)
                    continue;

                Params.Add((ParseParamName(attribute.Name), FromProto(attribute.Value)));
            }

            BattlenetRpcErrorCode result = HandleClientRequest(Params, responseValues);

            foreach (var (name, value) in responseValues)
            {
                Bgs.Protocol.V2.Attribute attr = new()
                {
                    Name = name,
                    Value = new()
                };
                attr.Value = ToProto(value);
                response.Result.Add(attr);
            }

            return result;
        }

        [Service(OriginalHash.GameUtilitiesService, 2)]
        BattlenetRpcErrorCode HandleGetAllValuesForAttribute(GetAllValuesForAttributeRequest request, GetAllValuesForAttributeResponse response, Action<BattlenetRpcErrorCode, IMessage> continuation)
        {
            if (!IsAuthed())
                return BattlenetRpcErrorCode.Denied;

            List<object> responseValues = [];

            BattlenetRpcErrorCode result = HandleGetAllValuesForAttribute(ParseParamName(request.AttributeKey), responseValues);

            foreach (var value in responseValues)
                response.AttributeValue.Add(ToProto(value));

            return result;
        }
    }
}