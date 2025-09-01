// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Chat.Commands
{
    [CommandGroup("list")]
    class ListCommands
    {
        [Command("creature", RBACPermissions.CommandListCreature, true)]
        static bool HandleListCreatureCommand(CommandHandler handler, VariantArg<CreatureEntryLinkData, uint> creatureId, OptionalArg<uint> countArg)
        {
            CreatureTemplate cInfo = Global.ObjectMgr.GetCreatureTemplate(creatureId);
            if (cInfo == null)
            {
                handler.SendSysMessage(CypherStrings.CommandInvalidcreatureid, creatureId);
                return false;
            }

            uint count = countArg.GetValueOrDefault(10);

            if (count == 0)
                return false;

            uint creatureCount = 0;
            SQLResult result = DB.World.Query("SELECT COUNT(guid) FROM creature WHERE id='{0}'", creatureId);
            if (!result.IsEmpty())
                creatureCount = result.Read<uint>(0);

            if (handler.GetSession() != null)
            {
                Player player = handler.GetSession().GetPlayer();
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
                    ulong guid = result.Read<ulong>(0);
                    float x = result.Read<float>(1);
                    float y = result.Read<float>(2);
                    float z = result.Read<float>(3);
                    ushort mapId = result.Read<ushort>(4);
                    bool liveFound = false;

                    // Get map (only support base map from console)
                    Map thisMap = null;
                    if (handler.GetSession() != null)
                        thisMap = handler.GetSession().GetPlayer().GetMap();

                    // If map found, try to find active version of this creature
                    if (thisMap != null)
                    {
                        var creBounds = thisMap.GetCreatureBySpawnIdStore().LookupByKey(guid);
                        foreach (var creature in creBounds)
                            handler.SendSysMessage(CypherStrings.CreatureListChat, guid, guid, cInfo.Name, x, y, z, mapId, creature.GetGUID().ToString(), creature.IsAlive() ? "*" : " ");
                        liveFound = !creBounds.Empty();
                    }

                    if (!liveFound)
                    {
                        if (handler.GetSession() != null)
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
        static bool HandleListItemCommand(CommandHandler handler, VariantArg<ItemLinkData> item, OptionalArg<uint> countArg)
        {
            uint itemId = item.GetValue().Item.GetId();
            uint count = countArg.GetValueOrDefault(10);

            if (count == 0)
                return false;

            // inventory case
            uint inventoryCount = 0;

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_INVENTORY_COUNT_ITEM);
            stmt.AddValue(0, itemId);
            SQLResult result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
                inventoryCount = result.Read<uint>(0);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_INVENTORY_ITEM_BY_ENTRY);
            stmt.AddValue(0, itemId);
            stmt.AddValue(1, count);
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                do
                {
                    ObjectGuid itemGuid = ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0));
                    uint itemBag = result.Read<uint>(1);
                    byte itemSlot = result.Read<byte>(2);
                    ObjectGuid ownerGuid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(3));
                    uint ownerAccountId = result.Read<uint>(4);
                    string ownerName = result.Read<string>(5);

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

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAIL_COUNT_ITEM);
            stmt.AddValue(0, itemId);
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
                mailCount = result.Read<uint>(0);

            if (count > 0)
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAIL_ITEMS_BY_ENTRY);
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
                    ulong itemGuid = result.Read<ulong>(0);
                    ulong itemSender = result.Read<ulong>(1);
                    ulong itemReceiver = result.Read<ulong>(2);
                    uint itemSenderAccountId = result.Read<uint>(3);
                    string itemSenderName = result.Read<string>(4);
                    uint itemReceiverAccount = result.Read<uint>(5);
                    string itemReceiverName = result.Read<string>(6);

                    string itemPos = "[in mail]";

                    handler.SendSysMessage(CypherStrings.ItemlistMail, itemGuid, itemSenderName, itemSender, itemSenderAccountId, itemReceiverName, itemReceiver, itemReceiverAccount, itemPos);

                    count--;
                }
                while (result.NextRow());
            }

            // auction case
            uint auctionCount = 0;

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_AUCTIONHOUSE_COUNT_ITEM);
            stmt.AddValue(0, itemId);
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
                auctionCount = result.Read<uint>(0);

            if (count > 0)
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_AUCTIONHOUSE_ITEM_BY_ENTRY);
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
                    ObjectGuid itemGuid = ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0));
                    ObjectGuid owner = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1));
                    uint ownerAccountId = result.Read<uint>(2);
                    string ownerName = result.Read<string>(3);

                    string itemPos = "[in auction]";

                    handler.SendSysMessage(CypherStrings.ItemlistAuction, itemGuid.ToString(), ownerName, owner.ToString(), ownerAccountId, itemPos);
                }
                while (result.NextRow());
            }

            // guild bank case
            uint guildCount = 0;

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_GUILD_BANK_COUNT_ITEM);
            stmt.AddValue(0, itemId);
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
                guildCount = result.Read<uint>(0);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_GUILD_BANK_ITEM_BY_ENTRY);
            stmt.AddValue(0, itemId);
            stmt.AddValue(1, count);
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                do
                {
                    ObjectGuid itemGuid = ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0));
                    ObjectGuid guildGuid = ObjectGuid.Create(HighGuid.Guild, result.Read<ulong>(1));
                    string guildName = result.Read<string>(2);

                    string itemPos = "[in guild bank]";

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
        static bool HandleListMailCommand(CommandHandler handler, OptionalArg<PlayerIdentifier> player)
        {
            if (!player.HasValue)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player.Value == null)
                return false;

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_COUNT);
            stmt.AddValue(0, player.Value.GetGUID().GetCounter());
            SQLResult result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                uint countMail = result.Read<uint>(0);

                string nameLink = handler.PlayerLink(player.Value.GetName());
                handler.SendSysMessage(CypherStrings.ListMailHeader, countMail, nameLink, player.Value.GetGUID().ToString());
                handler.SendSysMessage(CypherStrings.AccountListBar);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_INFO);
                stmt.AddValue(0, player.Value.GetGUID().GetCounter());
                SQLResult result1 = DB.Characters.Query(stmt);

                if (!result1.IsEmpty())
                {
                    do
                    {
                        uint messageId = result1.Read<uint>(0);
                        ulong senderId = result1.Read<ulong>(1);
                        string sender = result1.Read<string>(2);
                        ulong receiverId = result1.Read<ulong>(3);
                        string receiver = result1.Read<string>(4);
                        string subject = result1.Read<string>(5);
                        long deliverTime = result1.Read<long>(6);
                        long expireTime = result1.Read<long>(7);
                        ulong money = result1.Read<ulong>(8);
                        byte hasItem = result1.Read<byte>(9);
                        uint gold = (uint)(money / MoneyConstants.Gold);
                        uint silv = (uint)(money % MoneyConstants.Gold) / MoneyConstants.Silver;
                        uint copp = (uint)(money % MoneyConstants.Gold) % MoneyConstants.Silver;
                        string receiverStr = handler.PlayerLink(receiver);
                        string senderStr = handler.PlayerLink(sender);
                        handler.SendSysMessage(CypherStrings.ListMailInfo1, messageId, subject, gold, silv, copp);
                        handler.SendSysMessage(CypherStrings.ListMailInfo2, senderStr, senderId, receiverStr, receiverId);
                        handler.SendSysMessage(CypherStrings.ListMailInfo3, Time.UnixTimeToDateTime(deliverTime).ToLongDateString(), Time.UnixTimeToDateTime(expireTime).ToLongDateString());

                        if (hasItem == 1)
                        {
                            SQLResult result2 = DB.Characters.Query("SELECT item_guid FROM mail_items WHERE mail_id = '{0}'", messageId);
                            if (!result2.IsEmpty())
                            {
                                do
                                {
                                    uint item_guid = result2.Read<uint>(0);
                                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_ITEMS);
                                    stmt.AddValue(0, item_guid);
                                    SQLResult result3 = DB.Characters.Query(stmt);
                                    if (!result3.IsEmpty())
                                    {
                                        do
                                        {
                                            uint item_entry = result3.Read<uint>(0);
                                            uint item_count = result3.Read<uint>(1);

                                            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(item_entry);
                                            if (itemTemplate == null)
                                                continue;

                                            if (handler.GetSession() != null)
                                            {
                                                uint color = ItemConst.ItemQualityColors[(int)itemTemplate.GetQuality()];
                                                string itemStr = $"|c{color}|Hitem:{item_entry}:0:0:0:0:0:0:0:{handler.GetSession().GetPlayer().GetLevel()}:0:0:0:0:0|h[{itemTemplate.GetName(handler.GetSessionDbcLocale())}]|h|r";
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
        static bool HandleListObjectCommand(CommandHandler handler, VariantArg<GameobjectEntryLinkData, uint> gameObjectId, OptionalArg<uint> countArg)
        {
            GameObjectTemplate gInfo = Global.ObjectMgr.GetGameObjectTemplate(gameObjectId);
            if (gInfo == null)
            {
                handler.SendSysMessage(CypherStrings.CommandListobjinvalidid, gameObjectId);
                return false;
            }

            uint count = countArg.GetValueOrDefault(10);

            if (count == 0)
                return false;

            uint objectCount = 0;
            SQLResult result = DB.World.Query("SELECT COUNT(guid) FROM gameobject WHERE id='{0}'", gameObjectId);
            if (!result.IsEmpty())
                objectCount = result.Read<uint>(0);

            if (handler.GetSession() != null)
            {
                Player player = handler.GetSession().GetPlayer();
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
                    ulong guid = result.Read<ulong>(0);
                    float x = result.Read<float>(1);
                    float y = result.Read<float>(2);
                    float z = result.Read<float>(3);
                    ushort mapId = result.Read<ushort>(4);
                    uint entry = result.Read<uint>(5);
                    bool liveFound = false;

                    // Get map (only support base map from console)
                    Map thisMap = null;
                    if (handler.GetSession() != null)
                        thisMap = handler.GetSession().GetPlayer().GetMap();

                    // If map found, try to find active version of this object
                    if (thisMap != null)
                    {
                        var goBounds = thisMap.GetGameObjectBySpawnIdStore().LookupByKey(guid);
                        foreach (var go in goBounds)
                            handler.SendSysMessage(CypherStrings.GoListChat, guid, entry, guid, gInfo.name, x, y, z, mapId, go.GetGUID().ToString(), go.IsSpawned() ? "*" : " ");
                        liveFound = !goBounds.Empty();
                    }

                    if (!liveFound)
                    {
                        if (handler.GetSession() != null)
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
        static bool HandleListRespawnsCommand(CommandHandler handler, OptionalArg<uint> range)
        {
            Player player = handler.GetSession().GetPlayer();
            Map map = player.GetMap();

            Locale locale = handler.GetSession().GetSessionDbcLocale();
            string stringOverdue = Global.ObjectMgr.GetCypherString(CypherStrings.ListRespawnsOverdue, locale);

            uint zoneId = player.GetZoneId();
            string zoneName = GetZoneName(zoneId, locale);
            for (SpawnObjectType type = 0; type < SpawnObjectType.NumSpawnTypes; type++)
            {
                if (range.HasValue)
                    handler.SendSysMessage(CypherStrings.ListRespawnsRange, type, range.Value);
                else
                    handler.SendSysMessage(CypherStrings.ListRespawnsZone, type, zoneName, zoneId);

                handler.SendSysMessage(CypherStrings.ListRespawnsListheader);
                List<RespawnInfo> respawns = new();
                map.GetRespawnInfo(respawns, (SpawnObjectTypeMask)(1 << (int)type));
                foreach (RespawnInfo ri in respawns)
                {
                    SpawnMetadata data = Global.ObjectMgr.GetSpawnMetadata(ri.type, ri.spawnId);
                    if (data == null)
                        continue;

                    uint respawnZoneId = 0;
                    SpawnData edata = data.ToSpawnData();
                    if (edata != null)
                    {
                        respawnZoneId = map.GetZoneId(PhasingHandler.EmptyPhaseShift, edata.SpawnPoint);
                        if (range.HasValue)
                        {
                            if (!player.IsInDist(edata.SpawnPoint, range.Value))
                                continue;
                        }
                        else
                        {
                            if (zoneId != respawnZoneId)
                                continue;
                        }
                    }

                    uint gridY = ri.gridId / MapConst.MaxGrids;
                    uint gridX = ri.gridId % MapConst.MaxGrids;

                    string respawnTime = ri.respawnTime > GameTime.GetGameTime() ? Time.secsToTimeString((ulong)(ri.respawnTime - GameTime.GetGameTime()), TimeFormat.ShortText) : stringOverdue;
                    handler.SendSysMessage($"{ri.spawnId} | {ri.entry} | [{gridX:2},{gridY:2}] | {GetZoneName(respawnZoneId, locale)} ({respawnZoneId}) | {respawnTime}{(map.IsSpawnGroupActive(data.spawnGroupData.groupId) ? "" : " (inactive)")}");
                }
            }
            return true;
        }

        [Command("scenes", RBACPermissions.CommandListScenes)]
        static bool HandleListScenesCommand(CommandHandler handler)
        {
            Player target = handler.GetSelectedPlayer();
            if (target == null)
                target = handler.GetSession().GetPlayer();

            if (target == null)
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
        static bool HandleListSpawnPointsCommand(CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();
            Map map = player.GetMap();
            uint mapId = map.GetId();
            bool showAll = map.IsBattlegroundOrArena() || map.IsDungeon();
            handler.SendSysMessage($"Listing all spawn points in map {mapId} ({map.GetMapName()}){(showAll ? "" : " within 5000yd")}:");

            foreach (var pair in Global.ObjectMgr.GetAllCreatureData())
            {
                SpawnData data = pair.Value;
                if (data.MapId != mapId)
                    continue;

                CreatureTemplate cTemp = Global.ObjectMgr.GetCreatureTemplate(data.Id);
                if (cTemp == null)
                    continue;

                if (showAll || data.SpawnPoint.IsInDist2d(player, 5000.0f))
                    handler.SendSysMessage($"Type: {data.type} | SpawnId: {data.SpawnId} | Entry: {data.Id} ({cTemp.Name}) | X: {data.SpawnPoint.GetPositionX():3} | Y: {data.SpawnPoint.GetPositionY():3} | Z: {data.SpawnPoint.GetPositionZ():3}");
            }
            foreach (var pair in Global.ObjectMgr.GetAllGameObjectData())
            {
                SpawnData data = pair.Value;
                if (data.MapId != mapId)
                    continue;

                GameObjectTemplate goTemp = Global.ObjectMgr.GetGameObjectTemplate(data.Id);
                if (goTemp == null)
                    continue;

                if (showAll || data.SpawnPoint.IsInDist2d(player, 5000.0f))
                    handler.SendSysMessage($"Type: {data.type} | SpawnId: {data.SpawnId} | Entry: {data.Id} ({goTemp.name}) | X: {data.SpawnPoint.GetPositionX():3} | Y: {data.SpawnPoint.GetPositionY():3} | Z: {data.SpawnPoint.GetPositionZ():3}");
            }
            return true;
        }

        static string GetZoneName(uint zoneId, Locale locale)
        {
            AreaTableRecord zoneEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
            return zoneEntry != null ? zoneEntry.AreaName[locale] : "<unknown zone>";
        }

        [CommandGroup("auras")]
        class ListAuraCommands
        {
            [Command("", RBACPermissions.CommandListAuras)]
            static bool HandleListAllAurasCommand(CommandHandler handler)
            {
                return ListAurasCommand(handler, default, null);
            }

            [Command("id", RBACPermissions.CommandListAuras)]
            static bool HandleListAurasByIdCommand(CommandHandler handler, uint spellId)
            {
                return ListAurasCommand(handler, spellId, null);
            }

            [Command("name", RBACPermissions.CommandListAuras)]
            static bool HandleListAurasByNameCommand(CommandHandler handler, Tail namePart)
            {
                return ListAurasCommand(handler, default, namePart);
            }

            static bool ListAurasCommand(CommandHandler handler, OptionalArg<uint> spellId, string namePart)
            {
                Unit unit = handler.GetSelectedUnit();
                if (unit == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                    return false;
                }

                string talentStr = handler.GetCypherString(CypherStrings.Talent);
                string passiveStr = handler.GetCypherString(CypherStrings.Passive);

                var auras = unit.GetAppliedAuras();
                handler.SendSysMessage(CypherStrings.CommandTargetListauras, auras.Count);
                foreach (var (_, aurApp) in auras)
                {
                    Aura aura = aurApp.GetBase();
                    string name = aura.GetSpellInfo().SpellName[handler.GetSessionDbcLocale()];
                    bool talent = aura.GetSpellInfo().HasAttribute(SpellCustomAttributes.IsTalent);

                    if (!ShouldListAura(aura.GetSpellInfo(), spellId, namePart, handler.GetSessionDbcLocale()))
                        continue;

                    string ss_name = "|cffffffff|Hspell:" + aura.GetId() + "|h[" + name + "]|h|r";

                    handler.SendSysMessage(CypherStrings.CommandTargetAuradetail, aura.GetId(), (handler.GetSession() != null ? ss_name : name),
                        aurApp.GetEffectMask(), aura.GetCharges(), aura.GetStackAmount(), aurApp.GetSlot(),
                        aura.GetDuration(), aura.GetMaxDuration(), (aura.IsPassive() ? passiveStr : ""),
                        (talent ? talentStr : ""), aura.GetCasterGUID().IsPlayer() ? "player" : "creature",
                        aura.GetCasterGUID().ToString());
                }

                for (AuraType auraType = 0; auraType < AuraType.Total; ++auraType)
                {
                    var auraList = unit.GetAuraEffectsByType(auraType);
                    if (auraList.Empty())
                        continue;

                    bool sizeLogged = false;

                    foreach (var effect in auraList)
                    {
                        if (!ShouldListAura(effect.GetSpellInfo(), spellId, namePart, handler.GetSessionDbcLocale()))
                            continue;

                        if (!sizeLogged)
                        {
                            sizeLogged = true;
                            handler.SendSysMessage(CypherStrings.CommandTargetListauratype, auraList.Count, auraType);
                        }

                        handler.SendSysMessage(CypherStrings.CommandTargetAurasimple, effect.GetId(), effect.GetEffIndex(), effect.GetAmount());
                    }
                }

                return true;
            }

            static bool ShouldListAura(SpellInfo spellInfo, OptionalArg<uint> spellId, string namePart, Locale locale)
            {
                if (spellId.HasValue)
                    return spellInfo.Id == spellId.Value;

                if (!namePart.IsEmpty())
                {
                    string name = spellInfo.SpellName[locale];
                    return name.Like(namePart);
                }

                return true;
            }
        }
    }
}
