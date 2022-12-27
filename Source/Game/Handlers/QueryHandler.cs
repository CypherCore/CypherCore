/*
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
using Framework.Realm;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Misc;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;
using System.Numerics;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.QueryPlayerNames, Processing = PacketProcessing.Inplace)]
        void HandleQueryPlayerNames(QueryPlayerNames queryPlayerName)
        {
            QueryPlayerNamesResponse response = new();
            foreach (ObjectGuid guid in queryPlayerName.Players)
            {
                BuildNameQueryData(guid, out NameCacheLookupResult nameCacheLookupResult);
                response.Players.Add(nameCacheLookupResult);
            }

            SendPacket(response);
        }

        public void BuildNameQueryData(ObjectGuid guid, out NameCacheLookupResult lookupData)
        {
            lookupData = new();

            Player player = Global.ObjAccessor.FindPlayer(guid);

            lookupData.Player = guid;

            lookupData.Data = new();
            if (lookupData.Data.Initialize(guid, player))
                lookupData.Result = (byte)ResponseCodes.Success;
            else
                lookupData.Result = (byte)ResponseCodes.Failure; // name unknown
        }

        [WorldPacketHandler(ClientOpcodes.QueryTime, Processing = PacketProcessing.Inplace)]
        void HandleQueryTime(QueryTime packet)
        {
            SendQueryTimeResponse();
        }

        void SendQueryTimeResponse()
        {
            QueryTimeResponse queryTimeResponse = new();
            queryTimeResponse.CurrentTime = GameTime.GetGameTime();
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

                QueryGameObjectResponse queryGameObjectResponse = info.QueryData;

                Locale loc = GetSessionDbLocaleIndex();
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

                QueryGameObjectResponse response = new();
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

                QueryCreatureResponse queryCreatureResponse = ci.QueryData;

                Locale loc = GetSessionDbLocaleIndex();
                if (loc != Locale.enUS)
                {
                    CreatureLocale creatureLocale = Global.ObjectMgr.GetCreatureLocale(ci.Entry);
                    if (creatureLocale != null)
                    {
                        string name = queryCreatureResponse.Stats.Name[0];
                        string nameAlt = queryCreatureResponse.Stats.NameAlt[0];

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

                QueryCreatureResponse response = new();
                response.CreatureID = packet.CreatureID;
                SendPacket(response);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QueryNpcText, Processing = PacketProcessing.Inplace)]
        void HandleNpcTextQuery(QueryNPCText packet)
        {
            NpcText npcText = Global.ObjectMgr.GetNpcText(packet.TextID);

            QueryNPCTextResponse response = new();
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

        [WorldPacketHandler(ClientOpcodes.QueryPageText, Processing = PacketProcessing.Inplace)]
        void HandleQueryPageText(QueryPageText packet)
        {
            QueryPageTextResponse response = new();
            response.PageTextID = packet.PageTextID;

            uint pageID = packet.PageTextID;
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

                Locale locale = GetSessionDbLocaleIndex();
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
            CorpseLocation packet = new();
            Player player = Global.ObjAccessor.FindConnectedPlayer(queryCorpseLocation.Player);
            if (!player || !player.HasCorpse() || !_player.IsInSameRaidWith(player))
            {
                packet.Valid = false;                               // corpse not found
                packet.Player = queryCorpseLocation.Player;
                SendPacket(packet);
                return;
            }

            WorldLocation corpseLocation = player.GetCorpseLocation();
            uint corpseMapID = corpseLocation.GetMapId();
            uint mapID = corpseLocation.GetMapId();
            var pos = corpseLocation.GetPosition3D();

            // if corpse at different map
            if (mapID != player.GetMapId())
            {
                // search entrance map for proper show entrance
                MapRecord corpseMapEntry = CliDB.MapStorage.LookupByKey(mapID);
                if (corpseMapEntry != null)
                {
                    if (corpseMapEntry.IsDungeon() && corpseMapEntry.CorpseMapID >= 0)
                    {
                        // if corpse map have entrance
                        TerrainInfo entranceTerrain = Global.TerrainMgr.LoadTerrain((uint)corpseMapEntry.CorpseMapID);
                        if (entranceTerrain != null)
                        {
                            mapID = (uint)corpseMapEntry.CorpseMapID;

                            pos = new Vector3(Global.ObjectMgr.GetMapCorpsePosition(corpseMapEntry.Id),
                                entranceTerrain.GetStaticHeight(player.GetPhaseShift(), pos.X, pos.Y, MapConst.MaxHeight));
                        }
                    }
                }
            }

            packet.Valid = true;
            packet.Player = queryCorpseLocation.Player;
            packet.MapID = (int)corpseMapID;
            packet.ActualMapID = (int)mapID;
            packet.Position = pos;
            packet.Transport = ObjectGuid.Empty;
            SendPacket(packet);
        }

        [WorldPacketHandler(ClientOpcodes.QueryCorpseTransport)]
        void HandleQueryCorpseTransport(QueryCorpseTransport queryCorpseTransport)
        {
            CorpseTransportQuery response = new();
            response.Player = queryCorpseTransport.Player;

            Player player = Global.ObjAccessor.FindConnectedPlayer(queryCorpseTransport.Player);
            if (player)
            {
                Corpse corpse = player.GetCorpse();
                if (_player.IsInSameRaidWith(player) && corpse && !corpse.GetTransGUID().IsEmpty() && corpse.GetTransGUID() == queryCorpseTransport.Transport)
                {
                    response.Position = new Vector3(corpse.GetTransOffsetX(), corpse.GetTransOffsetY(), corpse.GetTransOffsetZ());
                    response.Facing = corpse.GetTransOffsetO();
                }
            }

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.QueryQuestCompletionNpcs, Processing = PacketProcessing.Inplace)]
        void HandleQueryQuestCompletionNPCs(QueryQuestCompletionNPCs queryQuestCompletionNPCs)
        {
            QuestCompletionNPCResponse response = new();

            foreach (var questID in queryQuestCompletionNPCs.QuestCompletionNPCs)
            {
                QuestCompletionNPC questCompletionNPC = new();

                if (Global.ObjectMgr.GetQuestTemplate(questID) == null)
                {
                    Log.outDebug(LogFilter.Network, "WORLD: Unknown quest {0} in CMSG_QUEST_NPC_QUERY by {1}", questID, GetPlayer().GetGUID());
                    continue;
                }

                questCompletionNPC.QuestID = questID;

                foreach (var id in Global.ObjectMgr.GetCreatureQuestInvolvedRelationReverseBounds(questID))
                    questCompletionNPC.NPCs.Add(id);

                foreach (var id in Global.ObjectMgr.GetGOQuestInvolvedRelationReverseBounds(questID))
                    questCompletionNPC.NPCs.Add(id | 0x80000000); // GO mask

                response.QuestCompletionNPCs.Add(questCompletionNPC);
            }

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.ItemTextQuery, Processing = PacketProcessing.Inplace)]
        void HandleItemTextQuery(ItemTextQuery packet)
        {
            QueryItemTextResponse queryItemTextResponse = new();
            queryItemTextResponse.Id = packet.Id;

            Item item = GetPlayer().GetItemByGuid(packet.Id);
            if (item)
            {
                queryItemTextResponse.Valid = true;
                queryItemTextResponse.Text = item.GetText();
            }

            SendPacket(queryItemTextResponse);
        }

        [WorldPacketHandler(ClientOpcodes.QueryRealmName, Processing = PacketProcessing.Inplace)]
        void HandleQueryRealmName(QueryRealmName queryRealmName)
        {
            RealmQueryResponse realmQueryResponse = new();
            realmQueryResponse.VirtualRealmAddress = queryRealmName.VirtualRealmAddress;

            RealmId realmHandle = new(queryRealmName.VirtualRealmAddress);
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
