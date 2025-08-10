// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.ClientBuild;
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
        void _LoadInventory(SQLResult result, SQLResult artifactsResult, SQLResult azeriteResult, SQLResult azeriteItemMilestonePowersResult, SQLResult azeriteItemUnlockedEssencesResult, SQLResult azeriteEmpoweredItemResult, uint timeDiff)
        {
            Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
            ItemAdditionalLoadInfo.Init(additionalData, artifactsResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

            if (!result.IsEmpty())
            {
                uint zoneId = GetZoneId();
                Dictionary<ObjectGuid, Bag> bagMap = new();                               // fast guid lookup for bags
                Dictionary<ObjectGuid, Item> invalidBagMap = new();                       // fast guid lookup for bags
                Queue<Item> problematicItems = new();
                SQLTransaction trans = new();

                // Prevent items from being added to the queue while loading
                m_itemUpdateQueueBlocked = true;
                do
                {
                    Item item = _LoadItem(trans, zoneId, timeDiff, result.GetFields());
                    if (item != null)
                    {
                        var addionalData = additionalData.LookupByKey(item.GetGUID().GetCounter());
                        if (addionalData != null)
                        {
                            if (item.GetTemplate().GetArtifactID() != 0 && addionalData.Artifact != null)
                                item.LoadArtifactData(this, addionalData.Artifact.Xp, addionalData.Artifact.ArtifactAppearanceId, addionalData.Artifact.ArtifactTierId, addionalData.Artifact.ArtifactPowers);

                            if (addionalData.AzeriteItem != null)
                            {
                                AzeriteItem azeriteItem = item.ToAzeriteItem();
                                if (azeriteItem != null)
                                    azeriteItem.LoadAzeriteItemData(this, addionalData.AzeriteItem);
                            }

                            if (addionalData.AzeriteEmpoweredItem != null)
                            {
                                AzeriteEmpoweredItem azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();
                                if (azeriteEmpoweredItem != null)
                                    azeriteEmpoweredItem.LoadAzeriteEmpoweredItemData(this, addionalData.AzeriteEmpoweredItem);
                            }
                        }


                        ulong dbId = result.Read<ulong>(52);
                        ObjectGuid bagGuid = dbId != 0 ? ObjectGuid.Create(HighGuid.Item, dbId) : ObjectGuid.Empty;
                        byte slot = result.Read<byte>(53);

                        GetSession().GetCollectionMgr().CheckHeirloomUpgrades(item);
                        GetSession().GetCollectionMgr().AddItemAppearance(item);

                        InventoryResult err = InventoryResult.Ok;
                        if (item.HasItemFlag(ItemFieldFlags.Child))
                        {
                            Item parent = GetItemByGuid(item.GetCreator());
                            if (parent != null)
                            {
                                parent.SetChildItem(item.GetGUID());
                                item.CopyArtifactDataFromParent(parent);
                            }
                            else
                            {
                                Log.outError(LogFilter.Player, $"Player._LoadInventory: Player '{GetName()}' ({GetGUID()}) has child item ({item.GetGUID()}, entry: {item.GetEntry()}) which can't be loaded into inventory because parent item was not found (Bag {bagGuid}, slot: {slot}). Item will be sent by mail.");
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
                                List<ItemPosCount> dest = new();
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
                                List<ItemPosCount> dest = new();
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
                                List<ItemPosCount> dest = new();
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
                    MailDraft draft = new(subject, "There were problems with equipping item(s).");
                    for (int i = 0; problematicItems.Count != 0 && i < SharedConst.MaxMailItems; ++i)
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
                    PreparedStatement stmt;

                    // Do not allow to have item limited to another map/zone in alive state
                    if (IsAlive() && item.IsLimitedToAnotherMapOrZone(GetMapId(), zoneId))
                    {
                        Log.outDebug(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: '{1}', map: {2}) has item (GUID: {3}, entry: {4}) limited to another map ({5}). Deleting item.",
                            GetGUID().ToString(), GetName(), GetMapId(), item.GetGUID().ToString(), item.GetEntry(), zoneId);
                        remove = true;
                    }
                    // "Conjured items disappear if you are logged out for more than 15 minutes"
                    else if (timeDiff > 15 * Time.Minute && proto.HasFlag(ItemFlags.Conjured))
                    {
                        Log.outDebug(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: {1}, diff: {2}) has conjured item (GUID: {3}, entry: {4}) with expired lifetime (15 minutes). Deleting item.",
                            GetGUID().ToString(), GetName(), timeDiff, item.GetGUID().ToString(), item.GetEntry());
                        remove = true;
                    }
                    if (item.IsRefundable())
                    {
                        if (item.IsRefundExpired())
                        {
                            Log.outDebug(LogFilter.Player, "LoadInventory: player (GUID: {0}, name: {1}) has item (GUID: {2}, entry: {3}) with expired refund time ({4}). Deleting refund data and removing " +
                                "efundable flag.", GetGUID().ToString(), GetName(), item.GetGUID().ToString(), item.GetEntry(), GetTotalPlayedTime() - item.GetPlayedTime());

                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_REFUND_INSTANCE);
                            stmt.AddValue(0, item.GetGUID().ToString());
                            trans.Append(stmt);

                            item.RemoveItemFlag(ItemFieldFlags.Refundable);
                        }
                        else
                        {
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_REFUNDS);
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
                                item.RemoveItemFlag(ItemFieldFlags.Refundable);
                            }
                        }
                    }
                    else if (item.IsBOPTradeable())
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_BOP_TRADE);
                        stmt.AddValue(0, item.GetGUID().ToString());
                        SQLResult result = DB.Characters.Query(stmt);
                        if (!result.IsEmpty())
                        {
                            string strGUID = result.Read<string>(0);
                            var GUIDlist = new StringArray(strGUID, ' ');
                            List<ObjectGuid> looters = new();
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
            return item;
        }
        void _LoadSkills(SQLResult result)
        {
            Race race = GetRace();
            Dictionary<uint, uint> loadedSkillValues = new();
            List<ushort> loadedProfessionsWithoutSlot = new(); // fixup old characters
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
                    var professionSlot = result.Read<sbyte>(3);

                    SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skill, race, GetClass());
                    if (rcEntry == null)
                    {
                        Log.outError(LogFilter.Player, $"Player::_LoadSkills: Player '{GetName()}' ({GetGUID()}, Race: {race}, Class: {GetClass()}) has forbidden skill {skill} for his race/class combination");
                        mSkillStatus.Add(skill, new SkillStatusData((uint)mSkillStatus.Count, SkillState.Deleted));
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
                                if (professionSlot != -1)
                                    SetUpdateFieldValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.ProfessionSkillLine, professionSlot), skill);
                                else
                                    loadedProfessionsWithoutSlot.Add(skill);
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
            foreach (var (skillId, skillValue) in loadedSkillValues)
            {
                LearnSkillRewardedSpells(skillId, skillValue, race);

                // enable parent skill line if missing
                var skillEntry = CliDB.SkillLineStorage.LookupByKey(skillId);
                if (skillEntry.ParentSkillLineID != 0 && skillEntry.ParentTierIndex > 0 && GetSkillStep(skillEntry.ParentSkillLineID) < skillEntry.ParentTierIndex)
                {
                    var rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skillEntry.ParentSkillLineID, GetRace(), GetClass());
                    if (rcEntry != null)
                    {
                        var tier = Global.ObjectMgr.GetSkillTier(rcEntry.SkillTierID);
                        if (tier != null)
                            SetSkill(skillEntry.ParentSkillLineID, (uint)skillEntry.ParentTierIndex, Math.Max(GetPureSkillValue(skillEntry.ParentSkillLineID), 1u), tier.GetValueForTierIndex(skillEntry.ParentTierIndex - 1));
                    }
                }

                List<SkillLineRecord> childSkillLines = Global.DB2Mgr.GetSkillLinesForParentSkill(skillId);
                if (childSkillLines != null)
                {
                    foreach (var childSkillLine in childSkillLines)
                    {
                        if (mSkillStatus.Count >= SkillConst.MaxPlayerSkills)
                            break;

                        if (!mSkillStatus.ContainsKey(childSkillLine.Id))
                        {
                            uint pos = (uint)mSkillStatus.Count;
                            SetSkillLineId(pos, (ushort)childSkillLine.Id);
                            SetSkillStartingRank(pos, 1);
                            mSkillStatus.Add(childSkillLine.Id, new SkillStatusData(pos, SkillState.Unchanged));
                        }
                    }
                }
            }

            foreach (ushort skill in loadedProfessionsWithoutSlot)
            {
                int emptyProfessionSlot = FindEmptyProfessionSlotFor(skill);
                if (emptyProfessionSlot != -1)
                {
                    SetUpdateFieldValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.ProfessionSkillLine, emptyProfessionSlot), skill);
                    mSkillStatus[skill].State = SkillState.Changed;
                }
            }

            if (HasSkill(SkillType.FistWeapons))
                SetSkill(SkillType.FistWeapons, 0, GetSkillValue(SkillType.Unarmed), GetMaxSkillValueForLevel());
        }

        void _LoadSpells(SQLResult result, SQLResult favoritesResult)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    AddSpell(result.Read<uint>(0), result.Read<bool>(1), false, false, result.Read<bool>(2), true);
                }
                while (result.NextRow());
            }

            if (!favoritesResult.IsEmpty())
            {
                do
                {
                    var spell = m_spells.LookupByKey(favoritesResult.Read<uint>(0));
                    if (spell != null)
                        spell.Favorite = true;
                } while (favoritesResult.NextRow());
            }
        }

        void _LoadAuras(SQLResult auraResult, SQLResult effectResult, uint timediff)
        {
            Log.outDebug(LogFilter.Player, "Loading auras for player {0}", GetGUID().ToString());

            ObjectGuid casterGuid = new();
            ObjectGuid itemGuid = new();
            Dictionary<AuraKey, AuraLoadEffectInfo> effectInfo = new();
            if (!effectResult.IsEmpty())
            {
                do
                {
                    uint effectIndex = effectResult.Read<byte>(4);
                    if (effectIndex < SpellConst.MaxEffects)
                    {
                        casterGuid.SetRawValue(effectResult.Read<byte[]>(0));
                        itemGuid.SetRawValue(effectResult.Read<byte[]>(1));

                        AuraKey key = new(casterGuid, itemGuid, effectResult.Read<uint>(2), effectResult.Read<uint>(3));
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
                    AuraKey key = new(casterGuid, itemGuid, auraResult.Read<uint>(2), auraResult.Read<uint>(3));
                    uint recalculateMask = auraResult.Read<uint>(4);
                    Difficulty difficulty = (Difficulty)auraResult.Read<byte>(5);
                    byte stackCount = auraResult.Read<byte>(6);
                    int maxDuration = auraResult.Read<int>(7);
                    int remainTime = auraResult.Read<int>(8);
                    byte remainCharges = auraResult.Read<byte>(9);
                    uint castItemId = auraResult.Read<uint>(10);
                    int castItemLevel = auraResult.Read<int>(11);

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(key.SpellId, difficulty);
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
                    if (remainTime != -1 && (!spellInfo.IsPositive() || spellInfo.HasAttribute(SpellAttr4.AuraExpiresOffline)))
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

                    AuraCreateInfo createInfo = new(castId, spellInfo, difficulty, key.EffectMask, this);
                    createInfo.SetCasterGUID(casterGuid);
                    createInfo.SetBaseAmount(info.BaseAmounts);
                    createInfo.SetCastItem(itemGuid, castItemId, castItemLevel);
                    createInfo.SetStackAmount(stackCount);

                    Aura aura = Aura.TryCreate(createInfo);
                    if (aura != null)
                    {
                        if (!aura.CanBeSaved())
                        {
                            aura.Remove();
                            continue;
                        }

                        aura.SetLoadedState(maxDuration, remainTime, remainCharges, recalculateMask, info.Amounts);
                        aura.ApplyForTargets();
                        Log.outInfo(LogFilter.Player, "Added aura spellid {0}, effectmask {1}", spellInfo.Id, key.EffectMask);
                    }
                }
                while (auraResult.NextRow());
            }

            // TODO: finish dragonriding - this forces old flight mode
            AddAura(404468, this);
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
                homebind.WorldRelocate(result.Read<uint>(0), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), result.Read<float>(5));
                homebindAreaId = result.Read<uint>(1);

                var map = CliDB.MapStorage.LookupByKey(homebind.GetMapId());

                // accept saved data only for valid position (and non instanceable), and accessable
                if (GridDefines.IsValidMapCoord(homebind) &&
                    !map.Instanceable() && GetSession().GetExpansion() >= map.Expansion())
                    ok = true;
                else
                {
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    DB.Characters.Execute(stmt);
                }
            }

            void saveHomebindToDb()
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PLAYER_HOMEBIND);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, homebind.GetMapId());
                stmt.AddValue(2, homebindAreaId);
                stmt.AddValue(3, homebind.GetPositionX());
                stmt.AddValue(4, homebind.GetPositionY());
                stmt.AddValue(5, homebind.GetPositionZ());
                stmt.AddValue(6, homebind.GetOrientation());
                DB.Characters.Execute(stmt);
            }
            ;

            if (!ok && HasAtLoginFlag(AtLoginFlags.FirstLogin))
            {
                var createPosition = m_createMode == PlayerCreateMode.NPE && info.createPositionNPE.HasValue ? info.createPositionNPE.Value : info.createPosition;

                if (!createPosition.TransportGuid.HasValue)
                {
                    homebind.WorldRelocate(createPosition.Loc);
                    homebindAreaId = Global.TerrainMgr.GetAreaId(PhasingHandler.EmptyPhaseShift, homebind);

                    saveHomebindToDb();
                    ok = true;
                }
            }

            if (!ok)
            {
                WorldSafeLocsEntry loc = Global.ObjectMgr.GetDefaultGraveyard(GetTeam());
                if (loc == null && GetRace() == Race.PandarenNeutral)
                    loc = Global.ObjectMgr.GetWorldSafeLoc(3295); // The Wandering Isle, Starting Area GY

                Cypher.Assert(loc != null, "Missing fallback graveyard location for faction {GetTeamId()}");

                homebind.WorldRelocate(loc.Loc);
                homebindAreaId = Global.TerrainMgr.GetAreaId(PhasingHandler.EmptyPhaseShift, loc.Loc);

                saveHomebindToDb();
            }

            Log.outDebug(LogFilter.Player, $"Setting player home position - mapid: {homebind.GetMapId()}, areaid: {homebindAreaId}, {homebind}");

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

                PlayerCurrency cur = new();
                cur.state = PlayerCurrencyState.Unchanged;
                cur.Quantity = result.Read<uint>(1);
                cur.WeeklyQuantity = result.Read<uint>(2);
                cur.TrackedQuantity = result.Read<uint>(3);
                cur.IncreasedCapQuantity = result.Read<uint>(4);
                cur.EarnedQuantity = result.Read<uint>(5);
                cur.Flags = (CurrencyDbFlags)result.Read<byte>(6);

                _currencyStorage.Add(currencyID, cur);
            } while (result.NextRow());
        }
        void LoadActions(SQLResult result)
        {
            _LoadActions(result);

            SendActionButtons(1);
        }
        void _LoadActions(SQLResult result)
        {
            m_actionButtons.Clear();
            if (!result.IsEmpty())
            {
                do
                {
                    byte button = result.Read<byte>(0);
                    ulong action = result.Read<ulong>(1);
                    byte type = result.Read<byte>(2);

                    ActionButton ab = AddActionButton(button, action, type);
                    if (ab != null)
                        ab.uState = ActionButtonUpdateState.UnChanged;
                    else
                    {
                        Log.outError(LogFilter.Player, $"Player::_LoadActions: Player '{GetName()}' ({GetGUID()}) has an invalid action button (Button: {button}, Action: {action}, Type: {type}). It will be deleted at next save. This can be due to a player changing their talents.");

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

            long lastDailyReset = Global.WorldMgr.GetNextDailyQuestsResetTime() - Time.Day;
            long lastWeeklyReset = Global.WorldMgr.GetNextWeeklyQuestsResetTime() - Time.Week;

            if (!result.IsEmpty())
            {
                do
                {
                    uint questId = result.Read<uint>(0);
                    // used to be new, no delete?
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
                    if (quest == null)
                        continue;

                    // find or create
                    QuestStatusData questStatusData = new();

                    QuestStatus qstatus = (QuestStatus)result.Read<byte>(1);
                    if (qstatus < QuestStatus.Max)
                        questStatusData.Status = qstatus;
                    else
                    {
                        questStatusData.Status = QuestStatus.Incomplete;
                        Log.outError(LogFilter.Player, $"Player._LoadQuestStatus: Player '{GetName()}' ({GetGUID()}) has invalid quest {questId} status ({qstatus}), replaced by QUEST_STATUS_INCOMPLETE(3).");
                    }

                    questStatusData.Explored = result.Read<byte>(2) > 0;

                    questStatusData.AcceptTime = result.Read<long>(3);
                    if (quest.HasFlagEx(QuestFlagsEx.RemoveOnPeriodicReset))
                    {
                        if ((quest.IsDaily() && questStatusData.AcceptTime < lastDailyReset)
                            || (quest.IsWeekly() && questStatusData.AcceptTime < lastWeeklyReset))
                        {
                            questStatusData.Status = QuestStatus.None;
                            m_QuestStatusSave[questId] = QuestSaveType.Delete;
                            SendPacket(new QuestForceRemoved(questId));
                        }
                    }

                    long endTime = result.Read<long>(4);
                    if (quest.LimitTime != 0 && !GetQuestRewardStatus(questId))
                    {
                        AddTimedQuest(questId);

                        if (endTime <= GameTime.GetGameTime())
                            questStatusData.Timer = 1;
                        else
                            questStatusData.Timer = (uint)((endTime - GameTime.GetGameTime()) * Time.InMilliseconds);
                    }
                    else
                        endTime = 0;

                    // add to quest log
                    if (slot < SharedConst.MaxQuestLogSize && questStatusData.Status != QuestStatus.None)
                    {
                        questStatusData.Slot = slot;

                        foreach (QuestObjective obj in quest.Objectives)
                            m_questObjectiveStatus.Add((obj.Type, obj.ObjectID), new QuestObjectiveStatusData() { QuestStatusPair = (questId, questStatusData), ObjectiveId = obj.Id });

                        SetQuestSlot(slot, questId);
                        SetQuestSlotEndTime(slot, endTime);

                        if (questStatusData.Status == QuestStatus.Complete)
                            SetQuestSlotState(slot, QuestSlotStateMask.Complete);
                        else if (questStatusData.Status == QuestStatus.Failed)
                            SetQuestSlotState(slot, QuestSlotStateMask.Fail);

                        if (quest.HasFlagEx(QuestFlagsEx.RecastAcceptSpellOnLogin) && quest.HasFlag(QuestFlags.PlayerCastAccept) && quest.SourceSpellID > 0)
                            CastSpell(this, quest.SourceSpellID, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

                        ++slot;
                    }

                    m_QuestStatus[questId] = questStatusData;
                    Log.outDebug(LogFilter.ServerLoading, "Quest status is {0} for quest {1} for player (GUID: {2})", questStatusData.Status, questId, GetGUID().ToString());

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
                    if (quest == null)
                        continue;

                    var questStatusData = m_QuestStatus.LookupByKey(questID);
                    if (questStatusData != null && questStatusData.Slot < SharedConst.MaxQuestLogSize)
                    {
                        byte storageIndex = result.Read<byte>(1);

                        var objective = quest.Objectives.FirstOrDefault(objective => objective.StorageIndex == storageIndex);
                        if (objective != null)
                        {
                            int data = result.Read<int>(2);
                            if (!objective.IsStoringFlag())
                                SetQuestSlotCounter(questStatusData.Slot, storageIndex, (ushort)data);
                            else if (data != 0)
                                SetQuestSlotObjectiveFlag(questStatusData.Slot, (sbyte)storageIndex);
                        }
                        else
                            Log.outError(LogFilter.Player, $"Player {GetName()} ({GetGUID()}) has quest {questID} out of range objective index {storageIndex}.");
                    }
                    else
                        Log.outError(LogFilter.Player, $"Player {GetName()} ({GetGUID()}) does not have quest {questID} but has objective data for it.");
                }
                while (result.NextRow());
            }
        }

        void _LoadQuestStatusObjectiveSpawnTrackings(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    uint questID = result.Read<uint>(0);
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(questID);
                    if (quest == null)
                        continue;

                    var itr = m_QuestStatus.LookupByKey(questID);
                    if (itr != null && itr.Slot < SharedConst.MaxQuestLogSize)
                    {
                        QuestStatusData questStatusData = itr;
                        sbyte storageIndex = result.Read<sbyte>(1);
                        var objectiveItr = quest.Objectives.Find(objective => objective.StorageIndex == storageIndex);
                        if (objectiveItr != null)
                        {
                            uint spawnTrackingId = result.Read<uint>(2);

                            if (Global.ObjectMgr.IsQuestObjectiveForSpawnTracking(spawnTrackingId, objectiveItr.Id))
                                questStatusData.SpawnTrackingList.Add((storageIndex, spawnTrackingId));
                            else
                                Log.outError(LogFilter.Player, $"Player::_LoadQuestStatusObjectiveSpawnTrackings: Player '{GetName()}' ({GetGUID()}) has objective {objectiveItr.Id} (quest {questID}) with unrelated spawn tracking {spawnTrackingId}.");
                        }
                        else
                            Log.outError(LogFilter.Player, $"Player::_LoadQuestStatusObjectiveSpawnTrackings: Player '{GetName()}' ({GetGUID()}) has quest {questID} out of range objective index {storageIndex}.");
                    }
                    else
                        Log.outError(LogFilter.Player, $"Player::_LoadQuestStatusObjectiveSpawnTrackings: Player {GetName()} ({GetGUID()}) does not have quest {questID} but has objective spawn trackings for it.");
                } while (result.NextRow());
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
                            SetQuestCompletedBit(quest_id, true);

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
                                    if (rewardProto.ItemSpecClassMask.HasAnyFlag(GetClassMask()))
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
                    m_lastDailyQuestTime = result.Read<long>(1);

                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
                    if (quest == null)
                        continue;

                    AddDynamicUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.DailyQuestsCompleted), quest_id);
                    SetQuestCompletedBit(quest_id, true);

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
                    SetQuestCompletedBit(quest_id, true);

                    Log.outDebug(LogFilter.Player, "Weekly quest {0} cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
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
                    long completedTime = result.Read<long>(2);
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
                    if (quest == null)
                        continue;

                    if (!m_seasonalquests.ContainsKey(event_id))
                        m_seasonalquests[event_id] = new();

                    m_seasonalquests[event_id][quest_id] = completedTime;
                    SetQuestCompletedBit(quest_id, true);

                    Log.outDebug(LogFilter.Player, "Seasonal quest {0} cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
                }
                while (result.NextRow());
            }

            m_SeasonalQuestChanged = false;
        }
        void _LoadMonthlyQuestStatus(SQLResult result)
        {
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
                    SetQuestCompletedBit(quest_id, true);

                    Log.outDebug(LogFilter.Player, "Monthly quest {0} cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
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

        void _LoadTraits(SQLResult configsResult, SQLResult entriesResult)
        {
            MultiMap<int, TraitEntryPacket> traitEntriesByConfig = new();
            if (!entriesResult.IsEmpty())
            {
                //         0              1            2                 3
                // SELECT traitConfigId, traitNodeId, traitNodeEntryId, rank FROM character_trait_entry WHERE guid = ?
                do
                {
                    TraitEntryPacket traitEntry = new();
                    traitEntry.TraitNodeID = entriesResult.Read<int>(1);
                    traitEntry.TraitNodeEntryID = entriesResult.Read<int>(2);
                    traitEntry.Rank = entriesResult.Read<int>(3);

                    if (!TraitMgr.IsValidEntry(traitEntry))
                        continue;

                    traitEntriesByConfig.Add(entriesResult.Read<int>(0), traitEntry);

                } while (entriesResult.NextRow());
            }

            if (!configsResult.IsEmpty())
            {
                //                    0     1                    2                  3                4            5              6      7
                // SELECT traitConfigId, type, chrSpecializationId, combatConfigFlags, localIdentifier, skillLineId, traitSystemId, `name` FROM character_trait_config WHERE guid = ?
                do
                {
                    TraitConfigPacket traitConfig = new();
                    traitConfig.ID = configsResult.Read<int>(0);
                    traitConfig.Type = (TraitConfigType)configsResult.Read<int>(1);
                    switch (traitConfig.Type)
                    {
                        case TraitConfigType.Combat:
                            traitConfig.ChrSpecializationID = configsResult.Read<int>(2);
                            traitConfig.CombatConfigFlags = (TraitCombatConfigFlags)configsResult.Read<int>(3);
                            traitConfig.LocalIdentifier = configsResult.Read<int>(4);
                            break;
                        case TraitConfigType.Profession:
                            traitConfig.SkillLineID = configsResult.Read<uint>(5);
                            break;
                        case TraitConfigType.Generic:
                            traitConfig.TraitSystemID = configsResult.Read<uint>(6);
                            break;
                        default:
                            break;
                    }

                    traitConfig.Name = configsResult.Read<string>(7);

                    foreach (var grantedEntry in TraitMgr.GetGrantedTraitEntriesForConfig(traitConfig, this))
                        traitConfig.Entries.Add(new TraitEntryPacket(grantedEntry));

                    var loadedEntriesNode = traitEntriesByConfig.LookupByKey(traitConfig.ID);
                    if (loadedEntriesNode != null)
                    {
                        foreach (var loadedEntry in loadedEntriesNode)
                        {
                            var itr = traitConfig.Entries.Find(entry => entry.TraitNodeID == loadedEntry.TraitNodeID && entry.TraitNodeEntryID == loadedEntry.TraitNodeEntryID);
                            if (itr == null)
                            {
                                traitConfig.Entries.Add(traitConfig.Entries.Last());
                                traitConfig.Entries[^1].TraitNodeID = loadedEntry.TraitNodeID;
                                traitConfig.Entries[^1].TraitNodeEntryID = loadedEntry.TraitNodeEntryID;
                            }
                            itr.Rank = loadedEntry.Rank;
                        }
                    }

                    if (TraitMgr.ValidateConfig(traitConfig, this, false, true) != TalentLearnResult.LearnOk)
                    {
                        traitConfig.Entries.Clear();
                        traitConfig.SubTrees.Clear();
                        foreach (TraitEntry grantedEntry in TraitMgr.GetGrantedTraitEntriesForConfig(traitConfig, this))
                            traitConfig.Entries.Add(new TraitEntryPacket(grantedEntry));

                        // rebuild subtrees
                        TraitMgr.ValidateConfig(traitConfig, this, false, true);
                    }

                    AddTraitConfig(traitConfig);

                } while (configsResult.NextRow());
            }

            // Remove orphaned trait entries from database
            foreach (var (traitConfigID, _) in traitEntriesByConfig)
                m_traitConfigStates[traitConfigID] = PlayerSpellState.Removed;

            bool hasConfigForSpec(int specId)
            {
                return m_activePlayerData.TraitConfigs.FindIndexIf(traitConfig =>
                {
                    return traitConfig.Type == (int)TraitConfigType.Combat
                        && traitConfig.ChrSpecializationID == specId
                        && (traitConfig.CombatConfigFlags & (int)TraitCombatConfigFlags.ActiveForSpec) != 0;
                }) >= 0;
            }

            int findFreeLocalIdentifier(int specId)
            {
                int index = 1;
                while (m_activePlayerData.TraitConfigs.FindIndexIf(traitConfig =>
                {
                    return traitConfig.Type == (int)TraitConfigType.Combat
                        && traitConfig.ChrSpecializationID == specId
                        && traitConfig.LocalIdentifier == index;
                }) >= 0)
                    ++index;

                return index;
            }

            for (uint i = 0; i < PlayerConst.MaxSpecializations - 1 /*initial spec doesnt get a config*/; ++i)
            {
                var spec = Global.DB2Mgr.GetChrSpecializationByIndex(GetClass(), i);
                if (spec != null)
                {
                    if (hasConfigForSpec((int)spec.Id))
                        continue;

                    TraitConfigPacket traitConfig = new();
                    traitConfig.Type = TraitConfigType.Combat;
                    traitConfig.ChrSpecializationID = (int)spec.Id;
                    traitConfig.CombatConfigFlags = TraitCombatConfigFlags.ActiveForSpec;
                    traitConfig.LocalIdentifier = findFreeLocalIdentifier((int)spec.Id);
                    traitConfig.Name = spec.Name[GetSession().GetSessionDbcLocale()];

                    CreateTraitConfig(traitConfig);
                }
            }

            int activeConfig = m_activePlayerData.TraitConfigs.FindIndexIf(traitConfig =>
            {
                return traitConfig.Type == (int)TraitConfigType.Combat
                    && traitConfig.ChrSpecializationID == (int)GetPrimarySpecialization()
                    && (traitConfig.CombatConfigFlags & (int)TraitCombatConfigFlags.ActiveForSpec) != 0;
            });

            if (activeConfig >= 0)
                SetActiveCombatTraitConfigID(m_activePlayerData.TraitConfigs[activeConfig].ID);

            foreach (TraitConfig traitConfig in m_activePlayerData.TraitConfigs)
            {
                switch ((TraitConfigType)(int)traitConfig.Type)
                {
                    case TraitConfigType.Combat:
                        if (traitConfig.ID != m_activePlayerData.ActiveCombatTraitConfigID)
                            continue;
                        break;
                    case TraitConfigType.Profession:
                        if (!HasSkill((uint)(int)traitConfig.SkillLineID))
                            continue;
                        break;
                    default:
                        break;
                }

                ApplyTraitConfig(traitConfig.ID, true);
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
                if (HasAtLoginFlag(AtLoginFlags.Resurrect))
                    ResurrectPlayer(0.5f);
                else if (!result.IsEmpty())
                {
                    _corpseLocation = new WorldLocation(result.Read<ushort>(0), result.Read<float>(1), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4));
                    if (!CliDB.MapStorage.LookupByKey(_corpseLocation.GetMapId()).Instanceable())
                        SetPlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
                    else
                        RemovePlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
                }
            }

            RemoveAtLoginFlag(AtLoginFlags.Resurrect);
        }
        void _LoadVoidStorage(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                // SELECT itemId, itemEntry, slot, creatorGuid, randomBonusListId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs FROM character_void_storage WHERE playerGuid = ?
                ulong itemId = result.Read<ulong>(0);
                uint itemEntry = result.Read<uint>(1);
                byte slot = result.Read<byte>(2);
                ObjectGuid creatorGuid = result.Read<ulong>(3) != 0 ? ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(3)) : ObjectGuid.Empty;
                uint randomBonusListId = result.Read<uint>(4);
                uint fixedScalingLevel = result.Read<uint>(5);
                uint artifactKnowledgeLevel = result.Read<uint>(6);
                ItemContext context = (ItemContext)result.Read<byte>(7);
                List<uint> bonusListIDs = new();
                var bonusListIdTokens = new StringArray(result.Read<string>(8), ' ');
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

                _voidStorageItems[slot] = new VoidStorageItem(itemId, itemEntry, creatorGuid, randomBonusListId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs);

                BonusData bonus = new(new ItemInstance(_voidStorageItems[slot]));
                GetSession().GetCollectionMgr().AddItemAppearance(itemEntry, bonus.AppearanceModID);
            }
            while (result.NextRow());
        }

        public void _LoadMail(SQLResult mailsResult, SQLResult mailItemsResult, SQLResult artifactResult, SQLResult azeriteItemResult, SQLResult azeriteItemMilestonePowersResult, SQLResult azeriteItemUnlockedEssencesResult, SQLResult azeriteEmpoweredItemResult)
        {
            Dictionary<ulong, Mail> mailById = new();

            if (!mailsResult.IsEmpty())
            {
                do
                {
                    Mail m = new();

                    m.messageID = mailsResult.Read<ulong>(0);
                    m.messageType = (MailMessageType)mailsResult.Read<byte>(1);
                    m.sender = mailsResult.Read<uint>(2);
                    m.receiver = mailsResult.Read<uint>(3);
                    m.subject = mailsResult.Read<string>(4);
                    m.body = mailsResult.Read<string>(5);
                    m.expire_time = mailsResult.Read<long>(6);
                    m.deliver_time = mailsResult.Read<long>(7);
                    m.money = mailsResult.Read<ulong>(8);
                    m.COD = mailsResult.Read<ulong>(9);
                    m.checkMask = (MailCheckMask)mailsResult.Read<byte>(10);
                    m.stationery = (MailStationery)mailsResult.Read<byte>(11);
                    m.mailTemplateId = mailsResult.Read<ushort>(12);

                    if (m.mailTemplateId != 0 && !CliDB.MailTemplateStorage.ContainsKey(m.mailTemplateId))
                    {
                        Log.outError(LogFilter.Player, $"Player:_LoadMail - Mail ({m.messageID}) have not existed MailTemplateId ({m.mailTemplateId}), remove at load");
                        m.mailTemplateId = 0;
                    }

                    m.state = MailState.Unchanged;

                    m_mail.Add(m);
                    mailById[m.messageID] = m;
                }
                while (mailsResult.NextRow());
            }

            if (!mailItemsResult.IsEmpty())
            {
                Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
                ItemAdditionalLoadInfo.Init(additionalData, artifactResult, azeriteItemResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

                do
                {
                    ulong mailId = mailItemsResult.Read<ulong>(53);
                    _LoadMailedItem(GetGUID(), this, mailId, mailById[mailId], mailItemsResult.GetFields(), additionalData.LookupByKey(mailItemsResult.Read<ulong>(0)));
                }
                while (mailItemsResult.NextRow());
            }

            UpdateNextMailTimeAndUnreads();
        }

        static Item _LoadMailedItem(ObjectGuid playerGuid, Player player, ulong mailId, Mail mail, SQLFields fields, ItemAdditionalLoadInfo addionalData)
        {
            ulong itemGuid = fields.Read<ulong>(0);
            uint itemEntry = fields.Read<uint>(1);

            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemEntry);
            if (proto == null)
            {
                Log.outError(LogFilter.Player, $"Player {(player != null ? player.GetName() : "<unknown>")} ({playerGuid}) has unknown item in mailed items (GUID: {itemGuid} template: {itemEntry}) in mail ({mailId}), deleted.");

                SQLTransaction trans = new();

                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_INVALID_MAIL_ITEM);
                stmt.AddValue(0, itemGuid);
                trans.Append(stmt);

                Item.DeleteFromDB(trans, itemGuid);
                AzeriteItem.DeleteFromDB(trans, itemGuid);
                AzeriteEmpoweredItem.DeleteFromDB(trans, itemGuid);

                DB.Characters.CommitTransaction(trans);
                return null;
            }

            Item item = Bag.NewItemOrBag(proto);
            ObjectGuid ownerGuid = fields.Read<ulong>(52) != 0 ? ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(52)) : ObjectGuid.Empty;
            if (!item.LoadFromDB(itemGuid, ownerGuid, fields, itemEntry))
            {
                Log.outError(LogFilter.Player, $"Player._LoadMailedItems: Item (GUID: {itemGuid}) in mail ({mailId}) doesn't exist, deleted from mail.");

                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM);
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
                    AzeriteItem azeriteItem = item.ToAzeriteItem();
                    if (azeriteItem != null)
                        azeriteItem.LoadAzeriteItemData(player, addionalData.AzeriteItem);
                }

                if (addionalData.AzeriteEmpoweredItem != null)
                {
                    AzeriteEmpoweredItem azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();
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
        void _LoadDeclinedNames(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            DeclinedNames declinedNames = m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.DeclinedNames);
            for (int i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                SetUpdateFieldValue(ref declinedNames.ModifyValue(declinedNames.Name, i), result.Read<string>(i));
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
        void _LoadStoredAuraTeleportLocations(SQLResult result)
        {
            //                                                       0      1      2          3          4          5
            //QueryResult* result = CharacterDatabase.PQuery("SELECT Spell, MapId, PositionX, PositionY, PositionZ, Orientation FROM character_spell_location WHERE Guid = ?", GetGUIDLow());

            m_storedAuraTeleportLocations.Clear();
            if (!result.IsEmpty())
            {
                do
                {
                    uint spellId = result.Read<uint>(0);

                    if (!Global.SpellMgr.HasSpellInfo(spellId, Difficulty.None))
                    {
                        Log.outError(LogFilter.Spells, $"Player._LoadStoredAuraTeleportLocations: Player {GetName()} ({GetGUID()}) spell (ID: {spellId}) does not exist");
                        continue;
                    }

                    WorldLocation location = new(result.Read<uint>(1), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), result.Read<float>(5));
                    if (!GridDefines.IsValidMapCoord(location))
                    {
                        Log.outError(LogFilter.Spells, $"Player._LoadStoredAuraTeleportLocations: Player {GetName()} ({GetGUID()}) spell (ID: {spellId}) has invalid position on map {location.GetMapId()}, {location}.");
                        continue;
                    }

                    StoredAuraTeleportLocation storedLocation = new();
                    storedLocation.Loc = location;
                    storedLocation.CurrentState = StoredAuraTeleportLocation.State.Unchanged;

                    m_storedAuraTeleportLocations[spellId] = storedLocation;
                }
                while (result.NextRow());
            }
        }
        void _LoadGroup(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                Group group = Global.GroupMgr.GetGroupByDbStoreId(result.Read<uint>(0));
                if (group != null)
                {
                    if (group.IsLeader(GetGUID()))
                        SetPlayerFlag(PlayerFlags.GroupLeader);

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

            if (GetGroup() == null || !GetGroup().IsLeader(GetGUID()))
                RemovePlayerFlag(PlayerFlags.GroupLeader);
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
                EquipmentSetInfo eqSet = new();
                eqSet.Data.Guid = result.Read<ulong>(0);
                eqSet.Data.Type = EquipmentSetInfo.EquipmentSetType.Equipment;
                eqSet.Data.SetID = result.Read<byte>(1);
                eqSet.Data.SetName = result.Read<string>(2);
                eqSet.Data.SetIcon = result.Read<string>(3);
                eqSet.Data.IgnoreMask = result.Read<uint>(4);
                eqSet.Data.AssignedSpecIndex = result.Read<int>(5);
                eqSet.state = EquipmentSetUpdateState.Unchanged;

                for (int i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                {
                    ulong guid = result.Read<uint>(6 + i);
                    if (guid != 0)
                        eqSet.Data.Pieces[i] = ObjectGuid.Create(HighGuid.Item, guid);
                }

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
                EquipmentSetInfo eqSet = new();

                eqSet.Data.Guid = result.Read<ulong>(0);
                eqSet.Data.Type = EquipmentSetInfo.EquipmentSetType.Transmog;
                eqSet.Data.SetID = result.Read<byte>(1);
                eqSet.Data.SetName = result.Read<string>(2);
                eqSet.Data.SetIcon = result.Read<string>(3);
                eqSet.Data.IgnoreMask = result.Read<uint>(4);
                eqSet.state = EquipmentSetUpdateState.Unchanged;

                for (int i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                    eqSet.Data.Appearances[i] = result.Read<int>(5 + i);

                for (int i = 0; i < eqSet.Data.Enchants.Length; ++i)
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
            //         0           1     2      3      4      5      6          7          8        9           10
            // SELECT instanceId, team, joinX, joinY, joinZ, joinO, joinMapId, taxiStart, taxiEnd, mountSpell, queueTypeId FROM character_Battleground_data WHERE guid = ?
            m_bgData.bgInstanceID = result.Read<uint>(0);
            m_bgData.bgTeam = (Team)result.Read<ushort>(1);
            m_bgData.joinPos = new WorldLocation(result.Read<ushort>(6), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), result.Read<float>(5));
            m_bgData.taxiPath[0] = result.Read<uint>(7);
            m_bgData.taxiPath[1] = result.Read<uint>(8);
            m_bgData.mountSpell = result.Read<uint>(9);
            m_bgData.queueId = BattlegroundQueueTypeId.FromPacked(result.Read<ulong>(10));
        }
        void _LoadPetStable(uint summonedPetNumber, SQLResult result)
        {
            if (result.IsEmpty())
                return;

            m_petStable = new();

            //         0      1        2      3    4           5     6     7        8          9       10      11        12              13       14              15
            // SELECT id, entry, modelid, level, exp, Reactstate, slot, name, renamed, curhealth, curmana, abdata, savetime, CreatedBySpell, PetType, specialization FROM character_pet WHERE owner = ?

            do
            {
                PetStable.PetInfo petInfo = new();
                petInfo.PetNumber = result.Read<uint>(0);
                petInfo.CreatureId = result.Read<uint>(1);
                petInfo.DisplayId = result.Read<uint>(2);
                petInfo.Level = result.Read<byte>(3);
                petInfo.Experience = result.Read<uint>(4);
                petInfo.ReactState = (ReactStates)result.Read<byte>(5);
                PetSaveMode slot = (PetSaveMode)result.Read<short>(6);
                petInfo.Name = result.Read<string>(7);
                petInfo.WasRenamed = result.Read<bool>(8);
                petInfo.Health = result.Read<uint>(9);
                petInfo.Mana = result.Read<uint>(10);
                petInfo.ActionBar = result.Read<string>(11);
                petInfo.LastSaveTime = result.Read<uint>(12);
                petInfo.CreatedBySpellId = result.Read<uint>(13);
                petInfo.Type = (PetType)result.Read<byte>(14);
                petInfo.SpecializationId = result.Read<ushort>(15);
                if (slot >= PetSaveMode.FirstActiveSlot && slot < PetSaveMode.LastActiveSlot)
                {
                    m_petStable.ActivePets[(int)slot] = petInfo;

                    if (m_petStable.ActivePets[(int)slot].Type == PetType.Hunter)
                        AddPetToUpdateFields(m_petStable.ActivePets[(int)slot], slot, PetStableFlags.Active);
                }
                else if (slot >= PetSaveMode.FirstStableSlot && slot < PetSaveMode.LastStableSlot)
                {
                    m_petStable.StabledPets[slot - PetSaveMode.FirstStableSlot] = petInfo;

                    if (m_petStable.StabledPets[slot - PetSaveMode.FirstStableSlot].Type == PetType.Hunter)
                        AddPetToUpdateFields(m_petStable.StabledPets[slot - PetSaveMode.FirstStableSlot], slot, PetStableFlags.Inactive);
                }
                else if (slot == PetSaveMode.NotInSlot)
                    m_petStable.UnslottedPets.Add(petInfo);

            } while (result.NextRow());

            if (Pet.GetLoadPetInfo(m_petStable, 0, summonedPetNumber, null).Item1 != null)
                m_temporaryUnsummonedPetNumber = summonedPetNumber;
        }

        void _SaveInventory(SQLTransaction trans)
        {
            PreparedStatement stmt;
            // force items in buyback slots to new state
            // and remove those that aren't already
            for (var i = InventorySlots.BuyBackStart; i < InventorySlots.BuyBackEnd; ++i)
            {
                Item item = m_items[i];
                if (item == null)
                    continue;

                ItemTemplate itemTemplate = item.GetTemplate();

                if (item.GetState() == ItemUpdateState.New)
                {
                    if (itemTemplate != null)
                        if (itemTemplate.HasFlag(ItemFlags.HasLoot))
                            Global.LootItemStorage.RemoveStoredLootForContainer(item.GetGUID().GetCounter());

                    continue;
                }

                item.DeleteFromInventoryDB(trans);
                item.DeleteFromDB(trans);
                m_items[i].FSetState(ItemUpdateState.New);

                if (itemTemplate != null)
                    if (itemTemplate.HasFlag(ItemFlags.HasLoot))
                        Global.LootItemStorage.RemoveStoredLootForContainer(item.GetGUID().GetCounter());
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
                    if (item.IsRefundable() && item.IsRefundExpired())
                        item.SetNotRefundable(this);
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
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_BAG_SLOT);
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
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_INVENTORY_ITEM);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, container != null ? container.GetGUID().GetCounter() : 0);
                        stmt.AddValue(2, item.GetSlot());
                        stmt.AddValue(3, item.GetGUID().GetCounter());
                        trans.Append(stmt);
                        break;
                    case ItemUpdateState.Removed:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_ITEM);
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

            SkillInfo skillInfoField = m_activePlayerData.Skill;

            foreach (var pair in mSkillStatus.ToList())
            {
                if (pair.Value.State == SkillState.Unchanged)
                    continue;

                ushort value = skillInfoField.SkillRank[pair.Value.Pos];
                ushort max = skillInfoField.SkillMaxRank[pair.Value.Pos];
                sbyte professionSlot = (sbyte)GetProfessionSlotFor(pair.Key);

                switch (pair.Value.State)
                {
                    case SkillState.New:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SKILLS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, (ushort)pair.Key);
                        stmt.AddValue(2, value);
                        stmt.AddValue(3, max);
                        stmt.AddValue(4, professionSlot);
                        trans.Append(stmt);
                        break;
                    case SkillState.Changed:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_SKILLS);
                        stmt.AddValue(0, value);
                        stmt.AddValue(1, max);
                        stmt.AddValue(2, professionSlot);
                        stmt.AddValue(3, GetGUID().GetCounter());
                        stmt.AddValue(4, (ushort)pair.Key);
                        trans.Append(stmt);
                        break;
                    case SkillState.Deleted:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SKILL_BY_SKILL);
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
        void _SaveSpells(SQLTransaction trans)
        {
            PreparedStatement stmt;

            foreach (var (id, spell) in m_spells.ToList())
            {
                if (spell.State == PlayerSpellState.Removed || spell.State == PlayerSpellState.Changed)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_BY_SPELL);
                    stmt.AddValue(0, id);
                    stmt.AddValue(1, GetGUID().GetCounter());
                    trans.Append(stmt);
                }

                if (spell.State == PlayerSpellState.New || spell.State == PlayerSpellState.Changed)
                {
                    // add only changed/new not dependent spells
                    if (!spell.Dependent)
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SPELL);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, id);
                        stmt.AddValue(2, spell.Active);
                        stmt.AddValue(3, spell.Disabled);
                        trans.Append(stmt);
                    }

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_FAVORITE);
                    stmt.AddValue(0, id);
                    stmt.AddValue(1, GetGUID().GetCounter());
                    trans.Append(stmt);

                    if (spell.Favorite)
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SPELL_FAVORITE);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, id);
                        trans.Append(stmt);
                    }
                }

                if (spell.State == PlayerSpellState.Removed)
                {
                    m_spells.Remove(id);
                    continue;
                }

                if (spell.State != PlayerSpellState.Temporary)
                    spell.State = PlayerSpellState.Unchanged;
            }
        }
        void _SaveAuras(SQLTransaction trans)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_EFFECT);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_AURA);
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
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_AURA);
                stmt.AddValue(index++, GetGUID().GetCounter());
                stmt.AddValue(index++, key.Caster.GetRawValue());
                stmt.AddValue(index++, key.Item.GetRawValue());
                stmt.AddValue(index++, key.SpellId);
                stmt.AddValue(index++, key.EffectMask);
                stmt.AddValue(index++, recalculateMask);
                stmt.AddValue(index++, (byte)aura.GetCastDifficulty());
                stmt.AddValue(index++, aura.GetStackAmount());
                stmt.AddValue(index++, aura.GetMaxDuration());
                stmt.AddValue(index++, aura.GetDuration());
                stmt.AddValue(index++, aura.GetCharges());
                stmt.AddValue(index++, aura.GetCastItemId());
                stmt.AddValue(index, aura.GetCastItemLevel());
                trans.Append(stmt);

                foreach (AuraEffect effect in aura.GetAuraEffects())
                {
                    if (effect != null)
                    {
                        index = 0;
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_AURA_EFFECT);
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
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_GLYPHS);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            for (byte spec = 0; spec < PlayerConst.MaxSpecializations; ++spec)
            {
                foreach (uint glyphId in GetGlyphs(spec))
                {
                    byte index = 0;

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_GLYPHS);
                    stmt.AddValue(index++, GetGUID().GetCounter());
                    stmt.AddValue(index++, spec);
                    stmt.AddValue(index++, glyphId);

                    trans.Append(stmt);
                }
            }
        }
        void _SaveCurrency(SQLTransaction trans)
        {
            PreparedStatement stmt;
            foreach (var (id, currency) in _currencyStorage)
            {
                CurrencyTypesRecord entry = CliDB.CurrencyTypesStorage.LookupByKey(id);
                if (entry == null) // should never happen
                    continue;

                switch (currency.state)
                {
                    case PlayerCurrencyState.New:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_PLAYER_CURRENCY);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, id);
                        stmt.AddValue(2, currency.Quantity);
                        stmt.AddValue(3, currency.WeeklyQuantity);
                        stmt.AddValue(4, currency.TrackedQuantity);
                        stmt.AddValue(5, currency.IncreasedCapQuantity);
                        stmt.AddValue(6, currency.EarnedQuantity);
                        stmt.AddValue(7, (byte)currency.Flags);
                        trans.Append(stmt);
                        break;
                    case PlayerCurrencyState.Changed:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_PLAYER_CURRENCY);
                        stmt.AddValue(0, currency.Quantity);
                        stmt.AddValue(1, currency.WeeklyQuantity);
                        stmt.AddValue(2, currency.TrackedQuantity);
                        stmt.AddValue(3, currency.IncreasedCapQuantity);
                        stmt.AddValue(4, currency.EarnedQuantity);
                        stmt.AddValue(5, (byte)currency.Flags);
                        stmt.AddValue(6, GetGUID().GetCounter());
                        stmt.AddValue(7, id);
                        trans.Append(stmt);
                        break;
                    default:
                        break;
                }

                currency.state = PlayerCurrencyState.Unchanged;
            }
        }

        public static void SavePlayerCustomizations(SQLTransaction trans, ulong guid, List<ChrCustomizationChoice> customizations)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
            stmt.AddValue(0, guid);
            trans.Append(stmt);

            foreach (var customization in customizations)
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_CUSTOMIZATION);
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

        void _SaveCustomizations(SQLTransaction trans)
        {
            if (!m_customizationsChanged)
                return;

            m_customizationsChanged = false;

            SavePlayerCustomizations(trans, GetGUID().GetCounter(), m_playerData.Customizations);
        }

        void _SaveActions(SQLTransaction trans)
        {
            int traitConfigId = 0;

            TraitConfig traitConfig = GetTraitConfig((int)(uint)m_activePlayerData.ActiveCombatTraitConfigID);
            if (traitConfig != null)
            {
                int usedSavedTraitConfigIndex = m_activePlayerData.TraitConfigs.FindIndexIf(savedConfig =>
                {
                    return (TraitConfigType)(int)savedConfig.Type == TraitConfigType.Combat
                        && ((TraitCombatConfigFlags)(int)savedConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None
                        && ((TraitCombatConfigFlags)(int)savedConfig.CombatConfigFlags & TraitCombatConfigFlags.SharedActionBars) == TraitCombatConfigFlags.None
                        && savedConfig.LocalIdentifier == traitConfig.LocalIdentifier;
                });

                if (usedSavedTraitConfigIndex >= 0)
                    traitConfigId = m_activePlayerData.TraitConfigs[usedSavedTraitConfigIndex].ID;
            }

            PreparedStatement stmt;

            foreach (var pair in m_actionButtons.ToList())
            {
                switch (pair.Value.uState)
                {
                    case ActionButtonUpdateState.New:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_ACTION);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, GetActiveTalentGroup());
                        stmt.AddValue(2, traitConfigId);
                        stmt.AddValue(3, pair.Key);
                        stmt.AddValue(4, pair.Value.GetAction());
                        stmt.AddValue(5, (byte)pair.Value.GetButtonType());
                        trans.Append(stmt);

                        pair.Value.uState = ActionButtonUpdateState.UnChanged;
                        break;
                    case ActionButtonUpdateState.Changed:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_ACTION);
                        stmt.AddValue(0, pair.Value.GetAction());
                        stmt.AddValue(1, (byte)pair.Value.GetButtonType());
                        stmt.AddValue(2, GetGUID().GetCounter());
                        stmt.AddValue(3, pair.Key);
                        stmt.AddValue(4, GetActiveTalentGroup());
                        stmt.AddValue(5, traitConfigId);
                        trans.Append(stmt);

                        pair.Value.uState = ActionButtonUpdateState.UnChanged;
                        break;
                    case ActionButtonUpdateState.Deleted:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION_BY_BUTTON_SPEC);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, GetActiveTalentGroup());
                        stmt.AddValue(3, traitConfigId);
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

            PreparedStatement stmt;
            bool keepAbandoned = !Global.WorldMgr.GetCleaningFlags().HasAnyFlag(CleaningFlags.Queststatus);

            foreach (var save in m_QuestStatusSave)
            {
                if (save.Value == QuestSaveType.Default)
                {
                    var data = m_QuestStatus.LookupByKey(save.Key);
                    if (data != null && (keepAbandoned || data.Status != QuestStatus.None))
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_CHAR_QUESTSTATUS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, save.Key);
                        stmt.AddValue(2, (byte)data.Status);
                        stmt.AddValue(3, data.Explored);
                        stmt.AddValue(4, data.AcceptTime);
                        stmt.AddValue(5, GetQuestSlotEndTime(data.Slot));
                        trans.Append(stmt);

                        // Save objectives
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_BY_QUEST);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, save.Key);
                        trans.Append(stmt);

                        Quest quest = Global.ObjectMgr.GetQuestTemplate(save.Key);

                        foreach (QuestObjective obj in quest.Objectives)
                        {
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_CHAR_QUESTSTATUS_OBJECTIVES);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            stmt.AddValue(1, save.Key);
                            stmt.AddValue(2, obj.StorageIndex);
                            stmt.AddValue(3, GetQuestSlotObjectiveData(data.Slot, obj));
                            trans.Append(stmt);
                        }

                        // Save spawn trackings
                        stmt = CharacterDatabase.GetPreparedStatement( CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_SPAWN_TRACKING_BY_QUEST);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, save.Key);
                        trans.Append(stmt);

                        foreach (var (questObjectiveStorageIndex, spawnTrackingId) in data.SpawnTrackingList)
                        {
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_CHAR_QUESTSTATUS_OBJECTIVES_SPAWN_TRACKING);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            stmt.AddValue(1, save.Key);
                            stmt.AddValue(2, questObjectiveStorageIndex);
                            stmt.AddValue(3, spawnTrackingId);
                            trans.Append(stmt);
                        }
                    }
                }
                else
                {
                    // Delete
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_BY_QUEST);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, save.Key);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_BY_QUEST);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, save.Key);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_SPAWN_TRACKING_BY_QUEST);
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
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_QUESTSTATUS_REWARDED);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, save.Key);
                    trans.Append(stmt);

                }
                else if (save.Value == QuestSaveType.ForceDelete || !keepAbandoned)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED_BY_QUEST);
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
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY);
            stmt.AddValue(0, GetGUID().GetCounter());

            foreach (int questId in m_activePlayerData.DailyQuestsCompleted)
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_DAILY);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, questId);
                stmt.AddValue(2, m_lastDailyQuestTime);
                trans.Append(stmt);

            }

            if (!m_DFQuests.Empty())
            {
                foreach (var id in m_DFQuests)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_DAILY);
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
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            foreach (var quest_id in m_weeklyquests)
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_WEEKLY);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, quest_id);
                trans.Append(stmt);
            }

            m_WeeklyQuestChanged = false;
        }
        void _SaveSeasonalQuestStatus(SQLTransaction trans)
        {
            if (!m_SeasonalQuestChanged)
                return;

            // we don't need transactions here.
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            m_SeasonalQuestChanged = false;

            if (m_seasonalquests.Empty())
                return;

            foreach (var (eventId, dictionary) in m_seasonalquests)
            {
                foreach (var (questId, completedTime) in dictionary)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_SEASONAL);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, questId);
                    stmt.AddValue(2, eventId);
                    stmt.AddValue(3, completedTime);
                    trans.Append(stmt);
                }
            }
        }
        void _SaveMonthlyQuestStatus(SQLTransaction trans)
        {
            if (!m_MonthlyQuestChanged || m_monthlyquests.Empty())
                return;

            // we don't need transactions here.
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            foreach (var questId in m_monthlyquests)
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_MONTHLY);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, questId);
                trans.Append(stmt);
            }

            m_MonthlyQuestChanged = false;
        }
        void _SaveTalents(SQLTransaction trans)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TALENT);
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

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_TALENT);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, pair.Key);
                    stmt.AddValue(2, group);
                    trans.Append(stmt);
                }
            }

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_PVP_TALENT);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            for (byte group = 0; group < PlayerConst.MaxSpecializations; ++group)
            {
                var talents = GetPvpTalentMap(group);
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_PVP_TALENT);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, talents[0]);
                stmt.AddValue(2, talents[1]);
                stmt.AddValue(3, talents[2]);
                stmt.AddValue(4, talents[3]);
                stmt.AddValue(5, group);
                trans.Append(stmt);
            }
        }

        void _SaveTraits(SQLTransaction trans)
        {
            PreparedStatement stmt = null;
            foreach (var (traitConfigId, state) in m_traitConfigStates)
            {
                switch (state)
                {
                    case PlayerSpellState.Changed:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, traitConfigId);
                        trans.Append(stmt);

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, traitConfigId);
                        trans.Append(stmt);

                        TraitConfig traitConfig = GetTraitConfig(traitConfigId);
                        if (traitConfig != null)
                        {
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_TRAIT_CONFIGS);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            stmt.AddValue(1, traitConfig.ID);
                            stmt.AddValue(2, traitConfig.Type);
                            switch ((TraitConfigType)(int)traitConfig.Type)
                            {
                                case TraitConfigType.Combat:
                                    stmt.AddValue(3, traitConfig.ChrSpecializationID);
                                    stmt.AddValue(4, traitConfig.CombatConfigFlags);
                                    stmt.AddValue(5, traitConfig.LocalIdentifier);
                                    stmt.AddNull(6);
                                    stmt.AddNull(7);
                                    break;
                                case TraitConfigType.Profession:
                                    stmt.AddNull(3);
                                    stmt.AddNull(4);
                                    stmt.AddNull(5);
                                    stmt.AddValue(6, traitConfig.SkillLineID);
                                    stmt.AddNull(7);
                                    break;
                                case TraitConfigType.Generic:
                                    stmt.AddNull(3);
                                    stmt.AddNull(4);
                                    stmt.AddNull(5);
                                    stmt.AddNull(6);
                                    stmt.AddValue(7, traitConfig.TraitSystemID);
                                    break;
                                default:
                                    break;
                            }

                            stmt.AddValue(8, traitConfig.Name);
                            trans.Append(stmt);

                            foreach (var traitEntry in traitConfig.Entries)
                            {
                                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_TRAIT_ENTRIES);
                                stmt.AddValue(0, GetGUID().GetCounter());
                                stmt.AddValue(1, traitConfig.ID);
                                stmt.AddValue(2, traitEntry.TraitNodeID);
                                stmt.AddValue(3, traitEntry.TraitNodeEntryID);
                                stmt.AddValue(4, traitEntry.Rank);
                                trans.Append(stmt);
                            }
                        }
                        break;
                    case PlayerSpellState.Removed:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, traitConfigId);
                        trans.Append(stmt);

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, traitConfigId);
                        trans.Append(stmt);

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION_BY_TRAIT_CONFIG);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, traitConfigId);
                        trans.Append(stmt);
                        break;
                    default:
                        break;
                }
            }

            m_traitConfigStates.Clear();
        }

        public void _SaveMail(SQLTransaction trans)
        {
            PreparedStatement stmt;

            foreach (var m in m_mail)
            {
                if (m.state == MailState.Changed)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_MAIL);
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
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM);
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
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
                    stmt.AddValue(0, m.messageID);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
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
        void _SaveStoredAuraTeleportLocations(SQLTransaction trans)
        {
            foreach (var pair in m_storedAuraTeleportLocations.ToList())
            {
                var storedLocation = pair.Value;
                if (storedLocation.CurrentState == StoredAuraTeleportLocation.State.Deleted)
                {
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATION);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);
                    m_storedAuraTeleportLocations.Remove(pair.Key);
                    continue;
                }

                if (storedLocation.CurrentState == StoredAuraTeleportLocation.State.Changed)
                {
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATION);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_AURA_STORED_LOCATION);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, pair.Key);
                    stmt.AddValue(2, storedLocation.Loc.GetMapId());
                    stmt.AddValue(3, storedLocation.Loc.GetPositionX());
                    stmt.AddValue(4, storedLocation.Loc.GetPositionY());
                    stmt.AddValue(5, storedLocation.Loc.GetPositionZ());
                    stmt.AddValue(6, storedLocation.Loc.GetOrientation());
                    trans.Append(stmt);
                }
            }
        }
        void _SaveStats(SQLTransaction trans)
        {
            // check if stat saving is enabled and if char level is high enough
            if (WorldConfig.GetIntValue(WorldCfg.MinLevelStatSave) == 0 || GetLevel() < WorldConfig.GetIntValue(WorldCfg.MinLevelStatSave))
                return;

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_STATS);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            byte index = 0;
            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_STATS);
            stmt.AddValue(index++, GetGUID().GetCounter());
            stmt.AddValue(index++, GetMaxHealth());

            for (byte i = 0; i < (int)PowerType.MaxPerClass; ++i)
                stmt.AddValue(index++, m_unitData.MaxPower[i]);

            for (byte i = 0; i < (int)Stats.Max; ++i)
                stmt.AddValue(index++, GetStat((Stats)i));

            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                stmt.AddValue(index++, GetResistance((SpellSchools)i));

            stmt.AddValue(index++, m_activePlayerData.BlockPercentage);
            stmt.AddValue(index++, m_activePlayerData.DodgePercentage);
            stmt.AddValue(index++, m_activePlayerData.ParryPercentage);
            stmt.AddValue(index++, m_activePlayerData.CritPercentage);
            stmt.AddValue(index++, m_activePlayerData.RangedCritPercentage);
            stmt.AddValue(index++, m_activePlayerData.SpellCritPercentage);
            stmt.AddValue(index++, m_unitData.AttackPower);
            stmt.AddValue(index++, m_unitData.RangedAttackPower);
            stmt.AddValue(index++, GetBaseSpellPowerBonus());
            stmt.AddValue(index, m_activePlayerData.CombatRatings[(int)CombatRating.ResiliencePlayerDamage]);
            stmt.AddValue(index++, m_activePlayerData.Mastery);
            stmt.AddValue(index++, m_activePlayerData.Versatility);

            trans.Append(stmt);
        }
        public void SaveInventoryAndGoldToDB(SQLTransaction trans)
        {
            _SaveInventory(trans);
            _SaveCurrency(trans);

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_MONEY);
            stmt.AddValue(0, GetMoney());
            stmt.AddValue(1, GetGUID().GetCounter());
            trans.Append(stmt);
        }
        void _SaveEquipmentSets(SQLTransaction trans)
        {
            foreach (var pair in _equipmentSets)
            {
                EquipmentSetInfo eqSet = pair.Value;
                PreparedStatement stmt;
                byte j = 0;
                switch (eqSet.state)
                {
                    case EquipmentSetUpdateState.Unchanged:
                        break;                                      // do nothing
                    case EquipmentSetUpdateState.Changed:
                        if (eqSet.Data.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
                        {
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_EQUIP_SET);
                            stmt.AddValue(j++, eqSet.Data.SetName);
                            stmt.AddValue(j++, eqSet.Data.SetIcon);
                            stmt.AddValue(j++, eqSet.Data.IgnoreMask);
                            stmt.AddValue(j++, eqSet.Data.AssignedSpecIndex);

                            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                                stmt.AddValue(j++, eqSet.Data.Pieces[i].GetCounter());

                            stmt.AddValue(j++, GetGUID().GetCounter());
                            stmt.AddValue(j++, eqSet.Data.Guid);
                            stmt.AddValue(j, eqSet.Data.SetID);
                        }
                        else
                        {
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_TRANSMOG_OUTFIT);
                            stmt.AddValue(j++, eqSet.Data.SetName);
                            stmt.AddValue(j++, eqSet.Data.SetIcon);
                            stmt.AddValue(j++, eqSet.Data.IgnoreMask);

                            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                                stmt.AddValue(j++, eqSet.Data.Appearances[i]);

                            for (int i = 0; i < eqSet.Data.Enchants.Length; ++i)
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
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_EQUIP_SET);
                            stmt.AddValue(j++, GetGUID().GetCounter());
                            stmt.AddValue(j++, eqSet.Data.Guid);
                            stmt.AddValue(j++, eqSet.Data.SetID);
                            stmt.AddValue(j++, eqSet.Data.SetName);
                            stmt.AddValue(j++, eqSet.Data.SetIcon);
                            stmt.AddValue(j++, eqSet.Data.IgnoreMask);
                            stmt.AddValue(j++, eqSet.Data.AssignedSpecIndex);

                            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                                stmt.AddValue(j++, eqSet.Data.Pieces[i].GetCounter());
                        }
                        else
                        {
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_TRANSMOG_OUTFIT);
                            stmt.AddValue(j++, GetGUID().GetCounter());
                            stmt.AddValue(j++, eqSet.Data.Guid);
                            stmt.AddValue(j++, eqSet.Data.SetID);
                            stmt.AddValue(j++, eqSet.Data.SetName);
                            stmt.AddValue(j++, eqSet.Data.SetIcon);
                            stmt.AddValue(j++, eqSet.Data.IgnoreMask);

                            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                                stmt.AddValue(j++, eqSet.Data.Appearances[i]);

                            for (int i = 0; i < eqSet.Data.Enchants.Length; ++i)
                                stmt.AddValue(j++, eqSet.Data.Enchants[i]);
                        }
                        trans.Append(stmt);
                        eqSet.state = EquipmentSetUpdateState.Unchanged;
                        break;
                    case EquipmentSetUpdateState.Deleted:
                        if (eqSet.Data.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_EQUIP_SET);
                        else
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_TRANSMOG_OUTFIT);
                        stmt.AddValue(0, eqSet.Data.Guid);
                        trans.Append(stmt);
                        _equipmentSets.Remove(pair.Key);
                        break;
                }
            }
        }
        void _SaveVoidStorage(SQLTransaction trans)
        {
            PreparedStatement stmt;
            for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
            {
                if (_voidStorageItems[i] == null) // unused item
                {
                    // DELETE FROM void_storage WHERE slot = ? AND playerGuid = ?
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_SLOT);
                    stmt.AddValue(0, i);
                    stmt.AddValue(1, GetGUID().GetCounter());
                }

                else
                {
                    // REPLACE INTO character_void_storage (itemId, playerGuid, itemEntry, slot, creatorGuid, randomPropertyType, randomProperty, upgradeId, fixedScalingLevel, artifactKnowledgeLevel, bonusListIDs) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_CHAR_VOID_STORAGE_ITEM);
                    stmt.AddValue(0, _voidStorageItems[i].ItemId);
                    stmt.AddValue(1, GetGUID().GetCounter());
                    stmt.AddValue(2, _voidStorageItems[i].ItemEntry);
                    stmt.AddValue(3, i);
                    stmt.AddValue(4, _voidStorageItems[i].CreatorGuid.GetCounter());
                    stmt.AddValue(5, (byte)_voidStorageItems[i].RandomBonusListId);
                    stmt.AddValue(6, _voidStorageItems[i].FixedScalingLevel);
                    stmt.AddValue(7, _voidStorageItems[i].ArtifactKnowledgeLevel);
                    stmt.AddValue(8, (byte)_voidStorageItems[i].Context);

                    StringBuilder bonusListIDs = new();
                    foreach (uint bonusListID in _voidStorageItems[i].BonusListIDs)
                        bonusListIDs.AppendFormat("{0} ", bonusListID);
                    stmt.AddValue(9, bonusListIDs.ToString());
                }

                trans.Append(stmt);
            }
        }
        void _SaveCUFProfiles(SQLTransaction trans)
        {
            PreparedStatement stmt;
            ulong lowGuid = GetGUID().GetCounter();

            for (byte i = 0; i < PlayerConst.MaxCUFProfiles; ++i)
            {
                if (_CUFProfiles[i] == null) // unused profile
                {
                    // DELETE FROM character_cuf_profiles WHERE guid = ? and id = ?
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_CUF_PROFILES_BY_ID);
                    stmt.AddValue(0, lowGuid);
                    stmt.AddValue(1, i);
                }
                else
                {
                    // REPLACE INTO character_cuf_profiles (guid, id, name, frameHeight, frameWidth, sortBy, healthText, boolOptions, unk146, unk147, unk148, unk150, unk152, unk154) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_CHAR_CUF_PROFILES);
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

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ACCOUNT_INSTANCE_LOCK_TIMES);
            stmt.AddValue(0, GetSession().GetAccountId());
            trans.Append(stmt);

            foreach (var pair in _instanceResetTimes)
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ACCOUNT_INSTANCE_LOCK_TIMES);
                stmt.AddValue(0, GetSession().GetAccountId());
                stmt.AddValue(1, pair.Key);
                stmt.AddValue(2, pair.Value);
                trans.Append(stmt);
            }
        }
        void _SaveBGData(SQLTransaction trans)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_BGDATA);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PLAYER_BGDATA);
            stmt.AddValue(0, GetGUID().GetCounter());
            stmt.AddValue(1, m_bgData.bgInstanceID);
            stmt.AddValue(2, (ushort)m_bgData.bgTeam);
            stmt.AddValue(3, m_bgData.joinPos.GetPositionX());
            stmt.AddValue(4, m_bgData.joinPos.GetPositionY());
            stmt.AddValue(5, m_bgData.joinPos.GetPositionZ());
            stmt.AddValue(6, m_bgData.joinPos.GetOrientation());
            stmt.AddValue(7, (ushort)m_bgData.joinPos.GetMapId());
            stmt.AddValue(8, m_bgData.taxiPath[0]);
            stmt.AddValue(9, m_bgData.taxiPath[1]);
            stmt.AddValue(10, m_bgData.mountSpell);
            stmt.AddValue(11, m_bgData.queueId.GetPacked());
            trans.Append(stmt);
        }

        public bool LoadFromDB(ObjectGuid guid, SQLQueryHolder<PlayerLoginQueryLoad> holder)
        {
            SQLResult result = holder.GetResult(PlayerLoginQueryLoad.From);
            if (result.IsEmpty())
            {
                Global.CharacterCacheStorage.GetCharacterNameByGuid(guid, out string cacheName);
                Log.outError(LogFilter.Player, "Player {0} {1} not found in table `characters`, can't load. ", cacheName, guid.ToString());
                return false;
            }

            int fieldIndex = 1;
            uint accountId = result.Read<uint>(fieldIndex++);
            string name = result.Read<string>(fieldIndex++);
            Race race = (Race)result.Read<byte>(fieldIndex++);
            Class class_ = (Class)result.Read<byte>(fieldIndex++);
            Gender gender = (Gender)result.Read<byte>(fieldIndex++);
            byte level = result.Read<byte>(fieldIndex++);
            uint xp = result.Read<uint>(fieldIndex++);
            ulong money = result.Read<ulong>(fieldIndex++);
            byte inventorySlots = result.Read<byte>(fieldIndex++);
            BagSlotFlags inventoryBagFlags = (BagSlotFlags)result.Read<uint>(fieldIndex++);
            BagSlotFlags[] bagSlotFlags = new BagSlotFlags[5];
            for (var i = 0; i < bagSlotFlags.Length; ++i)
                bagSlotFlags[i] = (BagSlotFlags)result.Read<uint>(fieldIndex++);

            byte bankSlots = result.Read<byte>(fieldIndex++);
            BagSlotFlags[] bankBagSlotFlags = new BagSlotFlags[7];
            BagSlotFlags bankBagFlags = (BagSlotFlags)result.Read<uint>(fieldIndex++);
            for (var i = 0; i < bankBagSlotFlags.Length; ++i)
                bankBagSlotFlags[i] = (BagSlotFlags)result.Read<uint>(fieldIndex++);

            PlayerRestState restState = (PlayerRestState)result.Read<byte>(fieldIndex++);
            PlayerFlags playerFlags = (PlayerFlags)result.Read<uint>(fieldIndex++);
            PlayerFlagsEx playerFlagsEx = (PlayerFlagsEx)result.Read<uint>(fieldIndex++);
            float position_x = result.Read<float>(fieldIndex++);
            float position_y = result.Read<float>(fieldIndex++);
            float position_z = result.Read<float>(fieldIndex++);
            uint mapId = result.Read<ushort>(fieldIndex++);
            float orientation = result.Read<float>(fieldIndex++);
            string taximask = result.Read<string>(fieldIndex++);
            long createTime = result.Read<long>(fieldIndex++);
            PlayerCreateMode createMode = (PlayerCreateMode)result.Read<byte>(fieldIndex++);
            byte cinematic = result.Read<byte>(fieldIndex++);
            uint totaltime = result.Read<uint>(fieldIndex++);
            uint leveltime = result.Read<uint>(fieldIndex++);
            float rest_bonus = result.Read<float>(fieldIndex++);
            long logout_time = result.Read<long>(fieldIndex++);
            byte is_logout_resting = result.Read<byte>(fieldIndex++);
            uint resettalents_cost = result.Read<uint>(fieldIndex++);
            long resettalents_time = result.Read<long>(fieldIndex++);
            uint primarySpecialization = result.Read<uint>(fieldIndex++);
            float trans_x = result.Read<float>(fieldIndex++);
            float trans_y = result.Read<float>(fieldIndex++);
            float trans_z = result.Read<float>(fieldIndex++);
            float trans_o = result.Read<float>(fieldIndex++);
            ulong transguid = result.Read<ulong>(fieldIndex++);
            PlayerExtraFlags extra_flags = (PlayerExtraFlags)result.Read<ushort>(fieldIndex++);
            uint summonedPetNumber = result.Read<uint>(fieldIndex++);
            ushort at_login = result.Read<ushort>(fieldIndex++);
            ushort zone = result.Read<ushort>(fieldIndex++);
            byte online = result.Read<byte>(fieldIndex++);
            long death_expire_time = result.Read<long>(fieldIndex++);
            string taxi_path = result.Read<string>(fieldIndex++);
            Difficulty dungeonDifficulty = (Difficulty)result.Read<byte>(fieldIndex++);
            uint totalKills = result.Read<uint>(fieldIndex++);
            ushort todayKills = result.Read<ushort>(fieldIndex++);
            ushort yesterdayKills = result.Read<ushort>(fieldIndex++);
            uint chosenTitle = result.Read<uint>(fieldIndex++);
            uint watchedFaction = result.Read<uint>(fieldIndex++);
            byte drunk = result.Read<byte>(fieldIndex++);
            uint health = result.Read<uint>(fieldIndex++);

            uint[] powers = new uint[(int)PowerType.MaxPerClass];
            for (var i = 0; i < powers.Length; ++i)
                powers[i] = result.Read<uint>(fieldIndex++);

            uint instance_id = result.Read<uint>(fieldIndex++);
            byte activeTalentGroup = result.Read<byte>(fieldIndex++);
            uint lootSpecId = result.Read<uint>(fieldIndex++);
            string exploredZones = result.Read<string>(fieldIndex++);
            string knownTitles = result.Read<string>(fieldIndex++);
            byte actionBars = result.Read<byte>(fieldIndex++);
            Difficulty raidDifficulty = (Difficulty)result.Read<byte>(fieldIndex++);
            Difficulty legacyRaidDifficulty = (Difficulty)result.Read<byte>(fieldIndex++);
            byte fishingSteps = result.Read<byte>(fieldIndex++);
            uint honor = result.Read<uint>(fieldIndex++);
            uint honorLevel = result.Read<uint>(fieldIndex++);
            PlayerRestState honorRestState = (PlayerRestState)result.Read<byte>(fieldIndex++);
            float honorRestBonus = result.Read<float>(fieldIndex++);
            byte numRespecs = result.Read<byte>(fieldIndex++);
            int personalTabardEmblemStyle = result.Read<int>(fieldIndex++);
            int personalTabardEmblemColor = result.Read<int>(fieldIndex++);
            int personalTabardBorderStyle = result.Read<int>(fieldIndex++);
            int personalTabardBorderColor = result.Read<int>(fieldIndex++);
            int personalTabardBackgroundColor = result.Read<int>(fieldIndex++);

            // check if the character's account in the db and the logged in account match.
            // player should be able to load/delete character only with correct account!
            if (accountId != GetSession().GetAccountId())
            {
                Log.outError(LogFilter.Player, "Player (GUID: {0}) loading from wrong account (is: {1}, should be: {2})", GetGUID().ToString(), GetSession().GetAccountId(), accountId);
                return false;
            }

            SQLResult banResult = holder.GetResult(PlayerLoginQueryLoad.Banned);
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
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)AtLoginFlags.Rename);
                stmt.AddValue(1, guid.GetCounter());
                DB.Characters.Execute(stmt);
                return false;
            }

            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.Name), GetName());

            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.WowAccount), GetSession().GetAccountGUID());
            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.BnetAccount), GetSession().GetBattlenetAccountGUID());

            if (gender >= Gender.None)
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong gender ({1}), can't be loaded.", guid.ToString(), gender);
                return false;
            }

            SetRace(race);
            SetClass(class_);
            SetGender(gender);

            // check if race/class combination is valid
            PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());
            if (info == null)
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong race/class ({1}/{2}), can't be loaded.", guid.ToString(), GetRace(), GetClass());
                return false;
            }

            SetLevel(level);
            SetXP(xp);

            StringArray exploredZonesStrings = new(exploredZones, ' ');
            for (int i = 0; i < exploredZonesStrings.Length && i / 2 < PlayerConst.ExploredZonesSize; ++i)
            {
                if (!ulong.TryParse(exploredZonesStrings[i], out ulong value))
                    value = 0;

                AddExploredZones(i / 2, (value << (32 * (i % 2))));
            }

            StringArray knownTitlesStrings = new(knownTitles, ' ');
            if ((knownTitlesStrings.Length % 2) == 0)
            {
                for (int i = 0; i < knownTitlesStrings.Length; ++i)
                    SetUpdateFieldFlagValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.KnownTitles, i / 2), (ulong)((long.Parse(knownTitlesStrings[i])) << (32 * (i % 2))));
            }

            SetObjectScale(1.0f);

            // load achievements before anything else to prevent multiple gains for the same achievement/criteria on every loading (as loading does call UpdateAchievementCriteria)
            m_achievementSys.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Achievements), holder.GetResult(PlayerLoginQueryLoad.CriteriaProgress));
            m_questObjectiveCriteriaMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectivesCriteria), holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectivesCriteriaProgress));

            SetMoney(Math.Min(money, PlayerConst.MaxMoneyAmount));

            List<ChrCustomizationChoice> customizations = new();
            SQLResult customizationsResult = holder.GetResult(PlayerLoginQueryLoad.Customizations);
            if (!customizationsResult.IsEmpty())
            {
                do
                {

                    ChrCustomizationChoice choice = new();
                    choice.ChrCustomizationOptionID = customizationsResult.Read<uint>(0);
                    choice.ChrCustomizationChoiceID = customizationsResult.Read<uint>(1);
                    customizations.Add(choice);

                } while (customizationsResult.NextRow());
            }

            SetCustomizations(customizations, false);
            SetInventorySlotCount(inventorySlots);
            SetBackpackAutoSortDisabled(inventoryBagFlags.HasFlag(BagSlotFlags.DisableAutoSort));
            SetBackpackSellJunkDisabled(inventoryBagFlags.HasFlag(BagSlotFlags.ExcludeJunkSell));
            for (int bagIndex = 0; bagIndex < bagSlotFlags.Length; ++bagIndex)
                ReplaceAllBagSlotFlags(bagIndex, bagSlotFlags[bagIndex]);

            SetBankBagSlotCount(bankSlots);
            SetBankAutoSortDisabled(bankBagFlags.HasFlag(BagSlotFlags.DisableAutoSort));
            for (int bagIndex = 0; bagIndex < bankBagSlotFlags.Length; ++bagIndex)
                ReplaceAllBankBagSlotFlags(bagIndex, bankBagSlotFlags[bagIndex]);

            SetNativeGender(gender);
            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.Inebriation), drunk);
            ReplaceAllPlayerFlags(playerFlags);
            ReplaceAllPlayerFlagsEx(playerFlagsEx);
            SetWatchedFactionIndex(watchedFaction);

            atLoginFlags = (AtLoginFlags)at_login;

            if (!GetSession().ValidateAppearance(GetRace(), GetClass(), gender, customizations))
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong Appearance values (Hair/Skin/Color), can't be loaded.", guid.ToString());
                return false;
            }

            // set which actionbars the client has active - DO NOT REMOVE EVER AGAIN (can be changed though, if it does change fieldwise)
            SetMultiActionBars(actionBars);

            m_fishingSteps = fishingSteps;

            InitDisplayIds();

            //Need to call it to initialize m_team (m_team can be calculated from race)
            //Other way is to saves m_team into characters table.
            SetFactionForRace(GetRace());

            // load home bind and check in same time class/race pair, it used later for restore broken positions
            if (!_LoadHomeBind(holder.GetResult(PlayerLoginQueryLoad.HomeBind)))
                return false;

            InitializeSkillFields();
            InitPrimaryProfessions();                               // to max set before any spell loaded

            // init saved position, and fix it later if problematic
            Relocate(position_x, position_y, position_z, orientation);

            SetDungeonDifficultyID(CheckLoadedDungeonDifficultyID(dungeonDifficulty));
            SetRaidDifficultyID(CheckLoadedRaidDifficultyID(raidDifficulty));
            SetLegacyRaidDifficultyID(CheckLoadedLegacyRaidDifficultyID(legacyRaidDifficulty));

            var RelocateToHomebind = new Action(() =>
            {
                mapId = homebind.GetMapId();
                instance_id = 0;
                Relocate(homebind);
            });

            _LoadGroup(holder.GetResult(PlayerLoginQueryLoad.Group));

            _LoadCurrency(holder.GetResult(PlayerLoginQueryLoad.Currency));
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.LifetimeHonorableKills), totalKills);
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.TodayHonorableKills), todayKills);
            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.YesterdayHonorableKills), yesterdayKills);

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
            else if (mapEntry.IsBattlegroundOrArena())
            {
                Battleground currentBg = null;
                if (m_bgData.bgInstanceID != 0)                                                //saved in Battleground
                    currentBg = Global.BattlegroundMgr.GetBattleground(m_bgData.bgInstanceID, BattlegroundTypeId.None);

                player_at_bg = currentBg != null && currentBg.IsPlayerInBattleground(GetGUID());

                if (player_at_bg && currentBg.GetStatus() != BattlegroundStatus.WaitLeave)
                {
                    map = currentBg.GetBgMap();

                    BattlegroundPlayer bgPlayer = currentBg.GetBattlegroundPlayerData(GetGUID());
                    if (bgPlayer != null)
                    {
                        AddBattlegroundQueueId(bgPlayer.queueTypeId);
                        m_bgData.bgTypeID = (BattlegroundTypeId)bgPlayer.queueTypeId.BattlemasterListId;

                        //join player to Battlegroundgroup
                        currentBg.EventPlayerLoggedIn(this);

                        SetInviteForBattlegroundQueueType(bgPlayer.queueTypeId, currentBg.GetInstanceID());
                        SetMercenaryForBattlegroundQueueType(bgPlayer.queueTypeId, currentBg.IsPlayerMercenaryInBattleground(GetGUID()));
                    }
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
                    instance_id = 0;

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
            else if (transguid != 0)
            {
                ObjectGuid transGUID = ObjectGuid.Create(HighGuid.Transport, transguid);

                Transport transport = null;
                Map transportMap = Global.MapMgr.CreateMap(mapId, this);
                if (transportMap != null)
                {
                    Transport transportOnMap = transportMap.GetTransport(transGUID);
                    if (transportOnMap != null)
                    {
                        if (transportOnMap.GetExpectedMapId() != mapId)
                        {
                            mapId = transportOnMap.GetExpectedMapId();
                            instanceId = 0;
                            transportMap = Global.MapMgr.CreateMap(mapId, this);
                            if (transportMap != null)
                                transport = transportMap.GetTransport(transGUID);
                        }
                        else
                            transport = transportOnMap;
                    }
                }

                if (transport != null)
                {
                    float x = trans_x;
                    float y = trans_y;
                    float z = trans_z;
                    float o = trans_o;

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
                        mapId = transport.GetMapId();

                        transport.AddPassenger(this);
                    }
                }
                else
                {
                    Log.outError(LogFilter.Player, "Player (guidlow {0}) have problems with transport guid ({1}). Teleport to bind location.", guid.ToString(), transguid);

                    RelocateToHomebind();
                }
            }
            // currently we do not support taxi in instance
            else if (!taxi_path.IsEmpty())
            {
                instance_id = 0;

                // Not finish taxi flight path
                if (m_bgData.HasTaxiPath())
                {
                    for (int i = 0; i < 2; ++i)
                        m_taxi.AddTaxiDestination(m_bgData.taxiPath[i]);
                }
                if (!m_taxi.LoadTaxiDestinationsFromString(taxi_path, GetTeam()))
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
            else if (mapEntry.IsDungeon() && instanceId != 0)
            {
                // try finding instance by id first
                map = Global.MapMgr.FindMap(mapId, instanceId);
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
            }

            // NOW player must have valid map
            // load the player's map here if it's not already loaded
            if (map == null)
                map = Global.MapMgr.CreateMap(mapId, this);

            AreaTriggerStruct areaTrigger = null;
            bool check = false;

            if (map == null)
            {
                areaTrigger = Global.ObjectMgr.GetGoBackTrigger(mapId);
                check = true;
            }
            else if (map.IsDungeon()) // if map is dungeon...
            {
                TransferAbortParams denyReason = map.CannotEnter(this); // ... and can't enter map, then look for entry point.
                if (denyReason != null)
                {
                    SendTransferAborted(map.GetId(), denyReason.Reason, denyReason.Arg, denyReason.MapDifficultyXConditionId);
                    areaTrigger = Global.ObjectMgr.GetGoBackTrigger(mapId);
                    check = true;
                }
                else if (instance_id != 0 && Global.InstanceLockMgr.FindActiveInstanceLock(guid, new MapDb2Entries(mapId, map.GetDifficultyID())) != null) // ... and instance is reseted then look for entrance.
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
            }

            if (map == null)
            {
                RelocateToHomebind();
                map = Global.MapMgr.CreateMap(mapId, this);
                if (map == null)
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

            if (player_at_bg)
                map.ToBattlegroundMap().GetBG().AddPlayer(this, m_bgData.queueId);

            // randomize first save time in range [CONFIG_INTERVAL_SAVE] around [CONFIG_INTERVAL_SAVE]
            // this must help in case next save after mass player load after server startup
            m_nextSave = RandomHelper.URand(m_nextSave / 2, m_nextSave * 3 / 2);

            SaveRecallPosition();

            long now = GameTime.GetGameTime();
            long logoutTime = logout_time;

            SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.LogoutTime), logoutTime);

            // since last logout (in seconds)
            uint time_diff = (uint)(now - logoutTime);

            // set value, including drunk invisibility detection
            // calculate sobering. after 15 minutes logged out, the player will be sober again
            if (time_diff < (uint)GetDrunkValue() * 9)
                SetDrunkValue((byte)(GetDrunkValue() - time_diff / 9));
            else
                SetDrunkValue(0);

            m_createTime = createTime;
            m_createMode = createMode;
            m_cinematic = cinematic;
            m_PlayedTimeTotal = totaltime;
            m_PlayedTimeLevel = leveltime;

            SetTalentResetCost(resettalents_cost);
            SetTalentResetTime(resettalents_time);

            m_taxi.LoadTaxiMask(taximask);            // must be before InitTaxiNodesForLevel

            _LoadPetStable(summonedPetNumber, holder.GetResult(PlayerLoginQueryLoad.PetSlots));

            // Honor system
            // Update Honor kills data
            m_lastHonorUpdateTime = logoutTime;
            UpdateHonorFields();

            m_deathExpireTime = death_expire_time;
            if (m_deathExpireTime > now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep)
                m_deathExpireTime = now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep - 1;

            RemoveUnitFlag2(UnitFlags2.ForceMovement);

            // make sure the unit is considered out of combat for proper loading
            ClearInCombat();

            // reset stats before loading any modifiers
            InitStatsForLevel();
            InitTaxiNodesForLevel();
            InitRunes();

            // rest bonus can only be calculated after InitStatsForLevel()
            _restMgr.LoadRestBonus(RestTypes.XP, restState, rest_bonus);

            // load skills after InitStatsForLevel because it triggering aura apply also
            _LoadSkills(holder.GetResult(PlayerLoginQueryLoad.Skills));
            UpdateSkillsForLevel();

            SetNumRespecs(numRespecs);
            SetPrimarySpecialization(primarySpecialization);
            SetActiveTalentGroup(activeTalentGroup);
            ChrSpecializationRecord primarySpec = GetPrimarySpecializationEntry();
            if (primarySpec == null || primarySpec.ClassID != (byte)GetClass() || GetActiveTalentGroup() >= PlayerConst.MaxSpecializations)
                ResetTalentSpecialization();

            ChrSpecializationRecord chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(lootSpecId);
            if (chrSpec != null)
            {
                if (chrSpec.ClassID == (uint)GetClass())
                    SetLootSpecId(lootSpecId);
            }

            UpdateDisplayPower();
            _LoadTalents(holder.GetResult(PlayerLoginQueryLoad.Talents));
            _LoadPvpTalents(holder.GetResult(PlayerLoginQueryLoad.PvpTalents));
            _LoadSpells(holder.GetResult(PlayerLoginQueryLoad.Spells), holder.GetResult(PlayerLoginQueryLoad.SpellFavorites));
            GetSession().GetCollectionMgr().LoadToys();
            GetSession().GetCollectionMgr().LoadHeirlooms();
            GetSession().GetCollectionMgr().LoadMounts();
            GetSession().GetCollectionMgr().LoadItemAppearances();
            GetSession().GetCollectionMgr().LoadTransmogIllusions();
            GetSession().GetCollectionMgr().LoadWarbandScenes();

            LearnSpecializationSpells();

            _LoadGlyphs(holder.GetResult(PlayerLoginQueryLoad.Glyphs));
            _LoadAuras(holder.GetResult(PlayerLoginQueryLoad.Auras), holder.GetResult(PlayerLoginQueryLoad.AuraEffects), time_diff);
            _LoadGlyphAuras();
            // add ghost flag (must be after aura load: PLAYER_FLAGS_GHOST set in aura)
            if (HasPlayerFlag(PlayerFlags.Ghost))
                m_deathState = DeathState.Dead;

            // Load spell locations - must be after loading auras
            _LoadStoredAuraTeleportLocations(holder.GetResult(PlayerLoginQueryLoad.AuraStoredLocations));

            // after spell load, learn rewarded spell if need also
            _LoadQuestStatus(holder.GetResult(PlayerLoginQueryLoad.QuestStatus));
            _LoadQuestStatusObjectives(holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectives));
            _LoadQuestStatusObjectiveSpawnTrackings(holder.GetResult(PlayerLoginQueryLoad.QuestStatusSpawnTracking));
            _LoadQuestStatusRewarded(holder.GetResult(PlayerLoginQueryLoad.QuestStatusRew));
            _LoadDailyQuestStatus(holder.GetResult(PlayerLoginQueryLoad.DailyQuestStatus));
            _LoadWeeklyQuestStatus(holder.GetResult(PlayerLoginQueryLoad.WeeklyQuestStatus));
            _LoadSeasonalQuestStatus(holder.GetResult(PlayerLoginQueryLoad.SeasonalQuestStatus));
            _LoadMonthlyQuestStatus(holder.GetResult(PlayerLoginQueryLoad.MonthlyQuestStatus));
            _LoadRandomBGStatus(holder.GetResult(PlayerLoginQueryLoad.RandomBg));

            // after spell and quest load
            InitTalentForLevel();
            LearnDefaultSkills();
            LearnCustomSpells();

            _LoadTraits(holder.GetResult(PlayerLoginQueryLoad.TraitConfigs), holder.GetResult(PlayerLoginQueryLoad.TraitEntries)); // must be after loading spells

            // must be before inventory (some items required reputation check)
            reputationMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Reputation));

            _LoadInventory(holder.GetResult(PlayerLoginQueryLoad.Inventory), holder.GetResult(PlayerLoginQueryLoad.Artifacts), holder.GetResult(PlayerLoginQueryLoad.Azerite),
                        holder.GetResult(PlayerLoginQueryLoad.AzeriteMilestonePowers), holder.GetResult(PlayerLoginQueryLoad.AzeriteUnlockedEssences), holder.GetResult(PlayerLoginQueryLoad.AzeriteEmpowered), time_diff);

            if (IsVoidStorageUnlocked())
                _LoadVoidStorage(holder.GetResult(PlayerLoginQueryLoad.VoidStorage));

            // update items with duration and realtime
            UpdateItemDuration(time_diff, true);

            StartLoadingActionButtons();

            // unread mails and next delivery time, actual mails not loaded
            _LoadMail(holder.GetResult(PlayerLoginQueryLoad.Mails),
                holder.GetResult(PlayerLoginQueryLoad.MailItems),
                holder.GetResult(PlayerLoginQueryLoad.MailItemsArtifact),
                holder.GetResult(PlayerLoginQueryLoad.MailItemsAzerite),
                holder.GetResult(PlayerLoginQueryLoad.MailItemsAzeriteMilestonePower),
                holder.GetResult(PlayerLoginQueryLoad.MailItemsAzeriteUnlockedEssence),
                holder.GetResult(PlayerLoginQueryLoad.MailItemsAzeriteEmpowered));

            m_social = Global.SocialMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.SocialList), GetGUID());

            // check PLAYER_CHOSEN_TITLE compatibility with PLAYER__FIELD_KNOWN_TITLES
            // note: PLAYER__FIELD_KNOWN_TITLES updated at quest status loaded
            if (chosenTitle != 0 && !HasTitle(chosenTitle))
                chosenTitle = 0;

            SetChosenTitle(chosenTitle);

            // has to be called after last Relocate() in Player.LoadFromDB
            SetFallInformation(0, GetPositionZ());

            GetSpellHistory().LoadFromDB<Player>(holder.GetResult(PlayerLoginQueryLoad.SpellCooldowns), holder.GetResult(PlayerLoginQueryLoad.SpellCharges));

            uint savedHealth = health;
            if (savedHealth == 0)
                m_deathState = DeathState.Corpse;

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
            SetHealth(savedHealth > GetMaxHealth() ? GetMaxHealth() : savedHealth);
            int loadedPowers = 0;
            for (PowerType i = 0; i < PowerType.Max; ++i)
            {
                if (Global.DB2Mgr.GetPowerIndexByClass(i, GetClass()) != (int)PowerType.Max)
                {
                    uint savedPower = powers[loadedPowers];
                    uint maxPower = m_unitData.MaxPower[loadedPowers];
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
                int runes = GetPower(PowerType.Runes);
                int maxRunes = GetMaxPower(PowerType.Runes);
                uint runeCooldown = GetRuneBaseCooldown();
                while (runes < maxRunes)
                {
                    SetRuneCooldown((byte)runes, runeCooldown);
                    ++runes;
                }
            }

            SetPersonalTabard(personalTabardEmblemStyle, personalTabardEmblemColor, personalTabardBorderStyle, personalTabardBorderColor, personalTabardBackgroundColor);

            Log.outDebug(LogFilter.Player, "The value of player {0} after load item and aura is: ", GetName());

            // GM state
            if (GetSession().HasPermission(RBACPermissions.RestoreSavedGmState))
            {
                switch (WorldConfig.GetIntValue(WorldCfg.GmLoginState))
                {
                    default:
                    case 0:
                        break;             // disable
                    case 1:
                        SetGameMaster(true);
                        break;             // enable
                    case 2:                                         // save state
                        if (extra_flags.HasAnyFlag(PlayerExtraFlags.GMOn))
                            SetGameMaster(true);
                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmVisibleState))
                {
                    default:
                    case 0:
                        SetGMVisible(false);
                        break;             // invisible
                    case 1:
                        break;             // visible
                    case 2:                                         // save state
                        if (extra_flags.HasAnyFlag(PlayerExtraFlags.GMInvisible))
                            SetGMVisible(false);
                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmChat))
                {
                    default:
                    case 0:
                        break;                 // disable
                    case 1:
                        SetGMChat(true);
                        break;                 // enable
                    case 2:                                         // save state
                        if (extra_flags.HasAnyFlag(PlayerExtraFlags.GMChat))
                            SetGMChat(true);
                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmWhisperingTo))
                {
                    default:
                    case 0:
                        break;         // disable
                    case 1:
                        SetAcceptWhispers(true);
                        break;         // enable
                    case 2:                                         // save state
                        if (extra_flags.HasAnyFlag(PlayerExtraFlags.AcceptWhispers))
                            SetAcceptWhispers(true);
                        break;
                }
            }

            InitPvP();

            // RaF stuff.
            if (GetSession().IsARecruiter() || (GetSession().GetRecruiterId() != 0))
                SetDynamicFlag(UnitDynFlags.ReferAFriend);

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
                float bubble0 = 0.031f;
                //speed collect rest bonus in offline, in logout, in tavern, city (section/in hour)
                float bubble1 = 0.125f;
                float bubble = is_logout_resting > 0
                    ? bubble1 * WorldConfig.GetFloatValue(WorldCfg.RateRestOfflineInTavernOrCity)
                    : bubble0 * WorldConfig.GetFloatValue(WorldCfg.RateRestOfflineInWilderness);

                _restMgr.AddRestBonus(RestTypes.XP, time_diff * _restMgr.CalcExtraPerSec(RestTypes.XP, bubble));
            }

            // Unlock battle pet system if it's enabled in bnet account
            if (GetSession().GetBattlePetMgr().IsBattlePetSystemEnabled())
                LearnSpell(SharedConst.SpellBattlePetTraining, false);

            m_achievementSys.CheckAllAchievementCriteria(this);
            m_questObjectiveCriteriaMgr.CheckAllQuestObjectiveCriteria(this);

            PushQuests();

            foreach (var transmogIllusion in CliDB.TransmogIllusionStorage.Values)
            {
                if (!transmogIllusion.HasFlag(TransmogIllusionFlags.PlayerConditionGrantsOnLogin))
                    continue;

                if (GetSession().GetCollectionMgr().HasTransmogIllusion(transmogIllusion.Id))
                    continue;

                if (!ConditionManager.IsPlayerMeetingCondition(this, (uint)transmogIllusion.UnlockConditionID))
                    continue;

                GetSession().GetCollectionMgr().AddTransmogIllusion(transmogIllusion.Id);
            }

            return true;
        }

        public void SaveToDB(bool create = false)
        {
            SQLTransaction loginTransaction = new();
            SQLTransaction characterTransaction = new();

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

            PreparedStatement stmt;
            byte index = 0;

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_FISHINGSTEPS);
            stmt.AddValue(0, GetGUID().GetCounter());
            characterTransaction.Append(stmt);

            static float finiteAlways(float f) { return float.IsFinite(f) ? f : 0.0f; }
            ;

            if (create)
            {
                //! Insert query
                /// @todo: Filter out more redundant fields that can take their default value at player create
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER);
                stmt.AddValue(index++, GetGUID().GetCounter());
                stmt.AddValue(index++, GetSession().GetAccountId());
                stmt.AddValue(index++, GetName());
                stmt.AddValue(index++, (byte)GetRace());
                stmt.AddValue(index++, (byte)GetClass());
                stmt.AddValue(index++, (byte)GetNativeGender());   // save gender from PLAYER_BYTES_3, UNIT_BYTES_0 changes with every transform effect
                stmt.AddValue(index++, GetLevel());
                stmt.AddValue(index++, GetXP());
                stmt.AddValue(index++, GetMoney());
                stmt.AddValue(index++, GetInventorySlotCount());

                BagSlotFlags inventoryFlags = BagSlotFlags.None;
                if (m_activePlayerData.BackpackAutoSortDisabled)
                    inventoryFlags |= BagSlotFlags.DisableAutoSort;
                if (m_activePlayerData.BackpackSellJunkDisabled)
                    inventoryFlags |= BagSlotFlags.ExcludeJunkSell;
                stmt.AddValue(index++, (uint)inventoryFlags);
                foreach (uint bagSlotFlag in m_activePlayerData.BagSlotFlags)
                    stmt.AddValue(index++, bagSlotFlag);
                stmt.AddValue(index++, GetBankBagSlotCount());

                inventoryFlags = BagSlotFlags.None;
                if (m_activePlayerData.BankAutoSortDisabled)
                    inventoryFlags |= BagSlotFlags.DisableAutoSort;
                stmt.AddValue(index++, (uint)inventoryFlags);
                foreach (uint bankBagSlotFlag in m_activePlayerData.BankBagSlotFlags)
                    stmt.AddValue(index++, bankBagSlotFlag);

                stmt.AddValue(index++, m_activePlayerData.RestInfo[(int)RestTypes.XP].StateID);
                stmt.AddValue(index++, m_playerData.PlayerFlags);
                stmt.AddValue(index++, m_playerData.PlayerFlagsEx);
                stmt.AddValue(index++, (ushort)GetMapId());
                stmt.AddValue(index++, GetInstanceId());
                stmt.AddValue(index++, (byte)GetDungeonDifficultyID());
                stmt.AddValue(index++, (byte)GetRaidDifficultyID());
                stmt.AddValue(index++, (byte)GetLegacyRaidDifficultyID());
                stmt.AddValue(index++, finiteAlways(GetPositionX()));
                stmt.AddValue(index++, finiteAlways(GetPositionY()));
                stmt.AddValue(index++, finiteAlways(GetPositionZ()));
                stmt.AddValue(index++, finiteAlways(GetOrientation()));
                stmt.AddValue(index++, finiteAlways(GetTransOffsetX()));
                stmt.AddValue(index++, finiteAlways(GetTransOffsetY()));
                stmt.AddValue(index++, finiteAlways(GetTransOffsetZ()));
                stmt.AddValue(index++, finiteAlways(GetTransOffsetO()));
                ulong transLowGUID = 0;
                Transport transport = GetTransport<Transport>();
                if (transport != null)
                    transLowGUID = transport.GetGUID().GetCounter();
                stmt.AddValue(index++, transLowGUID);

                StringBuilder ss = new();
                for (int i = 0; i < m_taxi.m_taximask.Length; ++i)
                    ss.Append(m_taxi.m_taximask[i] + " ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, m_createTime);
                stmt.AddValue(index++, (byte)m_createMode);
                stmt.AddValue(index++, m_cinematic);
                stmt.AddValue(index++, m_PlayedTimeTotal);
                stmt.AddValue(index++, m_PlayedTimeLevel);
                stmt.AddValue(index++, finiteAlways(_restMgr.GetRestBonus(RestTypes.XP)));
                stmt.AddValue(index++, GameTime.GetGameTime());
                stmt.AddValue(index++, (HasPlayerFlag(PlayerFlags.Resting) ? 1 : 0));
                //save, far from tavern/city
                //save, but in tavern/city
                stmt.AddValue(index++, GetTalentResetCost());
                stmt.AddValue(index++, GetTalentResetTime());
                stmt.AddValue(index++, (uint)GetPrimarySpecialization());
                stmt.AddValue(index++, (ushort)m_ExtraFlags);
                stmt.AddValue(index++, 0); // summonedPetNumber
                stmt.AddValue(index++, (ushort)atLoginFlags);
                stmt.AddValue(index++, m_deathExpireTime);

                ss.Clear();
                ss.Append(m_taxi.SaveTaxiDestinationsToString());

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, m_activePlayerData.LifetimeHonorableKills);
                stmt.AddValue(index++, m_activePlayerData.TodayHonorableKills);
                stmt.AddValue(index++, m_activePlayerData.YesterdayHonorableKills);
                stmt.AddValue(index++, m_playerData.PlayerTitle);
                stmt.AddValue(index++, m_activePlayerData.WatchedFactionIndex);
                stmt.AddValue(index++, GetDrunkValue());
                stmt.AddValue(index++, GetHealth());

                for (int i = 0; i < (int)PowerType.MaxPerClass; ++i)
                    stmt.AddValue(index++, m_unitData.Power[i]);

                stmt.AddValue(index++, GetSession().GetLatency());
                stmt.AddValue(index++, GetActiveTalentGroup());
                stmt.AddValue(index++, GetLootSpecId());

                ss.Clear();
                for (int i = 0; i < m_activePlayerData.BitVectors.GetValue().Values[(int)PlayerDataFlag.ExploredZonesIndex].Values.Size(); ++i)
                    ss.Append($"{(uint)(m_activePlayerData.BitVectors.GetValue().Values[(int)PlayerDataFlag.ExploredZonesIndex].Values[i] & 0xFFFFFFFF)} {(uint)((m_activePlayerData.BitVectors.GetValue().Values[(int)PlayerDataFlag.ExploredZonesIndex].Values[i] >> 32) & 0xFFFFFFFF)} ");

                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                // cache equipment...
                for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
                {
                    Item item = GetItemByPos(InventorySlots.Bag0, i);
                    if (item != null)
                    {
                        ss.Append($"{(uint)item.GetTemplate().GetInventoryType()} {item.GetDisplayId(this)} ");
                        SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetVisibleEnchantmentId(this));
                        if (enchant != null)
                            ss.Append(enchant.ItemVisual);
                        else
                            ss.Append('0');

                        ss.Append($" {(uint)CliDB.ItemStorage.LookupByKey(item.GetVisibleEntry(this)).SubclassID} {(uint)item.GetVisibleSecondaryModifiedAppearanceId(this)} ");
                    }
                    else
                        ss.Append("0 0 0 0 0 ");
                }

                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                for (int i = 0; i < m_activePlayerData.KnownTitles.Size(); ++i)
                    ss.Append($"{(uint)(m_activePlayerData.KnownTitles[i] & 0xFFFFFFFF)} {(uint)((m_activePlayerData.KnownTitles[i] >> 32) & 0xFFFFFFFF)} ");

                stmt.AddValue(index++, ss.ToString());

                stmt.AddValue(index++, m_activePlayerData.MultiActionBars);
                var currentRealm = Global.RealmMgr.GetCurrentRealm();
                if (currentRealm != null)
                    stmt.AddValue(index++, ClientBuildHelper.GetMinorMajorBugfixVersionForBuild(currentRealm.Build));
                else
                    stmt.AddValue(index++, 0);
            }
            else
            {
                // Update query
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER);
                stmt.AddValue(index++, GetName());
                stmt.AddValue(index++, (byte)GetRace());
                stmt.AddValue(index++, (byte)GetClass());
                stmt.AddValue(index++, (byte)GetNativeGender());   // save gender from PLAYER_BYTES_3, UNIT_BYTES_0 changes with every transform effect
                stmt.AddValue(index++, GetLevel());
                stmt.AddValue(index++, GetXP());
                stmt.AddValue(index++, GetMoney());
                stmt.AddValue(index++, GetInventorySlotCount());

                BagSlotFlags inventoryFlags = BagSlotFlags.None;
                if (m_activePlayerData.BackpackAutoSortDisabled)
                    inventoryFlags |= BagSlotFlags.DisableAutoSort;
                if (m_activePlayerData.BackpackSellJunkDisabled)
                    inventoryFlags |= BagSlotFlags.ExcludeJunkSell;
                stmt.AddValue(index++, (uint)inventoryFlags);
                foreach (uint bagSlotFlag in m_activePlayerData.BagSlotFlags)
                    stmt.AddValue(index++, bagSlotFlag);
                stmt.AddValue(index++, GetBankBagSlotCount());

                inventoryFlags = BagSlotFlags.None;
                if (m_activePlayerData.BankAutoSortDisabled)
                    inventoryFlags |= BagSlotFlags.DisableAutoSort;
                stmt.AddValue(index++, (uint)inventoryFlags);
                foreach (uint bankBagSlotFlag in m_activePlayerData.BankBagSlotFlags)
                    stmt.AddValue(index++, bankBagSlotFlag);

                stmt.AddValue(index++, m_activePlayerData.RestInfo[(int)RestTypes.XP].StateID);
                stmt.AddValue(index++, m_playerData.PlayerFlags);
                stmt.AddValue(index++, m_playerData.PlayerFlagsEx);

                if (!IsBeingTeleported())
                {
                    stmt.AddValue(index++, (ushort)GetMapId());
                    stmt.AddValue(index++, GetInstanceId());
                    stmt.AddValue(index++, (byte)GetDungeonDifficultyID());
                    stmt.AddValue(index++, (byte)GetRaidDifficultyID());
                    stmt.AddValue(index++, (byte)GetLegacyRaidDifficultyID());
                    stmt.AddValue(index++, finiteAlways(GetPositionX()));
                    stmt.AddValue(index++, finiteAlways(GetPositionY()));
                    stmt.AddValue(index++, finiteAlways(GetPositionZ()));
                    stmt.AddValue(index++, finiteAlways(GetOrientation()));
                }
                else
                {
                    stmt.AddValue(index++, (ushort)GetTeleportDest().Location.GetMapId());
                    stmt.AddValue(index++, 0);
                    stmt.AddValue(index++, (byte)GetDungeonDifficultyID());
                    stmt.AddValue(index++, (byte)GetRaidDifficultyID());
                    stmt.AddValue(index++, (byte)GetLegacyRaidDifficultyID());
                    stmt.AddValue(index++, finiteAlways(GetTeleportDest().Location.GetPositionX()));
                    stmt.AddValue(index++, finiteAlways(GetTeleportDest().Location.GetPositionY()));
                    stmt.AddValue(index++, finiteAlways(GetTeleportDest().Location.GetPositionZ()));
                    stmt.AddValue(index++, finiteAlways(GetTeleportDest().Location.GetOrientation()));
                }

                stmt.AddValue(index++, finiteAlways(GetTransOffsetX()));
                stmt.AddValue(index++, finiteAlways(GetTransOffsetY()));
                stmt.AddValue(index++, finiteAlways(GetTransOffsetZ()));
                stmt.AddValue(index++, finiteAlways(GetTransOffsetO()));
                ulong transLowGUID = 0;
                Transport transport = GetTransport<Transport>();
                if (transport != null)
                    transLowGUID = transport.GetGUID().GetCounter();
                stmt.AddValue(index++, transLowGUID);

                StringBuilder ss = new();
                for (int i = 0; i < m_taxi.m_taximask.Length; ++i)
                    ss.Append(m_taxi.m_taximask[i] + " ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, m_cinematic);
                stmt.AddValue(index++, m_PlayedTimeTotal);
                stmt.AddValue(index++, m_PlayedTimeLevel);
                stmt.AddValue(index++, finiteAlways(_restMgr.GetRestBonus(RestTypes.XP)));
                stmt.AddValue(index++, GameTime.GetGameTime());
                stmt.AddValue(index++, (HasPlayerFlag(PlayerFlags.Resting) ? 1 : 0));
                //save, far from tavern/city
                //save, but in tavern/city
                stmt.AddValue(index++, GetTalentResetCost());
                stmt.AddValue(index++, GetTalentResetTime());
                stmt.AddValue(index++, GetNumRespecs());
                stmt.AddValue(index++, (uint)GetPrimarySpecialization());
                stmt.AddValue(index++, (ushort)m_ExtraFlags);
                PetStable petStable = GetPetStable();
                if (petStable != null)
                    stmt.AddValue(index++, petStable.GetCurrentPet() != null && petStable.GetCurrentPet().Health > 0 ? petStable.GetCurrentPet().PetNumber : 0); // summonedPetNumber
                else
                    stmt.AddValue(index++, 0); // summonedPetNumber
                stmt.AddValue(index++, (ushort)atLoginFlags);
                stmt.AddValue(index++, GetZoneId());
                stmt.AddValue(index++, m_deathExpireTime);

                ss.Clear();
                ss.Append(m_taxi.SaveTaxiDestinationsToString());

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, m_activePlayerData.LifetimeHonorableKills);
                stmt.AddValue(index++, m_activePlayerData.TodayHonorableKills);
                stmt.AddValue(index++, m_activePlayerData.YesterdayHonorableKills);
                stmt.AddValue(index++, m_playerData.PlayerTitle);
                stmt.AddValue(index++, m_activePlayerData.WatchedFactionIndex);
                stmt.AddValue(index++, GetDrunkValue());
                stmt.AddValue(index++, GetHealth());

                for (int i = 0; i < (int)PowerType.MaxPerClass; ++i)
                    stmt.AddValue(index++, m_unitData.Power[i]);

                stmt.AddValue(index++, GetSession().GetLatency());
                stmt.AddValue(index++, GetActiveTalentGroup());
                stmt.AddValue(index++, GetLootSpecId());

                ss.Clear();
                for (int i = 0; i < m_activePlayerData.BitVectors.GetValue().Values[(int)PlayerDataFlag.ExploredZonesIndex].Values.Size(); ++i)
                    ss.Append($"{(uint)(m_activePlayerData.BitVectors.GetValue().Values[(int)PlayerDataFlag.ExploredZonesIndex].Values[i] & 0xFFFFFFFF)} {(uint)((m_activePlayerData.BitVectors.GetValue().Values[(int)PlayerDataFlag.ExploredZonesIndex].Values[i] >> 32) & 0xFFFFFFFF)} ");

                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                // cache equipment...
                for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
                {
                    Item item = GetItemByPos(InventorySlots.Bag0, i);
                    if (item != null)
                    {
                        ss.Append($"{(uint)item.GetTemplate().GetInventoryType()} {item.GetDisplayId(this)} ");
                        SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetVisibleEnchantmentId(this));
                        if (enchant != null)
                            ss.Append(enchant.ItemVisual);
                        else
                            ss.Append('0');

                        ss.Append($" {(uint)CliDB.ItemStorage.LookupByKey(item.GetVisibleEntry(this)).SubclassID} {(uint)item.GetVisibleSecondaryModifiedAppearanceId(this)} ");
                    }
                    else
                        ss.Append("0 0 0 0 0 ");
                }

                stmt.AddValue(index++, ss.ToString());

                ss.Clear();
                for (int i = 0; i < m_activePlayerData.KnownTitles.Size(); ++i)
                    ss.Append($"{(uint)(m_activePlayerData.KnownTitles[i] & 0xFFFFFFFF)} {(uint)((m_activePlayerData.KnownTitles[i] >> 32) & 0xFFFFFFFF)} ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, m_activePlayerData.MultiActionBars);

                stmt.AddValue(index++, IsInWorld && !GetSession().PlayerLogout() ? 1 : 0);
                stmt.AddValue(index++, m_activePlayerData.Honor);
                stmt.AddValue(index++, GetHonorLevel());
                stmt.AddValue(index++, m_activePlayerData.RestInfo[(int)RestTypes.Honor].StateID);
                stmt.AddValue(index++, finiteAlways(_restMgr.GetRestBonus(RestTypes.Honor)));
                var currentRealm = Global.RealmMgr.GetCurrentRealm();
                if (currentRealm != null)
                    stmt.AddValue(index++, ClientBuildHelper.GetMinorMajorBugfixVersionForBuild(currentRealm.Build));
                else
                    stmt.AddValue(index++, 0);

                // Index
                stmt.AddValue(index, GetGUID().GetCounter());
            }

            characterTransaction.Append(stmt);

            if (m_fishingSteps != 0)
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_FISHINGSTEPS);
                index = 0;
                stmt.AddValue(index++, GetGUID().GetCounter());
                stmt.AddValue(index++, m_fishingSteps);
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
            _SaveTraits(characterTransaction);
            _SaveSpells(characterTransaction);
            GetSpellHistory().SaveToDB<Player>(characterTransaction);
            _SaveActions(characterTransaction);
            _SaveAuras(characterTransaction);
            _SaveSkills(characterTransaction);
            _SaveStoredAuraTeleportLocations(characterTransaction);
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
            GetSession().GetCollectionMgr().SaveAccountTransmogIllusions(loginTransaction);
            GetSession().GetCollectionMgr().SaveAccountWarbandScenes(loginTransaction);

            var currentRealmId = Global.RealmMgr.GetCurrentRealmId();

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_BNET_LAST_PLAYER_CHARACTERS);
            stmt.AddValue(0, GetSession().GetAccountId());
            stmt.AddValue(1, currentRealmId.Region);
            stmt.AddValue(2, currentRealmId.Site);
            loginTransaction.Append(stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_BNET_LAST_PLAYER_CHARACTERS);
            stmt.AddValue(0, GetSession().GetAccountId());
            stmt.AddValue(1, currentRealmId.Region);
            stmt.AddValue(2, currentRealmId.Site);
            stmt.AddValue(3, currentRealmId.Index);
            stmt.AddValue(4, GetName());
            stmt.AddValue(5, GetGUID().GetCounter());
            stmt.AddValue(6, GameTime.GetGameTime());
            loginTransaction.Append(stmt);

            // save pet (hunter pet level and experience and all type pets health/mana).
            Pet pet = GetPet();
            if (pet != null)
                pet.SavePetToDB(PetSaveMode.AsCurrent);
        }
        void DeleteSpellFromAllPlayers(uint spellId)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_INVALID_SPELL_SPELLS);
            stmt.AddValue(0, spellId);
            DB.Characters.Execute(stmt);
        }

        public static uint GetZoneIdFromDB(ObjectGuid guid)
        {
            ulong guidLow = guid.GetCounter();
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_ZONE);
            stmt.AddValue(0, guidLow);
            SQLResult result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
                return 0;

            uint zone = result.Read<ushort>(0);
            if (zone == 0)
            {
                // stored zone is zero, use generic and slow zone detection
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION_XYZ);
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

                zone = Global.TerrainMgr.GetZoneId(PhasingHandler.EmptyPhaseShift, map, posx, posy, posz);

                if (zone > 0)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ZONE);

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
            ulong guid = playerGuid.GetCounter();
            CharDeleteMethod charDelete_method = (CharDeleteMethod)WorldConfig.GetIntValue(WorldCfg.ChardeleteMethod);
            CharacterCacheEntry characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(playerGuid);
            string name = "<Unknown>";
            if (characterInfo != null)
                name = characterInfo.Name;

            if (deleteFinally)
                charDelete_method = CharDeleteMethod.Remove;
            else if (characterInfo != null)    // To avoid a Select, we select loaded data. If it doesn't exist, return.
            {
                // Define the required variables
                uint charDeleteMinLvl;

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

            SQLTransaction trans = new();
            SQLTransaction loginTransaction = new();

            ulong guildId = Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(playerGuid);
            if (guildId != 0)
            {
                Guild guild = Global.GuildMgr.GetGuildById(guildId);
                if (guild != null)
                    guild.DeleteMember(trans, playerGuid, false, false, true);
            }

            // remove from arena teams
            LeaveAllArenaTeams(playerGuid);

            // the player was uninvited already on logout so just remove from group
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
            stmt.AddValue(0, guid);
            SQLResult resultGroup = DB.Characters.Query(stmt);

            if (!resultGroup.IsEmpty())
            {
                Group group = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));
                if (group != null)
                    RemoveFromGroup(group, playerGuid);
            }

            // Remove signs from petitions (also remove petitions if owner);
            RemovePetitionsAndSigns(playerGuid);

            switch (charDelete_method)
            {
                // Completely remove from the database
                case CharDeleteMethod.Remove:
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_COD_ITEM_MAIL);
                    stmt.AddValue(0, guid);
                    SQLResult resultMail = DB.Characters.Query(stmt);
                    if (!resultMail.IsEmpty())
                    {
                        MultiMap<ulong, Item> itemsByMail = new();

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
                        stmt.AddValue(0, guid);
                        SQLResult resultItems = DB.Characters.Query(stmt);

                        if (!resultItems.IsEmpty())
                        {
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_ARTIFACT);
                            stmt.AddValue(0, guid);
                            SQLResult artifactResult = DB.Characters.Query(stmt);

                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE);
                            stmt.AddValue(0, guid);
                            SQLResult azeriteResult = DB.Characters.Query(stmt);

                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_MILESTONE_POWER);
                            stmt.AddValue(0, guid);
                            SQLResult azeriteItemMilestonePowersResult = DB.Characters.Query(stmt);

                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_UNLOCKED_ESSENCE);
                            stmt.AddValue(0, guid);
                            SQLResult azeriteItemUnlockedEssencesResult = DB.Characters.Query(stmt);

                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_EMPOWERED);
                            stmt.AddValue(0, guid);
                            SQLResult azeriteEmpoweredItemResult = DB.Characters.Query(stmt);

                            Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
                            ItemAdditionalLoadInfo.Init(additionalData, artifactResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

                            do
                            {
                                ulong mailId = resultItems.Read<ulong>(53);
                                Item mailItem = _LoadMailedItem(playerGuid, null, mailId, null, resultItems.GetFields(), additionalData.LookupByKey(resultItems.Read<ulong>(0)));
                                if (mailItem != null)
                                    itemsByMail.Add(mailId, mailItem);

                            } while (resultItems.NextRow());
                        }

                        do
                        {
                            ulong mail_id = resultMail.Read<ulong>(0);
                            MailMessageType mailType = (MailMessageType)resultMail.Read<byte>(1);
                            ushort mailTemplateId = resultMail.Read<ushort>(2);
                            uint sender = resultMail.Read<uint>(3);
                            string subject = resultMail.Read<string>(4);
                            string body = resultMail.Read<string>(5);
                            ulong money = resultMail.Read<ulong>(6);
                            bool has_items = resultMail.Read<bool>(7);

                            // We can return mail now
                            // So firstly delete the old one
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
                            stmt.AddValue(0, mail_id);
                            trans.Append(stmt);

                            // Mail is not from player
                            if (mailType != MailMessageType.Normal)
                            {
                                if (has_items)
                                {
                                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                                    stmt.AddValue(0, mail_id);
                                    trans.Append(stmt);
                                }
                                continue;
                            }

                            MailDraft draft = new(subject, body);
                            if (mailTemplateId != 0)
                                draft = new MailDraft(mailTemplateId, false);    // items are already included

                            var itemsList = itemsByMail.LookupByKey(mail_id);
                            if (itemsList != null)
                            {
                                foreach (Item item in itemsList)
                                    draft.AddItem(item);

                                itemsByMail.Remove(mail_id);
                            }

                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                            stmt.AddValue(0, mail_id);
                            trans.Append(stmt);

                            uint pl_account = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(ObjectGuid.Create(HighGuid.Player, guid));

                            draft.AddMoney(money).SendReturnToSender(pl_account, guid, sender, trans);
                        }
                        while (resultMail.NextRow());

                        // Free remaining items
                        foreach (var pair in itemsByMail)
                            pair.Value.Dispose();
                    }

                    // Unsummon and delete for pets in world is not required: player deleted from CLI or character list with not loaded pet.
                    // NOW we can finally clear other DB data related to character
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_PET_IDS);
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
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_SOCIAL);
                    stmt.AddValue(0, guid);
                    SQLResult resultFriends = DB.Characters.Query(stmt);

                    if (!resultFriends.IsEmpty())
                    {
                        do
                        {
                            Player playerFriend = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, resultFriends.Read<ulong>(0)));
                            if (playerFriend != null)
                            {
                                playerFriend.GetSocial().RemoveFromSocialList(playerGuid, SocialFlag.All);
                                Global.SocialMgr.SendFriendStatus(playerFriend, FriendsResult.Removed, playerGuid);
                            }
                        } while (resultFriends.NextRow());
                    }

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_ACCOUNT_DATA);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_ARENA_STATS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_EFFECT);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_AURA);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_BGDATA);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_BATTLEGROUND_RANDOM);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_CUF_PROFILES);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_CURRENCY);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_GIFT);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_INSTANCE_LOCK_BY_GUID);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_SPAWN_TRACKING);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_REPUTATION);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_COOLDOWNS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_FRIEND);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_GUID);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEMS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME_BY_OWNER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENTS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_PROGRESS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_EQUIPMENTSETS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TRANSMOG_OUTFITS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOG_BY_PLAYER);
                    stmt.AddValue(0, guid);
                    stmt.AddValue(1, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG_BY_PLAYER);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_GLYPHS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TALENT);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SKILLS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_STATS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_CHAR_GUID);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_FISHINGSTEPS);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_FAVORITE_AUCTIONS_BY_CHAR);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATIONS_BY_GUID);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME_BY_OWNER);
                    stmt.AddValue(0, guid);
                    stmt.AddValue(1, Global.RealmMgr.GetCurrentRealmId().Index);
                    loginTransaction.Append(stmt);

                    stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PETS_BY_OWNER);
                    stmt.AddValue(0, guid);
                    stmt.AddValue(1, Global.RealmMgr.GetCurrentRealmId().Index);
                    loginTransaction.Append(stmt);

                    Corpse.DeleteFromDB(playerGuid, trans);

                    Garrison.DeleteFromDB(guid, trans);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES_BY_CHAR);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS_BY_CHAR);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    Global.CharacterCacheStorage.DeleteCharacterCacheEntry(playerGuid, name);
                    break;
                }
                // The character gets unlinked from the account, the name gets freed up and appears as deleted ingame
                case CharDeleteMethod.Unlink:
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_DELETE_INFO);
                    stmt.AddValue(0, guid);
                    trans.Append(stmt);

                    Global.CharacterCacheStorage.UpdateCharacterInfoDeleted(playerGuid, true, "");
                    break;
                }
                default:
                    Log.outError(LogFilter.Player, "Player:DeleteFromDB: Unsupported delete method: {0}.", charDelete_method);

                    if (trans.commands.Count > 0)
                        DB.Characters.CommitTransaction(trans);
                    return;
            }

            DB.Login.CommitTransaction(loginTransaction);
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

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_OLD_CHARS);
            stmt.AddValue(0, (uint)(GameTime.GetGameTime() - keepDays * Time.Day));
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
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER_POSITION);
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
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION);
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
