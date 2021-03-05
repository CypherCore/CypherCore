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
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Spells;
using System.Collections.Generic;

namespace Game.Chat.Commands
{
    [CommandGroup("list", RBACPermissions.CommandList, true)]
    class ListCommands
    {
        [Command("auras", RBACPermissions.CommandListAuras)]
        static bool HandleListAurasCommand(StringArguments args, CommandHandler handler)
        {
            var unit = handler.GetSelectedUnit();
            if (!unit)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            var talentStr = handler.GetCypherString(CypherStrings.Talent);
            var passiveStr = handler.GetCypherString(CypherStrings.Passive);

            var auras = unit.GetAppliedAuras();
            handler.SendSysMessage(CypherStrings.CommandTargetListauras, auras.Count);
            foreach (var pair in auras)
            {

                var aurApp = pair.Value;
                var aura = aurApp.GetBase();
                var name = aura.GetSpellInfo().SpellName[handler.GetSessionDbcLocale()];
                var talent = aura.GetSpellInfo().HasAttribute(SpellCustomAttributes.IsTalent);

                var ss_name = "|cffffffff|Hspell:" + aura.GetId() + "|h[" + name + "]|h|r";

                handler.SendSysMessage(CypherStrings.CommandTargetAuradetail, aura.GetId(), (handler.GetSession() != null ? ss_name : name),
                    aurApp.GetEffectMask(), aura.GetCharges(), aura.GetStackAmount(), aurApp.GetSlot(),
                    aura.GetDuration(), aura.GetMaxDuration(), (aura.IsPassive() ? passiveStr : ""),
                    (talent ? talentStr : ""), aura.GetCasterGUID().IsPlayer() ? "player" : "creature",
                    aura.GetCasterGUID().ToString());
            }

            for (ushort i = 0; i < (int)AuraType.Total; ++i)
            {
                var auraList = unit.GetAuraEffectsByType((AuraType)i);
                if (auraList.Empty())
                    continue;

                handler.SendSysMessage(CypherStrings.CommandTargetListauratype, auraList.Count, i);

                foreach (var eff in auraList)
                    handler.SendSysMessage(CypherStrings.CommandTargetAurasimple, eff.GetId(), eff.GetEffIndex(), eff.GetAmount());
            }

            return true;
        }

        [Command("creature", RBACPermissions.CommandListCreature, true)]
        static bool HandleListCreatureCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            // number or [name] Shift-click form |color|Hcreature_entry:creature_id|h[name]|h|r
            var id = handler.ExtractKeyFromLink(args, "Hcreature_entry");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out var creatureId) || creatureId == 0)
            {
                handler.SendSysMessage(CypherStrings.CommandInvalidcreatureid, creatureId);
                return false;
            }

            var cInfo = Global.ObjectMgr.GetCreatureTemplate(creatureId);
            if (cInfo == null)
            {
                handler.SendSysMessage(CypherStrings.CommandInvalidcreatureid, creatureId);
                return false;
            }

            if (!uint.TryParse(args.NextString(), out var count))
                count = 10;

            if (count == 0)
                return false;

            uint creatureCount = 0;
            var result = DB.World.Query("SELECT COUNT(guid) FROM creature WHERE id='{0}'", creatureId);
            if (!result.IsEmpty())
                creatureCount = result.Read<uint>(0);

            if (handler.GetSession() != null)
            {
                var player = handler.GetSession().GetPlayer();
                result = DB.World.Query("SELECT guid, position_x, position_y, position_z, map, (POW(position_x - '{0}', 2) + POW(position_y - '{1}', 2) + POW(position_z - '{2}', 2)) AS order_ FROM creature WHERE id = '{3}' ORDER BY order_ ASC LIMIT {4}",
                                player.GetPositionX(), player.GetPositionY(), player.GetPositionZ(), creatureId, count);
            }
            else
                result = DB.World.Query("SELECT guid, position_x, position_y, position_z, map FROM creature WHERE id = '{0}' LIMIT {1}",
                    creatureId, count);

            if (!result.IsEmpty())
            {
                do
                {
                    var guid = result.Read<ulong>(0);
                    var x = result.Read<float>(1);
                    var y = result.Read<float>(2);
                    var z = result.Read<float>(3);
                    var mapId = result.Read<ushort>(4);
                    var liveFound = false;

                    // Get map (only support base map from console)
                    Map thisMap;
                    if (handler.GetSession() != null)
                        thisMap = handler.GetSession().GetPlayer().GetMap();
                    else
                        thisMap = Global.MapMgr.FindBaseNonInstanceMap(mapId);

                    // If map found, try to find active version of this creature
                    if (thisMap)
                    {
                        var creBounds = thisMap.GetCreatureBySpawnIdStore().LookupByKey(guid);
                        if (!creBounds.Empty())
                        {
                            foreach (var creature in creBounds)
                            {
                                if (handler.GetSession())
                                    handler.SendSysMessage(CypherStrings.CreatureListChat, guid, guid, cInfo.Name, x, y, z, mapId, creature.GetGUID().ToString(), creature.IsAlive() ? "*" : " ");
                                else
                                    handler.SendSysMessage(CypherStrings.CreatureListConsole, guid, cInfo.Name, x, y, z, mapId, creature.GetGUID().ToString(), creature.IsAlive() ? "*" : " ");
                            }
                            liveFound = true;
                        }
                    }

                    if (!liveFound)
                    {
                        if (handler.GetSession())
                            handler.SendSysMessage(CypherStrings.CreatureListChat, guid, guid, cInfo.Name, x, y, z, mapId, "", "");
                        else
                            handler.SendSysMessage(CypherStrings.CreatureListConsole, guid, cInfo.Name, x, y, z, mapId, "", "");
                    }
                }
                while (result.NextRow());
            }

            handler.SendSysMessage(CypherStrings.CommandListcreaturemessage, creatureId, creatureCount);

            return true;
        }

        [Command("item", RBACPermissions.CommandListItem, true)]
        static bool HandleListItemCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var id = handler.ExtractKeyFromLink(args, "Hitem");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out var itemId) || itemId == 0)
            {
                handler.SendSysMessage(CypherStrings.CommandItemidinvalid, itemId);
                return false;
            }

            var itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
            if (itemTemplate == null)
            {
                handler.SendSysMessage(CypherStrings.CommandItemidinvalid, itemId);
                return false;
            }

            if (!uint.TryParse(args.NextString(), out var count))
                count = 10;

            if (count == 0)
                return false;

            // inventory case
            uint inventoryCount = 0;

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_INVENTORY_COUNT_ITEM);
            stmt.AddValue(0, itemId);
            var result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
                inventoryCount = result.Read<uint>(0);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_INVENTORY_ITEM_BY_ENTRY);
            stmt.AddValue(0, itemId);
            stmt.AddValue(1, count);
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                do
                {
                    var itemGuid = ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0));
                    var itemBag = result.Read<uint>(1);
                    var itemSlot = result.Read<byte>(2);
                    var ownerGuid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(3));
                    var ownerAccountId = result.Read<uint>(4);
                    var ownerName = result.Read<string>(5);

                    string itemPos;
                    if (Player.IsEquipmentPos((byte)itemBag, itemSlot))
                        itemPos = "[equipped]";
                    else if (Player.IsInventoryPos((byte)itemBag, itemSlot))
                        itemPos = "[in inventory]";
                    else if (Player.IsBankPos((byte)itemBag, itemSlot))
                        itemPos = "[in bank]";
                    else
                        itemPos = "";

                    handler.SendSysMessage(CypherStrings.ItemlistSlot, itemGuid.ToString(), ownerName, ownerGuid.ToString(), ownerAccountId, itemPos);

                    count--;
                }
                while (result.NextRow());
            }

            // mail case
            uint mailCount = 0;

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_COUNT_ITEM);
            stmt.AddValue(0, itemId);
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
                mailCount = result.Read<uint>(0);

            if (count > 0)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_ITEMS_BY_ENTRY);
                stmt.AddValue(0, itemId);
                stmt.AddValue(1, count);
                result = DB.Characters.Query(stmt);
            }
            else
                result = null;

            if (result != null && !result.IsEmpty())
            {
                do
                {
                    var itemGuid = result.Read<ulong>(0);
                    var itemSender = result.Read<ulong>(1);
                    var itemReceiver = result.Read<ulong>(2);
                    var itemSenderAccountId = result.Read<uint>(3);
                    var itemSenderName = result.Read<string>(4);
                    var itemReceiverAccount = result.Read<uint>(5);
                    var itemReceiverName = result.Read<string>(6);

                    var itemPos = "[in mail]";

                    handler.SendSysMessage(CypherStrings.ItemlistMail, itemGuid, itemSenderName, itemSender, itemSenderAccountId, itemReceiverName, itemReceiver, itemReceiverAccount, itemPos);

                    count--;
                }
                while (result.NextRow());
            }

            // auction case
            uint auctionCount = 0;

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_AUCTIONHOUSE_COUNT_ITEM);
            stmt.AddValue(0, itemId);
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
                auctionCount = result.Read<uint>(0);

            if (count > 0)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_AUCTIONHOUSE_ITEM_BY_ENTRY);
                stmt.AddValue(0, itemId);
                stmt.AddValue(1, count);
                result = DB.Characters.Query(stmt);
            }
            else
                result = null;

            if (result != null && !result.IsEmpty())
            {
                do
                {
                    var itemGuid = ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0));
                    var owner = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1));
                    var ownerAccountId = result.Read<uint>(2);
                    var ownerName = result.Read<string>(3);

                    var itemPos = "[in auction]";

                    handler.SendSysMessage(CypherStrings.ItemlistAuction, itemGuid.ToString(), ownerName, owner.ToString(), ownerAccountId, itemPos);
                }
                while (result.NextRow());
            }

            // guild bank case
            uint guildCount = 0;

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_BANK_COUNT_ITEM);
            stmt.AddValue(0, itemId);
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
                guildCount = result.Read<uint>(0);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_BANK_ITEM_BY_ENTRY);
            stmt.AddValue(0, itemId);
            stmt.AddValue(1, count);
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                do
                {
                    var itemGuid = ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0));
                    var guildGuid = ObjectGuid.Create(HighGuid.Guild, result.Read<ulong>(1));
                    var guildName = result.Read<string>(2);

                    var itemPos = "[in guild bank]";

                    handler.SendSysMessage(CypherStrings.ItemlistGuild, itemGuid.ToString(), guildName, guildGuid.ToString(), itemPos);

                    count--;
                }
                while (result.NextRow());
            }

            if (inventoryCount + mailCount + auctionCount + guildCount == 0)
            {
                handler.SendSysMessage(CypherStrings.CommandNoitemfound);
                return false;
            }

            handler.SendSysMessage(CypherStrings.CommandListitemmessage, itemId, inventoryCount + mailCount + auctionCount + guildCount, inventoryCount, mailCount, auctionCount, guildCount);
            return true;
        }

        [Command("mail", RBACPermissions.CommandListMail, true)]
        static bool HandleListMailCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target;
            ObjectGuid targetGuid;
            string targetName;

            var parseGUID = ObjectGuid.Create(HighGuid.Player, args.NextUInt64());
            if (Global.CharacterCacheStorage.GetCharacterNameByGuid(parseGUID, out targetName))
            {
                target = Global.ObjAccessor.FindPlayer(parseGUID);
                targetGuid = parseGUID;
            }
            else if (!handler.ExtractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_COUNT);
            stmt.AddValue(0, targetGuid.GetCounter());
            var result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                var countMail = result.Read<uint>(0);

                var nameLink = handler.PlayerLink(targetName);
                handler.SendSysMessage(CypherStrings.ListMailHeader, countMail, nameLink, targetGuid.ToString());
                handler.SendSysMessage(CypherStrings.AccountListBar);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_INFO);
                stmt.AddValue(0, targetGuid.GetCounter());
                var result1 = DB.Characters.Query(stmt);

                if (!result1.IsEmpty())
                {
                    do
                    {
                        var messageId = result1.Read<uint>(0);
                        var senderId = result1.Read<ulong>(1);
                        var sender = result1.Read<string>(2);
                        var receiverId = result1.Read<ulong>(3);
                        var receiver = result1.Read<string>(4);
                        var subject = result1.Read<string>(5);
                        long deliverTime = result1.Read<uint>(6);
                        long expireTime = result1.Read<uint>(7);
                        var money = result1.Read<ulong>(8);
                        var hasItem = result1.Read<byte>(9);
                        var gold = (uint)(money / MoneyConstants.Gold);
                        var silv = (uint)(money % MoneyConstants.Gold) / MoneyConstants.Silver;
                        var copp = (uint)(money % MoneyConstants.Gold) % MoneyConstants.Silver;
                        var receiverStr = handler.PlayerLink(receiver);
                        var senderStr = handler.PlayerLink(sender);
                        handler.SendSysMessage(CypherStrings.ListMailInfo1, messageId, subject, gold, silv, copp);
                        handler.SendSysMessage(CypherStrings.ListMailInfo2, senderStr, senderId, receiverStr, receiverId);
                        handler.SendSysMessage(CypherStrings.ListMailInfo3, Time.UnixTimeToDateTime(deliverTime).ToLongDateString(), Time.UnixTimeToDateTime(expireTime).ToLongDateString());

                        if (hasItem == 1)
                        {
                            var result2 = DB.Characters.Query("SELECT item_guid FROM mail_items WHERE mail_id = '{0}'", messageId);
                            if (!result2.IsEmpty())
                            {
                                do
                                {
                                    var item_guid = result2.Read<uint>(0);
                                    stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_ITEMS);
                                    stmt.AddValue(0, item_guid);
                                    var result3 = DB.Characters.Query(stmt);
                                    if (!result3.IsEmpty())
                                    {
                                        do
                                        {
                                            var item_entry = result3.Read<uint>(0);
                                            var item_count = result3.Read<uint>(1);

                                            var itemTemplate = Global.ObjectMgr.GetItemTemplate(item_entry);
                                            if (itemTemplate == null)
                                                continue;

                                            if (handler.GetSession() != null)
                                            {
                                                var color = ItemConst.ItemQualityColors[(int)itemTemplate.GetQuality()];
                                                var itemStr = $"|c{color}|Hitem:{item_entry}:0:0:0:0:0:0:0:{handler.GetSession().GetPlayer().GetLevel()}:0:0:0:0:0|h[{itemTemplate.GetName(handler.GetSessionDbcLocale())}]|h|r";
                                                handler.SendSysMessage(CypherStrings.ListMailInfoItem, itemStr, item_entry, item_guid, item_count);
                                            }
                                            else
                                                handler.SendSysMessage(CypherStrings.ListMailInfoItem, itemTemplate.GetName(handler.GetSessionDbcLocale()), item_entry, item_guid, item_count);
                                        }
                                        while (result3.NextRow());
                                    }
                                }
                                while (result2.NextRow());
                            }
                        }
                        handler.SendSysMessage(CypherStrings.AccountListBar);
                    }
                    while (result1.NextRow());
                }
                else
                    handler.SendSysMessage(CypherStrings.ListMailNotFound);
                return true;
            }
            else
                handler.SendSysMessage(CypherStrings.ListMailNotFound);
            return true;
        }

        [Command("object", RBACPermissions.CommandListObject, true)]
        static bool HandleListObjectCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            // number or [name] Shift-click form |color|Hgameobject_entry:go_id|h[name]|h|r
            var id = handler.ExtractKeyFromLink(args, "Hgameobject_entry");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out var gameObjectId) || gameObjectId == 0)
            {
                handler.SendSysMessage(CypherStrings.CommandListobjinvalidid, gameObjectId);
                return false;
            }

            var gInfo = Global.ObjectMgr.GetGameObjectTemplate(gameObjectId);
            if (gInfo == null)
            {
                handler.SendSysMessage(CypherStrings.CommandListobjinvalidid, gameObjectId);
                return false;
            }

            if (!uint.TryParse(args.NextString(), out var count))
                count = 10;

            if (count == 0)
                return false;

            uint objectCount = 0;
            var result = DB.World.Query("SELECT COUNT(guid) FROM gameobject WHERE id='{0}'", gameObjectId);
            if (!result.IsEmpty())
                objectCount = result.Read<uint>(0);

            if (handler.GetSession() != null)
            {
                var player = handler.GetSession().GetPlayer();
                result = DB.World.Query("SELECT guid, position_x, position_y, position_z, map, id, (POW(position_x - '{0}', 2) + POW(position_y - '{1}', 2) + POW(position_z - '{2}', 2)) AS order_ FROM gameobject WHERE id = '{3}' ORDER BY order_ ASC LIMIT {4}",
                    player.GetPositionX(), player.GetPositionY(), player.GetPositionZ(), gameObjectId, count);
            }
            else
                result = DB.World.Query("SELECT guid, position_x, position_y, position_z, map, id FROM gameobject WHERE id = '{0}' LIMIT {1}",
                    gameObjectId, count);

            if (!result.IsEmpty())
            {
                do
                {
                    var guid = result.Read<ulong>(0);
                    var x = result.Read<float>(1);
                    var y = result.Read<float>(2);
                    var z = result.Read<float>(3);
                    var mapId = result.Read<ushort>(4);
                    var entry = result.Read<uint>(5);
                    var liveFound = false;

                    // Get map (only support base map from console)
                    Map thisMap;
                    if (handler.GetSession() != null)
                        thisMap = handler.GetSession().GetPlayer().GetMap();
                    else
                        thisMap = Global.MapMgr.FindBaseNonInstanceMap(mapId);

                    // If map found, try to find active version of this object
                    if (thisMap)
                    {
                        var goBounds = thisMap.GetGameObjectBySpawnIdStore().LookupByKey(guid);
                        if (!goBounds.Empty())
                        {
                            foreach (var go in goBounds)
                            {
                                if (handler.GetSession())
                                    handler.SendSysMessage(CypherStrings.GoListChat, guid, entry, guid, gInfo.name, x, y, z, mapId, go.GetGUID(), go.IsSpawned() ? "*" : " ");
                                else
                                    handler.SendSysMessage(CypherStrings.GoListConsole, guid, gInfo.name, x, y, z, mapId, go.GetGUID(), go.IsSpawned() ? "*" : " ");
                            }
                            liveFound = true;
                        }
                    }

                    if (!liveFound)
                    {
                        if (handler.GetSession())
                            handler.SendSysMessage(CypherStrings.GoListChat, guid, entry, guid, gInfo.name, x, y, z, mapId, "", "");
                        else
                            handler.SendSysMessage(CypherStrings.GoListConsole, guid, gInfo.name, x, y, z, mapId, "", "");
                    }
                }
                while (result.NextRow());
            }

            handler.SendSysMessage(CypherStrings.CommandListobjmessage, gameObjectId, objectCount);

            return true;
        }

        [Command("respawns", RBACPermissions.CommandListRespawns)]
        static bool HandleListRespawnsCommand(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSession().GetPlayer();
            var map = player.GetMap();

            uint range = 0;
            if (!args.Empty())
                range = args.NextUInt32();

            var respawns = new List<RespawnInfo>();
            var locale = handler.GetSession().GetSessionDbcLocale();
            var stringOverdue = Global.ObjectMgr.GetCypherString(CypherStrings.ListRespawnsOverdue, locale);
            var stringCreature = Global.ObjectMgr.GetCypherString(CypherStrings.ListRespawnsCreatures, locale);
            var stringGameobject = Global.ObjectMgr.GetCypherString(CypherStrings.ListRespawnsGameobjects, locale);

            var zoneId = player.GetZoneId();
            if (range != 0)
                handler.SendSysMessage(CypherStrings.ListRespawnsRange, stringCreature, range);
            else
                handler.SendSysMessage(CypherStrings.ListRespawnsZone, stringCreature, GetZoneName(zoneId, handler.GetSessionDbcLocale()), zoneId);
            handler.SendSysMessage(CypherStrings.ListRespawnsListheader);
            map.GetRespawnInfo(respawns, SpawnObjectTypeMask.Creature, range != 0 ? 0 : zoneId);
            foreach (var ri in respawns)
            {
                var data = Global.ObjectMgr.GetCreatureData(ri.spawnId);
                if (data == null)
                    continue;

                if (range != 0 && !player.IsInDist(data.spawnPoint, range))
                    continue;

                var gridY = ri.gridId / MapConst.MaxGrids;
                var gridX = ri.gridId % MapConst.MaxGrids;

                var respawnTime = ri.respawnTime > Time.UnixTime ? Time.secsToTimeString((ulong)(ri.respawnTime - Time.UnixTime), true) : stringOverdue;
                handler.SendSysMessage($"{ri.spawnId} | {ri.entry} | [{gridX},{gridY}] | {GetZoneName(ri.zoneId, handler.GetSessionDbcLocale())} ({ri.zoneId}) | {(map.IsSpawnGroupActive(data.spawnGroupData.groupId) ? respawnTime : "inactive")}");
            }

            respawns.Clear();
            if (range != 0)
                handler.SendSysMessage(CypherStrings.ListRespawnsRange, stringGameobject, range);
            else
                handler.SendSysMessage(CypherStrings.ListRespawnsZone, stringGameobject, GetZoneName(zoneId, handler.GetSessionDbcLocale()), zoneId);
            handler.SendSysMessage(CypherStrings.ListRespawnsListheader);
            map.GetRespawnInfo(respawns, SpawnObjectTypeMask.GameObject, range != 0 ? 0 : zoneId);
            foreach (var ri in respawns)
            {
                var data = Global.ObjectMgr.GetGameObjectData(ri.spawnId);
                if (data == null)
                    continue;

                if (range != 0 && !player.IsInDist(data.spawnPoint, range))
                    continue;

                var gridY = ri.gridId / MapConst.MaxGrids;
                var gridX = ri.gridId % MapConst.MaxGrids;

                var respawnTime = ri.respawnTime > Time.UnixTime ? Time.secsToTimeString((ulong)(ri.respawnTime - Time.UnixTime), true) : stringOverdue;
                handler.SendSysMessage($"{ri.spawnId} | {ri.entry} | [{gridX},{gridY}] | {GetZoneName(ri.zoneId, handler.GetSessionDbcLocale())} ({ri.zoneId}) | {(map.IsSpawnGroupActive(data.spawnGroupData.groupId) ? respawnTime : "inactive")}");
            }
            return true;
        }

        [Command("scenes", RBACPermissions.CommandListScenes)]
        static bool HandleListScenesCommand(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedPlayer();
            if (!target)
                target = handler.GetSession().GetPlayer();

            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            var instanceByPackageMap = target.GetSceneMgr().GetSceneTemplateByInstanceMap();

            handler.SendSysMessage(CypherStrings.DebugSceneObjectList, target.GetSceneMgr().GetActiveSceneCount());

            foreach (var instanceByPackage in instanceByPackageMap)
                handler.SendSysMessage(CypherStrings.DebugSceneObjectDetail, instanceByPackage.Value.ScenePackageId, instanceByPackage.Key);

            return true;
        }

        [Command("spawnpoints", RBACPermissions.CommandListSpawnpoints)]
        static bool HandleListSpawnPointsCommand(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSession().GetPlayer();
            var map = player.GetMap();
            var mapId = map.GetId();
            var showAll = map.IsBattlegroundOrArena() || map.IsDungeon();
            handler.SendSysMessage($"Listing all spawn points in map {mapId} ({map.GetMapName()}){(showAll ? "" : " within 5000yd")}:");

            foreach (var pair in Global.ObjectMgr.GetAllCreatureData())
            {
                SpawnData data = pair.Value;
                if (data.spawnPoint.GetMapId() != mapId)
                    continue;

                var cTemp = Global.ObjectMgr.GetCreatureTemplate(data.Id);
                if (cTemp == null)
                    continue;

                if (showAll || data.spawnPoint.IsInDist2d(player, 5000.0f))
                    handler.SendSysMessage($"Type: {data.type} | SpawnId: {data.spawnId} | Entry: {data.Id} ({cTemp.Name}) | X: {data.spawnPoint.GetPositionX():3} | Y: {data.spawnPoint.GetPositionY():3} | Z: {data.spawnPoint.GetPositionZ():3}");
            }
            foreach (var pair in Global.ObjectMgr.GetAllGameObjectData())
            {
                SpawnData data = pair.Value;
                if (data.spawnPoint.GetMapId() != mapId)
                    continue;

                var goTemp = Global.ObjectMgr.GetGameObjectTemplate(data.Id);
                if (goTemp == null)
                    continue;

                if (showAll || data.spawnPoint.IsInDist2d(player, 5000.0f))
                    handler.SendSysMessage($"Type: {data.type} | SpawnId: {data.spawnId} | Entry: {data.Id} ({goTemp.name}) | X: {data.spawnPoint.GetPositionX():3} | Y: {data.spawnPoint.GetPositionY():3} | Z: {data.spawnPoint.GetPositionZ():3}");
            }
            return true;
        }

        static string GetZoneName(uint zoneId, Locale locale)
        {
            var zoneEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
            return zoneEntry != null ? zoneEntry.AreaName[locale] : "<unknown zone>";
        }
    }
}
