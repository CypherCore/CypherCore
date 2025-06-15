// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
                Difficulty difficulty = _player.GetMap().GetDifficultyID();

                // Cache only exists for difficulty base
                if (!WorldConfig.GetBoolValue(WorldCfg.CacheDataQueries) && difficulty == Difficulty.None)
                    SendPacket(ci.QueryData[(int)GetSessionDbLocaleIndex()]);
                else
                {
                    var response = ci.BuildQueryData(GetSessionDbLocaleIndex(), difficulty);
                    SendPacket(response);
                }
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
            if (player == null || !player.HasCorpse() || !_player.IsInSameRaidWith(player))
            {
                packet.Valid = false;                               // corpse not found
                packet.Player = queryCorpseLocation.Player;
                SendPacket(packet);
                return;
            }

            WorldLocation corpseLocation = player.GetCorpseLocation();
            uint corpseMapID = corpseLocation.GetMapId();
            uint mapID = corpseLocation.GetMapId();
            float x = corpseLocation.GetPositionX();
            float y = corpseLocation.GetPositionY();
            float z = corpseLocation.GetPositionZ();

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
                            x = corpseMapEntry.Corpse.X;
                            y = corpseMapEntry.Corpse.Y;
                            z = entranceTerrain.GetStaticHeight(player.GetPhaseShift(), mapID, x, y, MapConst.MaxHeight);
                        }
                    }
                }
            }

            packet.Valid = true;
            packet.Player = queryCorpseLocation.Player;
            packet.MapID = (int)corpseMapID;
            packet.ActualMapID = (int)mapID;
            packet.Position = new Vector3(x, y, z);
            packet.Transport = ObjectGuid.Empty; // TODO: If corpse is on transport, send transport offsets and transport guid
            SendPacket(packet);
        }

        [WorldPacketHandler(ClientOpcodes.QueryCorpseTransport)]
        void HandleQueryCorpseTransport(QueryCorpseTransport queryCorpseTransport)
        {
            CorpseTransportQuery response = new();
            response.Player = queryCorpseTransport.Player;

            Player player = Global.ObjAccessor.FindConnectedPlayer(queryCorpseTransport.Player);
            if (player != null && _player.IsInSameRaidWith(player))
            {
                Corpse corpse = _player.GetCorpse();
                if (corpse != null)
                {
                    Transport transport = (Transport)corpse.GetTransport();
                    if (transport != null && transport.GetGUID() == queryCorpseTransport.Transport)
                    {
                        response.Position = transport.GetPosition();
                        response.Facing = transport.GetOrientation();
                    }
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

        [WorldPacketHandler(ClientOpcodes.QuestPoiQuery, Processing = PacketProcessing.Inplace)]
        void HandleQuestPOIQuery(QuestPOIQuery packet)
        {
            if (packet.MissingQuestCount >= SharedConst.MaxQuestLogSize)
                return;

            // Read quest ids and add the in a unordered_set so we don't send POIs for the same quest multiple times
            HashSet<uint> questIds = new();
            for (int i = 0; i < packet.MissingQuestCount; ++i)
                questIds.Add(packet.MissingQuestPOIs[i]); // QuestID

            QuestPOIQueryResponse response = new();

            foreach (uint questId in questIds)
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

        [WorldPacketHandler(ClientOpcodes.ItemTextQuery, Processing = PacketProcessing.Inplace)]
        void HandleItemTextQuery(ItemTextQuery packet)
        {
            QueryItemTextResponse queryItemTextResponse = new();
            queryItemTextResponse.Id = packet.Id;

            Item item = GetPlayer().GetItemByGuid(packet.Id);
            if (item != null)
            {
                queryItemTextResponse.Valid = true;
                queryItemTextResponse.Text = item.GetText();
            }

            SendPacket(queryItemTextResponse);
        }

        [WorldPacketHandler(ClientOpcodes.QueryRealmName, Status = SessionStatus.Authed, Processing = PacketProcessing.Inplace)]
        void HandleQueryRealmName(QueryRealmName queryRealmName)
        {
            RealmQueryResponse realmQueryResponse = new();
            realmQueryResponse.VirtualRealmAddress = queryRealmName.VirtualRealmAddress;

            var realm = Global.RealmMgr.GetRealm(new RealmId(queryRealmName.VirtualRealmAddress));
            if (realm != null)
            {
                realmQueryResponse.LookupState = (byte)ResponseCodes.Success;
                realmQueryResponse.NameInfo.IsInternalRealm = false;
                realmQueryResponse.NameInfo.IsLocal = queryRealmName.VirtualRealmAddress == Global.WorldMgr.GetVirtualRealmAddress();
                realmQueryResponse.NameInfo.RealmNameActual = realm.Name;
                realmQueryResponse.NameInfo.RealmNameNormalized = realm.NormalizedName;
            }
            else
                realmQueryResponse.LookupState = (byte)ResponseCodes.Failure;

            SendPacket(realmQueryResponse);
        }

        [WorldPacketHandler(ClientOpcodes.QueryTreasurePicker)]
        void HandleQueryTreasurePicker(QueryTreasurePicker queryTreasurePicker)
        {
            Quest questInfo = Global.ObjectMgr.GetQuestTemplate(queryTreasurePicker.QuestID);
            if (questInfo == null)
                return;

            TreasurePickerResponse treasurePickerResponse = new();
            treasurePickerResponse.QuestID = queryTreasurePicker.QuestID;
            treasurePickerResponse.TreasurePickerID = queryTreasurePicker.TreasurePickerID;

            // TODO: Missing treasure picker implementation

            SendPacket(treasurePickerResponse);
        }
    }
}
