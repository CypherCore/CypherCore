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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Arenas;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Garrisons;
using Game.Groups;
using Game.Guilds;
using Game.Mails;
using Game.Maps;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Entities
{
    public partial class Player
    {
        void _LoadInventory(SQLResult result, SQLResult artifactsResult, uint timeDiff)
        {
            //                 0     1                       2                 3                   4                 5
            // SELECT a.itemGuid, a.xp, a.artifactAppearanceId, a.artifactTierId, ap.artifactPowerId, ap.purchasedRank FROM item_instance_artifact_powers ap LEFT JOIN item_instance_artifact a ON ap.itemGuid = a.itemGuid INNER JOIN character_inventory ci ON ci.item = ap.guid WHERE ci.guid = ?
            var artifactData = new Dictionary<ObjectGuid, Tuple<ulong, uint, uint, List<ItemDynamicFieldArtifactPowers>>>();
            if (!artifactsResult.IsEmpty())
            {
                do
                {
                    var artifactDataEntry = Tuple.Create(artifactsResult.Read<ulong>(1), artifactsResult.Read<uint>(2), artifactsResult.Read<uint>(3), new List<ItemDynamicFieldArtifactPowers>());
                    ItemDynamicFieldArtifactPowers artifactPowerData = new ItemDynamicFieldArtifactPowers();
                    artifactPowerData.ArtifactPowerId = artifactsResult.Read<uint>(4);
                    artifactPowerData.PurchasedRank = artifactsResult.Read<byte>(5);

                    ArtifactPowerRecord artifactPower = CliDB.ArtifactPowerStorage.LookupByKey(artifactPowerData.ArtifactPowerId);
                    if (artifactPower != null)
                    {
                        uint maxRank = artifactPower.MaxPurchasableRank;
                        // allow ARTIFACT_POWER_FLAG_FINAL to overflow maxrank here - needs to be handled in Item::CheckArtifactUnlock (will refund artifact power)
                        if (artifactPower.Flags.HasAnyFlag(ArtifactPowerFlag.MaxRankWithTier) && artifactPower.Tier < artifactDataEntry.Item3)
                            maxRank += artifactDataEntry.Item3 - artifactPower.Tier;

                        if (artifactPowerData.PurchasedRank > maxRank)
                            artifactPowerData.PurchasedRank = (byte)maxRank;

                        artifactPowerData.CurrentRankWithBonus = (byte)((artifactPower.Flags & ArtifactPowerFlag.First) == ArtifactPowerFlag.First ? 1 : 0);

                        artifactDataEntry.Item4.Add(artifactPowerData);
                    }

                    artifactData[ObjectGuid.Create(HighGuid.Item, artifactsResult.Read<ulong>(0))] = artifactDataEntry;

                } while (artifactsResult.NextRow());
            }

            if (!result.IsEmpty())
            {
                uint zoneId = GetZoneId();
                Dictionary<ObjectGuid, Bag> bagMap = new Dictionary<ObjectGuid, Bag>();                               // fast guid lookup for bags
                Dictionary<ObjectGuid, Item> invalidBagMap = new Dictionary<ObjectGuid, Item>();                       // fast guid lookup for bags
                Queue<Item> problematicItems = new Queue<Item>();
                SQLTransaction trans = new SQLTransaction();

                // Prevent items from being added to the queue while loading
                m_itemUpdateQueueBlocked = true;
                do
                {
                    Item item = _LoadItem(trans, zoneId, timeDiff, result.GetFields());
                    if (item != null)
                    {
                        var artifactDataPair = artifactData.LookupByKey(item.GetGUID());
                        if (item.GetTemplate().GetArtifactID() != 0 && artifactDataPair != null)
                            item.LoadArtifactData(this, artifactDataPair.Item1, artifactDataPair.Item2, artifactDataPair.Item3, artifactDataPair.Item4);

                        ulong counter = result.Read<ulong>(45);
                        ObjectGuid bagGuid = counter != 0 ? ObjectGuid.Create(HighGuid.Item, counter) : ObjectGuid.Empty;
                        byte slot = result.Read<byte>(46);

                        GetSession().GetCollectionMgr().CheckHeirloomUpgrades(item);
                        GetSession().GetCollectionMgr().AddItemAppearance(item);

                        InventoryResult err = InventoryResult.Ok;
                        if (item.HasFlag(ItemFields.Flags, ItemFieldFlags.Child))
                        {
                            Item parent = GetItemByGuid(item.GetGuidValue(ItemFields.Creator));
                            if (parent)
                            {
                                parent.SetChildItem(item.GetGUID());
                                item.CopyArtifactDataFromParent(parent);
                            }
                            else
                                err = InventoryResult.WrongBagType3; // send by mail
                        }

                        // Item is not in bag
                        if (bagGuid.IsEmpty())
                        {
                            item.SetContainer(null);
                            item.SetSlot(slot);

                            if (IsInventoryPos(InventorySlots.Bag0, slot))
                            {
                                List<ItemPosCount> dest = new List<ItemPosCount>();
                                err = CanStoreItem(InventorySlots.Bag0, slot, dest, item, false);
                                if (err == InventoryResult.Ok)
                                    item = StoreItem(dest, item, true);
                            }
                            else if (IsEquipmentPos(InventorySlots.Bag0, slot))
                            {
                                ushort dest;

                                err = CanEquipItem(slot, out dest, item, false, false);
                                if (err == InventoryResult.Ok)
                                    QuickEquipItem(dest, item);
                            }
                            else if (IsBankPos(InventorySlots.Bag0, slot))
                            {
                                List<ItemPosCount> dest = new List<ItemPosCount>();
                                err = CanBankItem(InventorySlots.Bag0, slot, dest, item, false, false);
                                if (err == InventoryResult.Ok)
                                    item = BankItem(dest, item, true);
                            }

                            // Remember bags that may contain items in them
                            if (err == InventoryResult.Ok)
                            {
                                if (IsBagPos(item.GetPos()))
                                {
                                    Bag pBag = item.ToBag();
                                    if (pBag != null)
                                        bagMap.Add(item.GetGUID(), pBag);
                                }
                            }
                            else if (IsBagPos(item.GetPos()))
                                if (item.IsBag())
                                    invalidBagMap.Add(item.GetGUID(), item);
                        }
                        else
                        {
                            item.SetSlot(ItemConst.NullSlot);
                            // Item is in the bag, find the bag
                            var bag = bagMap.LookupByKey(bagGuid);
                            if (bag != null)
                            {
                                List<ItemPosCount> dest = new List<ItemPosCount>();
                                err = CanStoreItem(bag.GetSlot(), slot, dest, item);
                                if (err == InventoryResult.Ok)
                                    item = StoreItem(dest, item, true);
                            }
                            else if (invalidBagMap.ContainsKey(bagGuid))
                            {
                                var invalidBag = invalidBagMap.LookupByKey(bagGuid);
                                if (problematicItems.Contains(invalidBag))
                                    err = InventoryResult.InternalBagError;
                            }
                            else
                            {
                                Log.outError(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: '{1}') has item (GUID: {2}, entry: {3}) which doesnt have a valid bag (Bag GUID: {4}, slot: {5}). Possible cheat?",
                                    GetGUID().ToString(), GetName(), item.GetGUID().ToString(), item.GetEntry(), bagGuid, slot);
                                item.DeleteFromInventoryDB(trans);
                                continue;
                            }

                        }

                        // Item's state may have changed after storing
                        if (err == InventoryResult.Ok)
                            item.SetState(ItemUpdateState.Unchanged, this);
                        else
                        {
                            Log.outError(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: '{1}') has item (GUID: {2}, entry: {3}) which can't be loaded into inventory (Bag GUID: {4}, slot: {5}) by reason {6}. " +
                                "Item will be sent by mail.", GetGUID().ToString(), GetName(), item.GetGUID().ToString(), item.GetEntry(), bagGuid, slot, err);
                            item.DeleteFromInventoryDB(trans);
                            problematicItems.Enqueue(item);
                        }
                    }
                } while (result.NextRow());

                m_itemUpdateQueueBlocked = false;

                // Send problematic items by mail
                while (problematicItems.Count != 0)
                {
                    string subject = Global.ObjectMgr.GetCypherString(CypherStrings.NotEquippedItem);
                    MailDraft draft = new MailDraft(subject, "There were problems with equipping item(s).");
                    for (int i = 0; problematicItems.Count != 0 && i < SharedConst.MaxMailItems; ++i)
                    {
                        draft.AddItem(problematicItems.Dequeue());
                    }
                    draft.SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied);
                }

                DB.Characters.CommitTransaction(trans);
            }

            _ApplyAllItemMods();
        }
        Item _LoadItem(SQLTransaction trans, uint zoneId, uint timeDiff, SQLFields fields)
        {
            Item item = null;
            ulong itemGuid = fields.Read<ulong>(0);
            uint itemEntry = fields.Read<uint>(1);
            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemEntry);
            if (proto != null)
            {
                bool remove = false;
                item = Bag.NewItemOrBag(proto);
                if (item.LoadFromDB(itemGuid, GetGUID(), fields, itemEntry))
                {
                    PreparedStatement stmt = null;

                    // Do not allow to have item limited to another map/zone in alive state
                    if (IsAlive() && item.IsLimitedToAnotherMapOrZone(GetMapId(), zoneId))
                    {
                        Log.outDebug(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: '{1}', map: {2}) has item (GUID: {3}, entry: {4}) limited to another map ({5}). Deleting item.",
                            GetGUID().ToString(), GetName(), GetMapId(), item.GetGUID().ToString(), item.GetEntry(), zoneId);
                        remove = true;
                    }
                    // "Conjured items disappear if you are logged out for more than 15 minutes"
                    else if (timeDiff > 15 * Time.Minute && proto.GetFlags().HasAnyFlag(ItemFlags.Conjured))
                    {
                        Log.outDebug(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: {1}, diff: {2}) has conjured item (GUID: {3}, entry: {4}) with expired lifetime (15 minutes). Deleting item.",
                            GetGUID().ToString(), GetName(), timeDiff, item.GetGUID().ToString(), item.GetEntry());
                        remove = true;
                    }
                    if (item.HasFlag((int)ItemFields.Flags, ItemFieldFlags.Refundable))
                    {
                        if (item.GetPlayedTime() > (2 * Time.Hour))
                        {
                            Log.outDebug(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: {1}) has item (GUID: {2}, entry: {3}) with expired refund time ({4}). Deleting refund data and removing " +
                                "efundable flag.", GetGUID().ToString(), GetName(), item.GetGUID().ToString(), item.GetEntry(), item.GetPlayedTime());

                            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_REFUND_INSTANCE);
                            stmt.AddValue(0, item.GetGUID().ToString());
                            trans.Append(stmt);

                            item.RemoveFlag(ItemFields.Flags, ItemFieldFlags.Refundable);
                        }
                        else
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_REFUNDS);
                            stmt.AddValue(0, item.GetGUID().GetCounter());
                            stmt.AddValue(1, GetGUID().GetCounter());
                            SQLResult result = DB.Characters.Query(stmt);
                            if (!result.IsEmpty())
                            {
                                item.SetRefundRecipient(GetGUID());
                                item.SetPaidMoney(result.Read<ulong>(0));
                                item.SetPaidExtendedCost(result.Read<ushort>(1));
                                AddRefundReference(item.GetGUID());
                            }
                            else
                            {
                                Log.outDebug(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: {1}) has item (GUID: {2}, entry: {3}) with refundable flags, but without data in item_refund_instance. Removing flag.",
                                    GetGUID().ToString(), GetName(), item.GetGUID().ToString(), item.GetEntry());
                                item.RemoveFlag(ItemFields.Flags, ItemFieldFlags.Refundable);
                            }
                        }
                    }
                    else if (item.HasFlag(ItemFields.Flags, ItemFieldFlags.BopTradeable))
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_BOP_TRADE);
                        stmt.AddValue(0, item.GetGUID().ToString());
                        SQLResult result = DB.Characters.Query(stmt);
                        if (!result.IsEmpty())
                        {
                            string strGUID = result.Read<string>(0);
                            var GUIDlist = new StringArray(strGUID, ' ');
                            List<ObjectGuid> looters = new List<ObjectGuid>();
                            for (var i = 0; i < GUIDlist.Length; ++i)
                            {
                                if (ulong.TryParse(GUIDlist[i], out ulong guid))
                                    looters.Add(ObjectGuid.Create(HighGuid.Item, guid));
                            }

                            if (looters.Count > 1 && item.GetTemplate().GetMaxStackSize() == 1 && item.IsSoulBound())
                            {
                                item.SetSoulboundTradeable(looters);
                                AddTradeableItem(item);
                            }
                            else
                                item.ClearSoulboundTradeable(this);
                        }
                        else
                        {
                            Log.outDebug(LogFilter.ServerLoading, "LoadInventory: player ({0}, name: {1}) has item ({2}, entry: {3}) with ITEM_FLAG_BOP_TRADEABLE flag, " +
                                "but without data in item_soulbound_trade_data. Removing flag.", GetGUID().ToString(), GetName(), item.GetGUID().ToString(), item.GetEntry());
                            item.RemoveFlag(ItemFields.Flags, ItemFieldFlags.BopTradeable);
                        }
                    }
                    else if (proto.GetHolidayID() != 0)
                    {
                        remove = true;
                        var events = Global.GameEventMgr.GetEventMap();
                        var activeEventsList = Global.GameEventMgr.GetActiveEventList();
                        foreach (var id in activeEventsList)
                        {
                            if (events[id].holiday_id == proto.GetHolidayID())
                            {
                                remove = false;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Log.outError(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: {1}) has broken item (GUID: {2}, entry: {3}) in inventory. Deleting item.",
                        GetGUID().ToString(), GetName(), itemGuid, itemEntry);
                    remove = true;
                }
                // Remove item from inventory if necessary
                if (remove)
                {
                    Item.DeleteFromInventoryDB(trans, itemGuid);
                    item.FSetState(ItemUpdateState.Removed);
                    item.SaveToDB(trans);                           // it also deletes item object!
                    item = null;
                }
            }
            else
            {
                Log.outError(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: {1}) has unknown item (entry: {2}) in inventory. Deleting item.",
                    GetGUID().ToString(), GetName(), itemEntry);
                Item.DeleteFromInventoryDB(trans, itemGuid);
                Item.DeleteFromDB(trans, itemGuid);
            }
            return item;
        }
        void _LoadSkills(SQLResult result)
        {
            var count = 0;
            Dictionary<uint, uint> loadedSkillValues = new Dictionary<uint, uint>();
            if (!result.IsEmpty())
            {
                do
                {
                    var skill = result.Read<ushort>(0);
                    var value = result.Read<ushort>(1);
                    var max = result.Read<ushort>(2);

                    SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skill, GetRace(), GetClass());
                    if (rcEntry == null)
                    {
                        Log.outError(LogFilter.Player, "Character: {0}(GUID: {1} Race: {2} Class: {3}) has skill {4} not allowed for his race/class combination",
                            GetName(), GetGUID().ToString(), GetRace(), GetClass(), skill);
                        mSkillStatus.Add(skill, new SkillStatusData(0, SkillState.Deleted));
                        continue;
                    }

                    // set fixed skill ranges
                    switch (Global.SpellMgr.GetSkillRangeType(rcEntry))
                    {
                        case SkillRangeType.Language:
                            value = max = 300;
                            break;
                        case SkillRangeType.Mono:
                            value = max = 1;
                            break;
                        case SkillRangeType.Level:
                            max = GetMaxSkillValueForLevel();
                            break;
                        default:
                            break;
                    }
                    if (value == 0)
                    {
                        Log.outError(LogFilter.Player, "Character {0} has skill {1} with value 0. Will be deleted.", GetGUID().ToString(), skill);

                        PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_SKILL);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, skill);
                        DB.Characters.Execute(stmt);
                        continue;
                    }
                    var field = (ushort)(count / 2);
                    var offset = (byte)(count & 1);

                    SetUInt16Value(ActivePlayerFields.SkillLineId + field, offset, skill);
                    ushort step = 0;

                    SkillLineRecord skillLine = CliDB.SkillLineStorage.LookupByKey(rcEntry.SkillID);
                    if (skillLine != null)
                    {
                        if (skillLine.CategoryID == SkillCategory.Secondary)
                            step = (ushort)(max / 75);

                        if (skillLine.CategoryID == SkillCategory.Profession)
                        {
                            step = (ushort)(max / 75);

                            if (skillLine.ParentSkillLineID != 0 && skillLine.ParentTierIndex != 0)
                            {
                                int professionSlot = FindProfessionSlotFor(skill);
                                if (professionSlot != -1)
                                    SetUInt32Value(ActivePlayerFields.ProfessionSkillLine + professionSlot, skill);
                            }
                        }
                    }

                    SetUInt16Value(ActivePlayerFields.SkillLineStep + field, offset, step);
                    SetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset, value);
                    SetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset, max);
                    SetUInt16Value(ActivePlayerFields.SkillLineTempBonus + field, offset, 0);
                    SetUInt16Value(ActivePlayerFields.SkillLinePermBonus + field, offset, 0);

                    mSkillStatus.Add(skill, new SkillStatusData((uint)count, SkillState.Unchanged));

                    loadedSkillValues[skill] = value;
                    count++;

                    if (count >= SkillConst.MaxPlayerSkills)                      // client limit
                    {
                        Log.outError(LogFilter.Player, "Character {0} has more than {1} skills.", GetGUID().ToString(), SkillConst.MaxPlayerSkills);
                        break;
                    }
                }
                while (result.NextRow());
            }
            // Learn skill rewarded spells after all skills have been loaded to prevent learning a skill from them before its loaded with proper value from DB
            foreach (var skill in loadedSkillValues)
                LearnSkillRewardedSpells(skill.Key, skill.Value);

            if (HasSkill(SkillType.FistWeapons))
                SetSkill(SkillType.FistWeapons, 0, GetSkillValue(SkillType.Unarmed), GetMaxSkillValueForLevel());

            for (; count < SkillConst.MaxPlayerSkills; count++)
            {
                var field = (ushort)(count / 2);
                var offset = (byte)(count & 1);

                SetUInt16Value(ActivePlayerFields.SkillLineId + field, offset, 0);
                SetUInt16Value(ActivePlayerFields.SkillLineStep + field, offset, 0);
                SetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset, 0);
                SetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset, 0);
                SetUInt16Value(ActivePlayerFields.SkillLineTempBonus + field, offset, 0);
                SetUInt16Value(ActivePlayerFields.SkillLinePermBonus + field, offset, 0);
            }
        }
        void _LoadSpells(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    AddSpell(result.Read<uint>(0), result.Read<bool>(1), false, false, result.Read<bool>(2), true);
                }
                while (result.NextRow());
            }
        }

        void _LoadAuras(SQLResult auraResult, SQLResult effectResult, uint timediff)
        {
            Log.outDebug(LogFilter.Player, "Loading auras for player {0}", GetGUID().ToString());

            ObjectGuid casterGuid = new ObjectGuid();
            ObjectGuid itemGuid = new ObjectGuid();
            Dictionary<AuraKey, AuraLoadEffectInfo> effectInfo = new Dictionary<AuraKey, AuraLoadEffectInfo>();
            if (!effectResult.IsEmpty())
            {
                do
                {
                    uint effectIndex = effectResult.Read<byte>(4);
                    if (effectIndex < SpellConst.MaxEffects)
                    {
                        casterGuid.SetRawValue(effectResult.Read<byte[]>(0));
                        itemGuid.SetRawValue(effectResult.Read<byte[]>(1));

                        AuraKey key = new AuraKey(casterGuid, itemGuid, effectResult.Read<uint>(2), effectResult.Read<uint>(3));
                        if (!effectInfo.ContainsKey(key))
                            effectInfo[key] = new AuraLoadEffectInfo();

                        AuraLoadEffectInfo info = effectInfo[key];
                        info.Amounts[effectIndex] = effectResult.Read<int>(5);
                        info.BaseAmounts[effectIndex] = effectResult.Read<int>(6);
                    }
                }
                while (effectResult.NextRow());
            }

            if (!auraResult.IsEmpty())
            {
                do
                {
                    casterGuid.SetRawValue(auraResult.Read<byte[]>(0));
                    itemGuid.SetRawValue(auraResult.Read<byte[]>(1));
                    AuraKey key = new AuraKey(casterGuid, itemGuid, auraResult.Read<uint>(2), auraResult.Read<uint>(3));
                    uint recalculateMask = auraResult.Read<uint>(4);
                    byte stackCount = auraResult.Read<byte>(5);
                    int maxDuration = auraResult.Read<int>(6);
                    int remainTime = auraResult.Read<int>(7);
                    byte remainCharges = auraResult.Read<byte>(8);
                    int castItemLevel = auraResult.Read<int>(9);

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(key.SpellId);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Player, "Unknown aura (spellid {0}), ignore.", key.SpellId);
                        continue;
                    }

                    // negative effects should continue counting down after logout
                    if (remainTime != -1 && !spellInfo.IsPositive())
                    {
                        if (remainTime / Time.InMilliseconds <= timediff)
                            continue;

                        remainTime -= (int)(timediff * Time.InMilliseconds);
                    }

                    // prevent wrong values of remaincharges
                    if (spellInfo.ProcCharges != 0)
                    {
                        // we have no control over the order of applying auras and modifiers allow auras
                        // to have more charges than value in SpellInfo
                        if (remainCharges <= 0)
                            remainCharges = (byte)spellInfo.ProcCharges;
                    }
                    else
                        remainCharges = 0;

                    AuraLoadEffectInfo info = effectInfo[key];
                    ObjectGuid castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, GetMapId(), spellInfo.Id, GetMap().GenerateLowGuid(HighGuid.Cast));
                    Aura aura = Aura.TryCreate(spellInfo, castId, key.EffectMask, this, null, info.BaseAmounts, null, casterGuid, itemGuid, castItemLevel);
                    if (aura != null)
                    {
                        if (!aura.CanBeSaved())
                        {
                            aura.Remove();
                            continue;
                        }

                        aura.SetLoadedState(maxDuration, remainTime, remainCharges, stackCount, recalculateMask, info.Amounts);
                        aura.ApplyForTargets();
                        Log.outInfo(LogFilter.Player, "Added aura spellid {0}, effectmask {1}", spellInfo.Id, key.EffectMask);
                    }
                }
                while (auraResult.NextRow());
            }
        }
        bool _LoadHomeBind(SQLResult result)
        {
            PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());
            if (info == null)
            {
                Log.outError(LogFilter.Player, "Player (Name {0}) has incorrect race/class ({1}/{2}) pair. Can't be loaded.", GetName(), GetRace(), GetClass());
                return false;
            }

            bool ok = false;
            if (!result.IsEmpty())
            {
                homebind = new WorldLocation();

                homebind.SetMapId(result.Read<uint>(0));
                homebindAreaId = result.Read<uint>(1);
                homebind.posX = result.Read<float>(2);
                homebind.posY = result.Read<float>(3);
                homebind.posZ = result.Read<float>(4);

                var map = CliDB.MapStorage.LookupByKey(homebind.GetMapId());

                // accept saved data only for valid position (and non instanceable), and accessable
                if (GridDefines.IsValidMapCoord(homebind.GetMapId(), homebind.posX, homebind.posY, homebind.posZ) &&
                    !map.Instanceable() && GetSession().GetExpansion() >= map.Expansion())
                    ok = true;
                else
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    DB.Characters.Execute(stmt);
                }
            }

            if (!ok)
            {
                homebind = new WorldLocation(info.MapId, info.PositionX, info.PositionY, info.PositionZ, info.Orientation);
                homebindAreaId = info.ZoneId;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PLAYER_HOMEBIND);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, homebind.GetMapId());
                stmt.AddValue(2, homebindAreaId);
                stmt.AddValue(3, homebind.posX);
                stmt.AddValue(4, homebind.posY);
                stmt.AddValue(5, homebind.posZ);
                DB.Characters.Execute(stmt);
            }

            Log.outDebug(LogFilter.Player, "Setting player home position - mapid: {0}, areaid: {1}, {2}",
                homebind.GetMapId(), homebindAreaId, homebind);

            return true;
        }
        void _LoadCurrency(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                ushort currencyID = result.Read<ushort>(0);

                var currency = CliDB.CurrencyTypesStorage.LookupByKey(currencyID);
                if (currency == null)
                    continue;

                PlayerCurrency cur = new PlayerCurrency();
                cur.state = PlayerCurrencyState.Unchanged;
                cur.Quantity = result.Read<uint>(1);
                cur.WeeklyQuantity = result.Read<uint>(2);
                cur.TrackedQuantity = result.Read<uint>(3);
                cur.Flags = result.Read<byte>(4);

                _currencyStorage.Add(currencyID, cur);
            } while (result.NextRow());
        }
        void _LoadActions(SQLResult result)
        {
            m_actionButtons.Clear();
            if (!result.IsEmpty())
            {
                do
                {
                    byte button = result.Read<byte>(0);
                    uint action = result.Read<uint>(1);
                    byte type = result.Read<byte>(2);

                    ActionButton ab = AddActionButton(button, action, type);
                    if (ab != null)
                        ab.uState = ActionButtonUpdateState.UnChanged;
                    else
                    {
                        Log.outError(LogFilter.Player, "  ...at loading, and will deleted in DB also");

                        // Will deleted in DB at next save (it can create data until save but marked as deleted)
                        m_actionButtons[button] = new ActionButton();
                        m_actionButtons[button].uState = ActionButtonUpdateState.Deleted;
                    }
                } while (result.NextRow());
            }
        }
        void _LoadQuestStatus(SQLResult result)
        {
            ushort slot = 0;
            if (!result.IsEmpty())
            {
                do
                {
                    uint quest_id = result.Read<uint>(0);
                    // used to be new, no delete?
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
                    if (quest != null)
                    {
                        // find or create
                        QuestStatusData questStatusData = new QuestStatusData();

                        byte qstatus = result.Read<byte>(1);
                        if (qstatus < (byte)QuestStatus.Max)
                            questStatusData.Status = (QuestStatus)qstatus;
                        else
                        {
                            questStatusData.Status = QuestStatus.Incomplete;
                            Log.outError(LogFilter.Player, "Player {0} (GUID: {1}) has invalid quest {2} status ({3}), replaced by QUEST_STATUS_INCOMPLETE(3).",
                                GetName(), GetGUID().ToString(), quest_id, qstatus);
                        }

                        long quest_time = result.Read<uint>(2);

                        if (quest.HasSpecialFlag(QuestSpecialFlags.Timed) && !GetQuestRewardStatus(quest_id))
                        {
                            AddTimedQuest(quest_id);

                            if (quest_time <= Global.WorldMgr.GetGameTime())
                                questStatusData.Timer = 1;
                            else
                                questStatusData.Timer = (uint)((quest_time - Global.WorldMgr.GetGameTime()) * Time.InMilliseconds);
                        }
                        else
                            quest_time = 0;

                        // add to quest log
                        if (slot < SharedConst.MaxQuestLogSize && questStatusData.Status != QuestStatus.None)
                        {
                            SetQuestSlot(slot, quest_id, (uint)quest_time); // cast can't be helped

                            if (questStatusData.Status == QuestStatus.Complete)
                                SetQuestSlotState(slot, QuestSlotStateMask.Complete);
                            else if (questStatusData.Status == QuestStatus.Failed)
                                SetQuestSlotState(slot, QuestSlotStateMask.Fail);

                            ++slot;
                        }

                        // Resize quest objective data to proper size
                        int maxStorageIndex = 0;
                        foreach (QuestObjective obj in quest.Objectives)
                            if (obj.StorageIndex > maxStorageIndex)
                            maxStorageIndex = obj.StorageIndex;

                        questStatusData.ObjectiveData = new int[maxStorageIndex + 1];

                        m_QuestStatus[quest_id] = questStatusData;
                        Log.outDebug(LogFilter.ServerLoading, "Quest status is {0} for quest {1} for player (GUID: {2})", questStatusData.Status, quest_id, GetGUID().ToString());
                    }
                }
                while (result.NextRow());
            }

            // clear quest log tail
            for (ushort i = slot; i < SharedConst.MaxQuestLogSize; ++i)
                SetQuestSlot(i, 0);
        }
        void _LoadQuestStatusObjectives(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    uint questID = result.Read<uint>(0);

                    Quest quest = Global.ObjectMgr.GetQuestTemplate(questID);
                    ushort slot = FindQuestSlot(questID);

                    var questStatusData = m_QuestStatus.LookupByKey(questID);
                    if (questStatusData != null && slot < SharedConst.MaxQuestLogSize && quest != null)
                    {
                        byte objectiveIndex = result.Read<byte>(1);

                        var objectiveItr = quest.Objectives.FirstOrDefault(objective => objective.StorageIndex == objectiveIndex);
                        if (objectiveIndex < questStatusData.ObjectiveData.Length && objectiveItr != null)
                        {
                            int data = result.Read<int>(2);
                            questStatusData.ObjectiveData[objectiveIndex] = data;
                            if (!objectiveItr.IsStoringFlag())
                                SetQuestSlotCounter(slot, objectiveIndex, (ushort)data);
                            else if (data != 0)
                                SetQuestSlotState(slot, (QuestSlotStateMask)(256 << objectiveIndex));
                        }
                        else
                            Log.outError(LogFilter.Player, "Player {0} ({1}) has quest {2} out of range objective index {3}.", GetName(), GetGUID().ToString(), questID, objectiveIndex);
                    }
                    else
                        Log.outError(LogFilter.Player, "Player {0} ({1}) does not have quest {2} but has objective data for it.", GetName(), GetGUID().ToString(), questID);
                }
                while (result.NextRow());
            }
        }
        void _LoadQuestStatusRewarded(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    uint quest_id = result.Read<uint>(0);
                    // used to be new, no delete?
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
                    if (quest != null)
                    {
                        // learn rewarded spell if unknown
                        LearnQuestRewardedSpells(quest);

                        // set rewarded title if any
                        if (quest.RewardTitleId != 0)
                        {
                            CharTitlesRecord titleEntry = CliDB.CharTitlesStorage.LookupByKey(quest.RewardTitleId);
                            if (titleEntry != null)
                                SetTitle(titleEntry);
                        }

                        // Skip loading special quests - they are also added to rewarded quests but only once and remain there forever
                        // instead add them separately from load daily/weekly/monthly/seasonal
                        if (!quest.IsDailyOrWeekly() && !quest.IsMonthly() && !quest.IsSeasonal())
                        {
                            uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);
                            if (questBit != 0)
                                SetQuestCompletedBit(questBit, true);
                        }

                        for (uint i = 0; i < quest.GetRewChoiceItemsCount(); ++i)
                            GetSession().GetCollectionMgr().AddItemAppearance(quest.RewardChoiceItemId[i]);

                        for (uint i = 0; i < quest.GetRewItemsCount(); ++i)
                            GetSession().GetCollectionMgr().AddItemAppearance(quest.RewardItemId[i]);

                        var questPackageItems = Global.DB2Mgr.GetQuestPackageItems(quest.PackageID);
                        if (questPackageItems != null)
                        {
                            foreach (QuestPackageItemRecord questPackageItem in questPackageItems)
                            {
                                ItemTemplate rewardProto = Global.ObjectMgr.GetItemTemplate(questPackageItem.ItemID);
                                if (rewardProto != null)
                                    if (rewardProto.ItemSpecClassMask.HasAnyFlag(getClassMask()))
                                        GetSession().GetCollectionMgr().AddItemAppearance(questPackageItem.ItemID);
                            }
                        }

                        if (quest.CanIncreaseRewardedQuestCounters())
                            m_RewardedQuests.Add(quest_id);
                    }
                }
                while (result.NextRow());
            }
        }
        void _LoadDailyQuestStatus(SQLResult result)
        {
            m_DFQuests.Clear();

            //QueryResult* result = CharacterDatabase.PQuery("SELECT quest, time FROM character_queststatus_daily WHERE guid = '{0}'");
            if (!result.IsEmpty())
            {
                do
                {
                    uint quest_id = result.Read<uint>(0);
                    Quest qQuest = Global.ObjectMgr.GetQuestTemplate(quest_id);
                    if (qQuest != null)
                    {
                        if (qQuest.IsDFQuest())
                        {
                            m_DFQuests.Add(qQuest.Id);
                            m_lastDailyQuestTime = result.Read<uint>(1);
                            continue;
                        }
                    }

                    // save _any_ from daily quest times (it must be after last reset anyway)
                    m_lastDailyQuestTime = result.Read<uint>(1);

                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
                    if (quest == null)
                        continue;

                    AddDynamicValue(ActivePlayerDynamicFields.DailyQuests, quest_id);
                    uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);
                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Daily quest ({0}) cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
                }
                while (result.NextRow());
            }

            m_DailyQuestChanged = false;
        }
        void _LoadWeeklyQuestStatus(SQLResult result)
        {
            m_weeklyquests.Clear();

            if (!result.IsEmpty())
            {
                do
                {
                    uint quest_id = result.Read<uint>(0);
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
                    if (quest == null)
                        continue;

                    m_weeklyquests.Add(quest_id);
                    uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);
                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Weekly quest {{0}} cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
                }
                while (result.NextRow());
            }

            m_WeeklyQuestChanged = false;
        }
        void _LoadSeasonalQuestStatus(SQLResult result)
        {
            m_seasonalquests.Clear();

            if (!result.IsEmpty())
            {
                do
                {
                    uint quest_id = result.Read<uint>(0);
                    uint event_id = result.Read<uint>(1);
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
                    if (quest == null)
                        continue;

                    m_seasonalquests.Add(event_id, quest_id);
                    uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);
                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Seasonal quest {{0}} cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
                }
                while (result.NextRow());
            }

            m_SeasonalQuestChanged = false;
        }
        void _LoadMonthlyQuestStatus()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_MONTHLY);
            stmt.AddValue(0, GetGUID().GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            m_monthlyquests.Clear();

            if (!result.IsEmpty())
            {
                do
                {
                    uint quest_id = result.Read<uint>(0);
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
                    if (quest == null)
                        continue;

                    m_monthlyquests.Add(quest_id);
                    uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);
                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Monthly quest {{0}} cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
                }
                while (result.NextRow());
            }

            m_MonthlyQuestChanged = false;
        }
        void _LoadTalents(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    TalentRecord talent = CliDB.TalentStorage.LookupByKey(result.Read<uint>(0));
                    if (talent != null)
                        AddTalent(talent, result.Read<byte>(1), false);
                }
                while (result.NextRow());
            }
        }
        void _LoadPvpTalents(SQLResult result)
        {
            // "SELECT talentID0, talentID1, talentID2, talentID3, talentGroup FROM character_pvp_talent WHERE guid = ?"
            if (!result.IsEmpty())
            {
                do
                {
                    for (byte slot = 0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
                    {
                        PvpTalentRecord talent = CliDB.PvpTalentStorage.LookupByKey(result.Read<uint>(slot));
                        if (talent != null)
                            AddPvpTalent(talent, result.Read<byte>(4), slot);
                    }
                }
                while (result.NextRow());
            }
        }
        void _LoadGlyphs(SQLResult result)
        {
            // SELECT talentGroup, glyphId from character_glyphs WHERE guid = ?
            if (result.IsEmpty())
                return;

            do
            {
                byte spec = result.Read<byte>(0);
                if (spec >= PlayerConst.MaxSpecializations || Global.DB2Mgr.GetChrSpecializationByIndex(GetClass(), spec) == null)
                    continue;

                ushort glyphId = result.Read<ushort>(1);
                if (!CliDB.GlyphPropertiesStorage.ContainsKey(glyphId))
                    continue;

                GetGlyphs(spec).Add(glyphId);

            } while (result.NextRow());
        }
        void _LoadGlyphAuras()
        {
            foreach (uint glyphId in GetGlyphs(GetActiveTalentGroup()))
                CastSpell(this, CliDB.GlyphPropertiesStorage.LookupByKey(glyphId).SpellID, true);
        }
        public void LoadCorpse(SQLResult result)
        {
            if (IsAlive() || HasAtLoginFlag(AtLoginFlags.Resurrect))
                SpawnCorpseBones(false);

            if (!IsAlive())
            {
                if (!result.IsEmpty() && !HasAtLoginFlag(AtLoginFlags.Resurrect))
                {
                    _corpseLocation = new WorldLocation(result.Read<ushort>(0), result.Read<float>(1), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4));
                    ApplyModFlag(ActivePlayerFields.LocalFlags, PlayerLocalFlags.ReleaseTimer, !CliDB.MapStorage.LookupByKey(_corpseLocation.GetMapId()).Instanceable());
                }
                else
                    ResurrectPlayer(0.5f);
            }

            RemoveAtLoginFlag(AtLoginFlags.Resurrect);
        }
        void _LoadBoundInstances(SQLResult result)
        {
            m_boundInstances.Clear();

            Group group = GetGroup();

            if (!result.IsEmpty())
            {
                do
                {
                    bool perm = result.Read<bool>(1);
                    uint mapId = result.Read<ushort>(2);
                    uint instanceId = result.Read<uint>(0);
                    byte difficulty = result.Read<byte>(3);
                    BindExtensionState extendState = (BindExtensionState)result.Read<byte>(4);

                    long resetTime = result.Read<uint>(5);
                    // the resettime for normal instances is only saved when the InstanceSave is unloaded
                    // so the value read from the DB may be wrong here but only if the InstanceSave is loaded
                    // and in that case it is not used

                    uint entranceId = result.Read<uint>(6);

                    bool deleteInstance = false;

                    MapRecord mapEntry = CliDB.MapStorage.LookupByKey(mapId);
                    string mapname = mapEntry != null ? mapEntry.MapName[Global.WorldMgr.GetDefaultDbcLocale()] : "Unknown";

                    if (mapEntry == null || !mapEntry.IsDungeon())
                    {
                        Log.outError(LogFilter.Player, "_LoadBoundInstances: player {0}({1}) has bind to not existed or not dungeon map {2} ({3})", GetName(), GetGUID().ToString(), mapId, mapname);
                        deleteInstance = true;
                    }
                    else if (difficulty >= (int)Difficulty.Max)
                    {
                        Log.outError(LogFilter.Player, "_LoadBoundInstances: player {0}({1}) has bind to not existed difficulty {2} instance for map {3} ({4})", GetName(), GetGUID().ToString(), difficulty, mapId, mapname);
                        deleteInstance = true;
                    }
                    else
                    {
                        MapDifficultyRecord mapDiff = Global.DB2Mgr.GetMapDifficultyData(mapId, (Difficulty)difficulty);
                        if (mapDiff == null)
                        {
                            Log.outError(LogFilter.Player, "_LoadBoundInstances: player {0}({1}) has bind to not existed difficulty {2} instance for map {3} ({4})", GetName(), GetGUID().ToString(), difficulty, mapId, mapname);
                            deleteInstance = true;
                        }
                        else if (!perm && group)
                        {
                            Log.outError(LogFilter.Player, "_LoadBoundInstances: player {0}({1}) is in group {2} but has a non-permanent character bind to map {3} ({4}), {5}, {6}",
                                GetName(), GetGUID().ToString(), group.GetGUID().ToString(), mapId, mapname, instanceId, difficulty);
                            deleteInstance = true;
                        }
                    }

                    if (deleteInstance)
                    {
                        PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INSTANCE_BY_INSTANCE_GUID);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, instanceId);
                        DB.Characters.Execute(stmt);

                        continue;
                    }

                    // since non permanent binds are always solo bind, they can always be reset
                    InstanceSave save = Global.InstanceSaveMgr.AddInstanceSave(mapId, instanceId, (Difficulty)difficulty, resetTime, entranceId, !perm, true);
                    if (save != null)
                        BindToInstance(save, perm, extendState, true);
                }
                while (result.NextRow());
            }
        }
        void _LoadVoidStorage(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                // SELECT itemId, itemEntry, slot, creatorGuid, randomProperty, suffixFactor, upgradeId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs FROM character_void_storage WHERE playerGuid = ?
                ulong itemId = result.Read<ulong>(0);
                uint itemEntry = result.Read<uint>(1);
                byte slot = result.Read<byte>(2);
                ObjectGuid creatorGuid = result.Read<ulong>(3) != 0 ? ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(3)) : ObjectGuid.Empty;
                ItemRandomEnchantmentId randomProperty = new ItemRandomEnchantmentId((ItemRandomEnchantmentType)result.Read<byte>(4), result.Read<uint>(5));
                uint suffixFactor = result.Read<uint>(6);
                uint upgradeId = result.Read<uint>(7);
                uint fixedScalingLevel = result.Read<uint>(8);
                uint artifactKnowledgeLevel = result.Read<uint>(9);
                byte context = result.Read<byte>(10);
                List<uint> bonusListIDs = new List<uint>();
                var bonusListIdTokens = new StringArray(result.Read<string>(11), ' ');
                for (var i = 0; i < bonusListIdTokens.Length; ++i)
                {
                    if (uint.TryParse(bonusListIdTokens[i], out uint id))
                        bonusListIDs.Add(id);
                }

                if (itemId == 0)
                {
                    Log.outError(LogFilter.Player, "Player:_LoadVoidStorage - Player (GUID: {0}, name: {1}) has an item with an invalid id (item id: item id: {2}, entry: {3}).", GetGUID().ToString(), GetName(), itemId, itemEntry);
                    continue;
                }

                if (Global.ObjectMgr.GetItemTemplate(itemEntry) == null)
                {
                    Log.outError(LogFilter.Player, "Player:_LoadVoidStorage - Player (GUID: {0}, name: {1}) has an item with an invalid entry (item id: item id: {2}, entry: {3}).", GetGUID().ToString(), GetName(), itemId, itemEntry);
                    continue;
                }

                if (slot >= SharedConst.VoidStorageMaxSlot)
                {
                    Log.outError(LogFilter.Player, "Player:_LoadVoidStorage - Player (GUID: {0}, name: {1}) has an item with an invalid slot (item id: item id: {2}, entry: {3}, slot: {4}).", GetGUID().ToString(), GetName(), itemId, itemEntry, slot);
                    continue;
                }

                _voidStorageItems[slot] = new VoidStorageItem(itemId, itemEntry, creatorGuid, randomProperty, suffixFactor, upgradeId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs);

                BonusData bonus = new BonusData(new ItemInstance(_voidStorageItems[slot]));

                GetSession().GetCollectionMgr().AddItemAppearance(itemEntry, bonus.AppearanceModID);
            }
            while (result.NextRow());
        }
        void _LoadMailInit(SQLResult resultUnread, SQLResult resultDelivery)
        {
            if (!resultUnread.IsEmpty())
                unReadMails = (byte)resultUnread.Read<ulong>(0);

            if (!resultDelivery.IsEmpty())
                m_nextMailDelivereTime = resultDelivery.Read<uint>(0);
        }
        public void _LoadMail()
        {
            m_mail.Clear();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL);
            stmt.AddValue(0, GetGUID().GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                do
                {
                    Mail m = new Mail();

                    m.messageID = result.Read<uint>(0);
                    m.messageType = (MailMessageType)result.Read<byte>(1);
                    m.sender = result.Read<uint>(2);
                    m.receiver = result.Read<uint>(3);
                    m.subject = result.Read<string>(4);
                    m.body = result.Read<string>(5);
                    bool has_items = result.Read<bool>(6);
                    m.expire_time = result.Read<uint>(7);
                    m.deliver_time = result.Read<uint>(8);
                    m.money = result.Read<ulong>(9);
                    m.COD = result.Read<ulong>(10);
                    m.checkMask = (MailCheckMask)result.Read<byte>(11);
                    m.stationery = (MailStationery)result.Read<byte>(12);
                    m.mailTemplateId = result.Read<ushort>(13);

                    if (m.mailTemplateId != 0 && !CliDB.MailTemplateStorage.ContainsKey(m.mailTemplateId))
                    {
                        Log.outError(LogFilter.Player, "Player:_LoadMail - Mail ({0}) have not existed MailTemplateId ({1}), remove at load", m.messageID, m.mailTemplateId);
                        m.mailTemplateId = 0;
                    }

                    m.state = MailState.Unchanged;

                    if (has_items)
                        _LoadMailedItems(m);

                    m_mail.Add(m);
                }
                while (result.NextRow());
            }
            m_mailsLoaded = true;
        }
        void _LoadMailedItems(Mail mail)
        {
            // data needs to be at first place for Item.LoadFromDB
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
            stmt.AddValue(0, mail.messageID);
            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
                return;

            do
            {
                ulong itemGuid = result.Read<ulong>(0);
                uint itemEntry = result.Read<uint>(1);

                mail.AddItem(itemGuid, itemEntry);

                ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemEntry);
                if (proto == null)
                {
                    Log.outError(LogFilter.Player, "Player {0} has unknown item_template (ProtoType) in mailed items(GUID: {1} template: {2}) in mail ({3}), deleted.", GetGUID().ToString(), itemGuid, itemEntry, mail.messageID);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_MAIL_ITEM);
                    stmt.AddValue(0, itemGuid);
                    DB.Characters.Execute(stmt);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
                    stmt.AddValue(0, itemGuid);
                    DB.Characters.Execute(stmt);
                    continue;
                }

                Item item = Bag.NewItemOrBag(proto);
                ObjectGuid ownerGuid = result.Read<ulong>(45) != 0 ? ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(45)) : ObjectGuid.Empty;
                if (!item.LoadFromDB(itemGuid, ownerGuid, result.GetFields(), itemEntry))
                {
                    Log.outError(LogFilter.Player, "Player:_LoadMailedItems - Item in mail ({0}) doesn't exist !!!! - item guid: {1}, deleted from mail", mail.messageID, itemGuid);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM);
                    stmt.AddValue(0, itemGuid);
                    DB.Characters.Execute(stmt);

                    item.FSetState(ItemUpdateState.Removed);

                    item.SaveToDB(null);                               // it also deletes item object !
                    continue;
                }

                AddMItem(item);
            }
            while (result.NextRow());
        }
        void _LoadDeclinedNames(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            _declinedname = new DeclinedName();
            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                _declinedname.name[i] = result.Read<string>(i);
        }
        void _LoadArenaTeamInfo(SQLResult result)
        {
            // arenateamid, played_week, played_season, personal_rating
            ushort[] personalRatingCache = { 0, 0, 0 };

            if (!result.IsEmpty())
            {
                do
                {
                    uint arenaTeamId = result.Read<uint>(0);

                    ArenaTeam arenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(arenaTeamId);
                    if (arenaTeam == null)
                    {
                        Log.outError(LogFilter.Player, "Player:_LoadArenaTeamInfo: couldn't load arenateam {0}", arenaTeamId);
                        continue;
                    }

                    byte arenaSlot = arenaTeam.GetSlot();

                    personalRatingCache[arenaSlot] = result.Read<ushort>(4);

                    SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.Id, arenaTeamId);
                    SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.Type, arenaTeam.GetArenaType());
                    SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.Member, (uint)(arenaTeam.GetCaptain() == GetGUID() ? 0 : 1));
                    SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.GamesWeek, result.Read<ushort>(1));
                    SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.GamesSeason, result.Read<ushort>(2));
                    SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.WinsSeason, result.Read<ushort>(3));
                }
                while (result.NextRow());
            }

            for (byte slot = 0; slot <= 2; ++slot)
            {
                SetArenaTeamInfoField(slot, ArenaTeamInfoType.PersonalRating, personalRatingCache[slot]);
            }
        }
        void _LoadGroup(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                Group group = Global.GroupMgr.GetGroupByDbStoreId(result.Read<uint>(0));
                if (group)
                {
                    if (group.IsLeader(GetGUID()))
                        SetFlag(PlayerFields.Flags, PlayerFlags.GroupLeader);

                    byte subgroup = group.GetMemberGroup(GetGUID());
                    SetGroup(group, subgroup);
                    SetPartyType(group.GetGroupCategory(), GroupType.Normal);
                    ResetGroupUpdateSequenceIfNeeded(group);

                    // the group leader may change the instance difficulty while the player is offline
                    SetDungeonDifficultyID(group.GetDungeonDifficultyID());
                    SetRaidDifficultyID(group.GetRaidDifficultyID());
                    SetLegacyRaidDifficultyID(group.GetLegacyRaidDifficultyID());
                }
            }

            if (!GetGroup() || !GetGroup().IsLeader(GetGUID()))
                RemoveFlag(PlayerFields.Flags, PlayerFlags.GroupLeader);
        }
        void _LoadInstanceTimeRestrictions(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                _instanceResetTimes.Add(result.Read<uint>(0), result.Read<long>(1));
            } while (result.NextRow());
        }
        void _LoadEquipmentSets(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                EquipmentSetInfo eqSet = new EquipmentSetInfo();
                eqSet.Data.Guid = result.Read<ulong>(0);
                eqSet.Data.Type = EquipmentSetInfo.EquipmentSetType.Equipment;
                eqSet.Data.SetID = result.Read<byte>(1);
                eqSet.Data.SetName = result.Read<string>(2);
                eqSet.Data.SetIcon = result.Read<string>(3);
                eqSet.Data.IgnoreMask = result.Read<uint>(4);
                eqSet.Data.AssignedSpecIndex = result.Read<int>(5);
                eqSet.state = EquipmentSetUpdateState.Unchanged;

                for (int i = 0; i < EquipmentSlot.End; ++i)
                {
                    ulong guid = result.Read<uint>(6 + i);
                    if (guid != 0)
                        eqSet.Data.Pieces[i] = ObjectGuid.Create(HighGuid.Item, guid);
                }

                eqSet.Data.Appearances.Fill(0);
                eqSet.Data.Enchants.Fill(0);

                if (eqSet.Data.SetID >= ItemConst.MaxEquipmentSetIndex)   // client limit
                    continue;

                _equipmentSets[eqSet.Data.Guid] = eqSet;
            }
            while (result.NextRow());
        }
        void _LoadTransmogOutfits(SQLResult result)
        {
            //             0         1     2         3            4            5            6            7            8            9
            //SELECT setguid, setindex, name, iconname, ignore_mask, appearance0, appearance1, appearance2, appearance3, appearance4,
            //             10           11           12           13           14            15            16            17            18            19            20            21
            //    appearance5, appearance6, appearance7, appearance8, appearance9, appearance10, appearance11, appearance12, appearance13, appearance14, appearance15, appearance16,
            //              22            23               24              25
            //    appearance17, appearance18, mainHandEnchant, offHandEnchant FROM character_transmog_outfits WHERE guid = ? ORDER BY setindex
            if (result.IsEmpty())
                return;

            do
            {
                EquipmentSetInfo eqSet = new EquipmentSetInfo();

                eqSet.Data.Guid = result.Read<ulong>(0);
                eqSet.Data.Type = EquipmentSetInfo.EquipmentSetType.Transmog;
                eqSet.Data.SetID = result.Read <byte>(1);
                eqSet.Data.SetName = result.Read <string>(2);
                eqSet.Data.SetIcon = result.Read <string>(3);
                eqSet.Data.IgnoreMask = result.Read <uint>(4);
                eqSet.state = EquipmentSetUpdateState.Unchanged;
                eqSet.Data.Pieces.Fill(ObjectGuid.Empty);

                for (int i = 0; i < EquipmentSlot.End; ++i)
                    eqSet.Data.Appearances[i] = result.Read<int>(5 + i);

                for (int i = 0; i < eqSet.Data.Enchants.Count; ++i)
                    eqSet.Data.Enchants[i] = result.Read<int>(24 + i);

                if (eqSet.Data.SetID >= ItemConst.MaxEquipmentSetIndex)   // client limit
                    continue;

                _equipmentSets[eqSet.Data.Guid] = eqSet;
            } while (result.NextRow());
        }
        void _LoadCUFProfiles(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                byte id = result.Read<byte>(0);
                string name = result.Read<string>(1);
                ushort frameHeight = result.Read<ushort>(2);
                ushort frameWidth = result.Read<ushort>(3);
                byte sortBy = result.Read<byte>(4);
                byte healthText = result.Read<byte>(5);
                uint boolOptions = result.Read<uint>(6);
                byte topPoint = result.Read<byte>(7);
                byte bottomPoint = result.Read<byte>(8);
                byte leftPoint = result.Read<byte>(9);
                ushort topOffset = result.Read<ushort>(10);
                ushort bottomOffset = result.Read<ushort>(11);
                ushort leftOffset = result.Read<ushort>(12);

                if (id > PlayerConst.MaxCUFProfiles)
                {
                    Log.outError(LogFilter.Player, "Player._LoadCUFProfiles - Player (GUID: {0}, name: {1}) has an CUF profile with invalid id (id: {2}), max is {3}.", GetGUID().ToString(), GetName(), id, PlayerConst.MaxCUFProfiles);
                    continue;
                }

                _CUFProfiles[id] = new CUFProfile(name, frameHeight, frameWidth, sortBy, healthText, boolOptions, topPoint, bottomPoint, leftPoint, topOffset, bottomOffset, leftOffset);
            }
            while (result.NextRow());
        }
        void _LoadRandomBGStatus(SQLResult result)
        {
            if (!result.IsEmpty())
                m_IsBGRandomWinner = true;
        }
        void _LoadBGData(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            // Expecting only one row
            //        0           1     2      3      4      5      6          7          8        9
            // SELECT instanceId, team, joinX, joinY, joinZ, joinO, joinMapId, taxiStart, taxiEnd, mountSpell FROM character_Battleground_data WHERE guid = ?
            m_bgData.bgInstanceID = result.Read<uint>(0);
            m_bgData.bgTeam = result.Read<ushort>(1);
            m_bgData.joinPos = new WorldLocation(result.Read<ushort>(6), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), result.Read<float>(5));
            m_bgData.taxiPath[0] = result.Read<uint>(7);
            m_bgData.taxiPath[1] = result.Read<uint>(8);
            m_bgData.mountSpell = result.Read<uint>(9);
        }

        void _SaveInventory(SQLTransaction trans)
        {
            PreparedStatement stmt;
            // force items in buyback slots to new state
            // and remove those that aren't already
            for (var i = InventorySlots.BuyBackStart; i < InventorySlots.BuyBackEnd; ++i)
            {
                Item item = m_items[i];
                if (item == null || item.GetState() == ItemUpdateState.New)
                    continue;

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_ITEM);
                stmt.AddValue(0, item.GetGUID().GetCounter());
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
                stmt.AddValue(0, item.GetGUID().GetCounter());
                trans.Append(stmt);
                m_items[i].FSetState(ItemUpdateState.New);
            }

            // Updated played time for refundable items. We don't do this in Player.Update because there's simply no need for it,
            // the client auto counts down in real time after having received the initial played time on the first
            // SMSG_ITEM_REFUND_INFO_RESPONSE packet.
            // Item.UpdatePlayedTime is only called when needed, which is in DB saves, and item refund info requests.
            foreach (var guid in m_refundableItems)
            {
                Item item = GetItemByGuid(guid);
                if (item != null)
                {
                    item.UpdatePlayedTime(this);
                    continue;
                }
                else
                {
                    Log.outError(LogFilter.Player, "Can't find item guid {0} but is in refundable storage for player {1} ! Removing.", guid, GetGUID().ToString());
                    m_refundableItems.Remove(guid);
                }
            }

            // update enchantment durations
            foreach (var enchant in m_enchantDuration)
                enchant.item.SetEnchantmentDuration(enchant.slot, enchant.leftduration, this);

            // if no changes
            if (ItemUpdateQueue.Count == 0)
                return;

            for (var i = 0; i < ItemUpdateQueue.Count; ++i)
            {
                Item item = ItemUpdateQueue[i];
                if (item == null)
                    continue;

                Bag container = item.GetContainer();
                if (item.GetState() != ItemUpdateState.Removed)
                {
                    Item test = GetItemByPos(item.GetBagSlot(), item.GetSlot());
                    if (test == null)
                    {
                        ulong bagTestGUID = 0;
                        Item test2 = GetItemByPos(InventorySlots.Bag0, item.GetBagSlot());
                        if (test2 != null)
                            bagTestGUID = test2.GetGUID().GetCounter();
                        Log.outError(LogFilter.Player, "Player(GUID: {0} Name: {1}).SaveInventory - the bag({2}) and slot({3}) values for the item with guid {4} (state {5}) are incorrect, " +
                            "the player doesn't have an item at that position!", GetGUID().ToString(), GetName(), item.GetBagSlot(), item.GetSlot(), item.GetGUID().ToString(), item.GetState());
                        // according to the test that was just performed nothing should be in this slot, delete
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_BAG_SLOT);
                        stmt.AddValue(0, bagTestGUID);
                        stmt.AddValue(1, item.GetSlot());
                        stmt.AddValue(2, GetGUID().GetCounter());
                        trans.Append(stmt);

                        // also THIS item should be somewhere else, cheat attempt
                        item.FSetState(ItemUpdateState.Removed); // we are IN updateQueue right now, can't use SetState which modifies the queue
                        DeleteRefundReference(item.GetGUID());
                    }
                    else if (test != item)
                    {
                        Log.outError(LogFilter.Player, "Player(GUID: {0} Name: {1}).SaveInventory - the bag({2}) and slot({3}) values for the item with guid {4} are incorrect, " +
                            "the item with guid {5} is there instead!", GetGUID().ToString(), GetName(), item.GetBagSlot(), item.GetSlot(), item.GetGUID().ToString(), test.GetGUID().ToString());
                        // save all changes to the item...
                        if (item.GetState() != ItemUpdateState.New) // only for existing items, no dupes
                            item.SaveToDB(trans);
                        // ...but do not save position in inventory
                        continue;
                    }
                }

                switch (item.GetState())
                {
                    case ItemUpdateState.New:
                    case ItemUpdateState.Changed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_INVENTORY_ITEM);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, container ? container.GetGUID().GetCounter() : 0);
                        stmt.AddValue(2, item.GetSlot());
                        stmt.AddValue(3, item.GetGUID().GetCounter());
                        trans.Append(stmt);
                        break;
                    case ItemUpdateState.Removed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_ITEM);
                        stmt.AddValue(0, item.GetGUID().GetCounter());
                        trans.Append(stmt);
                        break;
                    case ItemUpdateState.Unchanged:
                        break;
                }

                item.SaveToDB(trans);                                   // item have unchanged inventory record and can be save standalone
            }
            ItemUpdateQueue.Clear();
        }
        void _SaveSkills(SQLTransaction trans)
        {
            PreparedStatement stmt;// = null;

            foreach (var skill in mSkillStatus.ToList())
            {
                if (skill.Value.State == SkillState.Unchanged)
                    continue;

                if (skill.Value.State == SkillState.Deleted)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SKILL_BY_SKILL);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, skill.Key);
                    trans.Append(stmt);

                    mSkillStatus.Remove(skill.Key);
                    continue;
                }

                var field = (ushort)(skill.Value.Pos / 2);
                var offset = (byte)(skill.Value.Pos & 1);

                var value = GetUInt16Value(ActivePlayerFields.SkillLineRank + field, offset);
                var max = GetUInt16Value(ActivePlayerFields.SkillLineMaxRank + field, offset);

                switch (skill.Value.State)
                {
                    case SkillState.New:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SKILLS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, (ushort)skill.Key);
                        stmt.AddValue(2, value);
                        stmt.AddValue(3, max);
                        trans.Append(stmt);
                        break;
                    case SkillState.Changed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_SKILLS);
                        stmt.AddValue(0, value);
                        stmt.AddValue(1, max);
                        stmt.AddValue(2, GetGUID().GetCounter());
                        stmt.AddValue(3, (ushort)skill.Key);
                        trans.Append(stmt);
                        break;
                    default:
                        break;
                }
                skill.Value.State = SkillState.Unchanged;
            }
        }
        void _SaveSpells(SQLTransaction trans)
        {
            PreparedStatement stmt = null;

            foreach (var spell in m_spells.ToList())
            {
                if (spell.Value.State == PlayerSpellState.Removed || spell.Value.State == PlayerSpellState.Changed)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_BY_SPELL);
                    stmt.AddValue(0, spell.Key);
                    stmt.AddValue(1, GetGUID().GetCounter());
                    trans.Append(stmt);
                }

                // add only changed/new not dependent spells
                if (!spell.Value.Dependent && (spell.Value.State == PlayerSpellState.New || spell.Value.State == PlayerSpellState.Changed))
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SPELL);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, spell.Key);
                    stmt.AddValue(2, spell.Value.Active);
                    stmt.AddValue(3, spell.Value.Disabled);
                    trans.Append(stmt);
                }

                if (spell.Value.State == PlayerSpellState.Removed)
                    m_spells.Remove(spell.Key);
                else
                {
                    spell.Value.State = PlayerSpellState.Unchanged;
                    continue;
                }
            }
        }
        void _SaveAuras(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_EFFECT);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            byte index;
            foreach (var pair in GetOwnedAuras())
            {
                Aura aura = pair.Value;
                if (!aura.CanBeSaved())
                    continue;

                uint recalculateMask;
                AuraKey key = aura.GenerateKey(out recalculateMask);

                index = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AURA);
                stmt.AddValue(index++, GetGUID().GetCounter());
                stmt.AddValue(index++, key.Caster.GetRawValue());
                stmt.AddValue(index++, key.Item.GetRawValue());
                stmt.AddValue(index++, key.SpellId);
                stmt.AddValue(index++, key.EffectMask);
                stmt.AddValue(index++, recalculateMask);
                stmt.AddValue(index++, aura.GetStackAmount());
                stmt.AddValue(index++, aura.GetMaxDuration());
                stmt.AddValue(index++, aura.GetDuration());
                stmt.AddValue(index++, aura.GetCharges());
                stmt.AddValue(index, aura.GetCastItemLevel());
                trans.Append(stmt);

                foreach (AuraEffect effect in aura.GetAuraEffects())
                {
                    if (effect != null)
                    {
                        index = 0;
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AURA_EFFECT);
                        stmt.AddValue(index++, GetGUID().GetCounter());
                        stmt.AddValue(index++, key.Caster.GetRawValue());
                        stmt.AddValue(index++, key.Item.GetRawValue());
                        stmt.AddValue(index++, key.SpellId);
                        stmt.AddValue(index++, key.EffectMask);
                        stmt.AddValue(index++, effect.GetEffIndex());
                        stmt.AddValue(index++, effect.GetAmount());
                        stmt.AddValue(index++, effect.GetBaseAmount());
                        trans.Append(stmt);
                    }
                }
            }
        }
        void _SaveGlyphs(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_GLYPHS);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            for (byte spec = 0; spec < PlayerConst.MaxSpecializations; ++spec)
            {
                foreach (uint glyphId in GetGlyphs(spec))
                {
                    byte index = 0;

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_GLYPHS);
                    stmt.AddValue(index++, GetGUID().GetCounter());
                    stmt.AddValue(index++, spec);
                    stmt.AddValue(index++, glyphId);

                    trans.Append(stmt);
                }
            }
        }
        void _SaveCurrency(SQLTransaction trans)
        {
            PreparedStatement stmt = null;
            foreach (var pair in _currencyStorage)
            {
                CurrencyTypesRecord entry = CliDB.CurrencyTypesStorage.LookupByKey(pair.Key);
                if (entry == null) // should never happen
                    continue;

                switch (pair.Value.state)
                {
                    case PlayerCurrencyState.New:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_PLAYER_CURRENCY);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, pair.Value.Quantity);
                        stmt.AddValue(3, pair.Value.WeeklyQuantity);
                        stmt.AddValue(4, pair.Value.TrackedQuantity);
                        stmt.AddValue(5, pair.Value.Flags);
                        trans.Append(stmt);
                        break;
                    case PlayerCurrencyState.Changed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_PLAYER_CURRENCY);
                        stmt.AddValue(0, pair.Value.Quantity);
                        stmt.AddValue(1, pair.Value.WeeklyQuantity);
                        stmt.AddValue(2, pair.Value.TrackedQuantity);
                        stmt.AddValue(3, pair.Value.Flags);
                        stmt.AddValue(4, GetGUID().GetCounter());
                        stmt.AddValue(5, pair.Key);
                        trans.Append(stmt);
                        break;
                    default:
                        break;
                }

                pair.Value.state = PlayerCurrencyState.Unchanged;
            }
        }
        void _SaveActions(SQLTransaction trans)
        {
            PreparedStatement stmt = null;

            foreach (var pair in m_actionButtons.ToList())
            {
                switch (pair.Value.uState)
                {
                    case ActionButtonUpdateState.New:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_ACTION);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, GetActiveTalentGroup());
                        stmt.AddValue(2, pair.Key);
                        stmt.AddValue(3, pair.Value.GetAction());
                        stmt.AddValue(4, pair.Value.GetButtonType());
                        trans.Append(stmt);

                        pair.Value.uState = ActionButtonUpdateState.UnChanged;
                        break;
                    case ActionButtonUpdateState.Changed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_ACTION);
                        stmt.AddValue(0, pair.Value.GetAction());
                        stmt.AddValue(1, pair.Value.GetButtonType());
                        stmt.AddValue(2, GetGUID().GetCounter());
                        stmt.AddValue(3, pair.Key);
                        stmt.AddValue(4, GetActiveTalentGroup());
                        trans.Append(stmt);

                        pair.Value.uState = ActionButtonUpdateState.UnChanged;
                        break;
                    case ActionButtonUpdateState.Deleted:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION_BY_BUTTON_SPEC);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, GetActiveTalentGroup());
                        trans.Append(stmt);

                        m_actionButtons.Remove(pair.Key);
                        break;
                    default:
                        break;
                }
            }
        }
        void _SaveQuestStatus(SQLTransaction trans)
        {
            bool isTransaction = trans != null;
            if (!isTransaction)
                trans = new SQLTransaction();

            PreparedStatement stmt = null;
            bool keepAbandoned = !Global.WorldMgr.GetCleaningFlags().HasAnyFlag(CleaningFlags.Queststatus);

            foreach (var save in m_QuestStatusSave)
            {
                if (save.Value == QuestSaveType.Default)
                {
                    var data = m_QuestStatus.LookupByKey(save.Key);
                    if (data != null && (keepAbandoned || data.Status != QuestStatus.None))
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_QUESTSTATUS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, save.Key);
                        stmt.AddValue(2, data.Status);
                        stmt.AddValue(3, data.Timer / Time.InMilliseconds + Global.WorldMgr.GetGameTime());
                        trans.Append(stmt);

                        // Save objectives
                        for (int i = 0; i < data.ObjectiveData.Length; ++i)
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_QUESTSTATUS_OBJECTIVES);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            stmt.AddValue(1, save.Key);
                            stmt.AddValue(2, i);
                            stmt.AddValue(3, data.ObjectiveData[i]);
                            trans.Append(stmt);
                        }
                    }
                }
                else
                {
                    // Delete
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_BY_QUEST);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, save.Key);
                    trans.Append(stmt);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_BY_QUEST);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, save.Key);
                    trans.Append(stmt);
                }
            }

            m_QuestStatusSave.Clear();

            foreach (var save in m_RewardedQuestsSave)
            {
                if (save.Value == QuestSaveType.Default)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_QUESTSTATUS_REWARDED);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, save.Key);
                    trans.Append(stmt);

                }
                else if (save.Value == QuestSaveType.ForceDelete || !keepAbandoned)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED_BY_QUEST);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, save.Key);
                    trans.Append(stmt);
                }
            }

            m_RewardedQuestsSave.Clear();

            if (!isTransaction)
                DB.Characters.CommitTransaction(trans);
        }
        void _SaveDailyQuestStatus(SQLTransaction trans)
        {
            if (!m_DailyQuestChanged)
                return;

            m_DailyQuestChanged = false;

            // save last daily quest time for all quests: we need only mostly reset time for reset check anyway

            // we don't need transactions here.
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            var dailies = GetDynamicValues(ActivePlayerDynamicFields.DailyQuests);
            foreach (var questId in dailies)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_DAILY);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, questId);
                stmt.AddValue(2, m_lastDailyQuestTime);
                trans.Append(stmt);

            }

            if (!m_DFQuests.Empty())
            {
                foreach (var id in m_DFQuests)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_DAILY);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, id);
                    stmt.AddValue(2, m_lastDailyQuestTime);
                    trans.Append(stmt);
                }
            }
        }
        void _SaveWeeklyQuestStatus(SQLTransaction trans)
        {
            if (!m_WeeklyQuestChanged || m_weeklyquests.Empty())
                return;

            // we don't need transactions here.
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            foreach (var quest_id in m_weeklyquests)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_WEEKLY);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, quest_id);
                trans.Append(stmt);
            }

            m_WeeklyQuestChanged = false;
        }
        void _SaveSeasonalQuestStatus(SQLTransaction trans)
        {
            if (!m_SeasonalQuestChanged || m_seasonalquests.Empty())
                return;

            // we don't need transactions here.
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            foreach (var iter in m_seasonalquests)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_SEASONAL);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, iter.Value);
                stmt.AddValue(2, iter.Key);
                trans.Append(stmt);
            }

            m_SeasonalQuestChanged = false;
        }
        void _SaveMonthlyQuestStatus(SQLTransaction trans)
        {
            if (!m_MonthlyQuestChanged || m_monthlyquests.Empty())
                return;

            // we don't need transactions here.
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            foreach (var questId in m_monthlyquests)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_MONTHLY);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, questId);
                trans.Append(stmt);
            }

            m_MonthlyQuestChanged = false;
        }
        void _SaveTalents(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TALENT);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            for (byte group = 0; group < PlayerConst.MaxSpecializations; ++group)
            {
                var talents = GetTalentMap(group);
                foreach (var pair in talents.ToList())
                {
                    if (pair.Value == PlayerSpellState.Removed)
                    {
                        talents.Remove(pair.Key);
                        continue;
                    }

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_TALENT);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, pair.Key);
                    stmt.AddValue(2, group);
                    trans.Append(stmt);
                }
            }

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PVP_TALENT);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            for (byte group = 0; group < PlayerConst.MaxSpecializations; ++group)
            {
                var talents = GetPvpTalentMap(group);
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_PVP_TALENT);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, talents[0]);
                stmt.AddValue(2, talents[1]);
                stmt.AddValue(3, talents[2]);
                stmt.AddValue(4, talents[3]);
                stmt.AddValue(5, group);
                trans.Append(stmt);
            }
        }
        public void _SaveMail(SQLTransaction trans)
        {
            if (!m_mailsLoaded)
                return;

            PreparedStatement stmt;

            foreach (var m in m_mail)
            {
                if (m.state == MailState.Changed)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_MAIL);
                    stmt.AddValue(0, m.HasItems() ? 1 : 0);
                    stmt.AddValue(1, m.expire_time);
                    stmt.AddValue(2, m.deliver_time);
                    stmt.AddValue(3, m.money);
                    stmt.AddValue(4, m.COD);
                    stmt.AddValue(5, (byte)m.checkMask);
                    stmt.AddValue(6, m.messageID);

                    trans.Append(stmt);

                    if (!m.removedItems.Empty())
                    {
                        foreach (var id in m.removedItems)
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM);
                            stmt.AddValue(0, id);
                            trans.Append(stmt);
                        }
                        m.removedItems.Clear();
                    }
                    m.state = MailState.Unchanged;
                }
                else if (m.state == MailState.Deleted)
                {
                    if (m.HasItems())
                    {
                        foreach (var id in m.items)
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
                            stmt.AddValue(0, id.item_guid);
                            trans.Append(stmt);
                        }
                    }
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
                    stmt.AddValue(0, m.messageID);
                    trans.Append(stmt);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                    stmt.AddValue(0, m.messageID);
                    trans.Append(stmt);
                }
            }

            //deallocate deleted mails...
            foreach (var m in GetMails().ToList())
            {
                if (m.state == MailState.Deleted)
                    m_mail.Remove(m);
            }

            m_mailsUpdated = false;
        }
        void _SaveStats(SQLTransaction trans)
        {
            // check if stat saving is enabled and if char level is high enough
            if (WorldConfig.GetIntValue(WorldCfg.MinLevelStatSave) == 0 || getLevel() < WorldConfig.GetIntValue(WorldCfg.MinLevelStatSave))
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_STATS);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            byte index = 0;
            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_STATS);
            stmt.AddValue(index++, GetGUID().GetCounter());
            stmt.AddValue(index++, GetMaxHealth());

            for (byte i = 0; i < (int)PowerType.MaxPerClass; ++i)
                stmt.AddValue(index++, GetMaxPower((PowerType)i));

            for (byte i = 0; i < (int)Stats.Max; ++i)
                stmt.AddValue(index++, GetStat((Stats)i));

            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                stmt.AddValue(index++, GetResistance((SpellSchools)i) + GetBonusResistanceMod((SpellSchools)i));

            stmt.AddValue(index++, GetFloatValue(ActivePlayerFields.BlockPercentage));
            stmt.AddValue(index++, GetFloatValue(ActivePlayerFields.DodgePercentage));
            stmt.AddValue(index++, GetFloatValue(ActivePlayerFields.ParryPercentage));
            stmt.AddValue(index++, GetFloatValue(ActivePlayerFields.CritPercentage));
            stmt.AddValue(index++, GetFloatValue(ActivePlayerFields.RangedCritPercentage));
            stmt.AddValue(index++, GetFloatValue(ActivePlayerFields.SpellCritPercentage1));
            stmt.AddValue(index++, GetUInt32Value(UnitFields.AttackPower));
            stmt.AddValue(index++, GetUInt32Value(UnitFields.RangedAttackPower));
            stmt.AddValue(index++, GetBaseSpellPowerBonus());
            stmt.AddValue(index, GetUInt32Value(ActivePlayerFields.CombatRating + (int)CombatRating.ResiliencePlayerDamage));

            trans.Append(stmt);
        }
        public void SaveGoldToDB(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_MONEY);
            stmt.AddValue(0, GetMoney());
            stmt.AddValue(1, GetGUID().GetCounter());
            trans.Append(stmt);
        }
        public void SaveInventoryAndGoldToDB(SQLTransaction trans)
        {
            _SaveInventory(trans);
            _SaveCurrency(trans);
            SaveGoldToDB(trans);
        }
        void _SaveEquipmentSets(SQLTransaction trans)
        {
            foreach (var pair in _equipmentSets)
            {
                EquipmentSetInfo eqSet = pair.Value;
                PreparedStatement stmt = null;
                byte j = 0;
                switch (eqSet.state)
                {
                    case EquipmentSetUpdateState.Unchanged:
                        break;                                      // do nothing
                    case EquipmentSetUpdateState.Changed:
                        if (eqSet.Data.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_EQUIP_SET);
                            stmt.AddValue(j++, eqSet.Data.SetName);
                            stmt.AddValue(j++, eqSet.Data.SetIcon);
                            stmt.AddValue(j++, eqSet.Data.IgnoreMask);
                            stmt.AddValue(j++, eqSet.Data.AssignedSpecIndex);

                            for (byte i = 0; i < EquipmentSlot.End; ++i)
                                stmt.AddValue(j++, eqSet.Data.Pieces[i].GetCounter());

                            stmt.AddValue(j++, GetGUID().GetCounter());
                            stmt.AddValue(j++, eqSet.Data.Guid);
                            stmt.AddValue(j, eqSet.Data.SetID);
                        }
                        else
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_TRANSMOG_OUTFIT);
                            stmt.AddValue(j++, eqSet.Data.SetName);
                            stmt.AddValue(j++, eqSet.Data.SetIcon);
                            stmt.AddValue(j++, eqSet.Data.IgnoreMask);

                            for (byte i = 0; i < EquipmentSlot.End; ++i)
                                stmt.AddValue(j++, eqSet.Data.Appearances[i]);

                            for (int i = 0; i < eqSet.Data.Enchants.Count; ++i)
                                stmt.AddValue(j++, eqSet.Data.Enchants[i]);

                            stmt.AddValue(j++, GetGUID().GetCounter());
                            stmt.AddValue(j++, eqSet.Data.Guid);
                            stmt.AddValue(j, eqSet.Data.SetID);
                        }

                        trans.Append(stmt);
                        eqSet.state = EquipmentSetUpdateState.Unchanged;
                        break;
                    case EquipmentSetUpdateState.New:
                        if (eqSet.Data.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_EQUIP_SET);
                            stmt.AddValue(j++, GetGUID().GetCounter());
                            stmt.AddValue(j++, eqSet.Data.Guid);
                            stmt.AddValue(j++, eqSet.Data.SetID);
                            stmt.AddValue(j++, eqSet.Data.SetName);
                            stmt.AddValue(j++, eqSet.Data.SetIcon);
                            stmt.AddValue(j++, eqSet.Data.IgnoreMask);
                            stmt.AddValue(j++, eqSet.Data.AssignedSpecIndex);

                            for (byte i = 0; i < EquipmentSlot.End; ++i)
                                stmt.AddValue(j++, eqSet.Data.Pieces[i].GetCounter());
                        }
                        else
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_TRANSMOG_OUTFIT);
                            stmt.AddValue(j++, GetGUID().GetCounter());
                            stmt.AddValue(j++, eqSet.Data.Guid);
                            stmt.AddValue(j++, eqSet.Data.SetID);
                            stmt.AddValue(j++, eqSet.Data.SetName);
                            stmt.AddValue(j++, eqSet.Data.SetIcon);
                            stmt.AddValue(j++, eqSet.Data.IgnoreMask);

                            for (byte i = 0; i < EquipmentSlot.End; ++i)
                                stmt.AddValue(j++, eqSet.Data.Appearances[i]);

                            for (int i = 0; i < eqSet.Data.Enchants.Count; ++i)
                                stmt.AddValue(j++, eqSet.Data.Enchants[i]);
                        }
                        trans.Append(stmt);
                        eqSet.state = EquipmentSetUpdateState.Unchanged;
                        break;
                    case EquipmentSetUpdateState.Deleted:
                        if (eqSet.Data.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_EQUIP_SET);
                        else
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_TRANSMOG_OUTFIT);
                        stmt.AddValue(0, eqSet.Data.Guid);
                        trans.Append(stmt);
                        _equipmentSets.Remove(pair.Key);
                        break;
                }
            }
        }
        void _SaveVoidStorage(SQLTransaction trans)
        {
            PreparedStatement stmt = null;
            for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
            {
                if (_voidStorageItems[i] == null) // unused item
                {
                    // DELETE FROM void_storage WHERE slot = ? AND playerGuid = ?
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_SLOT);
                    stmt.AddValue(0, i);
                    stmt.AddValue(1, GetGUID().GetCounter());
                }

                else
                {
                    // REPLACE INTO character_void_storage (itemId, playerGuid, itemEntry, slot, creatorGuid, randomProperty, suffixFactor, upgradeId, fixedScalingLevel, artifactKnowledgeLevel, bonusListIDs) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_VOID_STORAGE_ITEM);
                    stmt.AddValue(0, _voidStorageItems[i].ItemId);
                    stmt.AddValue(1, GetGUID().GetCounter());
                    stmt.AddValue(2, _voidStorageItems[i].ItemEntry);
                    stmt.AddValue(3, i);
                    stmt.AddValue(4, _voidStorageItems[i].CreatorGuid.GetCounter());
                    stmt.AddValue(5, (byte)_voidStorageItems[i].ItemRandomPropertyId.Type);
                    stmt.AddValue(6, _voidStorageItems[i].ItemRandomPropertyId.Id);
                    stmt.AddValue(7, _voidStorageItems[i].ItemSuffixFactor);
                    stmt.AddValue(8, _voidStorageItems[i].ItemUpgradeId);
                    stmt.AddValue(9, _voidStorageItems[i].FixedScalingLevel);
                    stmt.AddValue(10, _voidStorageItems[i].ArtifactKnowledgeLevel);
                    stmt.AddValue(11, _voidStorageItems[i].Context);

                    StringBuilder bonusListIDs = new StringBuilder();
                    foreach (uint bonusListID in _voidStorageItems[i].BonusListIDs)
                        bonusListIDs.AppendFormat("{0} ", bonusListID);
                    stmt.AddValue(12, bonusListIDs.ToString());
                }

                trans.Append(stmt);
            }
        }
        void _SaveCUFProfiles(SQLTransaction trans)
        {
            PreparedStatement stmt = null;
            ulong lowGuid = GetGUID().GetCounter();

            for (byte i = 0; i < PlayerConst.MaxCUFProfiles; ++i)
            {
                if (_CUFProfiles[i] == null) // unused profile
                {
                    // DELETE FROM character_cuf_profiles WHERE guid = ? and id = ?
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_CUF_PROFILES_BY_ID);
                    stmt.AddValue(0, lowGuid);
                    stmt.AddValue(1, i);
                }
                else
                {
                    // REPLACE INTO character_cuf_profiles (guid, id, name, frameHeight, frameWidth, sortBy, healthText, boolOptions, unk146, unk147, unk148, unk150, unk152, unk154) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_CUF_PROFILES);
                    stmt.AddValue(0, lowGuid);
                    stmt.AddValue(1, i);
                    stmt.AddValue(2, _CUFProfiles[i].ProfileName);
                    stmt.AddValue(3, _CUFProfiles[i].FrameHeight);
                    stmt.AddValue(4, _CUFProfiles[i].FrameWidth);
                    stmt.AddValue(5, _CUFProfiles[i].SortBy);
                    stmt.AddValue(6, _CUFProfiles[i].HealthText);
                    stmt.AddValue(7, (uint)_CUFProfiles[i].GetUlongOptionValue()); // 25 of 32 fields used, fits in an int
                    stmt.AddValue(8, _CUFProfiles[i].TopPoint);
                    stmt.AddValue(9, _CUFProfiles[i].BottomPoint);
                    stmt.AddValue(10, _CUFProfiles[i].LeftPoint);
                    stmt.AddValue(11, _CUFProfiles[i].TopOffset);
                    stmt.AddValue(12, _CUFProfiles[i].BottomOffset);
                    stmt.AddValue(13, _CUFProfiles[i].LeftOffset);
                }

                trans.Append(stmt);
            }
        }
        void _SaveInstanceTimeRestrictions(SQLTransaction trans)
        {
            if (_instanceResetTimes.Empty())
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ACCOUNT_INSTANCE_LOCK_TIMES);
            stmt.AddValue(0, GetSession().GetAccountId());
            trans.Append(stmt);

            foreach (var pair in _instanceResetTimes)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ACCOUNT_INSTANCE_LOCK_TIMES);
                stmt.AddValue(0, GetSession().GetAccountId());
                stmt.AddValue(1, pair.Key);
                stmt.AddValue(2, pair.Value);
                trans.Append(stmt);
            }
        }
        void _SaveBGData(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_BGDATA);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PLAYER_BGDATA);
            stmt.AddValue(0, GetGUID().GetCounter());
            stmt.AddValue(1, m_bgData.bgInstanceID);
            stmt.AddValue(2, m_bgData.bgTeam);
            stmt.AddValue(3, m_bgData.joinPos.GetPositionX());
            stmt.AddValue(4, m_bgData.joinPos.GetPositionY());
            stmt.AddValue(5, m_bgData.joinPos.GetPositionZ());
            stmt.AddValue(6, m_bgData.joinPos.GetOrientation());
            stmt.AddValue(7, (ushort)m_bgData.joinPos.GetMapId());
            stmt.AddValue(8, m_bgData.taxiPath[0]);
            stmt.AddValue(9, m_bgData.taxiPath[1]);
            stmt.AddValue(10, m_bgData.mountSpell);
            trans.Append(stmt);
        }

        public bool LoadFromDB(ObjectGuid guid, SQLQueryHolder<PlayerLoginQueryLoad> holder)
        {
            SQLResult result = holder.GetResult(PlayerLoginQueryLoad.From);
            if (result.IsEmpty())
            {
                string name;
                ObjectManager.GetPlayerNameByGUID(guid, out name);
                Log.outError(LogFilter.Player, "Player {0} {1} not found in table `characters`, can't load. ", name, guid.ToString());
                return false;
            }

            uint dbAccountId = result.Read<uint>(1);

            // check if the character's account in the db and the logged in account match.
            // player should be able to load/delete character only with correct account!
            if (dbAccountId != GetSession().GetAccountId())
            {
                Log.outError(LogFilter.Player, "Player (GUID: {0}) loading from wrong account (is: {1}, should be: {2})", GetGUID().ToString(), GetSession().GetAccountId(), dbAccountId);
                return false;
            }

            SQLResult banResult = holder.GetResult(PlayerLoginQueryLoad.Banned);
            if (!banResult.IsEmpty())
            {
                Log.outError(LogFilter.Player, "{0} is banned, can't load.", guid.ToString());
                return false;
            }

            _Create(guid);

            SetName(result.Read<string>(2));

            // check name limitations
            if (ObjectManager.CheckPlayerName(GetName(), GetSession().GetSessionDbcLocale()) != ResponseCodes.CharNameSuccess ||
                (!GetSession().HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(GetName())))
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)AtLoginFlags.Rename);
                stmt.AddValue(1, guid.GetCounter());
                DB.Characters.Execute(stmt);
                return false;
            }

            // overwrite possible wrong/corrupted guid
            SetGuidValue(ObjectFields.Guid, guid);
            SetGuidValue(PlayerFields.WowAccount, GetSession().GetAccountGUID());

            uint gender = result.Read<byte>(5);
            if (gender >= (uint)Gender.None)
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong gender ({1}), can't be loaded.", guid.ToString(), gender);
                return false;
            }

            SetByteValue(UnitFields.Bytes0, 0, result.Read<byte>(3));
            SetByteValue(UnitFields.Bytes0, 1, result.Read<byte>(4));
            SetByteValue(UnitFields.Bytes0, 3, (byte)gender);

            // check if race/class combination is valid
            PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());
            if (info == null)
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong race/class ({1}/{2}), can't be loaded.", guid.ToString(), GetRace(), GetClass());
                return false;
            }

            SetUInt32Value(UnitFields.Level, result.Read<uint>(6));
            SetXP(result.Read<uint>(7));

            _LoadIntoDataField(result.Read<string>(66), (int)ActivePlayerFields.ExploredZones, PlayerConst.ExploredZonesSize);
            _LoadIntoDataField(result.Read<string>(67), (int)ActivePlayerFields.KnownTitles, PlayerConst.KnowTitlesSize * 2);

            SetObjectScale(1.0f);
            SetFloatValue(UnitFields.HoverHeight, 1.0f);

            // load achievements before anything else to prevent multiple gains for the same achievement/criteria on every loading (as loading does call UpdateAchievementCriteria)
            m_achievementSys.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Achievements), holder.GetResult(PlayerLoginQueryLoad.CriteriaProgress));
            m_questObjectiveCriteriaMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectivesCriteria), holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectivesCriteriaProgress));

            ulong money = result.Read<ulong>(8);
            if (money > PlayerConst.MaxMoneyAmount)
                money = PlayerConst.MaxMoneyAmount;
            SetMoney(money);

            Array<byte> customDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);
            customDisplay[0] = result.Read<byte>(14);
            customDisplay[1] = result.Read<byte>(15);
            customDisplay[2] = result.Read<byte>(16);

            SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId, result.Read<byte>(9));
            SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetFaceId, result.Read<byte>(10));
            SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairStyleId, result.Read<byte>(11));
            SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairColorId, result.Read<byte>(12));
            SetByteValue(PlayerFields.Bytes2, PlayerFieldOffsets.Bytes2OffsetFacialStyle, result.Read<byte>(13));
            for (byte i = 0; i < PlayerConst.CustomDisplaySize; ++i)
                SetByteValue(PlayerFields.Bytes2, (byte)(PlayerFieldOffsets.Bytes2OffsetCustomDisplayOption + i), customDisplay[i]);
            SetInventorySlotCount(result.Read<byte>(17));
            SetBankBagSlotCount(result.Read<byte>(18));
            SetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender, (byte)gender);
            SetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetInebriation, result.Read<byte>(55));
            SetUInt32Value(PlayerFields.Flags, result.Read<uint>(20));
            SetUInt32Value(PlayerFields.FlagsEx, result.Read<uint>(21));
            SetInt32Value(ActivePlayerFields.WatchedFactionIndex, (int)result.Read<uint>(54));

            if (!ValidateAppearance((Race)result.Read<byte>(3), (Class)result.Read<byte>(4), (Gender)gender, 
                GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairStyleId), 
                GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairColorId), 
                GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetFaceId),
                GetByteValue(PlayerFields.Bytes2, PlayerFieldOffsets.Bytes2OffsetFacialStyle), 
                GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId), customDisplay))
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong Appearance values (Hair/Skin/Color), can't be loaded.", guid.ToString());
                return false;
            }

            // set which actionbars the client has active - DO NOT REMOVE EVER AGAIN (can be changed though, if it does change fieldwise)
            SetByteValue(ActivePlayerFields.Bytes, PlayerFieldOffsets.FieldBytesOffsetActionBarToggles, result.Read<byte>(68));

            m_fishingSteps = result.Read<byte>(72);

            InitDisplayIds();

            // cleanup inventory related item value fields (its will be filled correctly in _LoadInventory)
            for (byte slot = EquipmentSlot.Start; slot < EquipmentSlot.End; ++slot)
            {
                SetGuidValue(ActivePlayerFields.InvSlotHead + (slot * 4), ObjectGuid.Empty);
                SetVisibleItemSlot(slot, null);

                m_items[slot] = null;
            }

            //Need to call it to initialize m_team (m_team can be calculated from race)
            //Other way is to saves m_team into characters table.
            SetFactionForRace(GetRace());

            // load home bind and check in same time class/race pair, it used later for restore broken positions
            if (!_LoadHomeBind(holder.GetResult(PlayerLoginQueryLoad.HomeBind)))
                return false;

            InitPrimaryProfessions();                               // to max set before any spell loaded

            // init saved position, and fix it later if problematic
            ulong transLowGUID = result.Read<ulong>(41);

            Relocate(result.Read<float>(22), result.Read<float>(23), result.Read<float>(24), result.Read<float>(26));

            uint mapId = result.Read<uint>(25);
            uint instanceId = result.Read<uint>(63);

            var RelocateToHomebind = new Action(() => { mapId = homebind.GetMapId(); instanceId = 0; Relocate(homebind); });

            SetDungeonDifficultyID(CheckLoadedDungeonDifficultyID((Difficulty)result.Read<byte>(49)));
            SetRaidDifficultyID(CheckLoadedRaidDifficultyID((Difficulty)result.Read<byte>(70)));
            SetLegacyRaidDifficultyID(CheckLoadedLegacyRaidDifficultyID((Difficulty)result.Read<byte>(71)));

            string taxi_nodes = result.Read<string>(48);

            _LoadGroup(holder.GetResult(PlayerLoginQueryLoad.Group));

            _LoadArenaTeamInfo(holder.GetResult(PlayerLoginQueryLoad.ArenaInfo));

            // check arena teams integrity
            for (byte arena_slot = 0; arena_slot < SharedConst.MaxArenaSlot; ++arena_slot)
            {
                uint arena_team_id = GetArenaTeamId(arena_slot);
                if (arena_team_id == 0)
                    continue;

                ArenaTeam at = Global.ArenaTeamMgr.GetArenaTeamById(arena_team_id);
                if (at != null)
                    if (at.IsMember(GetGUID()))
                        continue;

                // arena team not exist or not member, cleanup fields
                for (int j = 0; j < 6; ++j)
                    SetArenaTeamInfoField(arena_slot, (ArenaTeamInfoType)j, 0);
            }

            _LoadCurrency(holder.GetResult(PlayerLoginQueryLoad.Currency));
            SetUInt32Value(ActivePlayerFields.LifetimeHonorableKills, result.Read<uint>(50));
            SetUInt16Value(ActivePlayerFields.Kills, PlayerFieldOffsets.FieldKillsOffsetTodayKills, result.Read<ushort>(51));
            SetUInt16Value(ActivePlayerFields.Kills, PlayerFieldOffsets.FieldKillsOffsetYesterdayKills, result.Read<ushort>(52));

            _LoadBoundInstances(holder.GetResult(PlayerLoginQueryLoad.BoundInstances));
            _LoadInstanceTimeRestrictions(holder.GetResult(PlayerLoginQueryLoad.InstanceLockTimes));
            _LoadBGData(holder.GetResult(PlayerLoginQueryLoad.BgData));

            GetSession().SetPlayer(this);

            Map map = null;
            bool player_at_bg = false;
            var mapEntry = CliDB.MapStorage.LookupByKey(mapId);
            if (mapEntry == null || !IsPositionValid())
            {
                Log.outError(LogFilter.Player, "Player (guidlow {0}) have invalid coordinates (MapId: {1} {2}). Teleport to default race/class locations.", guid.ToString(), mapId, GetPosition());
                RelocateToHomebind();
            }
            else if (mapEntry != null && mapEntry.IsBattlegroundOrArena())
            {
                Battleground currentBg = null;
                if (m_bgData.bgInstanceID != 0)                                                //saved in Battleground
                    currentBg = Global.BattlegroundMgr.GetBattleground(m_bgData.bgInstanceID, BattlegroundTypeId.None);

                player_at_bg = currentBg != null && currentBg.IsPlayerInBattleground(GetGUID());

                if (player_at_bg && currentBg.GetStatus() != BattlegroundStatus.WaitLeave)
                {
                    map = currentBg.GetBgMap();

                    BattlegroundQueueTypeId bgQueueTypeId = Global.BattlegroundMgr.BGQueueTypeId(currentBg.GetTypeID(), currentBg.GetArenaType());
                    AddBattlegroundQueueId(bgQueueTypeId);

                    m_bgData.bgTypeID = currentBg.GetTypeID();

                    //join player to Battlegroundgroup
                    currentBg.EventPlayerLoggedIn(this);
                    currentBg.AddOrSetPlayerToCorrectBgGroup(this, (Team)m_bgData.bgTeam);

                    SetInviteForBattlegroundQueueType(bgQueueTypeId, currentBg.GetInstanceID());
                }
                // Bg was not found - go to Entry Point
                else
                {
                    // leave bg
                    if (player_at_bg)
                    {
                        player_at_bg = false;
                        currentBg.RemovePlayerAtLeave(GetGUID(), false, true);
                    }

                    // Do not look for instance if bg not found
                    WorldLocation _loc = GetBattlegroundEntryPoint();
                    mapId = _loc.GetMapId();
                    instanceId = 0;

                    if (mapId == 0xFFFFFFFF) // BattlegroundEntry Point not found (???)
                    {
                        Log.outError(LogFilter.Player, "Player (guidlow {0}) was in BG in database, but BG was not found, and entry point was invalid! Teleport to default race/class locations.", guid.ToString());
                        RelocateToHomebind();
                    }
                    else
                        Relocate(_loc);

                    // We are not in BG anymore
                    m_bgData.bgInstanceID = 0;
                }
            }
            // currently we do not support transport in bg
            else if (transLowGUID != 0)
            {
                ObjectGuid transGUID = ObjectGuid.Create(HighGuid.Transport, transLowGUID);

                Transport transport = null;
                Transport go = Global.ObjAccessor.FindTransport(transGUID);
                if (go)
                    transport = go;

                if (transport)
                {
                    float x = result.Read<float>(37);
                    float y = result.Read<float>(38);
                    float z = result.Read<float>(39);
                    float o = result.Read<float>(40);
                    m_movementInfo.transport.pos = new Position(x, y, z, o);
                    transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);

                    if (!GridDefines.IsValidMapCoord(x, y, z, o) ||
                        // transport size limited
                        Math.Abs(m_movementInfo.transport.pos.posX) > 250.0f ||
                        Math.Abs(m_movementInfo.transport.pos.posY) > 250.0f ||
                        Math.Abs(m_movementInfo.transport.pos.posZ) > 250.0f)
                    {
                        Log.outError(LogFilter.Player, "Player (guidlow {0}) have invalid transport coordinates (X: {1} Y: {2} Z: {3} O: {4}). Teleport to bind location.",
                            guid.ToString(), x, y, z, o);

                        m_movementInfo.transport.Reset();
                        RelocateToHomebind();
                    }
                    else
                    {
                        Relocate(x, y, z, o);
                        mapId = transport.GetMapId();

                        transport.AddPassenger(this);
                    }
                }
                else
                {
                    Log.outError(LogFilter.Player, "Player (guidlow {0}) have problems with transport guid ({1}). Teleport to bind location.",
                        guid.ToString(), transLowGUID);

                    RelocateToHomebind();
                }
            }
            // currently we do not support taxi in instance
            else if (!string.IsNullOrEmpty(taxi_nodes))
            {
                instanceId = 0;

                // Not finish taxi flight path
                if (m_bgData.HasTaxiPath())
                {
                    for (int i = 0; i < 2; ++i)
                        m_taxi.AddTaxiDestination(m_bgData.taxiPath[i]);
                }
                if (!m_taxi.LoadTaxiDestinationsFromString(taxi_nodes, GetTeam()))
                {
                    // problems with taxi path loading
                    TaxiNodesRecord nodeEntry = null;
                    uint node_id = m_taxi.GetTaxiSource();
                    if (node_id != 0)
                        nodeEntry = CliDB.TaxiNodesStorage.LookupByKey(node_id);

                    if (nodeEntry == null)                                      // don't know taxi start node, to homebind
                    {
                        Log.outError(LogFilter.Player, "Character {0} have wrong data in taxi destination list, teleport to homebind.", GetGUID().ToString());
                        RelocateToHomebind();
                    }
                    else                                                // have start node, to it
                    {
                        Log.outError(LogFilter.Player, "Character {0} have too short taxi destination list, teleport to original node.", GetGUID().ToString());
                        mapId = nodeEntry.ContinentID;
                        Relocate(nodeEntry.Pos.X, nodeEntry.Pos.Y, nodeEntry.Pos.Z, 0.0f);
                    }
                    m_taxi.ClearTaxiDestinations();
                }
                uint nodeid = m_taxi.GetTaxiSource();
                if (nodeid != 0)
                {
                    // save source node as recall coord to prevent recall and fall from sky
                    var nodeEntry = CliDB.TaxiNodesStorage.LookupByKey(nodeid);
                    if (nodeEntry != null && nodeEntry.ContinentID == GetMapId())
                    {
                        Cypher.Assert(nodeEntry != null);                                  // checked in m_taxi.LoadTaxiDestinationsFromString
                        mapId = nodeEntry.ContinentID;
                        Relocate(nodeEntry.Pos.X, nodeEntry.Pos.Y, nodeEntry.Pos.Z, 0.0f);
                    }

                    // flight will started later
                }
            }

            // Map could be changed before
            mapEntry = CliDB.MapStorage.LookupByKey(mapId);
            // client without expansion support
            if (mapEntry != null)
            {
                if (GetSession().GetExpansion() < mapEntry.Expansion())
                {
                    Log.outDebug(LogFilter.Player, "Player {0} using client without required expansion tried login at non accessible map {1}", GetName(), mapId);
                    RelocateToHomebind();
                }

                // fix crash (because of if (Map* map = _FindMap(instanceId)) in MapInstanced.CreateInstance)
                if (instanceId != 0)
                {
                    InstanceSave save = GetInstanceSave(mapId);
                    if (save != null)
                        if (save.GetInstanceId() != instanceId)
                            instanceId = 0;
                }
            }

            // NOW player must have valid map
            // load the player's map here if it's not already loaded
            if (!map)
                map = Global.MapMgr.CreateMap(mapId, this, instanceId);
            AreaTriggerStruct areaTrigger = null;
            bool check = false;

            if (!map)
            {
                areaTrigger = Global.ObjectMgr.GetGoBackTrigger(mapId);
                check = true;
            }
            else if (map.IsDungeon()) // if map is dungeon...
            {
                EnterState denyReason = ((InstanceMap)map).CannotEnter(this);
                if (denyReason != 0) // ... and can't enter map, then look for entry point.
                {
                    switch (denyReason)
                    {
                        case EnterState.CannotEnterDifficultyUnavailable:
                            SendTransferAborted(map.GetId(), TransferAbortReason.Difficulty, (byte)map.GetDifficultyID());
                            break;
                        case EnterState.CannotEnterInstanceBindMismatch:
                            SendSysMessage(CypherStrings.InstanceBindMismatch, map.GetMapName());
                            break;
                        case EnterState.CannotEnterTooManyInstances:
                            SendTransferAborted(map.GetId(), TransferAbortReason.TooManyInstances);
                            break;
                        case EnterState.CannotEnterMaxPlayers:
                            SendTransferAborted(map.GetId(), TransferAbortReason.MaxPlayers);
                            break;
                        case EnterState.CannotEnterZoneInCombat:
                            SendTransferAborted(map.GetId(), TransferAbortReason.ZoneInCombat);
                            break;
                        default:
                            break;
                    }
                    areaTrigger = Global.ObjectMgr.GetGoBackTrigger(mapId);
                    check = true;
                }
                else if (instanceId != 0 && Global.InstanceSaveMgr.GetInstanceSave(instanceId) == null) // ... and instance is reseted then look for entrance.
                {
                    areaTrigger = Global.ObjectMgr.GetMapEntranceTrigger(mapId);
                    check = true;
                }
            }

            if (check) // in case of special event when creating map...
            {
                if (areaTrigger != null) // ... if we have an areatrigger, then relocate to new map/coordinates.
                {
                    Relocate(areaTrigger.target_X, areaTrigger.target_Y, areaTrigger.target_Z, GetOrientation());
                    if (mapId != areaTrigger.target_mapId)
                    {
                        mapId = areaTrigger.target_mapId;
                        map = Global.MapMgr.CreateMap(mapId, this);
                    }
                }
                else
                {
                    Log.outError(LogFilter.Player, "Player {0} {1} Map: {2}, {3}. Areatrigger not found.", GetName(), guid.ToString(), mapId, GetPosition());
                    RelocateToHomebind();
                    map = null;
                }
            }

            if (!map)
            {
                mapId = info.MapId;
                Relocate(info.PositionX, info.PositionY, info.PositionZ, 0.0f);
                map = Global.MapMgr.CreateMap(mapId, this);
                if (!map)
                {
                    Log.outError(LogFilter.Player, "Player {0} {1} Map: {2}, {3}. Invalid default map coordinates or instance couldn't be created.", GetName(), guid.ToString(), mapId, GetPosition());
                    return false;
                }
            }

            SetMap(map);

            // now that map position is determined, check instance validity
            if (!CheckInstanceValidity(true) && !IsInstanceLoginGameMasterException())
                m_InstanceValid = false;

            if (player_at_bg)
                map.ToBattlegroundMap().GetBG().AddPlayer(this);

            // randomize first save time in range [CONFIG_INTERVAL_SAVE] around [CONFIG_INTERVAL_SAVE]
            // this must help in case next save after mass player load after server startup
            m_nextSave = RandomHelper.URand(m_nextSave / 2, m_nextSave * 3 / 2);

            SaveRecallPosition();

            long now = Time.UnixTime;
            long logoutTime = result.Read<int>(32);

            // since last logout (in seconds)
            uint time_diff = (uint)(now - logoutTime);

            // set value, including drunk invisibility detection
            // calculate sobering. after 15 minutes logged out, the player will be sober again
            byte newDrunkValue = 0;
            if (time_diff < (uint)GetDrunkValue() * 9)
                newDrunkValue = (byte)(GetDrunkValue() - time_diff / 9);

            SetDrunkValue(newDrunkValue);

            m_cinematic = result.Read<byte>(28);
            m_PlayedTimeTotal = result.Read<uint>(29);
            m_PlayedTimeLevel = result.Read<uint>(30);

            SetTalentResetCost(result.Read<uint>(34));
            SetTalentResetTime(result.Read<uint>(35));

            m_taxi.LoadTaxiMask(result.Read<string>(27));            // must be before InitTaxiNodesForLevel

            PlayerExtraFlags extraflags = (PlayerExtraFlags)result.Read<uint>(42);

            m_stableSlots = result.Read<byte>(43);
            if (m_stableSlots > 4)
            {
                Log.outError(LogFilter.Player, "Player can have not more {0} stable slots, but have in DB {1}", 4, m_stableSlots);
                m_stableSlots = 4;
            }

            atLoginFlags = (AtLoginFlags)result.Read<uint>(44);

            // Honor system
            // Update Honor kills data
            m_lastHonorUpdateTime = logoutTime;
            UpdateHonorFields();

            m_deathExpireTime = result.Read<uint>(47);
            if (m_deathExpireTime > now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep)
                m_deathExpireTime = now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep - 1;

            // clear charm/summon related fields
            SetOwnerGUID(ObjectGuid.Empty);
            SetGuidValue(UnitFields.CharmedBy, ObjectGuid.Empty);
            SetGuidValue(UnitFields.Charm, ObjectGuid.Empty);
            SetGuidValue(UnitFields.Summon, ObjectGuid.Empty);
            SetGuidValue(ActivePlayerFields.Farsight, ObjectGuid.Empty);
            SetCreatorGUID(ObjectGuid.Empty);

            RemoveFlag(UnitFields.Flags2, UnitFlags2.ForceMove);

            // reset some aura modifiers before aura apply
            SetUInt32Value(ActivePlayerFields.TrackCreatures, 0);
            SetUInt32Value(ActivePlayerFields.TrackResources, 0);

            // make sure the unit is considered out of combat for proper loading
            ClearInCombat();

            // make sure the unit is considered not in duel for proper loading
            SetGuidValue(PlayerFields.DuelArbiter, ObjectGuid.Empty);
            SetUInt32Value(PlayerFields.DuelTeam, 0);

            // reset stats before loading any modifiers
            InitStatsForLevel();
            InitTaxiNodesForLevel();
            InitRunes();

            // rest bonus can only be calculated after InitStatsForLevel()
            _restMgr.LoadRestBonus(RestTypes.XP, (PlayerRestState)result.Read<byte>(19), result.Read<float>(31));

            // load skills after InitStatsForLevel because it triggering aura apply also
            _LoadSkills(holder.GetResult(PlayerLoginQueryLoad.Skills));
            UpdateSkillsForLevel();

            SetPrimarySpecialization(result.Read<uint>(36));
            SetActiveTalentGroup(result.Read<byte>(64));
            ChrSpecializationRecord primarySpec = CliDB.ChrSpecializationStorage.LookupByKey(GetPrimarySpecialization());
            if (primarySpec == null || primarySpec.ClassID != (byte)GetClass() || GetActiveTalentGroup() >= PlayerConst.MaxSpecializations)
                ResetTalentSpecialization();

            uint lootSpecId = result.Read<byte>(65);
            ChrSpecializationRecord chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(lootSpecId);
            if (chrSpec != null)
            {
                if (chrSpec.ClassID == (uint)GetClass())
                    SetLootSpecId(lootSpecId);
            }

            ChrSpecializationRecord spec = Global.DB2Mgr.GetChrSpecializationByIndex(GetClass(), GetActiveTalentGroup());
            if (spec != null)
                SetUInt32Value(PlayerFields.CurrentSpecId, spec.Id);

            _LoadTalents(holder.GetResult(PlayerLoginQueryLoad.Talents));
            _LoadPvpTalents(holder.GetResult(PlayerLoginQueryLoad.PvpTalents));
            _LoadSpells(holder.GetResult(PlayerLoginQueryLoad.Spells));
            GetSession().GetCollectionMgr().LoadToys();
            GetSession().GetCollectionMgr().LoadHeirlooms();
            GetSession().GetCollectionMgr().LoadMounts();
            GetSession().GetCollectionMgr().LoadItemAppearances();

            LearnSpecializationSpells();

            _LoadGlyphs(holder.GetResult(PlayerLoginQueryLoad.Glyphs));
            _LoadAuras(holder.GetResult(PlayerLoginQueryLoad.Auras), holder.GetResult(PlayerLoginQueryLoad.AuraEffects), time_diff);
            _LoadGlyphAuras();
            // add ghost flag (must be after aura load: PLAYER_FLAGS_GHOST set in aura)
            if (HasFlag(PlayerFields.Flags, PlayerFlags.Ghost))
                m_deathState = DeathState.Dead;

            // after spell load, learn rewarded spell if need also
            _LoadQuestStatus(holder.GetResult(PlayerLoginQueryLoad.QuestStatus));
            _LoadQuestStatusObjectives(holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectives));
            _LoadQuestStatusRewarded(holder.GetResult(PlayerLoginQueryLoad.QuestStatusRew));
            _LoadDailyQuestStatus(holder.GetResult(PlayerLoginQueryLoad.DailyQuestStatus));
            _LoadWeeklyQuestStatus(holder.GetResult(PlayerLoginQueryLoad.WeeklyQuestStatus));
            _LoadSeasonalQuestStatus(holder.GetResult(PlayerLoginQueryLoad.SeasonalQuestStatus));
            _LoadRandomBGStatus(holder.GetResult(PlayerLoginQueryLoad.RandomBg));

            // after spell and quest load
            InitTalentForLevel();
            LearnDefaultSkills();
            LearnCustomSpells();
            if (getLevel() < PlayerConst.LevelMinHonor)
                ResetPvpTalents();

            // must be before inventory (some items required reputation check)
            reputationMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Reputation));

            _LoadInventory(holder.GetResult(PlayerLoginQueryLoad.Inventory), holder.GetResult(PlayerLoginQueryLoad.Artifacts), time_diff);

            if (IsVoidStorageUnlocked())
                _LoadVoidStorage(holder.GetResult(PlayerLoginQueryLoad.VoidStorage));

            // update items with duration and realtime
            UpdateItemDuration(time_diff, true);

            _LoadActions(holder.GetResult(PlayerLoginQueryLoad.Actions));

            // unread mails and next delivery time, actual mails not loaded
            _LoadMailInit(holder.GetResult(PlayerLoginQueryLoad.MailCount), holder.GetResult(PlayerLoginQueryLoad.MailDate));

            m_social = Global.SocialMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.SocialList), GetGUID());

            // check PLAYER_CHOSEN_TITLE compatibility with PLAYER__FIELD_KNOWN_TITLES
            // note: PLAYER__FIELD_KNOWN_TITLES updated at quest status loaded
            uint curTitle = result.Read<uint>(53);
            if (curTitle != 0 && !HasTitle(curTitle))
                curTitle = 0;

            SetUInt32Value(PlayerFields.ChosenTitle, curTitle);

            // has to be called after last Relocate() in Player.LoadFromDB
            SetFallInformation(0, GetPositionZ());

            GetSpellHistory().LoadFromDB<Player>(holder.GetResult(PlayerLoginQueryLoad.SpellCooldowns), holder.GetResult(PlayerLoginQueryLoad.SpellCharges));

            // Spell code allow apply any auras to dead character in load time in aura/spell/item loading
            // Do now before stats re-calculation cleanup for ghost state unexpected auras
            if (!IsAlive())
                RemoveAllAurasOnDeath();
            else
                RemoveAllAurasRequiringDeadTarget();

            //apply all stat bonuses from items and auras
            SetCanModifyStats(true);
            UpdateAllStats();

            // restore remembered power/health values (but not more max values)
            uint savedHealth = result.Read<uint>(56);
            SetHealth(savedHealth > GetMaxHealth() ? GetMaxHealth() : savedHealth);
            int loadedPowers = 0;
            for (PowerType i = 0; i < PowerType.Max; ++i)
            {
                if (Global.DB2Mgr.GetPowerIndexByClass(i, GetClass()) != (int)PowerType.Max)
                {
                    uint savedPower = result.Read<uint>(56 + loadedPowers);
                    uint maxPower = GetUInt32Value(UnitFields.MaxPower + loadedPowers);
                    SetPower(i, (int)(savedPower > maxPower ? maxPower : savedPower));
                    if (++loadedPowers >= (int)PowerType.MaxPerClass)
                        break;
                }
            }

            for (; loadedPowers < (int)PowerType.MaxPerClass; ++loadedPowers)
                SetUInt32Value(UnitFields.Power + loadedPowers, 0);

            SetPower(PowerType.LunarPower, 0);
            // Init rune recharge
            if (GetPowerIndex(PowerType.Runes) != (int)PowerType.Max)
            {
                int runes = GetPower(PowerType.Runes);
                int maxRunes = GetMaxPower(PowerType.Runes);
                uint runeCooldown = GetRuneBaseCooldown();
                while (runes < maxRunes)
                {
                    SetRuneCooldown((byte)runes, runeCooldown);
                    ++runes;
                }
            }

            Log.outDebug(LogFilter.Player, "The value of player {0} after load item and aura is: ", GetName());

            // GM state
            if (GetSession().HasPermission(RBACPermissions.RestoreSavedGmState))
            {
                switch (WorldConfig.GetIntValue(WorldCfg.GmLoginState))
                {
                    default:
                    case 0:
                        break;             // disable
                    case 1: SetGameMaster(true);
                        break;             // enable
                    case 2:                                         // save state
                        if (extraflags.HasAnyFlag(PlayerExtraFlags.GMOn))
                            SetGameMaster(true);
                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmVisibleState))
                {
                    default:
                    case 0: SetGMVisible(false);
                        break;             // invisible
                    case 1:
                        break;             // visible
                    case 2:                                         // save state
                        if (extraflags.HasAnyFlag(PlayerExtraFlags.GMInvisible))
                            SetGMVisible(false);
                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmChat))
                {
                    default:
                    case 0:
                        break;                 // disable
                    case 1: SetGMChat(true);
                        break;                 // enable
                    case 2:                                         // save state
                        if (extraflags.HasAnyFlag(PlayerExtraFlags.GMChat))
                            SetGMChat(true);
                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmWhisperingTo))
                {
                    default:
                    case 0:
                        break;         // disable
                    case 1: SetAcceptWhispers(true);
                        break;         // enable
                    case 2:                                         // save state
                        if (extraflags.HasAnyFlag(PlayerExtraFlags.AcceptWhispers))
                            SetAcceptWhispers(true);
                        break;
                }
            }

            // RaF stuff.
            m_grantableLevels = result.Read<byte>(69);
            if (GetSession().IsARecruiter() || (GetSession().GetRecruiterId() != 0))
                SetFlag(ObjectFields.DynamicFlags, UnitDynFlags.ReferAFriend);

            if (m_grantableLevels > 0)
                SetByteValue(ActivePlayerFields.Bytes, PlayerFieldOffsets.FieldBytesOffsetRafGrantableLevel, 0x01);

            _LoadDeclinedNames(holder.GetResult(PlayerLoginQueryLoad.DeclinedNames));

            _LoadEquipmentSets(holder.GetResult(PlayerLoginQueryLoad.EquipmentSets));
            _LoadTransmogOutfits(holder.GetResult(PlayerLoginQueryLoad.TransmogOutfits));

            _LoadCUFProfiles(holder.GetResult(PlayerLoginQueryLoad.CufProfiles));

            var garrison = new Garrison(this);
            if (garrison.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Garrison), 
                holder.GetResult(PlayerLoginQueryLoad.GarrisonBlueprints),
                holder.GetResult(PlayerLoginQueryLoad.GarrisonBuildings),
                holder.GetResult(PlayerLoginQueryLoad.GarrisonFollowers),
                holder.GetResult(PlayerLoginQueryLoad.GarrisonFollowerAbilities)))
                _garrison = garrison;

            _InitHonorLevelOnLoadFromDB(result.Read<uint>(73), result.Read<uint>(74));

            _restMgr.LoadRestBonus(RestTypes.Honor, (PlayerRestState)result.Read<byte>(75), result.Read<float>(76));
            if (time_diff > 0)
            {
                //speed collect rest bonus in offline, in logout, far from tavern, city (section/in hour)
                float bubble0 = 0.031f;
                //speed collect rest bonus in offline, in logout, in tavern, city (section/in hour)
                float bubble1 = 0.125f;
                float bubble = result.Read<byte>(33) > 0
                    ? bubble1 * WorldConfig.GetFloatValue(WorldCfg.RateRestOfflineInTavernOrCity)
                    : bubble0 * WorldConfig.GetFloatValue(WorldCfg.RateRestOfflineInWilderness);

                _restMgr.AddRestBonus(RestTypes.XP, time_diff * _restMgr.CalcExtraPerSec(RestTypes.XP, bubble));
            }

            m_achievementSys.CheckAllAchievementCriteria(this);
            m_questObjectiveCriteriaMgr.CheckAllQuestObjectiveCriteria(this);
            return true;
        }
        public void SaveToDB(bool create = false)
        {
            // delay auto save at any saves (manual, in code, or autosave)
            m_nextSave = WorldConfig.GetUIntValue(WorldCfg.IntervalSave);

            //lets allow only players in world to be saved
            if (IsBeingTeleportedFar())
            {
                ScheduleDelayedOperation(PlayerDelayedOperations.SavePlayer);
                return;
            }

            // first save/honor gain after midnight will also update the player's honor fields
            UpdateHonorFields();

            SQLTransaction trans = new SQLTransaction();
            var index = 0;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_FISHINGSTEPS);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            if (create)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER);
                //! Insert Select
                //! TO DO: Filter out more redundant fields that can take their default value at player create
                stmt.AddValue(index++, GetGUID().GetCounter());
                stmt.AddValue(index++, GetSession().GetAccountId());
                stmt.AddValue(index++, GetName());
                stmt.AddValue(index++, (byte)GetRace());
                stmt.AddValue(index++, (byte)GetClass());
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender));
                stmt.AddValue(index++, getLevel());
                stmt.AddValue(index++, GetUInt32Value(ActivePlayerFields.Xp));
                stmt.AddValue(index++, GetMoney());
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId));
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetFaceId));
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairStyleId));
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairColorId));
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes2, PlayerFieldOffsets.Bytes2OffsetFacialStyle));
                for (int i = 0; i < PlayerConst.CustomDisplaySize; ++i)
                    stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes2, (byte)(PlayerFieldOffsets.Bytes2OffsetCustomDisplayOption + i)));
                stmt.AddValue(index++, GetInventorySlotCount());
                stmt.AddValue(index++, GetBankBagSlotCount());
                stmt.AddValue(index++, (byte)GetUInt32Value(ActivePlayerFields.RestInfo + PlayerFieldOffsets.RestStateXp));
                stmt.AddValue(index++, GetUInt32Value(PlayerFields.Flags));
                stmt.AddValue(index++, GetUInt32Value(PlayerFields.FlagsEx));
                stmt.AddValue(index++, (ushort)GetMapId());
                stmt.AddValue(index++, GetInstanceId());
                stmt.AddValue(index++, (byte)GetDungeonDifficultyID());
                stmt.AddValue(index++, (byte)GetRaidDifficultyID());
                stmt.AddValue(index++, (byte)GetLegacyRaidDifficultyID());
                stmt.AddValue(index++, GetPositionX());
                stmt.AddValue(index++, GetPositionY());
                stmt.AddValue(index++, GetPositionZ());
                stmt.AddValue(index++, GetOrientation());
                stmt.AddValue(index++, GetTransOffsetX());
                stmt.AddValue(index++, GetTransOffsetY());
                stmt.AddValue(index++, GetTransOffsetZ());
                stmt.AddValue(index++, GetTransOffsetO());
                ulong transLowGUID = 0;
                if (GetTransport())
                    transLowGUID = GetTransport().GetGUID().GetCounter();
                stmt.AddValue(index++, transLowGUID);

                StringBuilder ss = new StringBuilder();
                for (int i = 0; i < PlayerConst.TaxiMaskSize; ++i)
                    ss.Append(m_taxi.m_taximask[i] + " ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, m_cinematic);
                stmt.AddValue(index++, m_PlayedTimeTotal);
                stmt.AddValue(index++, m_PlayedTimeLevel);
                stmt.AddValue(index++, _restMgr.GetRestBonus(RestTypes.XP));
                stmt.AddValue(index++, (uint)Time.UnixTime);
                stmt.AddValue(index++, HasFlag(PlayerFields.Flags, PlayerFlags.Resting) ? 1 : 0);
                //save, far from tavern/city
                //save, but in tavern/city
                stmt.AddValue(index++, GetTalentResetCost());
                stmt.AddValue(index++, GetTalentResetTime());

                stmt.AddValue(index++, GetPrimarySpecialization());
                stmt.AddValue(index++, (ushort)m_ExtraFlags);
                stmt.AddValue(index++, m_stableSlots);
                stmt.AddValue(index++, (ushort)atLoginFlags);
                stmt.AddValue(index++, GetZoneId());
                stmt.AddValue(index++, m_deathExpireTime);

                ss.Clear();
                ss.Append(m_taxi.SaveTaxiDestinationsToString());

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, GetUInt32Value(ActivePlayerFields.LifetimeHonorableKills));
                stmt.AddValue(index++, GetUInt16Value(ActivePlayerFields.Kills, 0));
                stmt.AddValue(index++, GetUInt16Value(ActivePlayerFields.Kills, 1));
                stmt.AddValue(index++, (uint)GetInt32Value(PlayerFields.ChosenTitle));
                stmt.AddValue(index++, GetUInt32Value(ActivePlayerFields.WatchedFactionIndex));
                stmt.AddValue(index++, GetDrunkValue());
                stmt.AddValue(index++, GetHealth());

                int storedPowers = 0;
                for (PowerType i = 0; i < PowerType.Max; ++i)
                {
                    if (Global.DB2Mgr.GetPowerIndexByClass(i, GetClass()) != (int)PowerType.Max)
                    {
                        stmt.AddValue(index++, GetUInt32Value(UnitFields.Power + storedPowers));
                        storedPowers++;
                        if (storedPowers >= (int)PowerType.MaxPerClass)
                            break;
                    }
                }

                for (; storedPowers < (int)PowerType.MaxPerClass; ++storedPowers)
                    stmt.AddValue(index++, 0);

                stmt.AddValue(index++, GetSession().GetLatency());

                stmt.AddValue(index++, GetActiveTalentGroup());

                stmt.AddValue(index++, GetLootSpecId());

                ss.Clear();
                for (var i = 0; i < PlayerConst.ExploredZonesSize; ++i)
                    ss.AppendFormat("{0} ", GetUInt32Value(ActivePlayerFields.ExploredZones + i));
                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                // cache equipment...
                for (byte i = 0; i < InventorySlots.BagEnd; ++i)
                {
                    Item item = GetItemByPos(InventorySlots.Bag0, i);
                    if (item != null)
                    {
                        ss.AppendFormat("{0} {1} ", (uint)item.GetTemplate().GetInventoryType(), item.GetDisplayId(this));
                        SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetVisibleEnchantmentId(this));
                        if (enchant != null)
                            ss.Append(enchant.ItemVisual);
                        else
                            ss.Append(0);

                        ss.Append(' ');
                    }
                    else
                        ss.Append("0 0 0 ");
                }
                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                for (var i = 0; i < PlayerConst.KnowTitlesSize * 2; ++i)
                    ss.AppendFormat("{0} ", GetUInt32Value(ActivePlayerFields.KnownTitles + i));
                stmt.AddValue(index++, ss.ToString());

                stmt.AddValue(index++, GetByteValue(ActivePlayerFields.Bytes, PlayerFieldOffsets.FieldBytesOffsetActionBarToggles));
                stmt.AddValue(index++, m_grantableLevels);
                stmt.AddValue(index++, Global.WorldMgr.GetRealm().Build);
            }
            else
            {
                // Update query
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER);
                stmt.AddValue(index++, GetName());
                stmt.AddValue(index++, (byte)GetRace());
                stmt.AddValue(index++, (byte)GetClass());
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender));
                stmt.AddValue(index++, getLevel());
                stmt.AddValue(index++, GetUInt32Value(ActivePlayerFields.Xp));
                stmt.AddValue(index++, GetMoney());
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId));
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetFaceId));
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairStyleId));
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairColorId));
                stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes2, PlayerFieldOffsets.Bytes2OffsetFacialStyle));
                for (int i = 0; i < PlayerConst.CustomDisplaySize; ++i)
                    stmt.AddValue(index++, GetByteValue(PlayerFields.Bytes2, (byte)(
                        PlayerFieldOffsets.Bytes2OffsetCustomDisplayOption + i)));
                stmt.AddValue(index++, GetInventorySlotCount());
                stmt.AddValue(index++, GetBankBagSlotCount());
                stmt.AddValue(index++, (byte)GetUInt32Value(ActivePlayerFields.RestInfo + PlayerFieldOffsets.RestStateXp));
                stmt.AddValue(index++, GetUInt32Value(PlayerFields.Flags));
                stmt.AddValue(index++, GetUInt32Value(PlayerFields.FlagsEx));

                if (!IsBeingTeleported())
                {
                    stmt.AddValue(index++, GetMapId());
                    stmt.AddValue(index++, GetInstanceId());
                    stmt.AddValue(index++, (byte)GetDungeonDifficultyID());
                    stmt.AddValue(index++, (byte)GetRaidDifficultyID());
                    stmt.AddValue(index++, (byte)GetLegacyRaidDifficultyID());
                    stmt.AddValue(index++, GetPositionX());
                    stmt.AddValue(index++, GetPositionY());
                    stmt.AddValue(index++, GetPositionZ());
                    stmt.AddValue(index++, GetOrientation());
                }
                else
                {
                    stmt.AddValue(index++, GetTeleportDest().GetMapId());
                    stmt.AddValue(index++, 0);
                    stmt.AddValue(index++, (byte)GetDungeonDifficultyID());
                    stmt.AddValue(index++, (byte)GetRaidDifficultyID());
                    stmt.AddValue(index++, (byte)GetLegacyRaidDifficultyID());
                    stmt.AddValue(index++, GetTeleportDest().GetPositionX());
                    stmt.AddValue(index++, GetTeleportDest().GetPositionY());
                    stmt.AddValue(index++, GetTeleportDest().GetPositionZ());
                    stmt.AddValue(index++, GetTeleportDest().GetOrientation());
                }
                stmt.AddValue(index++, GetTransOffsetX());
                stmt.AddValue(index++, GetTransOffsetY());
                stmt.AddValue(index++, GetTransOffsetZ());
                stmt.AddValue(index++, GetTransOffsetO());
                ulong transLowGUID = 0;
                if (GetTransport())
                    transLowGUID = GetTransport().GetGUID().GetCounter();
                stmt.AddValue(index++, transLowGUID);

                StringBuilder ss = new StringBuilder();
                for (int i = 0; i < PlayerConst.TaxiMaskSize; ++i)
                    ss.Append(m_taxi.m_taximask[i] + " ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, m_cinematic);
                stmt.AddValue(index++, m_PlayedTimeTotal);
                stmt.AddValue(index++, m_PlayedTimeLevel);
                stmt.AddValue(index++, _restMgr.GetRestBonus(RestTypes.XP));
                stmt.AddValue(index++, Time.UnixTime);
                stmt.AddValue(index++, HasFlag(PlayerFields.Flags, PlayerFlags.Resting) ? 1 : 0);
                //save, far from tavern/city
                //save, but in tavern/city
                stmt.AddValue(index++, GetTalentResetCost());
                stmt.AddValue(index++, GetTalentResetTime());

                stmt.AddValue(index++, GetPrimarySpecialization());
                stmt.AddValue(index++, (ushort)m_ExtraFlags);
                stmt.AddValue(index++, m_stableSlots);
                stmt.AddValue(index++, (ushort)atLoginFlags);
                stmt.AddValue(index++, GetZoneId());
                stmt.AddValue(index++, m_deathExpireTime);

                ss.Clear();
                ss.Append(m_taxi.SaveTaxiDestinationsToString());

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, GetUInt32Value(ActivePlayerFields.LifetimeHonorableKills));
                stmt.AddValue(index++, GetUInt16Value(ActivePlayerFields.Kills, 0));
                stmt.AddValue(index++, GetUInt16Value(ActivePlayerFields.Kills, 1));
                stmt.AddValue(index++, GetUInt32Value(PlayerFields.ChosenTitle));
                stmt.AddValue(index++, (uint)GetInt32Value(ActivePlayerFields.WatchedFactionIndex));
                stmt.AddValue(index++, GetDrunkValue());
                stmt.AddValue(index++, GetHealth());

                int storedPowers = 0;
                for (PowerType i = 0; i < PowerType.Max; ++i)
                {
                    if (Global.DB2Mgr.GetPowerIndexByClass(i, GetClass()) != (int)PowerType.Max)
                    {
                        stmt.AddValue(index++, GetUInt32Value(UnitFields.Power + storedPowers));
                        storedPowers++;
                        if (storedPowers >= (int)PowerType.MaxPerClass)
                            break;
                    }
                }

                for (; storedPowers < (int)PowerType.MaxPerClass; ++storedPowers)
                    stmt.AddValue(index++, 0);

                stmt.AddValue(index++, GetSession().GetLatency());

                stmt.AddValue(index++, GetActiveTalentGroup());

                stmt.AddValue(index++, GetLootSpecId());

                ss.Clear();
                for (var i = 0; i < PlayerConst.ExploredZonesSize; ++i)
                    ss.AppendFormat("{0} ", GetUInt32Value(ActivePlayerFields.ExploredZones + i));
                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                // cache equipment...
                for (byte i = 0; i < InventorySlots.BagEnd; ++i)
                {
                    Item item = GetItemByPos(InventorySlots.Bag0, i);
                    if (item != null)
                    {
                        ss.AppendFormat("{0} {1} ", (uint)item.GetTemplate().GetInventoryType(), item.GetDisplayId(this));
                        SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetVisibleEnchantmentId(this));
                        if (enchant != null)
                            ss.Append(enchant.ItemVisual);
                        else
                            ss.Append(0);

                        ss.Append(' ');
                    }
                    else
                        ss.Append("0 0 0 ");
                }
                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                for (var i = 0; i < PlayerConst.KnowTitlesSize * 2; ++i)
                    ss.AppendFormat("{0} ", GetUInt32Value(ActivePlayerFields.KnownTitles + i));

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, GetByteValue(ActivePlayerFields.Bytes, PlayerFieldOffsets.FieldBytesOffsetActionBarToggles));
                stmt.AddValue(index++, m_grantableLevels);

                stmt.AddValue(index++, IsInWorld && !GetSession().PlayerLogout() ? 1 : 0);
                stmt.AddValue(index++, GetUInt32Value(ActivePlayerFields.Honor));
                stmt.AddValue(index++, GetHonorLevel());
                stmt.AddValue(index++, (byte)GetUInt32Value(ActivePlayerFields.RestInfo + PlayerFieldOffsets.RestStateHonor));
                stmt.AddValue(index++, _restMgr.GetRestBonus(RestTypes.Honor));
                stmt.AddValue(index++, Global.WorldMgr.GetRealm().Build);

                // Index
                stmt.AddValue(index, GetGUID().GetCounter());
            }

            trans.Append(stmt);

            if (m_fishingSteps != 0)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_FISHINGSTEPS);
                index = 0;
                stmt.AddValue(index++, GetGUID().GetCounter());
                stmt.AddValue(index++, m_fishingSteps);
                trans.Append(stmt);
            }

            if (m_mailsUpdated)                                     //save mails only when needed
                _SaveMail(trans);

            _SaveBGData(trans);
            _SaveInventory(trans);
            _SaveVoidStorage(trans);
            _SaveQuestStatus(trans);
            _SaveDailyQuestStatus(trans);
            _SaveWeeklyQuestStatus(trans);
            _SaveSeasonalQuestStatus(trans);
            _SaveMonthlyQuestStatus(trans);
            _SaveGlyphs(trans);
            _SaveTalents(trans);
            _SaveSpells(trans);
            GetSpellHistory().SaveToDB<Player>(trans);
            _SaveActions(trans);
            _SaveAuras(trans);
            _SaveSkills(trans);
            m_achievementSys.SaveToDB(trans);
            reputationMgr.SaveToDB(trans);
            m_questObjectiveCriteriaMgr.SaveToDB(trans);
            _SaveEquipmentSets(trans);
            GetSession().SaveTutorialsData(trans);                 // changed only while character in game
            _SaveInstanceTimeRestrictions(trans);
            _SaveCurrency(trans);
            _SaveCUFProfiles(trans);
            if (_garrison != null)
                _garrison.SaveToDB(trans);

            // check if stats should only be saved on logout
            // save stats can be out of transaction
            if (GetSession().isLogingOut() || !WorldConfig.GetBoolValue(WorldCfg.StatsSaveOnlyOnLogout))
                _SaveStats(trans);

            DB.Characters.CommitTransaction(trans);

            // TODO: Move this out
            trans = new SQLTransaction();
            GetSession().GetCollectionMgr().SaveAccountToys(trans);
            GetSession().GetBattlePetMgr().SaveToDB(trans);
            GetSession().GetCollectionMgr().SaveAccountHeirlooms(trans);
            GetSession().GetCollectionMgr().SaveAccountMounts(trans);
            GetSession().GetCollectionMgr().SaveAccountItemAppearances(trans);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BNET_LAST_PLAYER_CHARACTERS);
            stmt.AddValue(0, GetSession().GetAccountId());
            stmt.AddValue(1, Global.WorldMgr.GetRealmId().Region);
            stmt.AddValue(2, Global.WorldMgr.GetRealmId().Site);
            trans.Append(stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_LAST_PLAYER_CHARACTERS);
            stmt.AddValue(0, GetSession().GetAccountId());
            stmt.AddValue(1, Global.WorldMgr.GetRealmId().Region);
            stmt.AddValue(2, Global.WorldMgr.GetRealmId().Site);
            stmt.AddValue(3, Global.WorldMgr.GetRealmId().Realm);
            stmt.AddValue(4, GetName());
            stmt.AddValue(5, GetGUID().GetCounter());
            stmt.AddValue(6, Time.UnixTime);
            trans.Append(stmt);

            DB.Login.CommitTransaction(trans);

            // save pet (hunter pet level and experience and all type pets health/mana).
            Pet pet = GetPet();
            if (pet != null)
                pet.SavePetToDB(PetSaveMode.AsCurrent);
        }
        void DeleteSpellFromAllPlayers(uint spellId)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_SPELL_SPELLS);
            stmt.AddValue(0, spellId);
            DB.Characters.Execute(stmt);
        }

        public static uint GetGuildIdFromDB(ObjectGuid guid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_MEMBER);
            stmt.AddValue(0, guid.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
                return result.Read<uint>(0);

            return 0;
        }
        public static byte GetRankFromDB(ObjectGuid guid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_MEMBER);
            stmt.AddValue(0, guid.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
                return result.Read<byte>(1);

            return 0;
        }
        public static uint GetZoneIdFromDB(ObjectGuid guid)
        {
            ulong guidLow = guid.GetCounter();
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_ZONE);
            stmt.AddValue(0, guidLow);
            SQLResult result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
                return 0;

            uint zone = result.Read<ushort>(0);

            if (zone == 0)
            {
                // stored zone is zero, use generic and slow zone detection
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION_XYZ);
                stmt.AddValue(0, guidLow);
                result = DB.Characters.Query(stmt);

                if (result.IsEmpty())
                    return 0;

                uint map = result.Read<ushort>(0);
                float posx = result.Read<float>(1);
                float posy = result.Read<float>(2);
                float posz = result.Read<float>(3);

                if (!CliDB.MapStorage.ContainsKey(map))
                    return 0;

                zone = Global.MapMgr.GetZoneId(PhasingHandler.EmptyPhaseShift, map, posx, posy, posz);

                if (zone > 0)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ZONE);

                    stmt.AddValue(0, zone);
                    stmt.AddValue(1, guidLow);

                    DB.Characters.Execute(stmt);
                }
            }

            return zone;
        }
        public static uint GetLevelFromDB(ObjectGuid guid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_LEVEL);
            stmt.AddValue(0, guid.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
                return 0;

            return result.Read<byte>(0);
        }
        public static void RemovePetitionsAndSigns(ObjectGuid guid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PETITION_SIG_BY_GUID);
            stmt.AddValue(0, guid.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                do                                                  // this part effectively does nothing, since the deletion / modification only takes place _after_ the PetitionSelect. Though I don't know if the result remains intact if I execute the delete Select beforehand.
                {                                                   // and SendPetitionSelectOpcode reads data from the DB
                    ObjectGuid ownerguid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
                    ObjectGuid petitionguid = ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(1));

                    // send update if charter owner in game
                    Player owner = Global.ObjAccessor.FindPlayer(ownerguid);
                    if (owner != null)
                        owner.GetSession().SendPetitionQuery(petitionguid);
                } while (result.NextRow());

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_PETITION_SIGNATURES);
                stmt.AddValue(0, guid.GetCounter());
                DB.Characters.Execute(stmt);

            }

            SQLTransaction trans = new SQLTransaction();
            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PETITION_BY_OWNER);
            stmt.AddValue(0, guid.GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PETITION_SIGNATURE_BY_OWNER);
            stmt.AddValue(0, guid.GetCounter());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }
        public static void DeleteFromDB(ObjectGuid playerGuid, uint accountId, bool updateRealmChars = true, bool deleteFinally = false)
        {
            // Avoid realm-update for non-existing account
            if (accountId == 0)
                updateRealmChars = false;

            // Convert guid to low GUID for CharacterNameData, but also other methods on success
            ulong guid = playerGuid.GetCounter();
            CharDeleteMethod charDelete_method = (CharDeleteMethod)WorldConfig.GetIntValue(WorldCfg.ChardeleteMethod);

            CharacterInfo characterInfo;
            if (deleteFinally)
                charDelete_method = CharDeleteMethod.Remove;
            else if ((characterInfo = Global.WorldMgr.GetCharacterInfo(playerGuid)) != null)    // To avoid a Select, we select loaded data. If it doesn't exist, return.
            {
                // Define the required variables
                uint charDeleteMinLvl;

                if (characterInfo.ClassID == Class.Deathknight)
                    charDeleteMinLvl = WorldConfig.GetUIntValue(WorldCfg.ChardeleteDeathKnightMinLevel);
                else if (characterInfo.ClassID == Class.DemonHunter)
                    charDeleteMinLvl = WorldConfig.GetUIntValue(WorldCfg.ChardeleteDemonHunterMinLevel);
                else
                    charDeleteMinLvl = WorldConfig.GetUIntValue(WorldCfg.ChardeleteMinLevel);

                // if we want to finalize the character removal or the character does not meet the level requirement of either heroic or non-heroic settings,
                // we set it to mode CHAR_DELETE_REMOVE
                if (characterInfo.Level < charDeleteMinLvl)
                    charDelete_method = CharDeleteMethod.Remove;
            }

            SQLTransaction trans = new SQLTransaction();
            uint guildId = GetGuildIdFromDB(playerGuid);
            if (guildId != 0)
            {
                Guild guild = Global.GuildMgr.GetGuildById(guildId);
                if (guild)
                    guild.DeleteMember(trans, playerGuid, false, false, true);
            }

            // remove from arena teams
            LeaveAllArenaTeams(playerGuid);

            // the player was uninvited already on logout so just remove from group
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
            stmt.AddValue(0, guid);
            SQLResult resultGroup = DB.Characters.Query(stmt);

            if (!resultGroup.IsEmpty())
            {
                Group group = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));
                if (group)
                    RemoveFromGroup(group, playerGuid);
            }

            // Remove signs from petitions (also remove petitions if owner);
            RemovePetitionsAndSigns(playerGuid);

            switch (charDelete_method)
            {
                // Completely remove from the database
                case CharDeleteMethod.Remove:
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_COD_ITEM_MAIL);
                        stmt.AddValue(0, guid);
                        SQLResult resultMail = DB.Characters.Query(stmt);
                        if (!resultMail.IsEmpty())
                        {
                            do
                            {
                                uint mail_id = resultMail.Read<uint>(0);
                                MailMessageType mailType = (MailMessageType)resultMail.Read<byte>(1);
                                ushort mailTemplateId = resultMail.Read<ushort>(2);
                                uint sender = resultMail.Read<uint>(3);
                                string subject = resultMail.Read<string>(4);
                                string body = resultMail.Read<string>(5);
                                ulong money = resultMail.Read<ulong>(6);
                                bool has_items = resultMail.Read<bool>(7);

                                // We can return mail now
                                // So firstly delete the old one
                                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
                                stmt.AddValue(0, mail_id);
                                trans.Append(stmt);

                                // Mail is not from player
                                if (mailType != MailMessageType.Normal)
                                {
                                    if (has_items)
                                    {
                                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                                        stmt.AddValue(0, mail_id);
                                        trans.Append(stmt);
                                    }
                                    continue;
                                }

                                MailDraft draft = new MailDraft(subject, body);
                                if (mailTemplateId != 0)
                                    draft = new MailDraft(mailTemplateId, false);    // items are already included

                                if (has_items)
                                {
                                    // Data needs to be at first place for Item.LoadFromDB
                                    stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
                                    stmt.AddValue(0, mail_id);
                                    SQLResult resultItems = DB.Characters.Query(stmt);
                                    if (!resultItems.IsEmpty())
                                    {
                                        do
                                        {
                                            ulong itemGuidLow = resultItems.Read<ulong>(0);
                                            uint itemEntry = resultItems.Read<uint>(1);

                                            ItemTemplate itemProto = Global.ObjectMgr.GetItemTemplate(itemEntry);
                                            if (itemProto == null)
                                            {
                                                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
                                                stmt.AddValue(0, itemGuidLow);
                                                trans.Append(stmt);
                                                continue;
                                            }

                                            Item item = Bag.NewItemOrBag(itemProto);
                                            if (!item.LoadFromDB(itemGuidLow, ObjectGuid.Create(HighGuid.Player, guid), resultItems.GetFields(), itemEntry))
                                            {
                                                item.FSetState(ItemUpdateState.Removed);
                                                item.SaveToDB(trans);              // it also deletes item object!
                                                continue;
                                            }

                                            draft.AddItem(item);
                                        }
                                        while (resultItems.NextRow());
                                    }
                                }

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                                stmt.AddValue(0, mail_id);
                                trans.Append(stmt);

                                uint pl_account = ObjectManager.GetPlayerAccountIdByGUID(ObjectGuid.Create(HighGuid.Player, guid));

                                draft.AddMoney(money).SendReturnToSender(pl_account, guid, sender, trans);
                            }
                            while (resultMail.NextRow());
                        }

                        // Unsummon and delete for pets in world is not required: player deleted from CLI or character list with not loaded pet.
                        // NOW we can finally clear other DB data related to character
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PETS);
                        stmt.AddValue(0, guid);
                        SQLResult resultPets = DB.Characters.Query(stmt);

                        if (!resultPets.IsEmpty())
                        {
                            do
                            {
                                uint petguidlow = resultPets.Read<uint>(0);
                                Pet.DeleteFromDB(petguidlow);
                            } while (resultPets.NextRow());
                        }

                        // Delete char from social list of online chars
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_SOCIAL);
                        stmt.AddValue(0, guid);
                        SQLResult resultFriends = DB.Characters.Query(stmt);

                        if (!resultFriends.IsEmpty())
                        {
                            do
                            {
                                Player playerFriend = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, resultFriends.Read<ulong>(0)));
                                if (playerFriend)
                                {
                                    playerFriend.GetSocial().RemoveFromSocialList(playerGuid, SocialFlag.All);
                                    Global.SocialMgr.SendFriendStatus(playerFriend, FriendsResult.Removed, playerGuid);
                                }
                            } while (resultFriends.NextRow());
                        }

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_ACCOUNT_DATA);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_ARENA_STATS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_EFFECT);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_BGDATA);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_BATTLEGROUND_RANDOM);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_CUF_PROFILES);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_CURRENCY);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_GIFT);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INSTANCE);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_REPUTATION);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_COOLDOWNS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_FRIEND);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_GUID);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEMS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENTS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_PROGRESS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_EQUIPMENTSETS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRANSMOG_OUTFITS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOG_BY_PLAYER);
                        stmt.AddValue(0, guid);
                        stmt.AddValue(1, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG_BY_PLAYER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_GLYPHS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TALENT);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SKILLS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_STATS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_CHAR_GUID);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_FISHINGSTEPS);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        Corpse.DeleteFromDB(playerGuid, trans);

                        Garrison.DeleteFromDB(guid, trans);

                        Global.WorldMgr.DeleteCharacterInfo(playerGuid);
                        break;
                    }
                // The character gets unlinked from the account, the name gets freed up and appears as deleted ingame
                case CharDeleteMethod.Unlink:
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_DELETE_INFO);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        Global.WorldMgr.UpdateCharacterInfoDeleted(playerGuid, true);
                        break;
                    }
                default:
                    Log.outError(LogFilter.Player, "Player:DeleteFromDB: Unsupported delete method: {0}.", charDelete_method);

                    if (trans.commands.Count > 0)
                        DB.Characters.CommitTransaction(trans);
                    return;
            }

            DB.Characters.CommitTransaction(trans);

            if (updateRealmChars)
                Global.WorldMgr.UpdateRealmCharCount(accountId);
        }

        public static void DeleteOldCharacters()
        {
            int keepDays = WorldConfig.GetIntValue(WorldCfg.ChardeleteKeepDays);
            if (keepDays == 0)
                return;

            DeleteOldCharacters(keepDays);
        }

        public static void DeleteOldCharacters(int keepDays)
        {
            Log.outInfo(LogFilter.Player, "Player:DeleteOldChars: Deleting all characters which have been deleted {0} days before...", keepDays);

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_OLD_CHARS);
            stmt.AddValue(0, (uint)(Time.UnixTime - keepDays * Time.Day));
            SQLResult result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                int count = 0;
                do
                {
                    DeleteFromDB(ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0)), result.Read<uint>(1), true, true);
                    count++;
                }
                while (result.NextRow());
                Log.outDebug(LogFilter.Player, "Player:DeleteOldChars: Deleted {0} character(s)", count);
            }
        }

        public static void SavePositionInDB(WorldLocation loc, uint zoneId, ObjectGuid guid, SQLTransaction trans = null)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_POSITION);
            stmt.AddValue(0, loc.GetPositionX());
            stmt.AddValue(1, loc.GetPositionY());
            stmt.AddValue(2, loc.GetPositionZ());
            stmt.AddValue(3, loc.GetOrientation());
            stmt.AddValue(4, (ushort)loc.GetMapId());
            stmt.AddValue(5, zoneId);
            stmt.AddValue(6, guid.GetCounter());

            DB.Characters.ExecuteOrAppend(trans, stmt);
        }
        public static bool LoadPositionFromDB(out WorldLocation loc, out bool inFlight, ObjectGuid guid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION);
            stmt.AddValue(0, guid.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            loc = new WorldLocation();
            inFlight = false;

            if (result.IsEmpty())
                return false;

            loc.posX = result.Read<float>(0);
            loc.posY = result.Read<float>(1);
            loc.posZ = result.Read<float>(2);
            loc.Orientation = result.Read<float>(3);
            loc.SetMapId(result.Read<ushort>(4));
            inFlight = !string.IsNullOrEmpty(result.Read<string>(5));

            return true;
        }
    }

    public enum CharDeleteMethod
    {
        Remove = 0,                      // Completely remove from the database
        Unlink = 1                       // The character gets unlinked from the account,
        // the name gets freed up and appears as deleted ingame
    }
}
