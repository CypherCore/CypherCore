// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol.GameUtilities.V1;
using Framework.Constants;
using Framework.IO;
using Framework.Web;
using Framework.Web.Rest.Realmlist;
using Game.Services;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Game
{
    public partial class WorldSession
    {
        [Service(OriginalHash.GameUtilitiesService, 1)]
        BattlenetRpcErrorCode HandleProcessClientRequest(ClientRequest request, ClientResponse response)
        {
            Bgs.Protocol.Attribute command = null;
            Dictionary<string, Bgs.Protocol.Variant> Params = new();
            string removeSuffix(string str)
            {
                var pos = str.IndexOf('_');
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
                Log.outError(LogFilter.SessionRpc, "{0} sent ClientRequest with no command.", GetPlayerInfo());
                return BattlenetRpcErrorCode.RpcMalformedRequest;
            }

            return removeSuffix(command.Name) switch
            {
                "Command_RealmListRequest_v1" => HandleRealmListRequest(Params, response),
                "Command_RealmJoinRequest_v1" => HandleRealmJoinRequest(Params, response),
                _ => BattlenetRpcErrorCode.RpcNotImplemented
            };
        }

        [Service(OriginalHash.GameUtilitiesService, 10)]
        BattlenetRpcErrorCode HandleGetAllValuesForAttribute(GetAllValuesForAttributeRequest request, GetAllValuesForAttributeResponse response)
        {
            if (!request.AttributeKey.Contains("Command_RealmListRequest_v1"))
            {
                Global.RealmMgr.WriteSubRegions(response);
                return BattlenetRpcErrorCode.Ok;
            }

            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleRealmListRequest(Dictionary<string, Bgs.Protocol.Variant> Params, ClientResponse response)
        {
            string subRegionId = "";
            var subRegion = Params.LookupByKey("Command_RealmListRequest_v1");
            if (subRegion != null)
                subRegionId = subRegion.StringValue;

            var compressed = Global.RealmMgr.GetRealmList(Global.WorldMgr.GetRealm().Build, subRegionId);
            if (compressed.Empty())
                return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

            Bgs.Protocol.Attribute attribute = new();
            attribute.Name = "Param_RealmList";
            attribute.Value = new Bgs.Protocol.Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
            response.Attribute.Add(attribute);

            var realmCharacterCounts = new RealmCharacterCountList();
            foreach (var characterCount in GetRealmCharacterCounts())
            {
                RealmCharacterCountEntry countEntry = new();
                countEntry.WowRealmAddress = (int)characterCount.Key;
                countEntry.Count = characterCount.Value;
                realmCharacterCounts.Counts.Add(countEntry);
            }

            var jsonData = Encoding.UTF8.GetBytes("JSONRealmCharacterCountList:" + JsonSerializer.Serialize(realmCharacterCounts) + "\0");
            var compressedData = ZLib.Compress(jsonData);

            compressed = BitConverter.GetBytes(jsonData.Length).Combine(compressedData);

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
                    GetSessionDbcLocale(), GetOS(), GetTimezoneOffset(), GetAccountName(), response);

            return BattlenetRpcErrorCode.Ok;
        }
    }
}