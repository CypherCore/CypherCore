﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Framework.GameMath;
using Framework.Realm;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Misc;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.QueryPlayerName)]
        void HandleNameQueryRequest(QueryPlayerName queryPlayerName)
        {
            SendNameQuery(queryPlayerName.Player);
        }

        public void SendNameQuery(ObjectGuid guid)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            var response = new QueryPlayerNameResponse();
            response.Player = guid;

            if (response.Data.Initialize(guid, player))
                response.Result = ResponseCodes.Success;
            else
                response.Result = ResponseCodes.Failure; // name unknown

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.QueryTime)]
        void HandleQueryTime(QueryTime packet)
        {
            SendQueryTimeResponse();
        }

        void SendQueryTimeResponse()
        {
            var queryTimeResponse = new QueryTimeResponse();
            queryTimeResponse.CurrentTime = Time.UnixTime;
            SendPacket(queryTimeResponse);
        }

        [WorldPacketHandler(ClientOpcodes.QueryGameObject, Processing = PacketProcessing.Inplace)]
        void HandleGameObjectQuery(QueryGameObject packet)
        {
            GameObjectTemplate info = Global.ObjectMgr.GetGameObjectTemplate(packet.GameObjectID);
            if (info != null)
            {
                if (!WorldConfig.GetBoolValue(WorldCfg.CacheDataQueries))
                    info.InitializeQueryData();

                var queryGameObjectResponse = info.QueryData;

                var loc = GetSessionDbLocaleIndex();
                if (loc != Locale.enUS)
                {
                    GameObjectLocale gameObjectLocale = Global.ObjectMgr.GetGameObjectLocale(queryGameObjectResponse.GameObjectID);
                    if (gameObjectLocale != null)
                    {
                        ObjectManager.GetLocaleString(gameObjectLocale.Name, loc, ref queryGameObjectResponse.Stats.Name[0]);
                        ObjectManager.GetLocaleString(gameObjectLocale.CastBarCaption, loc, ref queryGameObjectResponse.Stats.CastBarCaption);
                        ObjectManager.GetLocaleString(gameObjectLocale.Unk1, loc, ref queryGameObjectResponse.Stats.UnkString);
                    }
                }

                SendPacket(queryGameObjectResponse);
            }
            else
            {
                Log.outDebug(LogFilter.Network, $"WORLD: CMSG_GAMEOBJECT_QUERY - Missing gameobject info for (ENTRY: {packet.GameObjectID})");

                var response = new QueryGameObjectResponse();
                response.GameObjectID = packet.GameObjectID;
                response.Guid = packet.Guid;
                SendPacket(response);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QueryCreature, Processing = PacketProcessing.Inplace)]
        void HandleCreatureQuery(QueryCreature packet)
        {
            CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate(packet.CreatureID);
            if (ci != null)
            {
                if (!WorldConfig.GetBoolValue(WorldCfg.CacheDataQueries))
                    ci.InitializeQueryData();

                var queryCreatureResponse = ci.QueryData;

                var loc = GetSessionDbLocaleIndex();
                if (loc != Locale.enUS)
                {
                    CreatureLocale creatureLocale = Global.ObjectMgr.GetCreatureLocale(ci.Entry);
                    if (creatureLocale != null)
                    {
                        var name = queryCreatureResponse.Stats.Name[0];
                        var nameAlt = queryCreatureResponse.Stats.NameAlt[0];

                        ObjectManager.GetLocaleString(creatureLocale.Name, loc, ref name);
                        ObjectManager.GetLocaleString(creatureLocale.NameAlt, loc, ref nameAlt);
                        ObjectManager.GetLocaleString(creatureLocale.Title, loc, ref queryCreatureResponse.Stats.Title);
                        ObjectManager.GetLocaleString(creatureLocale.TitleAlt, loc, ref queryCreatureResponse.Stats.TitleAlt);

                        queryCreatureResponse.Stats.Name[0] = name;
                        queryCreatureResponse.Stats.NameAlt[0] = nameAlt;
                    }
                }

                SendPacket(queryCreatureResponse);
            }
            else
            {
                Log.outDebug(LogFilter.Network, $"WORLD: CMSG_QUERY_CREATURE - NO CREATURE INFO! (ENTRY: {packet.CreatureID})");

                var response = new QueryCreatureResponse();
                response.CreatureID = packet.CreatureID;
                SendPacket(response);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QueryNpcText)]
        void HandleNpcTextQuery(QueryNPCText packet)
        {
            NpcText npcText = Global.ObjectMgr.GetNpcText(packet.TextID);

            var response = new QueryNPCTextResponse();
            response.TextID = packet.TextID;

            if (npcText != null)
            {
                for (byte i = 0; i < SharedConst.MaxNpcTextOptions; ++i)
                {
                    response.Probabilities[i] = npcText.Data[i].Probability;
                    response.BroadcastTextID[i] = npcText.Data[i].BroadcastTextID;
                    if (!response.Allow && npcText.Data[i].BroadcastTextID != 0)
                        response.Allow = true;
                }
            }

            if (!response.Allow)
                Log.outError(LogFilter.Sql, "HandleNpcTextQuery: no BroadcastTextID found for text {0} in `npc_text table`", packet.TextID);

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.QueryPageText)]
        void HandleQueryPageText(QueryPageText packet)
        {
            var response = new QueryPageTextResponse();
            response.PageTextID = packet.PageTextID;

            var pageID = packet.PageTextID;
            while (pageID != 0)
            {
                PageText pageText = Global.ObjectMgr.GetPageText(pageID);

                if (pageText == null)
                    break;

                QueryPageTextResponse.PageTextInfo page;
                page.Id = pageID;
                page.NextPageID = pageText.NextPageID;
                page.Text = pageText.Text;
                page.PlayerConditionID = pageText.PlayerConditionID;
                page.Flags = pageText.Flags;

                var locale = GetSessionDbLocaleIndex();
                if (locale != Locale.enUS)
                {
                    PageTextLocale pageLocale = Global.ObjectMgr.GetPageTextLocale(pageID);
                    if (pageLocale != null)
                        ObjectManager.GetLocaleString(pageLocale.Text, locale, ref page.Text);
                }

                response.Pages.Add(page);
                pageID = pageText.NextPageID;
            }

            response.Allow = !response.Pages.Empty();
            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.QueryCorpseLocationFromClient)]
        void HandleQueryCorpseLocation(QueryCorpseLocationFromClient queryCorpseLocation)
        {
            var packet = new CorpseLocation();
            Player player = Global.ObjAccessor.FindConnectedPlayer(queryCorpseLocation.Player);
            if (!player || !player.HasCorpse() || !_player.IsInSameRaidWith(player))
            {
                packet.Valid = false;                               // corpse not found
                packet.Player = queryCorpseLocation.Player;
                SendPacket(packet);
                return;
            }

            var corpseLocation = player.GetCorpseLocation();
            var corpseMapID = corpseLocation.GetMapId();
            var mapID = corpseLocation.GetMapId();
            var x = corpseLocation.GetPositionX();
            var y = corpseLocation.GetPositionY();
            var z = corpseLocation.GetPositionZ();

            // if corpse at different map
            if (mapID != player.GetMapId())
            {
                // search entrance map for proper show entrance
                var corpseMapEntry = CliDB.MapStorage.LookupByKey(mapID);
                if (corpseMapEntry != null)
                {
                    if (corpseMapEntry.IsDungeon() && corpseMapEntry.CorpseMapID >= 0)
                    {
                        // if corpse map have entrance
                        var entranceMap = Global.MapMgr.CreateBaseMap((uint)corpseMapEntry.CorpseMapID);
                        if (entranceMap != null)
                        {
                            mapID = (uint)corpseMapEntry.CorpseMapID;
                            x = corpseMapEntry.Corpse.X;
                            y = corpseMapEntry.Corpse.Y;
                            z = entranceMap.GetHeight(player.GetPhaseShift(), x, y, MapConst.MaxHeight);
                        }
                    }
                }
            }

            packet.Valid = true;
            packet.Player = queryCorpseLocation.Player;
            packet.MapID = (int)corpseMapID;
            packet.ActualMapID = (int)mapID;
            packet.Position = new Vector3(x, y, z);
            packet.Transport = ObjectGuid.Empty;
            SendPacket(packet);
        }

        [WorldPacketHandler(ClientOpcodes.QueryCorpseTransport)]
        void HandleQueryCorpseTransport(QueryCorpseTransport queryCorpseTransport)
        {
            var response = new CorpseTransportQuery();
            response.Player = queryCorpseTransport.Player;

            Player player = Global.ObjAccessor.FindConnectedPlayer(queryCorpseTransport.Player);
            if (player)
            {
                var corpse = player.GetCorpse();
                if (_player.IsInSameRaidWith(player) && corpse && !corpse.GetTransGUID().IsEmpty() && corpse.GetTransGUID() == queryCorpseTransport.Transport)
                {
                    response.Position = new Vector3(corpse.GetTransOffsetX(), corpse.GetTransOffsetY(), corpse.GetTransOffsetZ());
                    response.Facing = corpse.GetTransOffsetO();
                }
            }

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.QueryQuestCompletionNpcs)]
        void HandleQueryQuestCompletionNPCs(QueryQuestCompletionNPCs queryQuestCompletionNPCs)
        {
            var response = new QuestCompletionNPCResponse();

            foreach (var questID in queryQuestCompletionNPCs.QuestCompletionNPCs)
            {
                var questCompletionNPC = new QuestCompletionNPC();

                if (Global.ObjectMgr.GetQuestTemplate(questID) == null)
                {
                    Log.outDebug(LogFilter.Network, "WORLD: Unknown quest {0} in CMSG_QUEST_NPC_QUERY by {1}", questID, GetPlayer().GetGUID());
                    continue;
                }

                questCompletionNPC.QuestID = questID;

                var creatures = Global.ObjectMgr.GetCreatureQuestInvolvedRelationReverseBounds(questID);
                foreach (var id in creatures)
                    questCompletionNPC.NPCs.Add(id);

                var gos = Global.ObjectMgr.GetGOQuestInvolvedRelationReverseBounds(questID);
                foreach (var id in gos)
                    questCompletionNPC.NPCs.Add(id | 0x80000000); // GO mask

                response.QuestCompletionNPCs.Add(questCompletionNPC);
            }

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.QuestPoiQuery)]
        void HandleQuestPOIQuery(QuestPOIQuery packet)
        {
            if (packet.MissingQuestCount >= SharedConst.MaxQuestLogSize)
                return;

            // Read quest ids and add the in a unordered_set so we don't send POIs for the same quest multiple times
            var questIds = new HashSet<uint>();
            for (var i = 0; i < packet.MissingQuestCount; ++i)
                questIds.Add(packet.MissingQuestPOIs[i]); // QuestID

            var response = new QuestPOIQueryResponse();

            foreach (var questId in questIds)
            {
                if (_player.FindQuestSlot(questId) != SharedConst.MaxQuestLogSize)
                {
                    QuestPOIData poiData = Global.ObjectMgr.GetQuestPOIData(questId);
                    if (poiData != null)
                        response.QuestPOIDataStats.Add(poiData);
                }
            }

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.ItemTextQuery)]
        void HandleItemTextQuery(ItemTextQuery packet)
        {
            var queryItemTextResponse = new QueryItemTextResponse();
            queryItemTextResponse.Id = packet.Id;

            Item item = GetPlayer().GetItemByGuid(packet.Id);
            if (item)
            {
                queryItemTextResponse.Valid = true;
                queryItemTextResponse.Text = item.GetText();
            }

            SendPacket(queryItemTextResponse);
        }

        [WorldPacketHandler(ClientOpcodes.QueryRealmName)]
        void HandleQueryRealmName(QueryRealmName queryRealmName)
        {
            var realmQueryResponse = new RealmQueryResponse();
            realmQueryResponse.VirtualRealmAddress = queryRealmName.VirtualRealmAddress;

            var realmHandle = new RealmId(queryRealmName.VirtualRealmAddress);
            if (Global.ObjectMgr.GetRealmName(realmHandle.Index, ref realmQueryResponse.NameInfo.RealmNameActual, ref realmQueryResponse.NameInfo.RealmNameNormalized))
            {
                realmQueryResponse.LookupState = (byte)ResponseCodes.Success;
                realmQueryResponse.NameInfo.IsInternalRealm = false;
                realmQueryResponse.NameInfo.IsLocal = queryRealmName.VirtualRealmAddress == Global.WorldMgr.GetRealm().Id.GetAddress();
            }
            else
                realmQueryResponse.LookupState = (byte)ResponseCodes.Failure;
        }
    }
}
