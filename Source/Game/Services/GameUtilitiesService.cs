// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol.GameUtilities.V2.Client;
using Bgs.Protocol.V2;
using Framework.Constants;
using Framework.IO;
using Framework.Web;
using Framework.Web.Rest.Realmlist;
using Game.Services;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Game
{
    public partial class WorldSession
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

        BattlenetRpcErrorCode HandleClientRequest(List<(string, object)> Params, List<(string, object)> responseValues)
        {
            var command = Params.Find(p => p.Item1.StartsWith("Command_"));
            if (command == default)
            {
                Log.outError(LogFilter.SessionRpc, $"{GetPlayerInfo()} sent ClientRequest with no command.");
                return BattlenetRpcErrorCode.RpcMalformedRequest;
            }

            switch (command.Item1)
            {
                case "Command_RealmListRequest_v1":
                    return GetRealmList(Params, responseValues);
                case "Command_RealmJoinRequest_v1":
                    return JoinRealm(Params, responseValues);
                default:
                    break;
            }

            Log.outError(LogFilter.SessionRpc, $"{GetPlayerInfo()} sent ClientRequest with unknown command {command.Item1}.");
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGetAllValuesForAttribute(string command, List<object> responseValues)
        {
            switch (command)
            {
                case "Command_RealmListRequest_v1":
                    foreach (string subRegion in Global.RealmMgr.GetSubRegions())
                        responseValues.Add(subRegion);
                    return BattlenetRpcErrorCode.Ok;
                default:
                    break;
            }

            Log.outError(LogFilter.SessionRpc, $"{GetPlayerInfo()} sent GetAllValuesForAttributeRequest with unknown command {command}.");
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode GetRealmList(List<(string, object)> Params, List<(string, object)> responseValues)
        {
            string subRegionId = null;
            object subRegion = FindParamValue(Params, "Command_RealmListRequest_v1");
            if (subRegion != null && subRegion is string)
                subRegionId = (string)subRegion;

            var realmListJson = Global.RealmMgr.GetRealmList(GetClientBuild(), GetSecurity(), subRegionId);

            if (realmListJson.Empty())
                return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

            RealmCharacterCountList realmCharacterCounts = new();
            foreach (var (realmAddress, count) in GetRealmCharacterCounts())
            {
                RealmCharacterCountEntry countEntry = new()
                {
                    WowRealmAddress = (int)realmAddress,
                    Count = count
                };
                realmCharacterCounts.Counts.Add(countEntry);
            }

            string json = "JSONRealmCharacterCountList:" + JsonSerializer.Serialize(realmCharacterCounts);

            var jsonData = Encoding.UTF8.GetBytes(json);
            var characterCountsJson = BitConverter.GetBytes(jsonData.Length).Combine(ZLib.Compress(jsonData));

            if (characterCountsJson.Length == 0)
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

            RealmJoinResult result = Global.RealmMgr.JoinRealm((uint)realmAddress, GetClientBuild(), GetClientBuildVariant(),
                IPAddress.Parse(GetRemoteAddress()), GetRealmListSecret(), GetSessionDbcLocale(),
                GetOS(), GetTimezoneOffset(), GetAccountName(), GetSecurity());

            if (result.Result == BattlenetRpcErrorCode.Ok)
            {
                responseValues.Add(("Param_RealmJoinTicket", result.JoinTicket));
                responseValues.Add(("Param_ServerAddresses", result.ServerAddresses));
                responseValues.Add(("Param_JoinSecret", result.JoinSecret));
            }

            return result.Result;
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
        BattlenetRpcErrorCode HandleProcessTask(ProcessTaskRequest request, ProcessTaskResponse response, Action<WorldSession, BattlenetRpcErrorCode, IMessage> continuation)
        {
            List<(string, object)> Params = [];
            List<(string, object)> responseValues = [];

            foreach (var attribute in request.Attribute)
            {
                if (!attribute.HasName || attribute.Value == null)
                    continue;

                Params.Add((ParseParamName(attribute.Name), FromProto(attribute.Value)));
            }

            BattlenetRpcErrorCode result = HandleClientRequest(Params, responseValues);

            foreach (var (name, value) in responseValues)
            {
                Bgs.Protocol.V2.Attribute attr = new();
                attr.Name = name;
                attr.Value = ToProto(value);
                response.Result.Add(attr);
            }

            return result;
        }

        [Service(OriginalHash.GameUtilitiesService, 2)]
        BattlenetRpcErrorCode HandleGetAllValuesForAttribute(GetAllValuesForAttributeRequest request, GetAllValuesForAttributeResponse response, Action<WorldSession, BattlenetRpcErrorCode, IMessage> continuation)
        {
            List<object> responseValues = [];

            BattlenetRpcErrorCode result = HandleGetAllValuesForAttribute(ParseParamName(request.AttributeKey), responseValues);

            foreach (Variant value in responseValues)
                response.AttributeValue.Add(ToProto(value));

            return result;
        }
    }
}