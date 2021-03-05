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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Arenas;
using Game.BattleGrounds;
using Game.Cache;
using Game.DataStorage;
using Game.Garrisons;
using Game.Groups;
using Game.Guilds;
using Game.Mails;
using Game.Maps;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Entities
{
    public partial class Player
    {
        private void _LoadInventory(SQLResult result, SQLResult artifactsResult, SQLResult azeriteResult, SQLResult azeriteItemMilestonePowersResult, SQLResult azeriteItemUnlockedEssencesResult, SQLResult azeriteEmpoweredItemResult, uint timeDiff)
        {
            var additionalData = new Dictionary<ulong, ItemAdditionalLoadInfo>();
            ItemAdditionalLoadInfo.Init(additionalData, artifactsResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

            if (!result.IsEmpty())
            {
                var zoneId = GetZoneId();
                var bagMap = new Dictionary<ObjectGuid, Bag>();                               // fast guid lookup for bags
                var invalidBagMap = new Dictionary<ObjectGuid, Item>();                       // fast guid lookup for bags
                var problematicItems = new Queue<Item>();
                var trans = new SQLTransaction();

                // Prevent items from being added to the queue while loading
                m_itemUpdateQueueBlocked = true;
                do
                {
                    var item = _LoadItem(trans, zoneId, timeDiff, result.GetFields());
                    if (item != null)
                    {
                        var addionalData = additionalData.LookupByKey(item.GetGUID().GetCounter());
                        if (addionalData != null)
                        {
                            if (item.GetTemplate().GetArtifactID() != 0 && addionalData.Artifact != null)
                                item.LoadArtifactData(this, addionalData.Artifact.Xp, addionalData.Artifact.ArtifactAppearanceId, addionalData.Artifact.ArtifactTierId, addionalData.Artifact.ArtifactPowers);

                            if (addionalData.AzeriteItem != null)
                            {
                                var azeriteItem = item.ToAzeriteItem();
                                if (azeriteItem != null)
                                    azeriteItem.LoadAzeriteItemData(this, addionalData.AzeriteItem);
                            }

                            if (addionalData.AzeriteEmpoweredItem != null)
                            {
                                var azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();
                                if (azeriteEmpoweredItem != null)
                                    azeriteEmpoweredItem.LoadAzeriteEmpoweredItemData(this, addionalData.AzeriteEmpoweredItem);
                            }
                        }


                        var counter = result.Read<ulong>(43);
                        var bagGuid = counter != 0 ? ObjectGuid.Create(HighGuid.Item, counter) : ObjectGuid.Empty;
                        var slot = result.Read<byte>(44);

                        GetSession().GetCollectionMgr().CheckHeirloomUpgrades(item);
                        GetSession().GetCollectionMgr().AddItemAppearance(item);

                        var err = InventoryResult.Ok;
                        if (item.HasItemFlag(ItemFieldFlags.Child))
                        {
                            var parent = GetItemByGuid(item.GetCreator());
                            if (parent)
                            {
                                parent.SetChildItem(item.GetGUID());
                                item.CopyArtifactDataFromParent(parent);
                            }
                            else
                            {
                                Log.outError(LogFilter.Player, $"Player._LoadInventory: Player '{GetName()}' ({GetGUID().ToString()}) has child item ({item.GetGUID()}, entry: {item.GetEntry()}) which can't be loaded into inventory because parent item was not found (Bag {bagGuid}, slot: {slot}). Item will be sent by mail.");
                                item.DeleteFromInventoryDB(trans);
                                problematicItems.Enqueue(item);
                                continue;
                            }
                        }

                        // Item is not in bag
                        if (bagGuid.IsEmpty())
                        {
                            item.SetContainer(null);
                            item.SetSlot(slot);

                            if (IsInventoryPos(InventorySlots.Bag0, slot))
                            {
                                var dest = new List<ItemPosCount>();
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
                                var dest = new List<ItemPosCount>();
                                err = CanBankItem(InventorySlots.Bag0, slot, dest, item, false, false);
                                if (err == InventoryResult.Ok)
                                    item = BankItem(dest, item, true);
                            }

                            // Remember bags that may contain items in them
                            if (err == InventoryResult.Ok)
                            {
                                if (IsBagPos(item.GetPos()))
                                {
                                    var pBag = item.ToBag();
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
                                var dest = new List<ItemPosCount>();
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
                    var subject = Global.ObjectMgr.GetCypherString(CypherStrings.NotEquippedItem);
                    var draft = new MailDraft(subject, "There were problems with equipping item(s).");
                    for (var i = 0; problematicItems.Count != 0 && i < SharedConst.MaxMailItems; ++i)
                    {
                        draft.AddItem(problematicItems.Dequeue());
                    }
                    draft.SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied);
                }

                DB.Characters.CommitTransaction(trans);
            }

            _ApplyAllItemMods();
            // Apply all azerite item mods, azerite empowered item mods will get applied through its spell script
            ApplyAllAzeriteItemMods(true);
        }

        private Item _LoadItem(SQLTransaction trans, uint zoneId, uint timeDiff, SQLFields fields)
        {
            var itemGuid = fields.Read<ulong>(0);
            var itemEntry = fields.Read<uint>(1);
            var proto = Global.ObjectMgr.GetItemTemplate(itemEntry);
            
            if (proto != null)
            {
                var remove = false;
                var item = Bag.NewItemOrBag(proto);
                if (item.LoadFromDB(itemGuid, GetGUID(), fields, itemEntry))
                {
                    PreparedStatement stmt;

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
                    if (item.HasItemFlag(ItemFieldFlags.Refundable))
                    {
                        if (item.GetPlayedTime() > (2 * Time.Hour))
                        {
                            Log.outDebug(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: {1}) has item (GUID: {2}, entry: {3}) with expired refund time ({4}). Deleting refund data and removing " +
                                "efundable flag.", GetGUID().ToString(), GetName(), item.GetGUID().ToString(), item.GetEntry(), item.GetPlayedTime());

                            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_REFUND_INSTANCE);
                            stmt.AddValue(0, item.GetGUID().ToString());
                            trans.Append(stmt);

                            item.RemoveItemFlag(ItemFieldFlags.Refundable);
                        }
                        else
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_REFUNDS);
                            stmt.AddValue(0, item.GetGUID().GetCounter());
                            stmt.AddValue(1, GetGUID().GetCounter());
                            var result = DB.Characters.Query(stmt);
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
                                item.RemoveItemFlag(ItemFieldFlags.Refundable);
                            }
                        }
                    }
                    else if (item.HasItemFlag(ItemFieldFlags.BopTradeable))
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_BOP_TRADE);
                        stmt.AddValue(0, item.GetGUID().ToString());
                        var result = DB.Characters.Query(stmt);
                        if (!result.IsEmpty())
                        {
                            var strGUID = result.Read<string>(0);
                            var GUIDlist = new StringArray(strGUID, ' ');
                            var looters = new List<ObjectGuid>();
                            for (var i = 0; i < GUIDlist.Length; ++i)
                            {
                                if (ulong.TryParse(GUIDlist[i], out var guid))
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
                            item.RemoveItemFlag(ItemFieldFlags.BopTradeable);
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
                AzeriteItem.DeleteFromDB(trans, itemGuid);
                AzeriteEmpoweredItem.DeleteFromDB(trans, itemGuid);
            }
            
            return null;
        }

        private void _LoadSkills(SQLResult result)
        {
            var count = 0u;
            var loadedSkillValues = new Dictionary<uint, uint>();
            if (!result.IsEmpty())
            {
                do
                {
                    if (mSkillStatus.Count >= SkillConst.MaxPlayerSkills)                      // client limit
                    {
                        Log.outError(LogFilter.Player, $"Player::_LoadSkills: Player '{GetName()}' ({GetGUID()}) has more than {SkillConst.MaxPlayerSkills} skills.");
                        break;
                    }

                    var skill = result.Read<ushort>(0);
                    var value = result.Read<ushort>(1);
                    var max = result.Read<ushort>(2);

                    var rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skill, GetRace(), GetClass());
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

                    if (!mSkillStatus.ContainsKey(skill))
                        mSkillStatus.Add(skill, new SkillStatusData((uint)mSkillStatus.Count, SkillState.Unchanged));

                    var skillStatusData = mSkillStatus[skill];
                    var step = (ushort)0u;

                    var skillLine = CliDB.SkillLineStorage.LookupByKey(rcEntry.SkillID);
                    if (skillLine != null)
                    {
                        if (skillLine.CategoryID == SkillCategory.Secondary)
                            step = (ushort)(max / 75);

                        if (skillLine.CategoryID == SkillCategory.Profession)
                        {
                            step = (ushort)(max / 75);

                            if (skillLine.ParentSkillLineID != 0 && skillLine.ParentTierIndex != 0)
                            {
                                var professionSlot = FindProfessionSlotFor(skill);
                                if (professionSlot != -1)
                                    SetUpdateFieldValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.ProfessionSkillLine, (int)professionSlot), skill);
                            }
                        }
                    }


                    SetSkillLineId(skillStatusData.Pos, skill);
                    SetSkillStep(skillStatusData.Pos, step);
                    SetSkillRank(skillStatusData.Pos, value);
                    SetSkillStartingRank(skillStatusData.Pos, 1);
                    SetSkillMaxRank(skillStatusData.Pos, max);
                    SetSkillTempBonus(skillStatusData.Pos, 0);
                    SetSkillPermBonus(skillStatusData.Pos, 0);

                    loadedSkillValues[skill] = value;
                }
                while (result.NextRow());
            }
            
            // Learn skill rewarded spells after all skills have been loaded to prevent learning a skill from them before its loaded with proper value from DB
            foreach (var skill in loadedSkillValues)
            {
                LearnSkillRewardedSpells(skill.Key, skill.Value);
                
                var childSkillLines = Global.DB2Mgr.GetSkillLinesForParentSkill(skill.Key);
                if (childSkillLines != null)
                {
                    foreach (var childItr in childSkillLines)
                    { 
                        if (mSkillStatus.Count >= SkillConst.MaxPlayerSkills)
                            break;

                        if (!mSkillStatus.ContainsKey(childItr.Id))
                        {
                            SetSkillLineId(count, (ushort)childItr.Id);
                            SetSkillStartingRank(count, 1);
                            mSkillStatus.Add(childItr.Id, new SkillStatusData(count, SkillState.Unchanged));
                        }
                    }
                }
            }

            if (HasSkill(SkillType.FistWeapons))
                SetSkill(SkillType.FistWeapons, 0, GetSkillValue(SkillType.Unarmed), GetMaxSkillValueForLevel());
        }

        private void _LoadSpells(SQLResult result)
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

        private void _LoadAuras(SQLResult auraResult, SQLResult effectResult, uint timediff)
        {
            Log.outDebug(LogFilter.Player, "Loading auras for player {0}", GetGUID().ToString());

            var casterGuid = new ObjectGuid();
            var itemGuid = new ObjectGuid();
            var effectInfo = new Dictionary<AuraKey, AuraLoadEffectInfo>();
            if (!effectResult.IsEmpty())
            {
                do
                {
                    var effectIndex = effectResult.Read<byte>(4);
                    if (effectIndex < SpellConst.MaxEffects)
                    {
                        casterGuid.SetRawValue(effectResult.Read<byte[]>(0));
                        itemGuid.SetRawValue(effectResult.Read<byte[]>(1));

                        var key = new AuraKey(casterGuid, itemGuid, effectResult.Read<uint>(2), effectResult.Read<uint>(3));
                        if (!effectInfo.ContainsKey(key))
                            effectInfo[key] = new AuraLoadEffectInfo();

                        var info = effectInfo[key];
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
                    var key = new AuraKey(casterGuid, itemGuid, auraResult.Read<uint>(2), auraResult.Read<uint>(3));
                    var recalculateMask = auraResult.Read<uint>(4);
                    var difficulty = (Difficulty)auraResult.Read<byte>(5);
                    var stackCount = auraResult.Read<byte>(6);
                    var maxDuration = auraResult.Read<int>(7);
                    var remainTime = auraResult.Read<int>(8);
                    var remainCharges = auraResult.Read<byte>(9);
                    var castItemId = auraResult.Read<uint>(10);
                    var castItemLevel = auraResult.Read<int>(11);

                    var spellInfo = Global.SpellMgr.GetSpellInfo(key.SpellId, difficulty);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Player, "Unknown aura (spellid {0}), ignore.", key.SpellId);
                        continue;
                    }

                    if (difficulty != Difficulty.None && !CliDB.DifficultyStorage.ContainsKey(difficulty))
                    {
                        Log.outError(LogFilter.Player, $"Player._LoadAuras: Player '{GetName()}' ({GetGUID()}) has an invalid aura difficulty {difficulty} (SpellID: {key.SpellId}), ignoring.");
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

                    var info = effectInfo[key];
                    var castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, GetMapId(), spellInfo.Id, GetMap().GenerateLowGuid(HighGuid.Cast));
                    var aura = Aura.TryCreate(spellInfo, castId, key.EffectMask, this, null, difficulty, info.BaseAmounts, null, casterGuid, itemGuid, castItemId, castItemLevel);
                    
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

        private bool _LoadHomeBind(SQLResult result)
        {
            var info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());
            if (info == null)
            {
                Log.outError(LogFilter.Player, "Player (Name {0}) has incorrect race/class ({1}/{2}) pair. Can't be loaded.", GetName(), GetRace(), GetClass());
                return false;
            }

            var ok = false;
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
                    var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    DB.Characters.Execute(stmt);
                }
            }

            if (!ok)
            {
                homebind = new WorldLocation(info.MapId, info.PositionX, info.PositionY, info.PositionZ, info.Orientation);
                homebindAreaId = info.ZoneId;

                var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PLAYER_HOMEBIND);
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

        private void _LoadCurrency(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                var currencyID = result.Read<ushort>(0);
                var currency = CliDB.CurrencyTypesStorage.LookupByKey(currencyID);
                if (currency == null)
                    continue;

                var cur = new PlayerCurrency();
                cur.state = PlayerCurrencyState.Unchanged;
                cur.Quantity = result.Read<uint>(1);
                cur.WeeklyQuantity = result.Read<uint>(2);
                cur.TrackedQuantity = result.Read<uint>(3);
                cur.Flags = result.Read<byte>(4);

                _currencyStorage.Add(currencyID, cur);
            } while (result.NextRow());
        }

        private void LoadActions(SQLResult result)
        {
            _LoadActions(result);

            SendActionButtons(1);
        }

        private void _LoadActions(SQLResult result)
        {
            m_actionButtons.Clear();
            if (!result.IsEmpty())
            {
                do
                {
                    var button = result.Read<byte>(0);
                    var action = result.Read<uint>(1);
                    var type = result.Read<byte>(2);

                    var ab = AddActionButton(button, action, type);
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

        private void _LoadQuestStatus(SQLResult result)
        {
            ushort slot = 0;
            if (!result.IsEmpty())
            {
                do
                {
                    var questID = result.Read<uint>(0);
                    
                    // used to be new, no delete?
                    var quest = Global.ObjectMgr.GetQuestTemplate(questID);
                    if (quest != null)
                    {
                        // find or create
                        var questStatusData = new QuestStatusData();

                        var questStatus = result.Read<byte>(1);
                        if (questStatus < (byte)QuestStatus.Max)
                            questStatusData.Status = (QuestStatus)questStatus;
                        else
                        {
                            questStatusData.Status = QuestStatus.Incomplete;
                            Log.outError(LogFilter.Player, "Player {0} (GUID: {1}) has invalid quest {2} status ({3}), replaced by QUEST_STATUS_INCOMPLETE(3).",
                                GetName(), GetGUID().ToString(), questID, questStatus);
                        }

                        var questTime = result.Read<long>(2);
                        if (quest.HasSpecialFlag(QuestSpecialFlags.Timed) && !GetQuestRewardStatus(questID))
                        {
                            AddTimedQuest(questID);

                            if (questTime <= GameTime.GetGameTime())
                                questStatusData.Timer = 1;
                            else
                                questStatusData.Timer = (uint)((questTime - GameTime.GetGameTime()) * Time.InMilliseconds);
                        }
                        else
                            questTime = 0;

                        // add to quest log
                        if (slot < SharedConst.MaxQuestLogSize && questStatusData.Status != QuestStatus.None)
                        {
                            SetQuestSlot(slot, questID, (uint)questTime); // cast can't be helped

                            if (questStatusData.Status == QuestStatus.Complete)
                                SetQuestSlotState(slot, QuestSlotStateMask.Complete);
                            else if (questStatusData.Status == QuestStatus.Failed)
                                SetQuestSlotState(slot, QuestSlotStateMask.Fail);

                            ++slot;
                        }

                        // Resize quest objective data to proper size
                        var maxStorageIndex = 0;
                        foreach (var obj in quest.Objectives)
                            if (obj.StorageIndex > maxStorageIndex)
                                maxStorageIndex = obj.StorageIndex;

                        questStatusData.ObjectiveData = new int[maxStorageIndex + 1];

                        m_QuestStatus[questID] = questStatusData;
                        Log.outDebug(LogFilter.ServerLoading, "Quest status is {0} for quest {1} for player (GUID: {2})", questStatusData.Status, questID, GetGUID().ToString());
                    }
                }
                while (result.NextRow());
            }

            // clear quest log tail
            for (var i = slot; i < SharedConst.MaxQuestLogSize; ++i)
                SetQuestSlot(i, 0);
        }

        private void _LoadQuestStatusObjectives(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    var questID = result.Read<uint>(0);

                    var quest = Global.ObjectMgr.GetQuestTemplate(questID);
                    var slot = FindQuestSlot(questID);

                    var questStatusData = m_QuestStatus.LookupByKey(questID);
                    if (questStatusData != null && slot < SharedConst.MaxQuestLogSize && quest != null)
                    {
                        var objectiveIndex = result.Read<byte>(1);

                        var objectiveItr = quest.Objectives.FirstOrDefault(objective => objective.StorageIndex == objectiveIndex);
                        if (objectiveIndex < questStatusData.ObjectiveData.Length && objectiveItr != null)
                        {
                            var data = result.Read<int>(2);
                            questStatusData.ObjectiveData[objectiveIndex] = data;
                            if (!objectiveItr.IsStoringFlag())
                                SetQuestSlotCounter(slot, objectiveIndex, (ushort)data);
                            else if (data != 0)
                                SetQuestSlotObjectiveFlag(slot, (sbyte)objectiveIndex);
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

        private void _LoadQuestStatusRewarded(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    var questID = result.Read<uint>(0);
                    
                    // used to be new, no delete?
                    var quest = Global.ObjectMgr.GetQuestTemplate(questID);
                    if (quest != null)
                    {
                        // learn rewarded spell if unknown
                        LearnQuestRewardedSpells(quest);

                        // set rewarded title if any
                        if (quest.RewardTitleId != 0)
                        {
                            var titleEntry = CliDB.CharTitlesStorage.LookupByKey(quest.RewardTitleId);
                            if (titleEntry != null)
                                SetTitle(titleEntry);
                        }

                        // Skip loading special quests - they are also added to rewarded quests but only once and remain there forever
                        // instead add them separately from load daily/weekly/monthly/seasonal
                        if (!quest.IsDailyOrWeekly() && !quest.IsMonthly() && !quest.IsSeasonal())
                        {
                            var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questID);
                            if (questBit != 0)
                                SetQuestCompletedBit(questBit, true);
                        }

                        for (var i = 0; i < quest.GetRewChoiceItemsCount(); ++i)
                            GetSession().GetCollectionMgr().AddItemAppearance(quest.RewardChoiceItemId[i]);

                        for (var i = 0; i < quest.GetRewItemsCount(); ++i)
                            GetSession().GetCollectionMgr().AddItemAppearance(quest.RewardItemId[i]);

                        var questPackageItems = Global.DB2Mgr.GetQuestPackageItems(quest.PackageID);
                        if (questPackageItems != null)
                        {
                            foreach (var questPackageItem in questPackageItems)
                            {
                                var rewardProto = Global.ObjectMgr.GetItemTemplate(questPackageItem.ItemID);
                                if (rewardProto != null)
                                    if (rewardProto.ItemSpecClassMask.HasAnyFlag(GetClassMask()))
                                        GetSession().GetCollectionMgr().AddItemAppearance(questPackageItem.ItemID);
                            }
                        }

                        if (quest.CanIncreaseRewardedQuestCounters())
                            m_RewardedQuests.Add(questID);
                    }
                }
                while (result.NextRow());
            }
        }

        private void _LoadDailyQuestStatus(SQLResult result)
        {
            m_DFQuests.Clear();

            //QueryResult* result = CharacterDatabase.PQuery("SELECT quest, time FROM character_queststatus_daily WHERE guid = '{0}'");
            if (!result.IsEmpty())
            {
                do
                {
                    var questID = result.Read<uint>(0);
                    var questTemplate = Global.ObjectMgr.GetQuestTemplate(questID);
                    if (questTemplate != null)
                    {
                        if (questTemplate.IsDFQuest())
                        {
                            m_DFQuests.Add(questTemplate.Id);
                            m_lastDailyQuestTime = result.Read<uint>(1);
                            continue;
                        }
                    }

                    // save _any_ from daily quest times (it must be after last reset anyway)
                    m_lastDailyQuestTime = result.Read<uint>(1);

                    var quest = Global.ObjectMgr.GetQuestTemplate(questID);
                    if (quest == null)
                        continue;

                    AddDynamicUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.DailyQuestsCompleted), questID);
                    var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questID);
                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Daily quest ({0}) cooldown for player (GUID: {1})", questID, GetGUID().ToString());
                }
                while (result.NextRow());
            }

            m_DailyQuestChanged = false;
        }

        private void _LoadWeeklyQuestStatus(SQLResult result)
        {
            m_weeklyquests.Clear();

            if (!result.IsEmpty())
            {
                do
                {
                    var questID = result.Read<uint>(0);
                    var questTemplate = Global.ObjectMgr.GetQuestTemplate(questID);
                    if (questTemplate == null)
                        continue;

                    m_weeklyquests.Add(questID);
                    var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questID);
                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Weekly quest {0} cooldown for player (GUID: {1})", questID, GetGUID().ToString());
                }
                while (result.NextRow());
            }

            m_WeeklyQuestChanged = false;
        }

        private void _LoadSeasonalQuestStatus(SQLResult result)
        {
            m_seasonalquests.Clear();

            if (!result.IsEmpty())
            {
                do
                {
                    var questID = result.Read<uint>(0);
                    var eventID = result.Read<uint>(1);
                    var questTemplate = Global.ObjectMgr.GetQuestTemplate(questID);
                    if (questTemplate == null)
                        continue;

                    m_seasonalquests.Add(eventID, questID);
                    var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questID);
                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Seasonal quest {0} cooldown for player (GUID: {1})", questID, GetGUID().ToString());
                }
                while (result.NextRow());
            }

            m_SeasonalQuestChanged = false;
        }

        private void _LoadMonthlyQuestStatus()
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_MONTHLY);
            stmt.AddValue(0, GetGUID().GetCounter());
            var result = DB.Characters.Query(stmt);

            m_monthlyquests.Clear();

            if (!result.IsEmpty())
            {
                do
                {
                    var questID = result.Read<uint>(0);
                    var questTemplate = Global.ObjectMgr.GetQuestTemplate(questID);
                    if (questTemplate == null)
                        continue;

                    m_monthlyquests.Add(questID);
                    var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questID);
                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Monthly quest {0} cooldown for player (GUID: {1})", questID, GetGUID().ToString());
                }
                while (result.NextRow());
            }

            m_MonthlyQuestChanged = false;
        }

        private void _LoadTalents(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    var talent = CliDB.TalentStorage.LookupByKey(result.Read<uint>(0));
                    if (talent != null)
                        AddTalent(talent, result.Read<byte>(1), false);
                }
                while (result.NextRow());
            }
        }

        private void _LoadPvpTalents(SQLResult result)
        {
            // "SELECT talentID0, talentID1, talentID2, talentID3, talentGroup FROM character_pvp_talent WHERE guid = ?"
            if (!result.IsEmpty())
            {
                do
                {
                    for (var slot = (byte)0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
                    {
                        var talent = CliDB.PvpTalentStorage.LookupByKey(result.Read<uint>(slot));
                        if (talent != null)
                            AddPvpTalent(talent, result.Read<byte>(4), slot);
                    }
                }
                while (result.NextRow());
            }
        }

        private void _LoadGlyphs(SQLResult result)
        {
            // SELECT talentGroup, glyphId from character_glyphs WHERE guid = ?
            if (result.IsEmpty())
                return;

            do
            {
                var spec = result.Read<byte>(0);
                if (spec >= PlayerConst.MaxSpecializations || Global.DB2Mgr.GetChrSpecializationByIndex(GetClass(), spec) == null)
                    continue;

                var glyphId = result.Read<ushort>(1);
                if (!CliDB.GlyphPropertiesStorage.ContainsKey(glyphId))
                    continue;

                GetGlyphs(spec).Add(glyphId);

            } while (result.NextRow());
        }

        private void _LoadGlyphAuras()
        {
            foreach (var glyphId in GetGlyphs(GetActiveTalentGroup()))
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
                    if (!CliDB.MapStorage.LookupByKey(_corpseLocation.GetMapId()).Instanceable())
                        AddPlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
                    else
                        RemovePlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
                }
                else
                    ResurrectPlayer(0.5f);
            }

            RemoveAtLoginFlag(AtLoginFlags.Resurrect);
        }

        private void _LoadBoundInstances(SQLResult result)
        {
            m_boundInstances.Clear();

            var group = GetGroup();
            if (!result.IsEmpty())
            {
                do
                {
                    var perm = result.Read<bool>(1);
                    var mapId = result.Read<ushort>(2);
                    var instanceId = result.Read<uint>(0);
                    var difficulty = result.Read<byte>(3);
                    var extendState = (BindExtensionState)result.Read<byte>(4);

                    var resetTime = result.Read<long>(5);
                    // the resettime for normal instances is only saved when the InstanceSave is unloaded
                    // so the value read from the DB may be wrong here but only if the InstanceSave is loaded
                    // and in that case it is not used

                    var entranceId = result.Read<uint>(6);

                    var deleteInstance = false;

                    var mapEntry = CliDB.MapStorage.LookupByKey(mapId);
                    var mapName = mapEntry != null ? mapEntry.MapName[Global.WorldMgr.GetDefaultDbcLocale()] : "Unknown";

                    if (mapEntry == null || !mapEntry.IsDungeon())
                    {
                        Log.outError(LogFilter.Player, "_LoadBoundInstances: player {0}({1}) has bind to not existed or not dungeon map {2} ({3})", GetName(), GetGUID().ToString(), mapId, mapName);
                        deleteInstance = true;
                    }
                    else if (CliDB.DifficultyStorage.HasRecord(difficulty))
                    {
                        Log.outError(LogFilter.Player, "_LoadBoundInstances: player {0}({1}) has bind to not existed difficulty {2} instance for map {3} ({4})", GetName(), GetGUID().ToString(), difficulty, mapId, mapName);
                        deleteInstance = true;
                    }
                    else
                    {
                        var mapDiff = Global.DB2Mgr.GetMapDifficultyData(mapId, (Difficulty)difficulty);
                        if (mapDiff == null)
                        {
                            Log.outError(LogFilter.Player, "_LoadBoundInstances: player {0}({1}) has bind to not existed difficulty {2} instance for map {3} ({4})", GetName(), GetGUID().ToString(), difficulty, mapId, mapName);
                            deleteInstance = true;
                        }
                        else if (!perm && group)
                        {
                            Log.outError(LogFilter.Player, "_LoadBoundInstances: player {0}({1}) is in group {2} but has a non-permanent character bind to map {3} ({4}), {5}, {6}",
                                GetName(), GetGUID().ToString(), group.GetGUID().ToString(), mapId, mapName, instanceId, difficulty);
                            deleteInstance = true;
                        }
                    }

                    if (deleteInstance)
                    {
                        var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INSTANCE_BY_INSTANCE_GUID);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, instanceId);
                        DB.Characters.Execute(stmt);

                        continue;
                    }

                    // since non permanent binds are always solo bind, they can always be reset
                    var save = Global.InstanceSaveMgr.AddInstanceSave(mapId, instanceId, (Difficulty)difficulty, resetTime, entranceId, !perm, true);
                    if (save != null)
                        BindToInstance(save, perm, extendState, true);
                }
                while (result.NextRow());
            }
        }

        private void _LoadVoidStorage(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                // SELECT itemId, itemEntry, slot, creatorGuid, randomBonusListId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs FROM character_void_storage WHERE playerGuid = ?
                var itemId = result.Read<ulong>(0);
                var itemEntry = result.Read<uint>(1);
                var slot = result.Read<byte>(2);
                var creatorGuid = result.Read<ulong>(3) != 0 ? ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(3)) : ObjectGuid.Empty;
                var randomBonusListId = result.Read<uint>(4);
                var fixedScalingLevel = result.Read<uint>(5);
                var artifactKnowledgeLevel = result.Read<uint>(6);
                var context = (ItemContext)result.Read<byte>(7);
                
                var bonusListIDs = new List<uint>();
                var bonusListIdTokens = new StringArray(result.Read<string>(8), ' ');
                for (var i = 0; i < bonusListIdTokens.Length; ++i)
                {
                    if (uint.TryParse(bonusListIdTokens[i], out var id))
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

                _voidStorageItems[slot] = new VoidStorageItem(itemId, itemEntry, creatorGuid, randomBonusListId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs);

                var bonus = new BonusData(new ItemInstance(_voidStorageItems[slot]));
                GetSession().GetCollectionMgr().AddItemAppearance(itemEntry, bonus.AppearanceModID);
            }
            while (result.NextRow());
        }

        private void _LoadMailInit(SQLResult resultUnread, SQLResult resultDelivery)
        {
            if (!resultUnread.IsEmpty())
                unReadMails = (byte)resultUnread.Read<ulong>(0);

            if (!resultDelivery.IsEmpty())
                m_nextMailDelivereTime = resultDelivery.Read<uint>(0);
        }
        
        public void _LoadMail()
        {
            m_mail.Clear();

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL);
            stmt.AddValue(0, GetGUID().GetCounter());
            var result = DB.Characters.Query(stmt);

            var mailById = new Dictionary<uint, Mail>();

            if (!result.IsEmpty())
            {
                do
                {
                    var m = new Mail
                    {
                        messageID = result.Read<uint>(0),
                        messageType = (MailMessageType) result.Read<byte>(1),
                        sender = result.Read<uint>(2),
                        receiver = result.Read<uint>(3),
                        subject = result.Read<string>(4),
                        body = result.Read<string>(5),
                        expire_time = result.Read<uint>(6),
                        deliver_time = result.Read<uint>(7),
                        money = result.Read<ulong>(8),
                        COD = result.Read<ulong>(9),
                        checkMask = (MailCheckMask) result.Read<byte>(10),
                        stationery = (MailStationery) result.Read<byte>(11),
                        mailTemplateId = result.Read<ushort>(12)
                    };

                    if (m.mailTemplateId != 0 && !CliDB.MailTemplateStorage.ContainsKey(m.mailTemplateId))
                    {
                        Log.outError(LogFilter.Player, "Player:_LoadMail - Mail ({0}) have not existed MailTemplateId ({1}), remove at load", m.messageID, m.mailTemplateId);
                        m.mailTemplateId = 0;
                    }

                    m.state = MailState.Unchanged;

                    m_mail.Add(m);
                    mailById[m.messageID] = m;
                }
                while (result.NextRow());
            }

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
            stmt.AddValue(0, GetGUID().GetCounter());
            result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_ARTIFACT);
                stmt.AddValue(0, GetGUID().GetCounter());
                var artifactResult = DB.Characters.Query(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE);
                stmt.AddValue(0, GetGUID().GetCounter());
                var azeriteResult = DB.Characters.Query(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_MILESTONE_POWER);
                stmt.AddValue(0, GetGUID().GetCounter());
                var azeriteItemMilestonePowersResult = DB.Characters.Query(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_UNLOCKED_ESSENCE);
                stmt.AddValue(0, GetGUID().GetCounter());
                var azeriteItemUnlockedEssencesResult = DB.Characters.Query(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_EMPOWERED);
                stmt.AddValue(0, GetGUID().GetCounter());
                var azeriteEmpoweredItemResult = DB.Characters.Query(stmt);

                var additionalData = new Dictionary<ulong, ItemAdditionalLoadInfo>();
                ItemAdditionalLoadInfo.Init(additionalData, artifactResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

                do
                {
                    var mailId = result.Read<uint>(44);
                    _LoadMailedItem(GetGUID(), this, mailId, mailById[mailId], result.GetFields(), additionalData.LookupByKey(result.Read<ulong>(0)));
                }
                while (result.NextRow());
            }

            m_mailsLoaded = true;
        }

        private static Item _LoadMailedItem(ObjectGuid playerGuid, Player player, uint mailId, Mail mail, SQLFields fields, ItemAdditionalLoadInfo addionalData)
        {
            var itemGuid = fields.Read<ulong>(0);
            var itemEntry = fields.Read<uint>(1);

            var proto = Global.ObjectMgr.GetItemTemplate(itemEntry);
            if (proto == null)
            {
                Log.outError(LogFilter.Player, $"Player {(player != null ? player.GetName() : "<unknown>")} ({playerGuid}) has unknown item in mailed items (GUID: {itemGuid} template: {itemEntry}) in mail ({mailId}), deleted.");

                var trans = new SQLTransaction();

                var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_MAIL_ITEM);
                stmt.AddValue(0, itemGuid);
                trans.Append(stmt);

                Item.DeleteFromDB(trans, itemGuid);
                AzeriteItem.DeleteFromDB(trans, itemGuid);
                AzeriteEmpoweredItem.DeleteFromDB(trans, itemGuid);

                DB.Characters.CommitTransaction(trans);
                return null;
            }

            var item = Bag.NewItemOrBag(proto);
            var ownerGuid = fields.Read<ulong>(43) != 0 ? ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(43)) : ObjectGuid.Empty;
            if (!item.LoadFromDB(itemGuid, ownerGuid, fields, itemEntry))
            {
                Log.outError(LogFilter.Player, $"Player._LoadMailedItems: Item (GUID: {itemGuid}) in mail ({mailId}) doesn't exist, deleted from mail.");

                var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM);
                stmt.AddValue(0, itemGuid);
                DB.Characters.Execute(stmt);

                item.FSetState(ItemUpdateState.Removed);

                item.SaveToDB(null);                               // it also deletes item object !
                return null;
            }

            if (addionalData != null)
            {
                if (item.GetTemplate().GetArtifactID() != 0 && addionalData.Artifact != null)
                    item.LoadArtifactData(player, addionalData.Artifact.Xp, addionalData.Artifact.ArtifactAppearanceId,
                        addionalData.Artifact.ArtifactTierId, addionalData.Artifact.ArtifactPowers);

                if (addionalData.AzeriteItem != null)
                {
                    var azeriteItem = item.ToAzeriteItem();
                    if (azeriteItem != null)
                        azeriteItem.LoadAzeriteItemData(player, addionalData.AzeriteItem);
                }

                if (addionalData.AzeriteEmpoweredItem != null)
                {
                    var azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();
                    if (azeriteEmpoweredItem != null)
                        azeriteEmpoweredItem.LoadAzeriteEmpoweredItemData(player, addionalData.AzeriteEmpoweredItem);
                }
            }

            if (mail != null)
                mail.AddItem(itemGuid, itemEntry);

            if (player != null)
                player.AddMItem(item);

            return item;
        }

        private void _LoadDeclinedNames(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            _declinedname = new DeclinedName();
            for (var i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                _declinedname.name[i] = result.Read<string>(i);
        }

        private void _LoadArenaTeamInfo(SQLResult result)
        {
            // arenateamid, played_week, played_season, personal_rating
            ushort[] personalRatingCache = { 0, 0, 0 };

            if (!result.IsEmpty())
            {
                do
                {
                    var arenaTeamId = result.Read<uint>(0);

                    var arenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(arenaTeamId);
                    if (arenaTeam == null)
                    {
                        Log.outError(LogFilter.Player, "Player:_LoadArenaTeamInfo: couldn't load arenateam {0}", arenaTeamId);
                        continue;
                    }

                    var arenaSlot = arenaTeam.GetSlot();

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

        private void _LoadGroup(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                var group = Global.GroupMgr.GetGroupByDbStoreId(result.Read<uint>(0));
                if (group)
                {
                    if (group.IsLeader(GetGUID()))
                        AddPlayerFlag(PlayerFlags.GroupLeader);

                    var subgroup = group.GetMemberGroup(GetGUID());
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
                RemovePlayerFlag(PlayerFlags.GroupLeader);
        }

        private void _LoadInstanceTimeRestrictions(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                _instanceResetTimes.Add(result.Read<uint>(0), result.Read<long>(1));
            } while (result.NextRow());
        }

        private void _LoadEquipmentSets(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                var eqSet = new EquipmentSetInfo
                {
                    Data =
                    {
                        Guid = result.Read<ulong>(0),
                        Type = EquipmentSetInfo.EquipmentSetType.Equipment,
                        SetID = result.Read<byte>(1),
                        SetName = result.Read<string>(2),
                        SetIcon = result.Read<string>(3),
                        IgnoreMask = result.Read<uint>(4),
                        AssignedSpecIndex = result.Read<int>(5)
                    },
                    state = EquipmentSetUpdateState.Unchanged
                };

                for (var i = 0; i < EquipmentSlot.End; ++i)
                {
                    var guid = (ulong)result.Read<uint>(6 + i);
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

        private void _LoadTransmogOutfits(SQLResult result)
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
                var eqSet = new EquipmentSetInfo
                {
                    Data =
                    {
                        Guid = result.Read<ulong>(0),
                        Type = EquipmentSetInfo.EquipmentSetType.Transmog,
                        SetID = result.Read<byte>(1),
                        SetName = result.Read<string>(2),
                        SetIcon = result.Read<string>(3),
                        IgnoreMask = result.Read<uint>(4),
                    },
                    state = EquipmentSetUpdateState.Unchanged
                };

                eqSet.Data.Pieces.Fill(ObjectGuid.Empty);

                for (var i = 0; i < EquipmentSlot.End; ++i)
                    eqSet.Data.Appearances[i] = result.Read<int>(5 + i);

                for (var i = 0; i < eqSet.Data.Enchants.Count; ++i)
                    eqSet.Data.Enchants[i] = result.Read<int>(24 + i);

                if (eqSet.Data.SetID >= ItemConst.MaxEquipmentSetIndex)   // client limit
                    continue;

                _equipmentSets[eqSet.Data.Guid] = eqSet;
            } while (result.NextRow());
        }

        private void _LoadCUFProfiles(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                var id = result.Read<byte>(0);
                var name = result.Read<string>(1);
                var frameHeight = result.Read<ushort>(2);
                var frameWidth = result.Read<ushort>(3);
                var sortBy = result.Read<byte>(4);
                var healthText = result.Read<byte>(5);
                var boolOptions = result.Read<uint>(6);
                var topPoint = result.Read<byte>(7);
                var bottomPoint = result.Read<byte>(8);
                var leftPoint = result.Read<byte>(9);
                var topOffset = result.Read<ushort>(10);
                var bottomOffset = result.Read<ushort>(11);
                var leftOffset = result.Read<ushort>(12);

                if (id > PlayerConst.MaxCUFProfiles)
                {
                    Log.outError(LogFilter.Player, "Player._LoadCUFProfiles - Player (GUID: {0}, name: {1}) has an CUF profile with invalid id (id: {2}), max is {3}.", GetGUID().ToString(), GetName(), id, PlayerConst.MaxCUFProfiles);
                    continue;
                }

                _CUFProfiles[id] = new CUFProfile(name, frameHeight, frameWidth, sortBy, healthText, boolOptions, topPoint, bottomPoint, leftPoint, topOffset, bottomOffset, leftOffset);
            }
            while (result.NextRow());
        }

        private void _LoadRandomBGStatus(SQLResult result)
        {
            if (!result.IsEmpty())
                m_IsBGRandomWinner = true;
        }

        private void _LoadBGData(SQLResult result)
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

        private void _SaveInventory(SQLTransaction trans)
        {
            // force items in buyback slots to new state
            // and remove those that aren't already
            for (var i = InventorySlots.BuyBackStart; i < InventorySlots.BuyBackEnd; ++i)
            {
                var item = m_items[i];
                if (item == null)
                    continue;

                var itemTemplate = item.GetTemplate();
                if (item.GetState() == ItemUpdateState.New)
                {
                    if (itemTemplate != null)
                        if (itemTemplate.GetFlags().HasAnyFlag(ItemFlags.HasLoot))
                            Global.LootItemStorage.RemoveStoredLootForContainer(item.GetGUID().GetCounter());

                    continue;
                }

                item.DeleteFromInventoryDB(trans);
                item.DeleteFromDB(trans);
                m_items[i].FSetState(ItemUpdateState.New);

                if (itemTemplate != null)
                    if (itemTemplate.GetFlags().HasAnyFlag(ItemFlags.HasLoot))
                        Global.LootItemStorage.RemoveStoredLootForContainer(item.GetGUID().GetCounter());
            }

            // Updated played time for refundable items. We don't do this in Player.Update because there's simply no need for it,
            // the client auto counts down in real time after having received the initial played time on the first
            // SMSG_ITEM_REFUND_INFO_RESPONSE packet.
            // Item.UpdatePlayedTime is only called when needed, which is in DB saves, and item refund info requests.
            foreach (var guid in m_refundableItems)
            {
                var item = GetItemByGuid(guid);
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
                var item = ItemUpdateQueue[i];
                if (item == null)
                    continue;

                PreparedStatement stmt;
                var container = item.GetContainer();
                if (item.GetState() != ItemUpdateState.Removed)
                {
                    var test = GetItemByPos(item.GetBagSlot(), item.GetSlot());
                    if (test == null)
                    {
                        var bagTestGUID = 0UL;
                        var test2 = GetItemByPos(InventorySlots.Bag0, item.GetBagSlot());
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

                        RemoveTradeableItem(item);
                        RemoveEnchantmentDurationsReferences(item);
                        RemoveItemDurations(item);

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

        private void _SaveSkills(SQLTransaction trans)
        {
            var skillInfoField = (SkillInfo)m_activePlayerData.Skill;
            foreach (var pair in mSkillStatus.ToList())
            {
                if (pair.Value.State == SkillState.Unchanged)
                    continue;

                var value = skillInfoField.SkillRank[pair.Value.Pos];
                var max = skillInfoField.SkillMaxRank[pair.Value.Pos];

                PreparedStatement stmt;
                switch (pair.Value.State)
                {
                    case SkillState.New:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SKILLS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, (ushort)pair.Key);
                        stmt.AddValue(2, value);
                        stmt.AddValue(3, max);
                        trans.Append(stmt);
                        break;
                    case SkillState.Changed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_SKILLS);
                        stmt.AddValue(0, value);
                        stmt.AddValue(1, max);
                        stmt.AddValue(2, GetGUID().GetCounter());
                        stmt.AddValue(3, (ushort)pair.Key);
                        trans.Append(stmt);
                        break;
                    case SkillState.Deleted:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SKILL_BY_SKILL);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, pair.Key);
                        trans.Append(stmt);
                        break;
                    default:
                        break;
                }
                pair.Value.State = SkillState.Unchanged;
            }
        }

        private void _SaveSpells(SQLTransaction trans)
        {
            foreach (var spell in m_spells.ToList())
            {
                PreparedStatement stmt;
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
                {
                    m_spells.Remove(spell.Key);
                    continue;
                }

                if (spell.Value.State != PlayerSpellState.Temporary)
                    spell.Value.State = PlayerSpellState.Unchanged;
            }
        }

        private void _SaveAuras(SQLTransaction trans)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_EFFECT);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);
            
            foreach (var pair in GetOwnedAuras())
            {
                var aura = pair.Value;
                if (!aura.CanBeSaved())
                    continue;

                var key = aura.GenerateKey(out var recalculateMask);
                var index = 0;
                
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AURA);
                stmt.AddValue(index++, GetGUID().GetCounter());
                stmt.AddValue(index++, key.Caster.GetRawValue());
                stmt.AddValue(index++, key.Item.GetRawValue());
                stmt.AddValue(index++, key.SpellId);
                stmt.AddValue(index++, key.EffectMask);
                stmt.AddValue(index++, recalculateMask);
                stmt.AddValue(index++, aura.GetCastDifficulty());
                stmt.AddValue(index++, aura.GetStackAmount());
                stmt.AddValue(index++, aura.GetMaxDuration());
                stmt.AddValue(index++, aura.GetDuration());
                stmt.AddValue(index++, aura.GetCharges());
                stmt.AddValue(index++, aura.GetCastItemId());
                stmt.AddValue(index, aura.GetCastItemLevel());
                trans.Append(stmt);

                foreach (var effect in aura.GetAuraEffects())
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
                        stmt.AddValue(index, effect.GetBaseAmount());
                        trans.Append(stmt);
                    }
                }
            }
        }

        private void _SaveGlyphs(SQLTransaction trans)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_GLYPHS);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            for (byte spec = 0; spec < PlayerConst.MaxSpecializations; ++spec)
            {
                foreach (var glyphId in GetGlyphs(spec))
                {
                    var index = 0;
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_GLYPHS);
                    stmt.AddValue(index++, GetGUID().GetCounter());
                    stmt.AddValue(index++, spec);
                    stmt.AddValue(index, glyphId);

                    trans.Append(stmt);
                }
            }
        }

        private void _SaveCurrency(SQLTransaction trans)
        {
            foreach (var pair in _currencyStorage)
            {
                var entry = CliDB.CurrencyTypesStorage.LookupByKey(pair.Key);
                if (entry == null) // should never happen
                    continue;

                PreparedStatement stmt;
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

        public static void SavePlayerCustomizations(SQLTransaction trans, ulong guid, List<ChrCustomizationChoice> customizations)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
            stmt.AddValue(0, guid);
            trans.Append(stmt);

            foreach (var customization in customizations)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_CUSTOMIZATION);
                stmt.AddValue(0, guid);
                stmt.AddValue(1, customization.ChrCustomizationOptionID);
                stmt.AddValue(2, customization.ChrCustomizationChoiceID);
                trans.Append(stmt);
            }
        }

        public static void SaveCustomizations(SQLTransaction trans, ulong guid, List<ChrCustomizationChoice> customizations)
        {
            SavePlayerCustomizations(trans, guid, customizations);
        }

        private void _SaveCustomizations(SQLTransaction trans)
        {
            if (!m_customizationsChanged)
                return;

            m_customizationsChanged = false;

            SavePlayerCustomizations(trans, GetGUID().GetCounter(), m_playerData.Customizations);
        }

        private void _SaveActions(SQLTransaction trans)
        {
            foreach (var pair in m_actionButtons.ToList())
            {
                PreparedStatement stmt;
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

        private void _SaveQuestStatus(SQLTransaction trans)
        {
            var isTransaction = trans != null;
            if (!isTransaction)
                trans = new SQLTransaction();

            PreparedStatement stmt = null;
            var keepAbandoned = !Global.WorldMgr.GetCleaningFlags().HasAnyFlag(CleaningFlags.Queststatus);

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
                        stmt.AddValue(3, data.Timer / Time.InMilliseconds + GameTime.GetGameTime());
                        trans.Append(stmt);

                        // Save objectives
                        for (var i = 0; i < data.ObjectiveData.Length; ++i)
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

        private void _SaveDailyQuestStatus(SQLTransaction trans)
        {
            if (!m_DailyQuestChanged)
                return;

            m_DailyQuestChanged = false;

            // save last daily quest time for all quests: we need only mostly reset time for reset check anyway

            // we don't need transactions here.
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY);
            stmt.AddValue(0, GetGUID().GetCounter());

            foreach (var questId in m_activePlayerData.DailyQuestsCompleted)
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

        private void _SaveWeeklyQuestStatus(SQLTransaction trans)
        {
            if (!m_WeeklyQuestChanged || m_weeklyquests.Empty())
                return;

            // we don't need transactions here.
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            foreach (var questID in m_weeklyquests)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_WEEKLY);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, questID);
                trans.Append(stmt);
            }

            m_WeeklyQuestChanged = false;
        }

        private void _SaveSeasonalQuestStatus(SQLTransaction trans)
        {
            if (!m_SeasonalQuestChanged)
                return;

            // we don't need transactions here.
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            m_SeasonalQuestChanged = false;

            if (m_seasonalquests.Empty())
                return;

            foreach (var iter in m_seasonalquests)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_SEASONAL);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, iter.Value);
                stmt.AddValue(2, iter.Key);
                trans.Append(stmt);
            }
        }

        private void _SaveMonthlyQuestStatus(SQLTransaction trans)
        {
            if (!m_MonthlyQuestChanged || m_monthlyquests.Empty())
                return;

            // we don't need transactions here.
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY);
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

        private void _SaveTalents(SQLTransaction trans)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TALENT);
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

            foreach (var m in m_mail)
            {
                PreparedStatement stmt;
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
                        foreach (var mailItemInfo in m.items)
                        {
                            Item.DeleteFromDB(trans, mailItemInfo.item_guid);
                            AzeriteItem.DeleteFromDB(trans, mailItemInfo.item_guid);
                            AzeriteEmpoweredItem.DeleteFromDB(trans, mailItemInfo.item_guid);
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

        private void _SaveStats(SQLTransaction trans)
        {
            // check if stat saving is enabled and if char level is high enough
            if (WorldConfig.GetIntValue(WorldCfg.MinLevelStatSave) == 0 || GetLevel() < WorldConfig.GetIntValue(WorldCfg.MinLevelStatSave))
                return;

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_STATS);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            byte index = 0;
            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_STATS);
            stmt.AddValue(index++, GetGUID().GetCounter());
            stmt.AddValue(index++, GetMaxHealth());

            for (byte i = 0; i < (byte)PowerType.MaxPerClass; ++i)
                stmt.AddValue(index++, GetMaxPower((PowerType)i));

            for (byte i = 0; i < (byte)Stats.Max; ++i)
                stmt.AddValue(index++, GetStat((Stats)i));

            for (var i = 0; i < (int)SpellSchools.Max; ++i)
                stmt.AddValue(index++, GetResistance((SpellSchools)i) + GetBonusResistanceMod((SpellSchools)i));

            stmt.AddValue(index++, (float)m_activePlayerData.BlockPercentage);
            stmt.AddValue(index++, (float)m_activePlayerData.DodgePercentage);
            stmt.AddValue(index++, (float)m_activePlayerData.ParryPercentage);
            stmt.AddValue(index++, (float)m_activePlayerData.CritPercentage);
            stmt.AddValue(index++, (float)m_activePlayerData.RangedCritPercentage);
            stmt.AddValue(index++, (float)m_activePlayerData.SpellCritPercentage);
            stmt.AddValue(index++, (int)m_unitData.AttackPower);
            stmt.AddValue(index++, (int)m_unitData.RangedAttackPower);
            stmt.AddValue(index++, GetBaseSpellPowerBonus());
            stmt.AddValue(index, m_activePlayerData.CombatRatings[(int)CombatRating.ResiliencePlayerDamage]);

            trans.Append(stmt);
        }
        
        public void SaveGoldToDB(SQLTransaction trans)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_MONEY);
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

        private void _SaveEquipmentSets(SQLTransaction trans)
        {
            foreach (var pair in _equipmentSets)
            {
                var eqSet = pair.Value;
                var j = 0;
                
                PreparedStatement stmt;
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

                            foreach (var enchant in eqSet.Data.Enchants)
                                stmt.AddValue(j++, enchant);

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

                            foreach (var enchant in eqSet.Data.Enchants)
                                stmt.AddValue(j++, enchant);
                        }
                        trans.Append(stmt);
                        eqSet.state = EquipmentSetUpdateState.Unchanged;
                        break;
                    case EquipmentSetUpdateState.Deleted:
                        stmt = DB.Characters.GetPreparedStatement(eqSet.Data.Type == EquipmentSetInfo.EquipmentSetType.Equipment ? CharStatements.DEL_EQUIP_SET : CharStatements.DEL_TRANSMOG_OUTFIT);
                        stmt.AddValue(0, eqSet.Data.Guid);
                        trans.Append(stmt);
                        _equipmentSets.Remove(pair.Key);
                        break;
                }
            }
        }

        private void _SaveVoidStorage(SQLTransaction trans)
        {
            for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
            {
                PreparedStatement stmt;
                if (_voidStorageItems[i] == null) // unused item
                {
                    // DELETE FROM void_storage WHERE slot = ? AND playerGuid = ?
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_SLOT);
                    stmt.AddValue(0, i);
                    stmt.AddValue(1, GetGUID().GetCounter());
                }
                else
                {
                    // REPLACE INTO character_void_storage (itemId, playerGuid, itemEntry, slot, creatorGuid, randomPropertyType, randomProperty, upgradeId, fixedScalingLevel, artifactKnowledgeLevel, bonusListIDs) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_VOID_STORAGE_ITEM);
                    stmt.AddValue(0, _voidStorageItems[i].ItemId);
                    stmt.AddValue(1, GetGUID().GetCounter());
                    stmt.AddValue(2, _voidStorageItems[i].ItemEntry);
                    stmt.AddValue(3, i);
                    stmt.AddValue(4, _voidStorageItems[i].CreatorGuid.GetCounter());
                    stmt.AddValue(5, (byte)_voidStorageItems[i].RandomBonusListId);
                    stmt.AddValue(6, _voidStorageItems[i].FixedScalingLevel);
                    stmt.AddValue(7, _voidStorageItems[i].ArtifactKnowledgeLevel);
                    stmt.AddValue(8, (byte)_voidStorageItems[i].Context);

                    var bonusListIDs = new StringBuilder();
                    foreach (var bonusListID in _voidStorageItems[i].BonusListIDs)
                        bonusListIDs.Append($"{bonusListID} ");
                        
                    stmt.AddValue(9, bonusListIDs.ToString());
                }

                trans.Append(stmt);
            }
        }

        private void _SaveCUFProfiles(SQLTransaction trans)
        {
            var lowGuid = GetGUID().GetCounter();
            for (byte i = 0; i < PlayerConst.MaxCUFProfiles; ++i)
            {
                PreparedStatement stmt;
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

        private void _SaveInstanceTimeRestrictions(SQLTransaction trans)
        {
            if (_instanceResetTimes.Empty())
                return;

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ACCOUNT_INSTANCE_LOCK_TIMES);
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

        private void _SaveBGData(SQLTransaction trans)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_BGDATA);
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
            var result = holder.GetResult(PlayerLoginQueryLoad.From);
            if (result.IsEmpty())
            {
                Global.CharacterCacheStorage.GetCharacterNameByGuid(guid, out string cacheName);
                Log.outError(LogFilter.Player, "Player {0} {1} not found in table `characters`, can't load. ", cacheName, guid.ToString());
                return false;
            }

            var fieldIndex = 1;
            
            var accountId = result.Read<uint>(fieldIndex++);
            var name = result.Read<string>(fieldIndex++);
            var race = (Race)result.Read<byte>(fieldIndex++);
            var classID = (Class)result.Read<byte>(fieldIndex++);
            var gender = (Gender)result.Read<byte>(fieldIndex++);
            var level = result.Read<byte>(fieldIndex++);
            var xp = result.Read<uint>(fieldIndex++);
            var money = result.Read<ulong>(fieldIndex++);
            var inventorySlots = result.Read<byte>(fieldIndex++);
            var bankSlots = result.Read<byte>(fieldIndex++);
            var restState = (PlayerRestState)result.Read<byte>(fieldIndex++);
            var playerFlags = (PlayerFlags)result.Read<uint>(fieldIndex++);
            var playerFlagsEx = (PlayerFlagsEx)result.Read<uint>(fieldIndex++);
            var positionX = result.Read<float>(fieldIndex++);
            var positionY = result.Read<float>(fieldIndex++);
            var positionZ = result.Read<float>(fieldIndex++);
            var mapId = result.Read<ushort>(fieldIndex++);
            var orientation = result.Read<float>(fieldIndex++);
            var taxiMask = result.Read<string>(fieldIndex++);
            var cinematic = result.Read<byte>(fieldIndex++);
            var totalTime = result.Read<uint> (fieldIndex++);
            var levelTime = result.Read<uint>(fieldIndex++);
            var restBonus = result.Read<float>(fieldIndex++);
            var logoutTime = result.Read<uint>(fieldIndex++);
            var isLogoutResting = result.Read<byte>(fieldIndex++);
            var resetTalentsCost = result.Read<uint>(fieldIndex++);
            var resetTalentsTime = result.Read<uint>(fieldIndex++);
            var primarySpecialization = result.Read<uint>(fieldIndex++);
            var transportPosX = result.Read<float>(fieldIndex++);
            var transportPosY = result.Read<float>(fieldIndex++);
            var transportPosZ = result.Read<float>(fieldIndex++);
            var transportOrientation = result.Read<float>(fieldIndex++);
            var transportGUID = result.Read<ulong>(fieldIndex++);
            var extraFlags = (PlayerExtraFlags)result.Read<ushort>(fieldIndex++);
            var stableSlots = result.Read<byte>(fieldIndex++);
            var atLogin = result.Read<ushort>(fieldIndex++);
            var zone = result.Read<ushort>(fieldIndex++);
            var online = result.Read<byte>(fieldIndex++);
            var deathExpireTime = result.Read<uint>(fieldIndex++);
            var taxiPath = result.Read<string>(fieldIndex++);
            var dungeonDifficulty = (Difficulty)result.Read<byte>(fieldIndex++);
            var totalKills = result.Read<uint>(fieldIndex++);
            var todayKills = result.Read<ushort>(fieldIndex++);
            var yesterdayKills = result.Read<ushort>(fieldIndex++);
            var chosenTitle = result.Read<uint>(fieldIndex++);
            var watchedFaction = result.Read<uint>(fieldIndex++);
            var drunk = result.Read<byte>(fieldIndex++);
            var health = result.Read<uint>(fieldIndex++);

            var powers = new uint[(int)PowerType.MaxPerClass];
            for (var i = 0; i < powers.Length; ++i)
                powers[i] = result.Read<uint>(fieldIndex++);

            var instanceID = result.Read<uint>(fieldIndex++);
            var activeTalentGroup = result.Read<byte>(fieldIndex++);
            var lootSpecId = result.Read<uint>(fieldIndex++);
            var exploredZones = result.Read<string>(fieldIndex++);
            var knownTitles = result.Read<string>(fieldIndex++);
            var actionBars = result.Read<byte>(fieldIndex++);
            var raidDifficulty = (Difficulty)result.Read<byte>(fieldIndex++);
            var legacyRaidDifficulty = (Difficulty)result.Read<byte>(fieldIndex++);
            var fishingSteps = result.Read<byte>(fieldIndex++);
            var honor = result.Read<uint>(fieldIndex++);
            var honorLevel = result.Read<uint>(fieldIndex++);
            var honorRestState = (PlayerRestState)result.Read<byte>(fieldIndex++);
            var honorRestBonus = result.Read<float>(fieldIndex++);
            var numRespecs = result.Read<byte>(fieldIndex);

            // check if the character's account in the db and the logged in account match.
            // player should be able to load/delete character only with correct account!
            if (accountId != GetSession().GetAccountId())
            {
                Log.outError(LogFilter.Player, "Player (GUID: {0}) loading from wrong account (is: {1}, should be: {2})", GetGUID().ToString(), GetSession().GetAccountId(), accountId);
                return false;
            }

            var banResult = holder.GetResult(PlayerLoginQueryLoad.Banned);
            if (!banResult.IsEmpty())
            {
                Log.outError(LogFilter.Player, "{0} is banned, can't load.", guid.ToString());
                return false;
            }

            _Create(guid);

            SetName(name);

            // check name limitations
            if (ObjectManager.CheckPlayerName(GetName(), GetSession().GetSessionDbcLocale()) != ResponseCodes.CharNameSuccess ||
                (!GetSession().HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(GetName())))
            {
                var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)AtLoginFlags.Rename);
                stmt.AddValue(1, guid.GetCounter());
                DB.Characters.Execute(stmt);
                return false;
            }

            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.WowAccount), GetSession().GetAccountGUID());
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.BnetAccount), GetSession().GetBattlenetAccountGUID());

            if (gender >= Gender.None)
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong gender ({1}), can't be loaded.", guid.ToString(), gender);
                return false;
            }

            SetRace(race);
            SetClass(classID);
            SetGender(gender);

            // check if race/class combination is valid
            var info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());
            if (info == null)
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong race/class ({1}/{2}), can't be loaded.", guid.ToString(), GetRace(), GetClass());
                return false;
            }

            SetLevel(level);
            SetXP(xp);

            var exploredZonesStrings = new StringArray(exploredZones, ' ');
            if (exploredZonesStrings.Length == PlayerConst.ExploredZonesSize * 2)
                for (var i = 0; i < exploredZonesStrings.Length; ++i)
                    SetUpdateFieldFlagValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.ExploredZones, i / 2), (ulong)((long.Parse(exploredZonesStrings[i])) << (32 * (i % 2))));

            var knownTitlesStrings = new StringArray(knownTitles, ' ');
            if ((knownTitlesStrings.Length % 2) == 0)
            {
                for (var i = 0; i < knownTitlesStrings.Length; ++i)
                    SetUpdateFieldFlagValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.KnownTitles, i / 2), (ulong)((long.Parse(knownTitlesStrings[i])) << (32 * (i % 2))));
            }

            SetObjectScale(1.0f);
            SetHoverHeight(1.0f);

            // load achievements before anything else to prevent multiple gains for the same achievement/criteria on every loading (as loading does call UpdateAchievementCriteria)
            m_achievementSys.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Achievements), holder.GetResult(PlayerLoginQueryLoad.CriteriaProgress));
            m_questObjectiveCriteriaMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectivesCriteria), holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectivesCriteriaProgress));

            SetMoney(Math.Min(money, PlayerConst.MaxMoneyAmount));

            var customizations = new List<ChrCustomizationChoice>();
            var customizationsResult = holder.GetResult(PlayerLoginQueryLoad.Customizations);
            if (!customizationsResult.IsEmpty())
            {
                do
                {
                    var choice = new ChrCustomizationChoice
                    {
                        ChrCustomizationOptionID = customizationsResult.Read<uint>(0),
                        ChrCustomizationChoiceID = customizationsResult.Read<uint>(1),
                    };
                    customizations.Add(choice);

                } while (customizationsResult.NextRow());
            }

            SetCustomizations(customizations, false);
            SetInventorySlotCount(inventorySlots);
            SetBankBagSlotCount(bankSlots);
            SetNativeSex(gender);
            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.Inebriation), drunk);
            SetPlayerFlags(playerFlags);
            SetPlayerFlagsEx(playerFlagsEx);
            SetWatchedFactionIndex(watchedFaction);

            if (!GetSession().ValidateAppearance(GetRace(), GetClass(), gender, customizations))
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong Appearance values (Hair/Skin/Color), can't be loaded.", guid.ToString());
                return false;
            }

            // set which actionbars the client has active - DO NOT REMOVE EVER AGAIN (can be changed though, if it does change fieldwise)
            SetMultiActionBars(actionBars);

            m_fishingSteps = fishingSteps;

            InitDisplayIds();

            // cleanup inventory related item value fields (its will be filled correctly in _LoadInventory)
            for (byte slot = EquipmentSlot.Start; slot < EquipmentSlot.End; ++slot)
            {
                SetInvSlot(slot, ObjectGuid.Empty);
                SetVisibleItemSlot(slot, null);

                m_items[slot] = null;
            }

            //Need to call it to initialize m_team (m_team can be calculated from race)
            //Other way is to saves m_team into characters table.
            SetFactionForRace(GetRace());

            // load home bind and check in same time class/race pair, it used later for restore broken positions
            if (!_LoadHomeBind(holder.GetResult(PlayerLoginQueryLoad.HomeBind)))
                return false;

            InitializeSkillFields();
            InitPrimaryProfessions();                               // to max set before any spell loaded

            // init saved position, and fix it later if problematic
            Relocate(positionX, positionY, positionZ, orientation);

            SetDungeonDifficultyID(CheckLoadedDungeonDifficultyID(dungeonDifficulty));
            SetRaidDifficultyID(CheckLoadedRaidDifficultyID(raidDifficulty));
            SetLegacyRaidDifficultyID(CheckLoadedLegacyRaidDifficultyID(legacyRaidDifficulty));

            var RelocateToHomebind = new Action(() => { mapId = (ushort)homebind.GetMapId(); instanceID = 0; Relocate(homebind); });

            _LoadGroup(holder.GetResult(PlayerLoginQueryLoad.Group));

            _LoadCurrency(holder.GetResult(PlayerLoginQueryLoad.Currency));
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.LifetimeHonorableKills), totalKills);
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TodayHonorableKills), todayKills);
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.YesterdayHonorableKills), yesterdayKills);

            _LoadBoundInstances(holder.GetResult(PlayerLoginQueryLoad.BoundInstances));
            _LoadInstanceTimeRestrictions(holder.GetResult(PlayerLoginQueryLoad.InstanceLockTimes));
            _LoadBGData(holder.GetResult(PlayerLoginQueryLoad.BgData));

            GetSession().SetPlayer(this);

            Map map = null;
            var playerAtBattleGround = false;
            var mapEntry = CliDB.MapStorage.LookupByKey(mapId);
            if (mapEntry == null || !IsPositionValid())
            {
                Log.outError(LogFilter.Player, "Player (guidlow {0}) have invalid coordinates (MapId: {1} {2}). Teleport to default race/class locations.", guid.ToString(), mapId, GetPosition());
                RelocateToHomebind();
            }
            else if (mapEntry.IsBattlegroundOrArena())
            {
                Battleground currentBg = null;
                if (m_bgData.bgInstanceID != 0)                                                //saved in Battleground
                    currentBg = Global.BattlegroundMgr.GetBattleground(m_bgData.bgInstanceID, BattlegroundTypeId.None);

                playerAtBattleGround = currentBg != null && currentBg.IsPlayerInBattleground(GetGUID());
                if (playerAtBattleGround && currentBg.GetStatus() != BattlegroundStatus.WaitLeave)
                {
                    map = currentBg.GetBgMap();

                    var bgQueueTypeId = currentBg.GetQueueId();
                    AddBattlegroundQueueId(bgQueueTypeId);

                    m_bgData.bgTypeID = currentBg.GetTypeID();

                    //join player to BattleGround Group
                    currentBg.EventPlayerLoggedIn(this);

                    SetInviteForBattlegroundQueueType(bgQueueTypeId, currentBg.GetInstanceID());
                }
                // Bg was not found - go to Entry Point
                else
                {
                    // leave bg
                    if (playerAtBattleGround)
                    {
                        playerAtBattleGround = false;
                        currentBg.RemovePlayerAtLeave(GetGUID(), false, true);
                    }

                    // Do not look for instance if bg not found
                    var battlegroundEntryPoint = GetBattlegroundEntryPoint();
                    mapId = (ushort)battlegroundEntryPoint.GetMapId();
                    instanceID = 0;

                    if (mapId == 0xFFFFFFFF) // BattlegroundEntry Point not found (???)
                    {
                        Log.outError(LogFilter.Player, "Player (guidlow {0}) was in BG in database, but BG was not found, and entry point was invalid! Teleport to default race/class locations.", guid.ToString());
                        RelocateToHomebind();
                    }
                    else
                        Relocate(battlegroundEntryPoint);

                    // We are not in BG anymore
                    m_bgData.bgInstanceID = 0;
                }
            }
            // currently we do not support transport in bg
            else if (transportGUID != 0)
            {
                var transGUID = ObjectGuid.Create(HighGuid.Transport, transportGUID);

                var transport = Global.ObjAccessor.FindTransport(transGUID) != null 
                    ? Global.ObjAccessor.FindTransport(transGUID) 
                    : null;
                if (transport)
                {
                    var x = transportPosX;
                    var y = transportPosY;
                    var z = transportPosZ;
                    var o = transportOrientation;

                    m_movementInfo.transport.pos = new Position(x, y, z, o);
                    transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);

                    if (!GridDefines.IsValidMapCoord(x, y, z, o) ||
                        // transport size limited
                        Math.Abs(m_movementInfo.transport.pos.posX) > 250.0f ||
                        Math.Abs(m_movementInfo.transport.pos.posY) > 250.0f ||
                        Math.Abs(m_movementInfo.transport.pos.posZ) > 250.0f)
                    {
                        Log.outError(LogFilter.Player, "Player (guidlow {0}) have invalid transport coordinates (X: {1} Y: {2} Z: {3} O: {4}). Teleport to bind location.", guid.ToString(), x, y, z, o);

                        m_movementInfo.transport.Reset();
                        RelocateToHomebind();
                    }
                    else
                    {
                        Relocate(x, y, z, o);
                        mapId = (ushort)transport.GetMapId();

                        transport.AddPassenger(this);
                    }
                }
                else
                {
                    Log.outError(LogFilter.Player, "Player (guidlow {0}) have problems with transport guid ({1}). Teleport to bind location.", guid.ToString(), transportGUID);

                    RelocateToHomebind();
                }
            }
            // currently we do not support taxi in instance
            else if (!taxiPath.IsEmpty())
            {
                instanceID = 0;

                // Not finish taxi flight path
                if (m_bgData.HasTaxiPath())
                {
                    for (var i = 0; i < 2; ++i)
                        m_taxi.AddTaxiDestination(m_bgData.taxiPath[i]);
                }
                
                var nodeID = m_taxi.GetTaxiSource();
                if (!m_taxi.LoadTaxiDestinationsFromString(taxiPath, GetTeam()))
                {
                    // problems with taxi path loading
                    TaxiNodesRecord nodeEntry = null;
                    if (nodeID != 0)
                        nodeEntry = CliDB.TaxiNodesStorage.LookupByKey(nodeID);

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
                
                if (nodeID != 0)
                {
                    // save source node as recall coord to prevent recall and fall from sky
                    var nodeEntry = CliDB.TaxiNodesStorage.LookupByKey(nodeID);
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
                if (instanceID != 0)
                {
                    var save = GetInstanceSave(mapId);
                    if (save != null)
                        if (save.GetInstanceId() != instanceID)
                            instanceID = 0;
                }
            }

            // NOW player must have valid map
            // load the player's map here if it's not already loaded
            if (!map)
                map = Global.MapMgr.CreateMap(mapId, this, instanceID);

            AreaTriggerStruct areaTrigger = null;
            var check = false;

            if (!map)
            {
                areaTrigger = Global.ObjectMgr.GetGoBackTrigger(mapId);
                check = true;
            }
            else if (map.IsDungeon()) // if map is dungeon...
            {
                var denyReason = ((InstanceMap)map).CannotEnter(this);
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
                else if (instanceID != 0 && Global.InstanceSaveMgr.GetInstanceSave(instanceID) == null) // ... and instance is reset then look for entrance.
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
                        mapId = (ushort)areaTrigger.target_mapId;
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
                mapId = (ushort)info.MapId;
                Relocate(info.PositionX, info.PositionY, info.PositionZ, 0.0f);
                map = Global.MapMgr.CreateMap(mapId, this);
                if (!map)
                {
                    Log.outError(LogFilter.Player, "Player {0} {1} Map: {2}, {3}. Invalid default map coordinates or instance couldn't be created.", GetName(), guid.ToString(), mapId, GetPosition());
                    return false;
                }
            }

            SetMap(map);
            UpdatePositionData();

            // now that map position is determined, check instance validity
            if (!CheckInstanceValidity(true) && !IsInstanceLoginGameMasterException())
                m_InstanceValid = false;

            if (playerAtBattleGround)
                map.ToBattlegroundMap().GetBG().AddPlayer(this);

            // randomize first save time in range [CONFIG_INTERVAL_SAVE] around [CONFIG_INTERVAL_SAVE]
            // this must help in case next save after mass player load after server startup
            m_nextSave = RandomHelper.URand(m_nextSave / 2, m_nextSave * 3 / 2);

            SaveRecallPosition();

            var now = Time.UnixTime;
            
            // since last logout (in seconds)
            var time_diff = (uint)(now - logoutTime);

            // set value, including drunk invisibility detection
            // calculate sobering. after 15 minutes logged out, the player will be sober again
            byte newDrunkValue = 0;
            if (time_diff < (uint)GetDrunkValue() * 9)
                newDrunkValue = (byte)(GetDrunkValue() - time_diff / 9);

            SetDrunkValue(newDrunkValue);

            m_cinematic = cinematic;
            m_PlayedTimeTotal = totalTime;
            m_PlayedTimeLevel = levelTime;

            SetTalentResetCost(resetTalentsCost);
            SetTalentResetTime(resetTalentsTime);

            m_taxi.LoadTaxiMask(taxiMask);            // must be before InitTaxiNodesForLevel

            m_stableSlots = stableSlots;
            if (m_stableSlots > 4)
            {
                Log.outError(LogFilter.Player, "Player can have not more {0} stable slots, but have in DB {1}", 4, m_stableSlots);
                m_stableSlots = 4;
            }

            atLoginFlags = (AtLoginFlags)atLogin;

            // Honor system
            // Update Honor kills data
            m_lastHonorUpdateTime = logoutTime;
            UpdateHonorFields();

            m_deathExpireTime = deathExpireTime;
            if (m_deathExpireTime > now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep)
                m_deathExpireTime = now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep - 1;

            RemoveUnitFlag2(UnitFlags2.ForceMove);

            // make sure the unit is considered out of combat for proper loading
            ClearInCombat();

            // reset stats before loading any modifiers
            InitStatsForLevel();
            InitTaxiNodesForLevel();
            InitRunes();

            // rest bonus can only be calculated after InitStatsForLevel()
            _restMgr.LoadRestBonus(RestTypes.XP, restState, restBonus);

            // load skills after InitStatsForLevel because it triggering aura apply also
            _LoadSkills(holder.GetResult(PlayerLoginQueryLoad.Skills));
            UpdateSkillsForLevel();

            SetNumRespecs(numRespecs);
            SetPrimarySpecialization(primarySpecialization);
            SetActiveTalentGroup(activeTalentGroup);
            
            var primarySpec = CliDB.ChrSpecializationStorage.LookupByKey(GetPrimarySpecialization());
            if (primarySpec == null || primarySpec.ClassID != (byte)GetClass() || GetActiveTalentGroup() >= PlayerConst.MaxSpecializations)
                ResetTalentSpecialization();

            var chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(lootSpecId);
            if (chrSpec != null)
            {
                if (chrSpec.ClassID == (uint)GetClass())
                    SetLootSpecId(lootSpecId);
            }

            UpdateDisplayPower();
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
            if (HasPlayerFlag(PlayerFlags.Ghost))
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
            if (GetLevel() < PlayerConst.LevelMinHonor)
                ResetPvpTalents();

            // must be before inventory (some items required reputation check)
            reputationMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Reputation));

            _LoadInventory(holder.GetResult(PlayerLoginQueryLoad.Inventory), holder.GetResult(PlayerLoginQueryLoad.Artifacts), holder.GetResult(PlayerLoginQueryLoad.Azerite),
                        holder.GetResult(PlayerLoginQueryLoad.AzeriteMilestonePowers), holder.GetResult(PlayerLoginQueryLoad.AzeriteUnlockedEssences), holder.GetResult(PlayerLoginQueryLoad.AzeriteEmpowered), time_diff);

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
            if (chosenTitle != 0 && !HasTitle(chosenTitle))
                chosenTitle = 0;

            SetChosenTitle(chosenTitle);

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
            SetHealth(health > GetMaxHealth() ? GetMaxHealth() : health);
            
            var loadedPowers = 0;
            for (PowerType i = 0; i < PowerType.Max; ++i)
            {
                if (Global.DB2Mgr.GetPowerIndexByClass(i, GetClass()) != (int)PowerType.Max)
                {
                    var savedPower = powers[loadedPowers];
                    var maxPower = m_unitData.MaxPower[loadedPowers];
                    SetPower(i, (int)(savedPower > maxPower ? maxPower : savedPower));
                    if (++loadedPowers >= (int)PowerType.MaxPerClass)
                        break;
                }
            }

            for (; loadedPowers < (int)PowerType.MaxPerClass; ++loadedPowers)
                SetUpdateFieldValue(ref m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Power, loadedPowers), 0);

            SetPower(PowerType.LunarPower, 0);
            // Init rune recharge
            if (GetPowerIndex(PowerType.Runes) != (int)PowerType.Max)
            {
                var runes = GetPower(PowerType.Runes);
                var maxRunes = GetMaxPower(PowerType.Runes);
                var runeCooldown = GetRuneBaseCooldown();
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
                        break;             // disable
                    case 1: SetGameMaster(true);
                        break;             // enable
                    case 2:                                         // save state
                        if (extraFlags.HasAnyFlag(PlayerExtraFlags.GMOn))
                            SetGameMaster(true);
                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmVisibleState))
                {
                    default:
                        SetGMVisible(false);
                        break;             // invisible
                    case 1:
                        break;             // visible
                    case 2:                                         // save state
                        if (extraFlags.HasAnyFlag(PlayerExtraFlags.GMInvisible))
                            SetGMVisible(false);
                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmChat))
                {
                    default:
                        break;                 // disable
                    case 1: SetGMChat(true);
                        break;                 // enable
                    case 2:                                         // save state
                        if (extraFlags.HasAnyFlag(PlayerExtraFlags.GMChat))
                            SetGMChat(true);
                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmWhisperingTo))
                {
                    default:
                        break;         // disable
                    case 1: SetAcceptWhispers(true);
                        break;         // enable
                    case 2:                                         // save state
                        if (extraFlags.HasAnyFlag(PlayerExtraFlags.AcceptWhispers))
                            SetAcceptWhispers(true);
                        break;
                }
            }

            // RaF stuff.
            if (GetSession().IsARecruiter() || (GetSession().GetRecruiterId() != 0))
                AddDynamicFlag(UnitDynFlags.ReferAFriend);

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

            _InitHonorLevelOnLoadFromDB(honor, honorLevel);

            _restMgr.LoadRestBonus(RestTypes.Honor, honorRestState, honorRestBonus);
            if (time_diff > 0)
            {
                //speed collect rest bonus in offline, in logout, far from tavern, city (section/in hour)
                var bubble0 = 0.031f;
                //speed collect rest bonus in offline, in logout, in tavern, city (section/in hour)
                var bubble1 = 0.125f;
                var bubble = isLogoutResting > 0
                    ? bubble1 * WorldConfig.GetFloatValue(WorldCfg.RateRestOfflineInTavernOrCity)
                    : bubble0 * WorldConfig.GetFloatValue(WorldCfg.RateRestOfflineInWilderness);

                _restMgr.AddRestBonus(RestTypes.XP, time_diff * _restMgr.CalcExtraPerSec(RestTypes.XP, bubble));
            }

            m_achievementSys.CheckAllAchievementCriteria(this);
            m_questObjectiveCriteriaMgr.CheckAllQuestObjectiveCriteria(this);

            PushQuests();
            return true;
        }

        public void SaveToDB(bool create = false)
        {
            var loginTransaction = new SQLTransaction();
            var characterTransaction = new SQLTransaction();

            SaveToDB(loginTransaction, characterTransaction, create);

            DB.Characters.CommitTransaction(characterTransaction);
            DB.Login.CommitTransaction(loginTransaction);
        }

        public void SaveToDB(SQLTransaction loginTransaction, SQLTransaction characterTransaction, bool create = false)
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

            Log.outDebug(LogFilter.Player, $"Player::SaveToDB: The value of player {GetName()} at save: ");

            if (!create)
                Global.ScriptMgr.OnPlayerSave(this);
                
            byte index = 0;

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_FISHINGSTEPS);
            stmt.AddValue(0, GetGUID().GetCounter());
            characterTransaction.Append(stmt);

            static float FiniteAlways(float f) { return !float.IsInfinity(f) ? f : 0.0f; };

            if (create)
            {
                //! Insert query
                /// @todo: Filter out more redundant fields that can take their default value at player create
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER);
                stmt.AddValue(index++, GetGUID().GetCounter());
                stmt.AddValue(index++, GetSession().GetAccountId());
                stmt.AddValue(index++, GetName());
                stmt.AddValue(index++, (byte)GetRace());
                stmt.AddValue(index++, (byte)GetClass());
                stmt.AddValue(index++, (byte)m_playerData.NativeSex);   // save gender from PLAYER_BYTES_3, UNIT_BYTES_0 changes with every transform effect
                stmt.AddValue(index++, GetLevel());
                stmt.AddValue(index++, (uint)GetNativeSex());
                stmt.AddValue(index++, GetMoney());
                stmt.AddValue(index++, GetInventorySlotCount());
                stmt.AddValue(index++, GetBankBagSlotCount());
                stmt.AddValue(index++, (byte)m_activePlayerData.RestInfo[(int)RestTypes.XP].StateID);
                stmt.AddValue(index++, (uint)m_playerData.PlayerFlags);
                stmt.AddValue(index++, (uint)m_playerData.PlayerFlagsEx);
                stmt.AddValue(index++, (ushort)GetMapId());
                stmt.AddValue(index++, GetInstanceId());
                stmt.AddValue(index++, (byte)GetDungeonDifficultyID());
                stmt.AddValue(index++, (byte)GetRaidDifficultyID());
                stmt.AddValue(index++, (byte)GetLegacyRaidDifficultyID());
                stmt.AddValue(index++, FiniteAlways(GetPositionX()));
                stmt.AddValue(index++, FiniteAlways(GetPositionY()));
                stmt.AddValue(index++, FiniteAlways(GetPositionZ()));
                stmt.AddValue(index++, FiniteAlways(GetOrientation()));
                stmt.AddValue(index++, FiniteAlways(GetTransOffsetX()));
                stmt.AddValue(index++, FiniteAlways(GetTransOffsetY()));
                stmt.AddValue(index++, FiniteAlways(GetTransOffsetZ()));
                stmt.AddValue(index++, FiniteAlways(GetTransOffsetO()));
                
                var transLowGUID = 0UL;
                if (GetTransport())
                    transLowGUID = GetTransport().GetGUID().GetCounter();
                stmt.AddValue(index++, transLowGUID);

                var ss = new StringBuilder();
                for (var i = 0; i < PlayerConst.TaxiMaskSize; ++i)
                    ss.Append(m_taxi.m_taximask[i] + " ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, m_cinematic);
                stmt.AddValue(index++, m_PlayedTimeTotal);
                stmt.AddValue(index++, m_PlayedTimeLevel);
                stmt.AddValue(index++, FiniteAlways(_restMgr.GetRestBonus(RestTypes.XP)));
                stmt.AddValue(index++, (uint)Time.UnixTime);
                stmt.AddValue(index++, (HasPlayerFlag(PlayerFlags.Resting) ? 1 : 0));
                //save, far from tavern/city
                //save, but in tavern/city
                stmt.AddValue(index++, GetTalentResetCost());
                stmt.AddValue(index++, GetTalentResetTime());
                stmt.AddValue(index++, GetPrimarySpecialization());
                stmt.AddValue(index++, (ushort)m_ExtraFlags);
                stmt.AddValue(index++, m_stableSlots);
                stmt.AddValue(index++, (ushort)atLoginFlags);
                stmt.AddValue(index++, GetZoneId());
                stmt.AddValue(index++, (uint)m_deathExpireTime);

                ss.Clear();
                ss.Append(m_taxi.SaveTaxiDestinationsToString());

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, (uint)m_activePlayerData.LifetimeHonorableKills);
                stmt.AddValue(index++, (ushort)m_activePlayerData.TodayHonorableKills);
                stmt.AddValue(index++, (ushort)m_activePlayerData.YesterdayHonorableKills);
                stmt.AddValue(index++, (uint)m_playerData.PlayerTitle);
                stmt.AddValue(index++, (uint)m_activePlayerData.WatchedFactionIndex);
                stmt.AddValue(index++, GetDrunkValue());
                stmt.AddValue(index++, GetHealth());

                var storedPowers = 0;
                for (PowerType powerType = 0; powerType < PowerType.Max; ++powerType)
                {
                    if (GetPowerIndex(powerType) != (int)PowerType.Max)
                    {
                        stmt.AddValue(index++, m_unitData.Power[storedPowers]);
                        if (++storedPowers >= (int)PowerType.MaxPerClass)
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
                    ss.Append($"{(uint)(m_activePlayerData.ExploredZones[i] & 0xFFFFFFFF)} {(uint)((m_activePlayerData.ExploredZones[i] >> 32) & 0xFFFFFFFF)} ");

                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                // cache equipment...
                for (byte i = 0; i < InventorySlots.BagEnd; ++i)
                {
                    var item = GetItemByPos(InventorySlots.Bag0, i);
                    if (item != null)
                    {
                        ss.Append($"{(uint)item.GetTemplate().GetInventoryType()} {item.GetDisplayId(this)} ");
                        var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetVisibleEnchantmentId(this));
                        if (enchant != null)
                            ss.Append(enchant.ItemVisual);
                        else
                            ss.Append('0');

                        ss.Append($" {(uint)CliDB.ItemStorage.LookupByKey(item.GetVisibleEntry(this)).SubclassID} ");
                    }
                    else
                        ss.Append("0 0 0 0 ");
                }

                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                for (var i = 0; i < m_activePlayerData.KnownTitles.Size(); ++i)
                    ss.Append($"{(uint)(m_activePlayerData.KnownTitles[i] & 0xFFFFFFFF)} {(uint)((m_activePlayerData.KnownTitles[i] >> 32) & 0xFFFFFFFF)} ");

                stmt.AddValue(index++, ss.ToString());

                stmt.AddValue(index++, (byte)m_activePlayerData.MultiActionBars);
                stmt.AddValue(index, Global.RealmMgr.GetMinorMajorBugfixVersionForBuild(Global.WorldMgr.GetRealm().Build));
            }
            else
            {
                // Update query
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER);
                stmt.AddValue(index++, GetName());
                stmt.AddValue(index++, (byte)GetRace());
                stmt.AddValue(index++, (byte)GetClass());
                stmt.AddValue(index++, (byte)m_playerData.NativeSex);   // save gender from PLAYER_BYTES_3, UNIT_BYTES_0 changes with every transform effect
                stmt.AddValue(index++, GetLevel());
                stmt.AddValue(index++, (uint)GetNativeSex());
                stmt.AddValue(index++, GetMoney());
                stmt.AddValue(index++, GetInventorySlotCount());
                stmt.AddValue(index++, GetBankBagSlotCount());
                stmt.AddValue(index++, (byte)m_activePlayerData.RestInfo[(int)RestTypes.XP].StateID);
                stmt.AddValue(index++, (uint)m_playerData.PlayerFlags);
                stmt.AddValue(index++, (uint)m_playerData.PlayerFlagsEx);

                if (!IsBeingTeleported())
                {
                    stmt.AddValue(index++, (ushort)GetMapId());
                    stmt.AddValue(index++, GetInstanceId());
                    stmt.AddValue(index++, (byte)GetDungeonDifficultyID());
                    stmt.AddValue(index++, (byte)GetRaidDifficultyID());
                    stmt.AddValue(index++, (byte)GetLegacyRaidDifficultyID());
                    stmt.AddValue(index++, FiniteAlways(GetPositionX()));
                    stmt.AddValue(index++, FiniteAlways(GetPositionY()));
                    stmt.AddValue(index++, FiniteAlways(GetPositionZ()));
                    stmt.AddValue(index++, FiniteAlways(GetOrientation()));
                }
                else
                {
                    stmt.AddValue(index++, (ushort)GetTeleportDest().GetMapId());
                    stmt.AddValue(index++, 0);
                    stmt.AddValue(index++, (byte)GetDungeonDifficultyID());
                    stmt.AddValue(index++, (byte)GetRaidDifficultyID());
                    stmt.AddValue(index++, (byte)GetLegacyRaidDifficultyID());
                    stmt.AddValue(index++, FiniteAlways(GetTeleportDest().GetPositionX()));
                    stmt.AddValue(index++, FiniteAlways(GetTeleportDest().GetPositionY()));
                    stmt.AddValue(index++, FiniteAlways(GetTeleportDest().GetPositionZ()));
                    stmt.AddValue(index++, FiniteAlways(GetTeleportDest().GetOrientation()));
                }

                stmt.AddValue(index++, FiniteAlways(GetTransOffsetX()));
                stmt.AddValue(index++, FiniteAlways(GetTransOffsetY()));
                stmt.AddValue(index++, FiniteAlways(GetTransOffsetZ()));
                stmt.AddValue(index++, FiniteAlways(GetTransOffsetO()));
                
                var transLowGUID = 0UL;
                if (GetTransport())
                    transLowGUID = GetTransport().GetGUID().GetCounter();
                stmt.AddValue(index++, transLowGUID);

                var ss = new StringBuilder();
                for (var i = 0; i < PlayerConst.TaxiMaskSize; ++i)
                    ss.Append(m_taxi.m_taximask[i] + " ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, m_cinematic);
                stmt.AddValue(index++, m_PlayedTimeTotal);
                stmt.AddValue(index++, m_PlayedTimeLevel);
                stmt.AddValue(index++, FiniteAlways(_restMgr.GetRestBonus(RestTypes.XP)));
                stmt.AddValue(index++, (uint)Time.UnixTime);
                stmt.AddValue(index++, (HasPlayerFlag(PlayerFlags.Resting) ? 1 : 0));
                //save, far from tavern/city
                //save, but in tavern/city
                stmt.AddValue(index++, GetTalentResetCost());
                stmt.AddValue(index++, GetTalentResetTime());
                stmt.AddValue(index++, GetNumRespecs());
                stmt.AddValue(index++, GetPrimarySpecialization());
                stmt.AddValue(index++, (ushort)m_ExtraFlags);
                stmt.AddValue(index++, m_stableSlots);
                stmt.AddValue(index++, (ushort)atLoginFlags);
                stmt.AddValue(index++, GetZoneId());
                stmt.AddValue(index++, m_deathExpireTime);

                ss.Clear();
                ss.Append(m_taxi.SaveTaxiDestinationsToString());

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, (uint)m_activePlayerData.LifetimeHonorableKills);
                stmt.AddValue(index++, (ushort)m_activePlayerData.TodayHonorableKills);
                stmt.AddValue(index++, (ushort)m_activePlayerData.YesterdayHonorableKills);
                stmt.AddValue(index++, (uint)m_playerData.PlayerTitle);
                stmt.AddValue(index++, (uint)m_activePlayerData.WatchedFactionIndex);
                stmt.AddValue(index++, GetDrunkValue());
                stmt.AddValue(index++, GetHealth());

                var storedPowers = 0;
                for (PowerType powerType = 0; powerType < PowerType.Max; ++powerType)
                {
                    if (GetPowerIndex(powerType) != (int)PowerType.Max)
                    {
                        stmt.AddValue(index++, m_unitData.Power[storedPowers]);
                        if (++storedPowers >= (int)PowerType.MaxPerClass)
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
                    ss.Append($"{(uint)(m_activePlayerData.ExploredZones[i] & 0xFFFFFFFF)} {(uint)((m_activePlayerData.ExploredZones[i] >> 32) & 0xFFFFFFFF)} ");

                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                // cache equipment...
                for (byte i = 0; i < InventorySlots.BagEnd; ++i)
                {
                    var item = GetItemByPos(InventorySlots.Bag0, i);
                    if (item != null)
                    {
                        ss.Append($"{(uint)item.GetTemplate().GetInventoryType()} {item.GetDisplayId(this)} ");
                        
                        var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetVisibleEnchantmentId(this));
                        if (enchant != null)
                            ss.Append(enchant.ItemVisual);
                        else
                            ss.Append('0');

                        ss.Append($" {(uint)CliDB.ItemStorage.LookupByKey(item.GetVisibleEntry(this)).SubclassID} ");
                    }
                    else
                        ss.Append("0 0 0 0 ");
                }

                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                for (var i = 0; i < m_activePlayerData.KnownTitles.Size(); ++i)
                    ss.Append($"{(uint)(m_activePlayerData.KnownTitles[i] & 0xFFFFFFFF)} {(uint)((m_activePlayerData.KnownTitles[i] >> 32) & 0xFFFFFFFF)} ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, (byte)m_activePlayerData.MultiActionBars);

                stmt.AddValue(index++, IsInWorld && !GetSession().PlayerLogout() ? 1 : 0);
                stmt.AddValue(index++, (uint)m_activePlayerData.Honor);
                stmt.AddValue(index++, GetHonorLevel());
                stmt.AddValue(index++, (byte)m_activePlayerData.RestInfo[(int)RestTypes.Honor].StateID);
                stmt.AddValue(index++, FiniteAlways(_restMgr.GetRestBonus(RestTypes.Honor)));
                stmt.AddValue(index++, Global.WorldMgr.GetRealm().Build);

                // Index
                stmt.AddValue(index, GetGUID().GetCounter());
            }

            characterTransaction.Append(stmt);

            if (m_fishingSteps != 0)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_FISHINGSTEPS);
                index = 0;
                stmt.AddValue(index++, GetGUID().GetCounter());
                stmt.AddValue(index, m_fishingSteps);
                characterTransaction.Append(stmt);
            }

            if (m_mailsUpdated)                                     //save mails only when needed
                _SaveMail(characterTransaction);

            _SaveCustomizations(characterTransaction);
            _SaveBGData(characterTransaction);
            _SaveInventory(characterTransaction);
            _SaveVoidStorage(characterTransaction);
            _SaveQuestStatus(characterTransaction);
            _SaveDailyQuestStatus(characterTransaction);
            _SaveWeeklyQuestStatus(characterTransaction);
            _SaveSeasonalQuestStatus(characterTransaction);
            _SaveMonthlyQuestStatus(characterTransaction);
            _SaveGlyphs(characterTransaction);
            _SaveTalents(characterTransaction);
            _SaveSpells(characterTransaction);
            GetSpellHistory().SaveToDB<Player>(characterTransaction);
            _SaveActions(characterTransaction);
            _SaveAuras(characterTransaction);
            _SaveSkills(characterTransaction);
            m_achievementSys.SaveToDB(characterTransaction);
            reputationMgr.SaveToDB(characterTransaction);
            m_questObjectiveCriteriaMgr.SaveToDB(characterTransaction);
            _SaveEquipmentSets(characterTransaction);
            GetSession().SaveTutorialsData(characterTransaction);                 // changed only while character in game
            _SaveInstanceTimeRestrictions(characterTransaction);
            _SaveCurrency(characterTransaction);
            _SaveCUFProfiles(characterTransaction);
            if (_garrison != null)
                _garrison.SaveToDB(characterTransaction);

            // check if stats should only be saved on logout
            // save stats can be out of transaction
            if (GetSession().IsLogingOut() || !WorldConfig.GetBoolValue(WorldCfg.StatsSaveOnlyOnLogout))
                _SaveStats(characterTransaction);

            // TODO: Move this out
            GetSession().GetCollectionMgr().SaveAccountToys(loginTransaction);
            GetSession().GetBattlePetMgr().SaveToDB(loginTransaction);
            GetSession().GetCollectionMgr().SaveAccountHeirlooms(loginTransaction);
            GetSession().GetCollectionMgr().SaveAccountMounts(loginTransaction);
            GetSession().GetCollectionMgr().SaveAccountItemAppearances(loginTransaction);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BNET_LAST_PLAYER_CHARACTERS);
            stmt.AddValue(0, GetSession().GetAccountId());
            stmt.AddValue(1, Global.WorldMgr.GetRealmId().Region);
            stmt.AddValue(2, Global.WorldMgr.GetRealmId().Site);
            loginTransaction.Append(stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_LAST_PLAYER_CHARACTERS);
            stmt.AddValue(0, GetSession().GetAccountId());
            stmt.AddValue(1, Global.WorldMgr.GetRealmId().Region);
            stmt.AddValue(2, Global.WorldMgr.GetRealmId().Site);
            stmt.AddValue(3, Global.WorldMgr.GetRealmId().Index);
            stmt.AddValue(4, GetName());
            stmt.AddValue(5, GetGUID().GetCounter());
            stmt.AddValue(6, Time.UnixTime);
            loginTransaction.Append(stmt);

            // save pet (hunter pet level and experience and all type pets health/mana).
            var pet = GetPet();
            if (pet)
                pet.SavePetToDB(PetSaveMode.AsCurrent);
        }

        private void DeleteSpellFromAllPlayers(uint spellId)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_SPELL_SPELLS);
            stmt.AddValue(0, spellId);
            DB.Characters.Execute(stmt);
        }

        public static uint GetZoneIdFromDB(ObjectGuid guid)
        {
            var guidLow = guid.GetCounter();
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_ZONE);
            stmt.AddValue(0, guidLow);
            var result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
                return 0;

            var zone = (uint)result.Read<ushort>(0);
            if (zone == 0)
            {
                // stored zone is zero, use generic and slow zone detection
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION_XYZ);
                stmt.AddValue(0, guidLow);
                result = DB.Characters.Query(stmt);

                if (result.IsEmpty())
                    return 0;

                var map = result.Read<ushort>(0);
                var posX = result.Read<float>(1);
                var posY = result.Read<float>(2);
                var posZ = result.Read<float>(3);

                if (!CliDB.MapStorage.ContainsKey(map))
                    return 0;

                zone = Global.MapMgr.GetZoneId(PhasingHandler.EmptyPhaseShift, map, posX, posY, posZ);
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
        
        public static void RemovePetitionsAndSigns(ObjectGuid guid)
        {
            Global.PetitionMgr.RemoveSignaturesBySigner(guid);
            Global.PetitionMgr.RemovePetitionsByOwner(guid);
        }
        
        public static void DeleteFromDB(ObjectGuid playerGuid, uint accountId, bool updateRealmChars = true, bool deleteFinally = false)
        {
            // Avoid realm-update for non-existing account
            if (accountId == 0)
                updateRealmChars = false;

            // Convert guid to low GUID for CharacterNameData, but also other methods on success
            var guid = playerGuid.GetCounter();
            var charDelete_method = (CharDeleteMethod)WorldConfig.GetIntValue(WorldCfg.ChardeleteMethod);
            var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(playerGuid);
            var name = "<Unknown>";
            if (characterInfo != null)
                name = characterInfo.Name;

            if (deleteFinally)
                charDelete_method = CharDeleteMethod.Remove;
            else if (characterInfo != null)    // To avoid a Select, we select loaded data. If it doesn't exist, return.
            {
                // Define the required variables
                var charDeleteMinLvl = 0u;

                if (characterInfo.ClassId == Class.Deathknight)
                    charDeleteMinLvl = WorldConfig.GetUIntValue(WorldCfg.ChardeleteDeathKnightMinLevel);
                else if (characterInfo.ClassId == Class.DemonHunter)
                    charDeleteMinLvl = WorldConfig.GetUIntValue(WorldCfg.ChardeleteDemonHunterMinLevel);
                else
                    charDeleteMinLvl = WorldConfig.GetUIntValue(WorldCfg.ChardeleteMinLevel);

                // if we want to finalize the character removal or the character does not meet the level requirement of either heroic or non-heroic settings,
                // we set it to mode CHAR_DELETE_REMOVE
                if (characterInfo.Level < charDeleteMinLvl)
                    charDelete_method = CharDeleteMethod.Remove;
            }

            var trans = new SQLTransaction();
            var guildId = Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(playerGuid);
            if (guildId != 0)
            {
                var guild = Global.GuildMgr.GetGuildById(guildId);
                if (guild)
                    guild.DeleteMember(trans, playerGuid, false, false, true);
            }

            // remove from arena teams
            LeaveAllArenaTeams(playerGuid);

            // the player was uninvited already on logout so just remove from group
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
            stmt.AddValue(0, guid);
            var resultGroup = DB.Characters.Query(stmt);

            if (!resultGroup.IsEmpty())
            {
                var group = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));
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
                        var resultMail = DB.Characters.Query(stmt);
                        if (!resultMail.IsEmpty())
                        {
                            var itemsByMail = new MultiMap<uint, Item>();

                            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
                            stmt.AddValue(0, guid);
                            var resultItems = DB.Characters.Query(stmt);

                            if (!resultItems.IsEmpty())
                            {
                                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_ARTIFACT);
                                stmt.AddValue(0, guid);
                                var artifactResult = DB.Characters.Query(stmt);

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE);
                                stmt.AddValue(0, guid);
                                var azeriteResult = DB.Characters.Query(stmt);

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_MILESTONE_POWER);
                                stmt.AddValue(0, guid);
                                var azeriteItemMilestonePowersResult = DB.Characters.Query(stmt);

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_UNLOCKED_ESSENCE);
                                stmt.AddValue(0, guid);
                                var azeriteItemUnlockedEssencesResult = DB.Characters.Query(stmt);

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_EMPOWERED);
                                stmt.AddValue(0, guid);
                                var azeriteEmpoweredItemResult = DB.Characters.Query(stmt);

                                var additionalData = new Dictionary<ulong, ItemAdditionalLoadInfo>();
                                ItemAdditionalLoadInfo.Init(additionalData, artifactResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

                                do
                                {
                                    var mailId = resultItems.Read<uint>(44);
                                    var mailItem = _LoadMailedItem(playerGuid, null, mailId, null, resultItems.GetFields(), additionalData.LookupByKey(resultItems.Read<ulong>(0)));
                                    if (mailItem != null)
                                        itemsByMail.Add(mailId, mailItem);

                                } while (resultItems.NextRow());
                            }

                            do
                            {
                                var mailID = resultMail.Read<uint>(0);
                                var mailType = (MailMessageType)resultMail.Read<byte>(1);
                                var mailTemplateId = resultMail.Read<ushort>(2);
                                var sender = resultMail.Read<uint>(3);
                                var subject = resultMail.Read<string>(4);
                                var body = resultMail.Read<string>(5);
                                var money = resultMail.Read<ulong>(6);
                                var hasItems = resultMail.Read<bool>(7);

                                // We can return mail now
                                // So firstly delete the old one
                                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
                                stmt.AddValue(0, mailID);
                                trans.Append(stmt);

                                // Mail is not from player
                                if (mailType != MailMessageType.Normal)
                                {
                                    if (hasItems)
                                    {
                                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                                        stmt.AddValue(0, mailID);
                                        trans.Append(stmt);
                                    }
                                    continue;
                                }

                                var draft = new MailDraft(subject, body);
                                if (mailTemplateId != 0)
                                    draft = new MailDraft(mailTemplateId, false);    // items are already included

                                var itemsList = itemsByMail.LookupByKey(mailID);
                                if (itemsList != null)
                                {
                                    foreach (Item item in itemsList)
                                        draft.AddItem(item);

                                    itemsByMail.Remove(mailID);
                                }

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                                stmt.AddValue(0, mailID);
                                trans.Append(stmt);

                                var pl_account = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(ObjectGuid.Create(HighGuid.Player, guid));

                                draft.AddMoney(money).SendReturnToSender(pl_account, guid, sender, trans);
                            }
                            while (resultMail.NextRow());

                            // Free remaining items
                            foreach (var pair in itemsByMail)
                                pair.Value.Dispose();
                        }

                        // Unsummon and delete for pets in world is not required: player deleted from CLI or character list with not loaded pet.
                        // NOW we can finally clear other DB data related to character
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PETS);
                        stmt.AddValue(0, guid);
                        var resultPets = DB.Characters.Query(stmt);

                        if (!resultPets.IsEmpty())
                        {
                            do
                            {
                                var petGUIDLow = resultPets.Read<uint>(0);
                                Pet.DeleteFromDB(petGUIDLow);
                            } while (resultPets.NextRow());
                        }

                        // Delete char from social list of online chars
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_SOCIAL);
                        stmt.AddValue(0, guid);
                        var resultFriends = DB.Characters.Query(stmt);

                        if (!resultFriends.IsEmpty())
                        {
                            do
                            {
                                var playerFriend = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, resultFriends.Read<ulong>(0)));
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

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
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

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE_BY_OWNER);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED_BY_OWNER);
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

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_FAVORITE_AUCTIONS_BY_CHAR);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        Corpse.DeleteFromDB(playerGuid, trans);

                        Garrison.DeleteFromDB(guid, trans);

                        Global.CharacterCacheStorage.DeleteCharacterCacheEntry(playerGuid, name);
                        break;
                    }
                // The character gets unlinked from the account, the name gets freed up and appears as deleted ingame
                case CharDeleteMethod.Unlink:
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_DELETE_INFO);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        Global.CharacterCacheStorage.UpdateCharacterInfoDeleted(playerGuid, true);
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
            var keepDays = WorldConfig.GetIntValue(WorldCfg.ChardeleteKeepDays);
            if (keepDays == 0)
                return;

            DeleteOldCharacters(keepDays);
        }

        public static void DeleteOldCharacters(int keepDays)
        {
            Log.outInfo(LogFilter.Player, "Player:DeleteOldChars: Deleting all characters which have been deleted {0} days before...", keepDays);

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_OLD_CHARS);
            stmt.AddValue(0, (uint)(Time.UnixTime - keepDays * Time.Day));
            var result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                var count = 0;
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
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_POSITION);
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
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION);
            stmt.AddValue(0, guid.GetCounter());
            var result = DB.Characters.Query(stmt);

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
