/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Game.Entities;
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
            Unit unit = handler.getSelectedUnit();
            if (!unit)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            string talentStr = handler.GetCypherString(CypherStrings.Talent);
            string passiveStr = handler.GetCypherString(CypherStrings.Passive);

            var auras = unit.GetAppliedAuras();
            handler.SendSysMessage(CypherStrings.CommandTargetListauras, auras.Count);
            foreach (var pair in auras)
            {

                AuraApplication aurApp = pair.Value;
                Aura aura = aurApp.GetBase();
                string name = aura.GetSpellInfo().SpellName[handler.GetSessionDbcLocale()];
                bool talent = aura.GetSpellInfo().HasAttribute(SpellCustomAttributes.IsTalent);

                string ss_name = "|cffffffff|Hspell:" + aura.GetId() + "|h[" + name + "]|h|r";

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
            string id = handler.extractKeyFromLink(args, "Hcreature_entry");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out uint creatureId) || creatureId == 0)
            {
                handler.SendSysMessage(CypherStrings.CommandInvalidcreatureid, creatureId);
                return false;
            }

            CreatureTemplate cInfo = Global.ObjectMgr.GetCreatureTemplate(creatureId);
            if (cInfo == null)
            {
                handler.SendSysMessage(CypherStrings.CommandInvalidcreatureid, creatureId);
                return false;
            }

            if (!uint.TryParse(args.NextString(), out uint count))
                count = 10;

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

                    if (handler.GetSession() != null)
                        handler.SendSysMessage(CypherStrings.CreatureListChat, guid, guid, cInfo.Name, x, y, z, mapId);
                    else
                        handler.SendSysMessage(CypherStrings.CreatureListConsole, guid, cInfo.Name, x, y, z, mapId);
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

            string id = handler.extractKeyFromLink(args, "Hitem");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out uint itemId) || itemId == 0)
            {
                handler.SendSysMessage(CypherStrings.CommandItemidinvalid, itemId);
                return false;
            }

            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
            if (itemTemplate == null)
            {
                handler.SendSysMessage(CypherStrings.CommandItemidinvalid, itemId);
                return false;
            }

            if (!uint.TryParse(args.NextString(), out uint count))
                count = 10;

            if (count == 0)
                return false;

            // inventory case
            uint inventoryCount = 0;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_INVENTORY_COUNT_ITEM);
            stmt.AddValue(0, itemId);
            SQLResult result = DB.Characters.Query(stmt);

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
        static bool HandleListMailCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target;
            ObjectGuid targetGuid;
            string targetName;
            PreparedStatement stmt = null;

            ObjectGuid parseGUID = ObjectGuid.Create(HighGuid.Player, args.NextUInt64());
            if (ObjectManager.GetPlayerNameByGUID(parseGUID, out targetName))
            {
                target = Global.ObjAccessor.FindPlayer(parseGUID);
                targetGuid = parseGUID;
            }
            else if (!handler.extractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_COUNT);
            stmt.AddValue(0, targetGuid.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                uint countMail = result.Read<uint>(0);

                string nameLink = handler.playerLink(targetName);
                handler.SendSysMessage(CypherStrings.ListMailHeader, countMail, nameLink, targetGuid.ToString());
                handler.SendSysMessage(CypherStrings.AccountListBar);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_INFO);
                stmt.AddValue(0, targetGuid.GetCounter());
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
                        long deliverTime = result1.Read<uint>(6);
                        long expireTime = result1.Read<uint>(7);
                        ulong money = result1.Read<ulong>(8);
                        byte hasItem = result1.Read<byte>(9);
                        uint gold = (uint)(money / MoneyConstants.Gold);
                        uint silv = (uint)(money % MoneyConstants.Gold) / MoneyConstants.Silver;
                        uint copp = (uint)(money % MoneyConstants.Gold) % MoneyConstants.Silver;
                        string receiverStr = handler.playerLink(receiver);
                        string senderStr = handler.playerLink(sender);
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
                                    stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_ITEMS);
                                    stmt.AddValue(0, item_guid);
                                    SQLResult result3 = DB.Characters.Query(stmt);
                                    if (!result3.IsEmpty())
                                    {
                                        do
                                        {
                                            uint item_entry = result3.Read<uint>(0);
                                            uint item_count = result3.Read<uint>(1);
                                            SQLResult result4 = DB.World.Query("SELECT name, quality FROM item_template WHERE entry = '{0}'", item_entry);

                                            string item_name = result4.Read<string>(0);
                                            int item_quality = result4.Read<byte>(1);
                                            if (handler.GetSession() != null)
                                            {
                                                uint color = ItemConst.ItemQualityColors[item_quality];
                                                string itemStr = "|c" + color + "|Hitem:" + item_entry + ":0:0:0:0:0:0:0:0:0|h[" + item_name + "]|h|r";
                                                handler.SendSysMessage(CypherStrings.ListMailInfoItem, itemStr, item_entry, item_guid, item_count);
                                            }
                                            else
                                                handler.SendSysMessage(CypherStrings.ListMailInfoItem, item_name, item_entry, item_guid, item_count);
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
            string id = handler.extractKeyFromLink(args, "Hgameobject_entry");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out uint gameObjectId) || gameObjectId == 0)
            {
                handler.SendSysMessage(CypherStrings.CommandListobjinvalidid, gameObjectId);
                return false;
            }

            GameObjectTemplate gInfo = Global.ObjectMgr.GetGameObjectTemplate(gameObjectId);
            if (gInfo == null)
            {
                handler.SendSysMessage(CypherStrings.CommandListobjinvalidid, gameObjectId);
                return false;
            }

            if (!uint.TryParse(args.NextString(), out uint count))
                count = 10;

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

                    if (handler.GetSession() != null)
                        handler.SendSysMessage(CypherStrings.GoListChat, guid, entry, guid, gInfo.name, x, y, z, mapId);
                    else
                        handler.SendSysMessage(CypherStrings.GoListConsole, guid, gInfo.name, x, y, z, mapId);
                }
                while (result.NextRow());
            }

            handler.SendSysMessage(CypherStrings.CommandListobjmessage, gameObjectId, objectCount);

            return true;
        }

        [Command("scenes", RBACPermissions.CommandListScenes)]
        static bool HandleListScenesCommand(StringArguments args, CommandHandler handler)
        {
            Player target = handler.getSelectedPlayer();
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
    }
}
