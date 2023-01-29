// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Game.Entities
{
    public partial class Player
    {
        private void _LoadInventory(SQLResult result, SQLResult artifactsResult, SQLResult azeriteResult, SQLResult azeriteItemMilestonePowersResult, SQLResult azeriteItemUnlockedEssencesResult, SQLResult azeriteEmpoweredItemResult, uint timeDiff)
        {
            Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
            ItemAdditionalLoadInfo.Init(additionalData, artifactsResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

            if (!result.IsEmpty())
            {
                uint zoneId = GetZoneId();
                Dictionary<ObjectGuid, Bag> bagMap = new(); // fast Guid lookup for bags
                Dictionary<ObjectGuid, Item> invalidBagMap = new(); // fast Guid lookup for bags
                Queue<Item> problematicItems = new();
                SQLTransaction trans = new();

                // Prevent items from being added to the queue while loading
                ItemUpdateQueueBlocked = true;

                do
                {
                    Item item = _LoadItem(trans, zoneId, timeDiff, result.GetFields());

                    if (item != null)
                    {
                        var addionalData = additionalData.LookupByKey(item.GetGUID().GetCounter());

                        if (addionalData != null)
                        {
                            if (item.GetTemplate().GetArtifactID() != 0 &&
                                addionalData.Artifact != null)
                                item.LoadArtifactData(this, addionalData.Artifact.Xp, addionalData.Artifact.ArtifactAppearanceId, addionalData.Artifact.ArtifactTierId, addionalData.Artifact.ArtifactPowers);

                            if (addionalData.AzeriteItem != null)
                            {
                                AzeriteItem azeriteItem = item.ToAzeriteItem();

                                azeriteItem?.LoadAzeriteItemData(this, addionalData.AzeriteItem);
                            }

                            if (addionalData.AzeriteEmpoweredItem != null)
                            {
                                AzeriteEmpoweredItem azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();

                                azeriteEmpoweredItem?.LoadAzeriteEmpoweredItemData(this, addionalData.AzeriteEmpoweredItem);
                            }
                        }


                        ulong counter = result.Read<ulong>(51);
                        ObjectGuid bagGuid = counter != 0 ? ObjectGuid.Create(HighGuid.Item, counter) : ObjectGuid.Empty;
                        byte slot = result.Read<byte>(52);

                        GetSession().GetCollectionMgr().CheckHeirloomUpgrades(item);
                        GetSession().GetCollectionMgr().AddItemAppearance(item);

                        InventoryResult err = InventoryResult.Ok;

                        if (item.HasItemFlag(ItemFieldFlags.Child))
                        {
                            Item parent = GetItemByGuid(item.GetCreator());

                            if (parent)
                            {
                                parent.SetChildItem(item.GetGUID());
                                item.CopyArtifactDataFromParent(parent);
                            }
                            else
                            {
                                Log.outError(LogFilter.Player, $"Player._LoadInventory: Player '{GetName()}' ({GetGUID()}) has child Item ({item.GetGUID()}, entry: {item.GetEntry()}) which can't be loaded into inventory because parent Item was not found (Bag {bagGuid}, Slot: {slot}). Item will be sent by mail.");
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
                            {
                                if (item.IsBag())
                                    invalidBagMap.Add(item.GetGUID(), item);
                            }
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
                                Log.outError(LogFilter.Player,
                                             "LoadInventory: player (GUID: {0}, Name: '{1}') has Item (GUID: {2}, entry: {3}) which doesnt have a valid bag (Bag GUID: {4}, Slot: {5}). Possible cheat?",
                                             GetGUID().ToString(),
                                             GetName(),
                                             item.GetGUID().ToString(),
                                             item.GetEntry(),
                                             bagGuid,
                                             slot);

                                item.DeleteFromInventoryDB(trans);

                                continue;
                            }
                        }

                        // Item's State may have changed after storing
                        if (err == InventoryResult.Ok)
                        {
                            item.SetState(ItemUpdateState.Unchanged, this);
                        }
                        else
                        {
                            Log.outError(LogFilter.Player,
                                         "LoadInventory: player (GUID: {0}, Name: '{1}') has Item (GUID: {2}, entry: {3}) which can't be loaded into inventory (Bag GUID: {4}, Slot: {5}) by reason {6}. " +
                                         "Item will be sent by mail.",
                                         GetGUID().ToString(),
                                         GetName(),
                                         item.GetGUID().ToString(),
                                         item.GetEntry(),
                                         bagGuid,
                                         slot,
                                         err);

                            item.DeleteFromInventoryDB(trans);
                            problematicItems.Enqueue(item);
                        }
                    }
                } while (result.NextRow());

                ItemUpdateQueueBlocked = false;

                // Send problematic items by mail
                while (problematicItems.Count != 0)
                {
                    string subject = Global.ObjectMgr.GetCypherString(CypherStrings.NotEquippedItem);
                    MailDraft draft = new(subject, "There were problems with equipping Item(s).");

                    for (int i = 0; problematicItems.Count != 0 && i < SharedConst.MaxMailItems; ++i)
                        draft.AddItem(problematicItems.Dequeue());

                    draft.SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied);
                }

                DB.Characters.CommitTransaction(trans);
            }

            _ApplyAllItemMods();
            // Apply all azerite Item mods, azerite empowered Item mods will get applied through its spell script
            ApplyAllAzeriteItemMods(true);
        }

        private Item _LoadItem(SQLTransaction trans, uint zoneId, uint timeDiff, SQLFields fields)
        {
            Item item = null;
            ulong itemGuid = fields.Read<ulong>(0);
            uint itemEntry = fields.Read<uint>(1);
            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemEntry);

            if (proto != null)
            {
                bool remove = false;
                item = Item.NewItemOrBag(proto);

                if (item.LoadFromDB(itemGuid, GetGUID(), fields, itemEntry))
                {
                    PreparedStatement stmt;

                    // Do not allow to have Item limited to another map/zone in alive State
                    if (IsAlive() &&
                        item.IsLimitedToAnotherMapOrZone(GetMapId(), zoneId))
                    {
                        Log.outDebug(LogFilter.Player,
                                     "LoadInventory: player (GUID: {0}, Name: '{1}', map: {2}) has Item (GUID: {3}, entry: {4}) limited to another map ({5}). Deleting Item.",
                                     GetGUID().ToString(),
                                     GetName(),
                                     GetMapId(),
                                     item.GetGUID().ToString(),
                                     item.GetEntry(),
                                     zoneId);

                        remove = true;
                    }
                    // "Conjured items disappear if you are logged out for more than 15 minutes"
                    else if (timeDiff > 15 * Time.Minute &&
                             proto.HasFlag(ItemFlags.Conjured))
                    {
                        Log.outDebug(LogFilter.Player,
                                     "LoadInventory: player (GUID: {0}, Name: {1}, diff: {2}) has conjured Item (GUID: {3}, entry: {4}) with expired Lifetime (15 minutes). Deleting Item.",
                                     GetGUID().ToString(),
                                     GetName(),
                                     timeDiff,
                                     item.GetGUID().ToString(),
                                     item.GetEntry());

                        remove = true;
                    }

                    if (item.IsRefundable())
                    {
                        if (item.GetPlayedTime() > (2 * Time.Hour))
                        {
                            Log.outDebug(LogFilter.Player,
                                         "LoadInventory: player (GUID: {0}, Name: {1}) has Item (GUID: {2}, entry: {3}) with expired refund Time ({4}). Deleting refund _data and removing " +
                                         "efundable flag.",
                                         GetGUID().ToString(),
                                         GetName(),
                                         item.GetGUID().ToString(),
                                         item.GetEntry(),
                                         item.GetPlayedTime());

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
                                Log.outDebug(LogFilter.Player,
                                             "LoadInventory: player (GUID: {0}, Name: {1}) has Item (GUID: {2}, entry: {3}) with refundable Flags, but without _data in item_refund_instance. Removing flag.",
                                             GetGUID().ToString(),
                                             GetName(),
                                             item.GetGUID().ToString(),
                                             item.GetEntry());

                                item.RemoveItemFlag(ItemFieldFlags.Refundable);
                            }
                        }
                    }
                    else if (item.IsBOPTradeable())
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_BOP_TRADE);
                        stmt.AddValue(0, item.GetGUID().ToString());
                        SQLResult result = DB.Characters.Query(stmt);

                        if (!result.IsEmpty())
                        {
                            string strGUID = result.Read<string>(0);
                            var GUIDlist = new StringArray(strGUID, ' ');
                            List<ObjectGuid> looters = new();

                            for (var i = 0; i < GUIDlist.Length; ++i)
                                if (ulong.TryParse(GUIDlist[i], out ulong guid))
                                    looters.Add(ObjectGuid.Create(HighGuid.Item, guid));

                            if (looters.Count > 1 &&
                                item.GetTemplate().GetMaxStackSize() == 1 &&
                                item.IsSoulBound())
                            {
                                item.SetSoulboundTradeable(looters);
                                AddTradeableItem(item);
                            }
                            else
                            {
                                item.ClearSoulboundTradeable(this);
                            }
                        }
                        else
                        {
                            Log.outDebug(LogFilter.ServerLoading,
                                         "LoadInventory: player ({0}, Name: {1}) has Item ({2}, entry: {3}) with ITEM_FLAG_BOP_TRADEABLE flag, " +
                                         "but without _data in item_soulbound_trade_data. Removing flag.",
                                         GetGUID().ToString(),
                                         GetName(),
                                         item.GetGUID().ToString(),
                                         item.GetEntry());

                            item.RemoveItemFlag(ItemFieldFlags.BopTradeable);
                        }
                    }
                    else if (proto.GetHolidayID() != 0)
                    {
                        remove = true;
                        var events = Global.GameEventMgr.GetEventMap();
                        var activeEventsList = Global.GameEventMgr.GetActiveEventList();

                        foreach (var id in activeEventsList)
                            if (events[id].holiday_id == proto.GetHolidayID())
                            {
                                remove = false;

                                break;
                            }
                    }
                }
                else
                {
                    Log.outError(LogFilter.Player,
                                 "LoadInventory: player (GUID: {0}, Name: {1}) has broken Item (GUID: {2}, entry: {3}) in inventory. Deleting Item.",
                                 GetGUID().ToString(),
                                 GetName(),
                                 itemGuid,
                                 itemEntry);

                    remove = true;
                }

                // Remove Item from inventory if necessary
                if (remove)
                {
                    Item.DeleteFromInventoryDB(trans, itemGuid);
                    item.FSetState(ItemUpdateState.Removed);
                    item.SaveToDB(trans); // it also deletes Item object!
                    item = null;
                }
            }
            else
            {
                Log.outError(LogFilter.Player,
                             "LoadInventory: player (GUID: {0}, Name: {1}) has unknown Item (entry: {2}) in inventory. Deleting Item.",
                             GetGUID().ToString(),
                             GetName(),
                             itemEntry);

                Item.DeleteFromInventoryDB(trans, itemGuid);
                Item.DeleteFromDB(trans, itemGuid);
                AzeriteItem.DeleteFromDB(trans, itemGuid);
                AzeriteEmpoweredItem.DeleteFromDB(trans, itemGuid);
            }

            return item;
        }

        private void _LoadSkills(SQLResult result)
        {
            Race race = GetRace();
            uint count = 0;
            Dictionary<uint, uint> loadedSkillValues = new();

            if (!result.IsEmpty())
                do
                {
                    if (_mSkillStatus.Count >= SkillConst.MaxPlayerSkills) // client limit
                    {
                        Log.outError(LogFilter.Player, $"Player::_LoadSkills: Player '{GetName()}' ({GetGUID()}) has more than {SkillConst.MaxPlayerSkills} Skills.");

                        break;
                    }

                    var skill = result.Read<ushort>(0);
                    var value = result.Read<ushort>(1);
                    var max = result.Read<ushort>(2);

                    SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skill, race, GetClass());

                    if (rcEntry == null)
                    {
                        Log.outError(LogFilter.Player, $"Player::_LoadSkills: Player '{GetName()}' ({GetGUID()}, Race: {race}, Class: {GetClass()}) has forbidden skill {skill} for his race/class combination");
                        _mSkillStatus.Add(skill, new SkillStatusData((uint)_mSkillStatus.Count, SkillState.Deleted));

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

                    if (!_mSkillStatus.ContainsKey(skill))
                        _mSkillStatus.Add(skill, new SkillStatusData((uint)_mSkillStatus.Count, SkillState.Unchanged));

                    var skillStatusData = _mSkillStatus[skill];
                    ushort step = 0;

                    SkillLineRecord skillLine = CliDB.SkillLineStorage.LookupByKey(rcEntry.SkillID);

                    if (skillLine != null)
                    {
                        if (skillLine.CategoryID == SkillCategory.Secondary)
                            step = (ushort)(max / 75);

                        if (skillLine.CategoryID == SkillCategory.Profession)
                        {
                            step = (ushort)(max / 75);

                            if (skillLine.ParentSkillLineID != 0 &&
                                skillLine.ParentTierIndex != 0)
                            {
                                int professionSlot = FindProfessionSlotFor(skill);

                                if (professionSlot != -1)
                                    SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ProfessionSkillLine, (int)professionSlot), skill);
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
                } while (result.NextRow());

            // Learn skill rewarded spells after all Skills have been loaded to prevent learning a skill from them before its loaded with proper value from DB
            foreach (var skill in loadedSkillValues)
            {
                LearnSkillRewardedSpells(skill.Key, skill.Value, race);
                List<SkillLineRecord> childSkillLines = Global.DB2Mgr.GetSkillLinesForParentSkill(skill.Key);

                if (childSkillLines != null)
                    foreach (var childItr in childSkillLines)
                    {
                        if (_mSkillStatus.Count >= SkillConst.MaxPlayerSkills)
                            break;

                        if (!_mSkillStatus.ContainsKey(childItr.Id))
                        {
                            SetSkillLineId(count, (ushort)childItr.Id);
                            SetSkillStartingRank(count, 1);
                            _mSkillStatus.Add(childItr.Id, new SkillStatusData(count, SkillState.Unchanged));
                        }
                    }
            }

            if (HasSkill(SkillType.FistWeapons))
                SetSkill(SkillType.FistWeapons, 0, GetSkillValue(SkillType.Unarmed), GetMaxSkillValueForLevel());
        }

        private void _LoadSpells(SQLResult result, SQLResult favoritesResult)
        {
            if (!result.IsEmpty())
                do
                {
                    AddSpell(result.Read<uint>(0), result.Read<bool>(1), false, false, result.Read<bool>(2), true);
                } while (result.NextRow());

            if (!favoritesResult.IsEmpty())
                do
                {
                    var spell = _spells.LookupByKey(favoritesResult.Read<uint>(0));

                    if (spell != null)
                        spell.Favorite = true;
                } while (favoritesResult.NextRow());
        }

        private void _LoadAuras(SQLResult auraResult, SQLResult effectResult, uint timediff)
        {
            Log.outDebug(LogFilter.Player, "Loading Auras for player {0}", GetGUID().ToString());

            ObjectGuid casterGuid = new();
            ObjectGuid itemGuid = new();
            Dictionary<AuraKey, AuraLoadEffectInfo> effectInfo = new();

            if (!effectResult.IsEmpty())
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
                } while (effectResult.NextRow());

            if (!auraResult.IsEmpty())
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

                    if (difficulty != Difficulty.None &&
                        !CliDB.DifficultyStorage.ContainsKey(difficulty))
                    {
                        Log.outError(LogFilter.Player, $"Player._LoadAuras: Player '{GetName()}' ({GetGUID()}) has an invalid aura difficulty {difficulty} (SpellID: {key.SpellId}), ignoring.");

                        continue;
                    }

                    // negative effects should continue counting down after logout
                    if (remainTime != -1 &&
                        (!spellInfo.IsPositive() || spellInfo.HasAttribute(SpellAttr4.AuraExpiresOffline)))
                    {
                        if (remainTime / Time.InMilliseconds <= timediff)
                            continue;

                        remainTime -= (int)(timediff * Time.InMilliseconds);
                    }

                    // prevent wrong values of remaincharges
                    if (spellInfo.ProcCharges != 0)
                    {
                        // we have no control over the order of applying Auras and modifiers allow Auras
                        // to have more charges than value in SpellInfo
                        if (remainCharges <= 0)
                            remainCharges = (byte)spellInfo.ProcCharges;
                    }
                    else
                    {
                        remainCharges = 0;
                    }

                    AuraLoadEffectInfo info = effectInfo[key];
                    ObjectGuid castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, GetMapId(), spellInfo.Id, GetMap().GenerateLowGuid(HighGuid.Cast));

                    AuraCreateInfo createInfo = new(castId, spellInfo, difficulty, key.EffectMask, this);
                    createInfo.SetCasterGUID(casterGuid);
                    createInfo.SetBaseAmount(info.BaseAmounts);
                    createInfo.SetCastItem(itemGuid, castItemId, castItemLevel);

                    Aura aura = Aura.TryCreate(createInfo);

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
                } while (auraResult.NextRow());
        }

        private bool _LoadHomeBind(SQLResult result)
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
                _homebind.WorldRelocate(result.Read<uint>(0), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), result.Read<float>(5));
                _homebindAreaId = result.Read<uint>(1);

                var map = CliDB.MapStorage.LookupByKey(_homebind.GetMapId());

                // accept saved _data only for valid position (and non instanceable), and accessable
                if (GridDefines.IsValidMapCoord(_homebind) &&
                    !map.Instanceable() &&
                    GetSession().GetExpansion() >= map.Expansion())
                {
                    ok = true;
                }
                else
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    DB.Characters.Execute(stmt);
                }
            }

            void saveHomebindToDb()
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PLAYER_HOMEBIND);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, _homebind.GetMapId());
                stmt.AddValue(2, _homebindAreaId);
                stmt.AddValue(3, _homebind.GetPositionX());
                stmt.AddValue(4, _homebind.GetPositionY());
                stmt.AddValue(5, _homebind.GetPositionZ());
                stmt.AddValue(6, _homebind.GetOrientation());
                DB.Characters.Execute(stmt);
            }

            ;

            if (!ok &&
                HasAtLoginFlag(AtLoginFlags.FirstLogin))
            {
                var createPosition = _createMode == PlayerCreateMode.NPE && info.CreatePositionNPE.HasValue ? info.CreatePositionNPE.Value : info.CreatePositionInfo;

                if (!createPosition.TransportGuid.HasValue)
                {
                    _homebind.WorldRelocate(createPosition.Loc);
                    _homebindAreaId = Global.TerrainMgr.GetAreaId(PhasingHandler.EmptyPhaseShift, _homebind);

                    saveHomebindToDb();
                    ok = true;
                }
            }

            if (!ok)
            {
                WorldSafeLocsEntry loc = Global.ObjectMgr.GetDefaultGraveYard(GetTeam());

                if (loc == null &&
                    GetRace() == Race.PandarenNeutral)
                    loc = Global.ObjectMgr.GetWorldSafeLoc(3295); // The Wandering Isle, Starting Area GY

                Cypher.Assert(loc != null, "Missing fallback graveyard location for faction {GetTeamId()}");

                _homebind.WorldRelocate(loc.Loc);
                _homebindAreaId = Global.TerrainMgr.GetAreaId(PhasingHandler.EmptyPhaseShift, loc.Loc);

                saveHomebindToDb();
            }

            Log.outDebug(LogFilter.Player, $"Setting player home position - mapid: {_homebind.GetMapId()}, areaid: {_homebindAreaId}, {_homebind}");

            return true;
        }

        private void _LoadCurrency(SQLResult result)
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
                cur.State = PlayerCurrencyState.Unchanged;
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
            _actionButtons.Clear();

            if (!result.IsEmpty())
                do
                {
                    byte button = result.Read<byte>(0);
                    ulong action = result.Read<ulong>(1);
                    byte type = result.Read<byte>(2);

                    ActionButton ab = AddActionButton(button, action, type);

                    if (ab != null)
                    {
                        ab.UState = ActionButtonUpdateState.UnChanged;
                    }
                    else
                    {
                        Log.outError(LogFilter.Player, $"Player::_LoadActions: Player '{GetName()}' ({GetGUID()}) has an invalid Action Button (Button: {button}, Action: {action}, Type: {type}). It will be deleted at next save. This can be due to a player changing their talents.");

                        // Will deleted in DB at next save (it can create _data until save but marked as deleted)
                        _actionButtons[button] = new ActionButton();
                        _actionButtons[button].UState = ActionButtonUpdateState.Deleted;
                    }
                } while (result.NextRow());
        }

        private void _LoadQuestStatus(SQLResult result)
        {
            ushort slot = 0;

            if (!result.IsEmpty())
                do
                {
                    uint questId = result.Read<uint>(0);
                    // used to be new, no delete?
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);

                    if (quest != null)
                    {
                        // find or create
                        QuestStatusData questStatusData = new();

                        byte qstatus = result.Read<byte>(1);

                        if (qstatus < (byte)QuestStatus.Max)
                        {
                            questStatusData.Status = (QuestStatus)qstatus;
                        }
                        else
                        {
                            questStatusData.Status = QuestStatus.Incomplete;

                            Log.outError(LogFilter.Player,
                                         "Player {0} (GUID: {1}) has invalid quest {2} status ({3}), replaced by QUEST_STATUS_INCOMPLETE(3).",
                                         GetName(),
                                         GetGUID().ToString(),
                                         questId,
                                         qstatus);
                        }

                        questStatusData.Explored = result.Read<byte>(2) > 0;

                        long acceptTime = result.Read<long>(3);
                        long endTime = result.Read<long>(4);

                        if (quest.LimitTime != 0 &&
                            !GetQuestRewardStatus(questId))
                        {
                            AddTimedQuest(questId);

                            if (endTime <= GameTime.GetGameTime())
                                questStatusData.Timer = 1;
                            else
                                questStatusData.Timer = (uint)((endTime - GameTime.GetGameTime()) * Time.InMilliseconds);
                        }
                        else
                        {
                            endTime = 0;
                        }

                        // add to quest log
                        if (slot < SharedConst.MaxQuestLogSize &&
                            questStatusData.Status != QuestStatus.None)
                        {
                            questStatusData.Slot = slot;

                            foreach (QuestObjective obj in quest.Objectives)
                                _questObjectiveStatus.Add((obj.Type, obj.ObjectID),
                                                          new QuestObjectiveStatusData()
                                                          {
                                                              QuestStatusPair = (questId, questStatusData),
                                                              Objective = obj
                                                          });

                            SetQuestSlot(slot, questId);
                            SetQuestSlotEndTime(slot, endTime);
                            SetQuestSlotAcceptTime(slot, acceptTime);

                            if (questStatusData.Status == QuestStatus.Complete)
                                SetQuestSlotState(slot, QuestSlotStateMask.Complete);
                            else if (questStatusData.Status == QuestStatus.Failed)
                                SetQuestSlotState(slot, QuestSlotStateMask.Fail);

                            ++slot;
                        }

                        _questStatus[questId] = questStatusData;
                        Log.outDebug(LogFilter.ServerLoading, "Quest status is {0} for quest {1} for player (GUID: {2})", questStatusData.Status, questId, GetGUID().ToString());
                    }
                } while (result.NextRow());

            // clear quest log tail
            for (ushort i = slot; i < SharedConst.MaxQuestLogSize; ++i)
                SetQuestSlot(i, 0);
        }

        private void _LoadQuestStatusObjectives(SQLResult result)
        {
            if (!result.IsEmpty())
                do
                {
                    uint questID = result.Read<uint>(0);

                    Quest quest = Global.ObjectMgr.GetQuestTemplate(questID);

                    var questStatusData = _questStatus.LookupByKey(questID);

                    if (questStatusData != null &&
                        questStatusData.Slot < SharedConst.MaxQuestLogSize &&
                        quest != null)
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
                        {
                            Log.outError(LogFilter.Player, $"Player {GetName()} ({GetGUID()}) has quest {questID} out of range objective index {storageIndex}.");
                        }
                    }
                    else
                    {
                        Log.outError(LogFilter.Player, $"Player {GetName()} ({GetGUID()}) does not have quest {questID} but has objective _data for it.");
                    }
                } while (result.NextRow());
        }

        private void _LoadQuestStatusRewarded(SQLResult result)
        {
            if (!result.IsEmpty())
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
                        if (!quest.IsDailyOrWeekly() &&
                            !quest.IsMonthly() &&
                            !quest.IsSeasonal())
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
                            foreach (QuestPackageItemRecord questPackageItem in questPackageItems)
                            {
                                ItemTemplate rewardProto = Global.ObjectMgr.GetItemTemplate(questPackageItem.ItemID);

                                if (rewardProto != null)
                                    if (rewardProto.ItemSpecClassMask.HasAnyFlag(GetClassMask()))
                                        GetSession().GetCollectionMgr().AddItemAppearance(questPackageItem.ItemID);
                            }

                        if (quest.CanIncreaseRewardedQuestCounters())
                            _rewardedQuests.Add(quest_id);
                    }
                } while (result.NextRow());
        }

        private void _LoadDailyQuestStatus(SQLResult result)
        {
            _dfQuests.Clear();

            //QueryResult* result = CharacterDatabase.PQuery("SELECT quest, Time FROM character_queststatus_daily WHERE Guid = '{0}'");
            if (!result.IsEmpty())
                do
                {
                    uint quest_id = result.Read<uint>(0);
                    Quest qQuest = Global.ObjectMgr.GetQuestTemplate(quest_id);

                    if (qQuest != null)
                        if (qQuest.IsDFQuest())
                        {
                            _dfQuests.Add(qQuest.Id);
                            _lastDailyQuestTime = result.Read<uint>(1);

                            continue;
                        }

                    // save _any_ from daily quest times (it must be after last reset anyway)
                    _lastDailyQuestTime = result.Read<long>(1);

                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

                    if (quest == null)
                        continue;

                    AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.DailyQuestsCompleted), quest_id);
                    uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);

                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Daily quest ({0}) cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
                } while (result.NextRow());

            _dailyQuestChanged = false;
        }

        private void _LoadWeeklyQuestStatus(SQLResult result)
        {
            _weeklyquests.Clear();

            if (!result.IsEmpty())
                do
                {
                    uint quest_id = result.Read<uint>(0);
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

                    if (quest == null)
                        continue;

                    _weeklyquests.Add(quest_id);
                    uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);

                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Weekly quest {0} cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
                } while (result.NextRow());

            _weeklyQuestChanged = false;
        }

        private void _LoadSeasonalQuestStatus(SQLResult result)
        {
            _seasonalquests.Clear();

            if (!result.IsEmpty())
                do
                {
                    uint quest_id = result.Read<uint>(0);
                    uint event_id = result.Read<uint>(1);
                    long completedTime = result.Read<long>(2);
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

                    if (quest == null)
                        continue;

                    if (!_seasonalquests.ContainsKey(event_id))
                        _seasonalquests[event_id] = new Dictionary<uint, long>();

                    _seasonalquests[event_id][quest_id] = completedTime;

                    uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);

                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Seasonal quest {0} cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
                } while (result.NextRow());

            _seasonalQuestChanged = false;
        }

        private void _LoadMonthlyQuestStatus()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_MONTHLY);
            stmt.AddValue(0, GetGUID().GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            _monthlyquests.Clear();

            if (!result.IsEmpty())
                do
                {
                    uint quest_id = result.Read<uint>(0);
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

                    if (quest == null)
                        continue;

                    _monthlyquests.Add(quest_id);
                    uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);

                    if (questBit != 0)
                        SetQuestCompletedBit(questBit, true);

                    Log.outDebug(LogFilter.Player, "Monthly quest {0} cooldown for player (GUID: {1})", quest_id, GetGUID().ToString());
                } while (result.NextRow());

            _monthlyQuestChanged = false;
        }

        private void _LoadTalents(SQLResult result)
        {
            if (!result.IsEmpty())
                do
                {
                    TalentRecord talent = CliDB.TalentStorage.LookupByKey(result.Read<uint>(0));

                    if (talent != null)
                        AddTalent(talent, result.Read<byte>(1), false);
                } while (result.NextRow());
        }

        private void _LoadPvpTalents(SQLResult result)
        {
            // "SELECT talentID0, talentID1, talentID2, talentID3, talentGroup FROM character_pvp_talent WHERE Guid = ?"
            if (!result.IsEmpty())
                do
                {
                    for (byte slot = 0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
                    {
                        PvpTalentRecord talent = CliDB.PvpTalentStorage.LookupByKey(result.Read<uint>(slot));

                        if (talent != null)
                            AddPvpTalent(talent, result.Read<byte>(4), slot);
                    }
                } while (result.NextRow());
        }

        private void _LoadTraits(SQLResult configsResult, SQLResult entriesResult)
        {
            MultiMap<int, TraitEntryPacket> traitEntriesByConfig = new();

            if (!entriesResult.IsEmpty())
                //                    0            1,                2     3             4
                // SELECT traitConfigId, traitNodeId, traitNodeEntryId, rank, grantedRanks FROM character_trait_entry WHERE Guid = ?
                do
                {
                    TraitEntryPacket traitEntry = new();
                    traitEntry.TraitNodeID = entriesResult.Read<int>(1);
                    traitEntry.TraitNodeEntryID = entriesResult.Read<int>(2);
                    traitEntry.Rank = entriesResult.Read<int>(3);
                    traitEntry.GrantedRanks = entriesResult.Read<int>(4);

                    if (!TraitMgr.IsValidEntry(traitEntry))
                        continue;

                    traitEntriesByConfig.Add(entriesResult.Read<int>(0), traitEntry);
                } while (entriesResult.NextRow());

            if (!configsResult.IsEmpty())
                //                    0     1                    2                  3                4            5              6      7
                // SELECT traitConfigId, Type, chrSpecializationId, combatConfigFlags, localIdentifier, skillLineId, traitSystemId, `Name` FROM character_trait_config WHERE Guid = ?
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
                            traitConfig.TraitSystemID = configsResult.Read<int>(6);

                            break;
                        default:
                            break;
                    }

                    traitConfig.Name = configsResult.Read<string>(7);

                    foreach (var traitEntry in traitEntriesByConfig.LookupByKey(traitConfig.ID))
                        traitConfig.Entries.Add(traitEntry);

                    if (TraitMgr.ValidateConfig(traitConfig, this) != TalentLearnResult.LearnOk)
                    {
                        traitConfig.Entries.Clear();

                        foreach (TraitEntry grantedEntry in TraitMgr.GetGrantedTraitEntriesForConfig(traitConfig, this))
                            traitConfig.Entries.Add(new TraitEntryPacket(grantedEntry));
                    }

                    AddTraitConfig(traitConfig);
                } while (configsResult.NextRow());

            bool hasConfigForSpec(int specId)
            {
                return ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return traitConfig.Type == (int)TraitConfigType.Combat && traitConfig.ChrSpecializationID == specId && (traitConfig.CombatConfigFlags & (int)TraitCombatConfigFlags.ActiveForSpec) != 0; }) >= 0;
            }

            int findFreeLocalIdentifier(int specId)
            {
                int index = 1;

                while (ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return traitConfig.Type == (int)TraitConfigType.Combat && traitConfig.ChrSpecializationID == specId && traitConfig.LocalIdentifier == index; }) >= 0)
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

            int activeConfig = ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return traitConfig.Type == (int)TraitConfigType.Combat && traitConfig.ChrSpecializationID == GetPrimarySpecialization() && (traitConfig.CombatConfigFlags & (int)TraitCombatConfigFlags.ActiveForSpec) != 0; });

            if (activeConfig >= 0)
                SetActiveCombatTraitConfigID(ActivePlayerData.TraitConfigs[activeConfig].ID);

            foreach (TraitConfig traitConfig in ActivePlayerData.TraitConfigs)
            {
                switch ((TraitConfigType)(int)traitConfig.Type)
                {
                    case TraitConfigType.Combat:
                        if (traitConfig.ID != ActivePlayerData.ActiveCombatTraitConfigID)
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

        private void _LoadGlyphs(SQLResult result)
        {
            // SELECT talentGroup, glyphId from character_glyphs WHERE Guid = ?
            if (result.IsEmpty())
                return;

            do
            {
                byte spec = result.Read<byte>(0);

                if (spec >= PlayerConst.MaxSpecializations ||
                    Global.DB2Mgr.GetChrSpecializationByIndex(GetClass(), spec) == null)
                    continue;

                ushort glyphId = result.Read<ushort>(1);

                if (!CliDB.GlyphPropertiesStorage.ContainsKey(glyphId))
                    continue;

                GetGlyphs(spec).Add(glyphId);
            } while (result.NextRow());
        }

        private void _LoadGlyphAuras()
        {
            foreach (uint glyphId in GetGlyphs(GetActiveTalentGroup()))
                CastSpell(this, CliDB.GlyphPropertiesStorage.LookupByKey(glyphId).SpellID, true);
        }

        public void LoadCorpse(SQLResult result)
        {
            if (IsAlive() ||
                HasAtLoginFlag(AtLoginFlags.Resurrect))
                SpawnCorpseBones(false);

            if (!IsAlive())
            {
                if (HasAtLoginFlag(AtLoginFlags.Resurrect))
                {
                    ResurrectPlayer(0.5f);
                }
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

        private void _LoadVoidStorage(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                // SELECT ItemId, itemEntry, Slot, creatorGuid, randomBonusListId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs FROM character_void_storage WHERE playerGuid = ?
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
                    if (uint.TryParse(bonusListIdTokens[i], out uint id))
                        bonusListIDs.Add(id);

                if (itemId == 0)
                {
                    Log.outError(LogFilter.Player, "Player:_LoadVoidStorage - Player (GUID: {0}, Name: {1}) has an Item with an invalid Id (Item Id: Item Id: {2}, entry: {3}).", GetGUID().ToString(), GetName(), itemId, itemEntry);

                    continue;
                }

                if (Global.ObjectMgr.GetItemTemplate(itemEntry) == null)
                {
                    Log.outError(LogFilter.Player, "Player:_LoadVoidStorage - Player (GUID: {0}, Name: {1}) has an Item with an invalid entry (Item Id: Item Id: {2}, entry: {3}).", GetGUID().ToString(), GetName(), itemId, itemEntry);

                    continue;
                }

                if (slot >= SharedConst.VoidStorageMaxSlot)
                {
                    Log.outError(LogFilter.Player, "Player:_LoadVoidStorage - Player (GUID: {0}, Name: {1}) has an Item with an invalid Slot (Item Id: Item Id: {2}, entry: {3}, Slot: {4}).", GetGUID().ToString(), GetName(), itemId, itemEntry, slot);

                    continue;
                }

                _voidStorageItems[slot] = new VoidStorageItem(itemId, itemEntry, creatorGuid, randomBonusListId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs);

                BonusData bonus = new(new ItemInstance(_voidStorageItems[slot]));
                GetSession().GetCollectionMgr().AddItemAppearance(itemEntry, bonus.AppearanceModID);
            } while (result.NextRow());
        }

        public void _LoadMail(SQLResult mailsResult, SQLResult mailItemsResult, SQLResult artifactResult, SQLResult azeriteItemResult, SQLResult azeriteItemMilestonePowersResult, SQLResult azeriteItemUnlockedEssencesResult, SQLResult azeriteEmpoweredItemResult)
        {
            _mail.Clear();

            Dictionary<uint, Mail> mailById = new();

            if (!mailsResult.IsEmpty())
                do
                {
                    Mail m = new();

                    m.messageID = mailsResult.Read<uint>(0);
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

                    if (m.mailTemplateId != 0 &&
                        !CliDB.MailTemplateStorage.ContainsKey(m.mailTemplateId))
                    {
                        Log.outError(LogFilter.Player, $"Player:_LoadMail - Mail ({m.messageID}) have not existed MailTemplateId ({m.mailTemplateId}), remove at load");
                        m.mailTemplateId = 0;
                    }

                    m.state = MailState.Unchanged;

                    _mail.Add(m);
                    mailById[m.messageID] = m;
                } while (mailsResult.NextRow());

            if (!mailItemsResult.IsEmpty())
            {
                Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
                ItemAdditionalLoadInfo.Init(additionalData, artifactResult, azeriteItemResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

                do
                {
                    uint mailId = mailItemsResult.Read<uint>(52);
                    _LoadMailedItem(GetGUID(), this, mailId, mailById[mailId], mailItemsResult.GetFields(), additionalData.LookupByKey(mailItemsResult.Read<ulong>(0)));
                } while (mailItemsResult.NextRow());
            }

            UpdateNextMailTimeAndUnreads();
        }

        private static Item _LoadMailedItem(ObjectGuid playerGuid, Player player, uint mailId, Mail mail, SQLFields fields, ItemAdditionalLoadInfo addionalData)
        {
            ulong itemGuid = fields.Read<ulong>(0);
            uint itemEntry = fields.Read<uint>(1);

            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemEntry);

            if (proto == null)
            {
                Log.outError(LogFilter.Player, $"Player {(player != null ? player.GetName() : "<unknown>")} ({playerGuid}) has unknown Item in mailed items (GUID: {itemGuid} template: {itemEntry}) in mail ({mailId}), deleted.");

                SQLTransaction trans = new();

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_MAIL_ITEM);
                stmt.AddValue(0, itemGuid);
                trans.Append(stmt);

                Item.DeleteFromDB(trans, itemGuid);
                AzeriteItem.DeleteFromDB(trans, itemGuid);
                AzeriteEmpoweredItem.DeleteFromDB(trans, itemGuid);

                DB.Characters.CommitTransaction(trans);

                return null;
            }

            Item item = Item.NewItemOrBag(proto);
            ObjectGuid ownerGuid = fields.Read<ulong>(51) != 0 ? ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(51)) : ObjectGuid.Empty;

            if (!item.LoadFromDB(itemGuid, ownerGuid, fields, itemEntry))
            {
                Log.outError(LogFilter.Player, $"Player._LoadMailedItems: Item (GUID: {itemGuid}) in mail ({mailId}) doesn't exist, deleted from mail.");

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM);
                stmt.AddValue(0, itemGuid);
                DB.Characters.Execute(stmt);

                item.FSetState(ItemUpdateState.Removed);

                item.SaveToDB(null); // it also deletes Item object !

                return null;
            }

            if (addionalData != null)
            {
                if (item.GetTemplate().GetArtifactID() != 0 &&
                    addionalData.Artifact != null)
                    item.LoadArtifactData(player,
                                          addionalData.Artifact.Xp,
                                          addionalData.Artifact.ArtifactAppearanceId,
                                          addionalData.Artifact.ArtifactTierId,
                                          addionalData.Artifact.ArtifactPowers);

                if (addionalData.AzeriteItem != null)
                {
                    AzeriteItem azeriteItem = item.ToAzeriteItem();

                    azeriteItem?.LoadAzeriteItemData(player, addionalData.AzeriteItem);
                }

                if (addionalData.AzeriteEmpoweredItem != null)
                {
                    AzeriteEmpoweredItem azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();

                    azeriteEmpoweredItem?.LoadAzeriteEmpoweredItemData(player, addionalData.AzeriteEmpoweredItem);
                }
            }

            mail?.AddItem(itemGuid, itemEntry);

            player?.AddMItem(item);

            return item;
        }

        private void _LoadDeclinedNames(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            _declinedname = new DeclinedName();

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                _declinedname.Name[i] = result.Read<string>(i);
        }

        private void _LoadArenaTeamInfo(SQLResult result)
        {
            // arenateamid, played_week, played_season, personal_rating
            ushort[] personalRatingCache =
            {
                0, 0, 0
            };

            if (!result.IsEmpty())
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
                } while (result.NextRow());

            for (byte slot = 0; slot <= 2; ++slot)
                SetArenaTeamInfoField(slot, ArenaTeamInfoType.PersonalRating, personalRatingCache[slot]);
        }

        private void _LoadStoredAuraTeleportLocations(SQLResult result)
        {
            //                                                       0      1      2          3          4          5
            //QueryResult* result = CharacterDatabase.PQuery("SELECT Spell, _mapId, PositionX, PositionY, PositionZ, Orientation FROM character_spell_location WHERE Guid = ?", GetGUIDLow());

            _storedAuraTeleportLocations.Clear();

            if (!result.IsEmpty())
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

                    _storedAuraTeleportLocations[spellId] = storedLocation;
                } while (result.NextRow());
        }

        private void _LoadGroup(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                Group group = Global.GroupMgr.GetGroupByDbStoreId(result.Read<uint>(0));

                if (group)
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

            if (!GetGroup() ||
                !GetGroup().IsLeader(GetGUID()))
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
                EquipmentSetInfo eqSet = new();
                eqSet.Data.Guid = result.Read<ulong>(0);
                eqSet.Data.Type = EquipmentSetInfo.EquipmentSetType.Equipment;
                eqSet.Data.SetID = result.Read<byte>(1);
                eqSet.Data.SetName = result.Read<string>(2);
                eqSet.Data.SetIcon = result.Read<string>(3);
                eqSet.Data.IgnoreMask = result.Read<uint>(4);
                eqSet.Data.AssignedSpecIndex = result.Read<int>(5);
                eqSet.State = EquipmentSetUpdateState.Unchanged;

                for (int i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                {
                    ulong guid = result.Read<uint>(6 + i);

                    if (guid != 0)
                        eqSet.Data.Pieces[i] = ObjectGuid.Create(HighGuid.Item, guid);
                }

                if (eqSet.Data.SetID >= ItemConst.MaxEquipmentSetIndex) // client limit
                    continue;

                _equipmentSets[eqSet.Data.Guid] = eqSet;
            } while (result.NextRow());
        }

        private void _LoadTransmogOutfits(SQLResult result)
        {
            //             0         1     2         3            4            5            6            7            8            9
            //SELECT setguid, setindex, Name, iconname, ignore_mask, appearance0, appearance1, appearance2, appearance3, appearance4,
            //             10           11           12           13           14            15            16            17            18            19            20            21
            //    appearance5, appearance6, appearance7, appearance8, appearance9, appearance10, appearance11, appearance12, appearance13, appearance14, appearance15, appearance16,
            //              22            23               24              25
            //    appearance17, appearance18, mainHandEnchant, offHandEnchant FROM character_transmog_outfits WHERE Guid = ? ORDER BY setindex
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
                eqSet.State = EquipmentSetUpdateState.Unchanged;

                for (int i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                    eqSet.Data.Appearances[i] = result.Read<int>(5 + i);

                for (int i = 0; i < eqSet.Data.Enchants.Length; ++i)
                    eqSet.Data.Enchants[i] = result.Read<int>(24 + i);

                if (eqSet.Data.SetID >= ItemConst.MaxEquipmentSetIndex) // client limit
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
                    Log.outError(LogFilter.Player, "Player._LoadCUFProfiles - Player (GUID: {0}, Name: {1}) has an CUF profile with invalid Id (Id: {2}), max is {3}.", GetGUID().ToString(), GetName(), id, PlayerConst.MaxCUFProfiles);

                    continue;
                }

                _cUFProfiles[id] = new CUFProfile(name, frameHeight, frameWidth, sortBy, healthText, boolOptions, topPoint, bottomPoint, leftPoint, topOffset, bottomOffset, leftOffset);
            } while (result.NextRow());
        }

        private void _LoadRandomBGStatus(SQLResult result)
        {
            if (!result.IsEmpty())
                _isBGRandomWinner = true;
        }

        private void _LoadBGData(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            // Expecting only one row
            //        0           1     2      3      4      5      6          7          8        9
            // SELECT InstanceId, team, joinX, joinY, joinZ, joinO, joinMapId, taxiStart, taxiEnd, MountSpell FROM character_Battleground_data WHERE Guid = ?
            _bgData.InstanceID = result.Read<uint>(0);
            _bgData.Team = result.Read<ushort>(1);
            _bgData.JoinPos = new WorldLocation(result.Read<ushort>(6), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), result.Read<float>(5));
            _bgData.TaxiPath[0] = result.Read<uint>(7);
            _bgData.TaxiPath[1] = result.Read<uint>(8);
            _bgData.MountSpell = result.Read<uint>(9);
        }

        private void _LoadPetStable(uint summonedPetNumber, SQLResult result)
        {
            if (result.IsEmpty())
                return;

            _petStable = new PetStable();

            //         0      1        2      3    4           5     6     7        8          9       10      11        12              13       14              15
            // SELECT Id, entry, modelid, level, exp, Reactstate, Slot, Name, renamed, curhealth, curmana, abdata, savetime, CreatedBySpell, PetType, specialization FROM character_pet WHERE owner = ?
            if (!result.IsEmpty())
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

                    if (slot >= PetSaveMode.FirstActiveSlot &&
                        slot < PetSaveMode.LastActiveSlot)
                        _petStable.ActivePets[(int)slot] = petInfo;
                    else if (slot >= PetSaveMode.FirstStableSlot &&
                             slot < PetSaveMode.LastStableSlot)
                        _petStable.StabledPets[slot - PetSaveMode.FirstStableSlot] = petInfo;
                    else if (slot == PetSaveMode.NotInSlot)
                        _petStable.UnslottedPets.Add(petInfo);
                } while (result.NextRow());

            if (Pet.GetLoadPetInfo(_petStable, 0, summonedPetNumber, null).Item1 != null)
                _temporaryUnsummonedPetNumber = summonedPetNumber;
        }


        private void _SaveInventory(SQLTransaction trans)
        {
            PreparedStatement stmt;

            // Force items in buyback slots to new State
            // and remove those that aren't already
            for (var i = InventorySlots.BuyBackStart; i < InventorySlots.BuyBackEnd; ++i)
            {
                Item item = _items[i];

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
                _items[i].FSetState(ItemUpdateState.New);

                if (itemTemplate != null)
                    if (itemTemplate.HasFlag(ItemFlags.HasLoot))
                        Global.LootItemStorage.RemoveStoredLootForContainer(item.GetGUID().GetCounter());
            }

            // Updated played Time for refundable items. We don't do this in Player.Update because there's simply no need for it,
            // the client auto counts down in real Time after having received the initial played Time on the first
            // SMSG_ITEM_REFUND_INFO_RESPONSE packet.
            // Item.UpdatePlayedTime is only called when needed, which is in DB saves, and Item refund info requests.
            foreach (var guid in _refundableItems)
            {
                Item item = GetItemByGuid(guid);

                if (item != null)
                {
                    item.UpdatePlayedTime(this);

                    continue;
                }
                else
                {
                    Log.outError(LogFilter.Player, "Can't find Item Guid {0} but is in refundable storage for player {1} ! Removing.", guid, GetGUID().ToString());
                    _refundableItems.Remove(guid);
                }
            }

            // update enchantment durations
            foreach (var enchant in _enchantDuration)
                enchant.Item.SetEnchantmentDuration(enchant.Slot, enchant.Leftduration, this);

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

                        Log.outError(LogFilter.Player,
                                     "Player(GUID: {0} Name: {1}).SaveInventory - the bag({2}) and Slot({3}) values for the Item with Guid {4} (State {5}) are incorrect, " +
                                     "the player doesn't have an Item at that position!",
                                     GetGUID().ToString(),
                                     GetName(),
                                     item.GetBagSlot(),
                                     item.GetSlot(),
                                     item.GetGUID().ToString(),
                                     item.GetState());

                        // according to the test that was just performed nothing should be in this Slot, delete
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_BAG_SLOT);
                        stmt.AddValue(0, bagTestGUID);
                        stmt.AddValue(1, item.GetSlot());
                        stmt.AddValue(2, GetGUID().GetCounter());
                        trans.Append(stmt);

                        RemoveTradeableItem(item);
                        RemoveEnchantmentDurationsReferences(item);
                        RemoveItemDurations(item);

                        // also THIS Item should be somewhere else, cheat attempt
                        item.FSetState(ItemUpdateState.Removed); // we are IN updateQueue right now, can't use SetState which modifies the queue
                        DeleteRefundReference(item.GetGUID());
                    }
                    else if (test != item)
                    {
                        Log.outError(LogFilter.Player,
                                     "Player(GUID: {0} Name: {1}).SaveInventory - the bag({2}) and Slot({3}) values for the Item with Guid {4} are incorrect, " +
                                     "the Item with Guid {5} is there instead!",
                                     GetGUID().ToString(),
                                     GetName(),
                                     item.GetBagSlot(),
                                     item.GetSlot(),
                                     item.GetGUID().ToString(),
                                     test.GetGUID().ToString());

                        // save all changes to the Item...
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

                item.SaveToDB(trans); // Item have unchanged inventory record and can be save standalone
            }

            ItemUpdateQueue.Clear();
        }

        private void _SaveSkills(SQLTransaction trans)
        {
            PreparedStatement stmt; // = null;

            SkillInfo skillInfoField = ActivePlayerData.Skill;

            foreach (var pair in _mSkillStatus.ToList())
            {
                if (pair.Value.State == SkillState.Unchanged)
                    continue;

                ushort value = skillInfoField.SkillRank[pair.Value.Pos];
                ushort max = skillInfoField.SkillMaxRank[pair.Value.Pos];

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
            PreparedStatement stmt;

            foreach (var (id, spell) in _spells.ToList())
            {
                if (spell.State == PlayerSpellState.Removed ||
                    spell.State == PlayerSpellState.Changed)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_BY_SPELL);
                    stmt.AddValue(0, id);
                    stmt.AddValue(1, GetGUID().GetCounter());
                    trans.Append(stmt);
                }

                if (spell.State == PlayerSpellState.New ||
                    spell.State == PlayerSpellState.Changed)
                {
                    // add only changed/new not dependent spells
                    if (!spell.Dependent)
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SPELL);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, id);
                        stmt.AddValue(2, spell.Active);
                        stmt.AddValue(3, spell.Disabled);
                        trans.Append(stmt);
                    }

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_FAVORITE);
                    stmt.AddValue(0, id);
                    stmt.AddValue(1, GetGUID().GetCounter());
                    trans.Append(stmt);

                    if (spell.Favorite)
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SPELL_FAVORITE);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, id);
                        trans.Append(stmt);
                    }
                }

                if (spell.State == PlayerSpellState.Removed)
                {
                    _spells.Remove(id);

                    continue;
                }

                if (spell.State != PlayerSpellState.Temporary)
                    spell.State = PlayerSpellState.Unchanged;
            }
        }

        private void _SaveAuras(SQLTransaction trans)
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
                stmt.AddValue(index++, (byte)aura.GetCastDifficulty());
                stmt.AddValue(index++, aura.GetStackAmount());
                stmt.AddValue(index++, aura.GetMaxDuration());
                stmt.AddValue(index++, aura.GetDuration());
                stmt.AddValue(index++, aura.GetCharges());
                stmt.AddValue(index++, aura.GetCastItemId());
                stmt.AddValue(index, aura.GetCastItemLevel());
                trans.Append(stmt);

                foreach (AuraEffect effect in aura.GetAuraEffects())
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

        private void _SaveGlyphs(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_GLYPHS);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            for (byte spec = 0; spec < PlayerConst.MaxSpecializations; ++spec)
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

        private void _SaveCurrency(SQLTransaction trans)
        {
            PreparedStatement stmt;

            foreach (var pair in _currencyStorage)
            {
                CurrencyTypesRecord entry = CliDB.CurrencyTypesStorage.LookupByKey(pair.Key);

                if (entry == null) // should never happen
                    continue;

                switch (pair.Value.State)
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

                pair.Value.State = PlayerCurrencyState.Unchanged;
            }
        }

        public static void SavePlayerCustomizations(SQLTransaction trans, ulong guid, List<ChrCustomizationChoice> customizations)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
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
            if (!_customizationsChanged)
                return;

            _customizationsChanged = false;

            SavePlayerCustomizations(trans, GetGUID().GetCounter(), PlayerData.Customizations);
        }

        private void _SaveActions(SQLTransaction trans)
        {
            int traitConfigId = 0;

            TraitConfig traitConfig = GetTraitConfig((int)(uint)ActivePlayerData.ActiveCombatTraitConfigID);

            if (traitConfig != null)
            {
                int usedSavedTraitConfigIndex = ActivePlayerData.TraitConfigs.FindIndexIf(savedConfig => { return (TraitConfigType)(int)savedConfig.Type == TraitConfigType.Combat && ((TraitCombatConfigFlags)(int)savedConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None && ((TraitCombatConfigFlags)(int)savedConfig.CombatConfigFlags & TraitCombatConfigFlags.SharedActionBars) == TraitCombatConfigFlags.None && savedConfig.LocalIdentifier == traitConfig.LocalIdentifier; });

                if (usedSavedTraitConfigIndex >= 0)
                    traitConfigId = ActivePlayerData.TraitConfigs[usedSavedTraitConfigIndex].ID;
            }

            PreparedStatement stmt;

            foreach (var pair in _actionButtons.ToList())
                switch (pair.Value.UState)
                {
                    case ActionButtonUpdateState.New:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_ACTION);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, GetActiveTalentGroup());
                        stmt.AddValue(2, traitConfigId);
                        stmt.AddValue(3, pair.Key);
                        stmt.AddValue(4, pair.Value.GetAction());
                        stmt.AddValue(5, (byte)pair.Value.GetButtonType());
                        trans.Append(stmt);

                        pair.Value.UState = ActionButtonUpdateState.UnChanged;

                        break;
                    case ActionButtonUpdateState.Changed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_ACTION);
                        stmt.AddValue(0, pair.Value.GetAction());
                        stmt.AddValue(1, (byte)pair.Value.GetButtonType());
                        stmt.AddValue(2, GetGUID().GetCounter());
                        stmt.AddValue(3, pair.Key);
                        stmt.AddValue(4, GetActiveTalentGroup());
                        stmt.AddValue(5, traitConfigId);
                        trans.Append(stmt);

                        pair.Value.UState = ActionButtonUpdateState.UnChanged;

                        break;
                    case ActionButtonUpdateState.Deleted:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION_BY_BUTTON_SPEC);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, GetActiveTalentGroup());
                        stmt.AddValue(3, traitConfigId);
                        trans.Append(stmt);

                        _actionButtons.Remove(pair.Key);

                        break;
                    default:
                        break;
                }
        }

        private void _SaveQuestStatus(SQLTransaction trans)
        {
            bool isTransaction = trans != null;

            if (!isTransaction)
                trans = new SQLTransaction();

            PreparedStatement stmt;
            bool keepAbandoned = !Global.WorldMgr.GetCleaningFlags().HasAnyFlag(CleaningFlags.Queststatus);

            foreach (var save in _questStatusSave)
                if (save.Value == QuestSaveType.Default)
                {
                    var data = _questStatus.LookupByKey(save.Key);

                    if (data != null &&
                        (keepAbandoned || data.Status != QuestStatus.None))
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_QUESTSTATUS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, save.Key);
                        stmt.AddValue(2, (byte)data.Status);
                        stmt.AddValue(3, data.Explored);
                        stmt.AddValue(4, (long)GetQuestSlotAcceptTime(data.Slot));
                        stmt.AddValue(5, (long)GetQuestSlotEndTime(data.Slot));
                        trans.Append(stmt);

                        // Save objectives
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_BY_QUEST);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, save.Key);
                        trans.Append(stmt);

                        Quest quest = Global.ObjectMgr.GetQuestTemplate(save.Key);

                        foreach (QuestObjective obj in quest.Objectives)
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_QUESTSTATUS_OBJECTIVES);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            stmt.AddValue(1, save.Key);
                            stmt.AddValue(2, obj.StorageIndex);
                            stmt.AddValue(3, GetQuestSlotObjectiveData(data.Slot, obj));
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

            _questStatusSave.Clear();

            foreach (var save in _rewardedQuestsSave)
                if (save.Value == QuestSaveType.Default)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_QUESTSTATUS_REWARDED);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, save.Key);
                    trans.Append(stmt);
                }
                else if (save.Value == QuestSaveType.ForceDelete ||
                         !keepAbandoned)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED_BY_QUEST);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, save.Key);
                    trans.Append(stmt);
                }

            _rewardedQuestsSave.Clear();

            if (!isTransaction)
                DB.Characters.CommitTransaction(trans);
        }

        private void _SaveDailyQuestStatus(SQLTransaction trans)
        {
            if (!_dailyQuestChanged)
                return;

            _dailyQuestChanged = false;

            // save last daily quest Time for all quests: we need only mostly reset Time for reset check anyway

            // we don't need transactions here.
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY);
            stmt.AddValue(0, GetGUID().GetCounter());

            foreach (int questId in ActivePlayerData.DailyQuestsCompleted)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_DAILY);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, questId);
                stmt.AddValue(2, _lastDailyQuestTime);
                trans.Append(stmt);
            }

            if (!_dfQuests.Empty())
                foreach (var id in _dfQuests)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_DAILY);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, id);
                    stmt.AddValue(2, _lastDailyQuestTime);
                    trans.Append(stmt);
                }
        }

        private void _SaveWeeklyQuestStatus(SQLTransaction trans)
        {
            if (!_weeklyQuestChanged ||
                _weeklyquests.Empty())
                return;

            // we don't need transactions here.
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            foreach (var quest_id in _weeklyquests)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_WEEKLY);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, quest_id);
                trans.Append(stmt);
            }

            _weeklyQuestChanged = false;
        }

        private void _SaveSeasonalQuestStatus(SQLTransaction trans)
        {
            if (!_seasonalQuestChanged)
                return;

            // we don't need transactions here.
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            _seasonalQuestChanged = false;

            if (_seasonalquests.Empty())
                return;

            foreach (var (eventId, dictionary) in _seasonalquests)
            {
                foreach (var (questId, completedTime) in dictionary)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_SEASONAL);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, questId);
                    stmt.AddValue(2, eventId);
                    stmt.AddValue(3, completedTime);
                    trans.Append(stmt);
                }
            }
        }

        private void _SaveMonthlyQuestStatus(SQLTransaction trans)
        {
            if (!_monthlyQuestChanged ||
                _monthlyquests.Empty())
                return;

            // we don't need transactions here.
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            foreach (var questId in _monthlyquests)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_MONTHLY);
                stmt.AddValue(0, GetGUID().GetCounter());
                stmt.AddValue(1, questId);
                trans.Append(stmt);
            }

            _monthlyQuestChanged = false;
        }

        private void _SaveTalents(SQLTransaction trans)
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

        private void _SaveTraits(SQLTransaction trans)
        {
            PreparedStatement stmt = null;

            foreach (var (traitConfigId, state) in _traitConfigStates)
                switch (state)
                {
                    case PlayerSpellState.Changed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, traitConfigId);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, traitConfigId);
                        trans.Append(stmt);

                        TraitConfig traitConfig = GetTraitConfig(traitConfigId);

                        if (traitConfig != null)
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_TRAIT_CONFIGS);
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
                                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_TRAIT_ENTRIES);
                                stmt.AddValue(0, GetGUID().GetCounter());
                                stmt.AddValue(1, traitConfig.ID);
                                stmt.AddValue(2, traitEntry.TraitNodeID);
                                stmt.AddValue(3, traitEntry.TraitNodeEntryID);
                                stmt.AddValue(4, traitEntry.Rank);
                                stmt.AddValue(5, traitEntry.GrantedRanks);
                                trans.Append(stmt);
                            }
                        }

                        break;
                    case PlayerSpellState.Removed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, traitConfigId);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, traitConfigId);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION_BY_TRAIT_CONFIG);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, traitConfigId);
                        trans.Append(stmt);

                        break;
                    default:
                        break;
                }

            _traitConfigStates.Clear();
        }

        public void _SaveMail(SQLTransaction trans)
        {
            PreparedStatement stmt;

            foreach (var m in _mail)
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
                        foreach (var mailItemInfo in m.items)
                        {
                            Item.DeleteFromDB(trans, mailItemInfo.item_guid);
                            AzeriteItem.DeleteFromDB(trans, mailItemInfo.item_guid);
                            AzeriteEmpoweredItem.DeleteFromDB(trans, mailItemInfo.item_guid);
                        }

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
                    stmt.AddValue(0, m.messageID);
                    trans.Append(stmt);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                    stmt.AddValue(0, m.messageID);
                    trans.Append(stmt);
                }

            //deallocate deleted mails...
            foreach (var m in GetMails().ToList())
                if (m.state == MailState.Deleted)
                    _mail.Remove(m);

            MailUpdated = false;
        }

        private void _SaveStoredAuraTeleportLocations(SQLTransaction trans)
        {
            foreach (var pair in _storedAuraTeleportLocations.ToList())
            {
                var storedLocation = pair.Value;

                if (storedLocation.CurrentState == StoredAuraTeleportLocation.State.Deleted)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATION);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);
                    _storedAuraTeleportLocations.Remove(pair.Key);

                    continue;
                }

                if (storedLocation.CurrentState == StoredAuraTeleportLocation.State.Changed)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATION);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_AURA_STORED_LOCATION);
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

        private void _SaveStats(SQLTransaction trans)
        {
            // check if stat saving is enabled and if char level is high enough
            if (WorldConfig.GetIntValue(WorldCfg.MinLevelStatSave) == 0 ||
                GetLevel() < WorldConfig.GetIntValue(WorldCfg.MinLevelStatSave))
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_STATS);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            byte index = 0;
            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_STATS);
            stmt.AddValue(index++, GetGUID().GetCounter());
            stmt.AddValue(index++, GetMaxHealth());

            for (byte i = 0; i < (int)PowerType.MaxPerClass; ++i)
                stmt.AddValue(index++, UnitData.MaxPower[i]);

            for (byte i = 0; i < (int)Stats.Max; ++i)
                stmt.AddValue(index++, GetStat((Stats)i));

            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                stmt.AddValue(index++, GetResistance((SpellSchools)i));

            stmt.AddValue(index++, ActivePlayerData.BlockPercentage);
            stmt.AddValue(index++, ActivePlayerData.DodgePercentage);
            stmt.AddValue(index++, ActivePlayerData.ParryPercentage);
            stmt.AddValue(index++, ActivePlayerData.CritPercentage);
            stmt.AddValue(index++, ActivePlayerData.RangedCritPercentage);
            stmt.AddValue(index++, ActivePlayerData.SpellCritPercentage);
            stmt.AddValue(index++, UnitData.AttackPower);
            stmt.AddValue(index++, UnitData.RangedAttackPower);
            stmt.AddValue(index++, GetBaseSpellPowerBonus());
            stmt.AddValue(index, ActivePlayerData.CombatRatings[(int)CombatRating.ResiliencePlayerDamage]);

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

        private void _SaveEquipmentSets(SQLTransaction trans)
        {
            foreach (var pair in _equipmentSets)
            {
                EquipmentSetInfo eqSet = pair.Value;
                PreparedStatement stmt;
                byte j = 0;

                switch (eqSet.State)
                {
                    case EquipmentSetUpdateState.Unchanged:
                        break; // do nothing
                    case EquipmentSetUpdateState.Changed:
                        if (eqSet.Data.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_EQUIP_SET);
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
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_TRANSMOG_OUTFIT);
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
                        eqSet.State = EquipmentSetUpdateState.Unchanged;

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

                            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
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

                            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                                stmt.AddValue(j++, eqSet.Data.Appearances[i]);

                            for (int i = 0; i < eqSet.Data.Enchants.Length; ++i)
                                stmt.AddValue(j++, eqSet.Data.Enchants[i]);
                        }

                        trans.Append(stmt);
                        eqSet.State = EquipmentSetUpdateState.Unchanged;

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

        private void _SaveVoidStorage(SQLTransaction trans)
        {
            PreparedStatement stmt;

            for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
            {
                if (_voidStorageItems[i] == null) // unused Item
                {
                    // DELETE FROM void_storage WHERE Slot = ? AND playerGuid = ?
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_SLOT);
                    stmt.AddValue(0, i);
                    stmt.AddValue(1, GetGUID().GetCounter());
                }

                else
                {
                    // REPLACE INTO character_void_storage (ItemId, playerGuid, itemEntry, Slot, creatorGuid, randomPropertyType, randomProperty, upgradeId, fixedScalingLevel, artifactKnowledgeLevel, bonusListIDs) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
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

                    StringBuilder bonusListIDs = new();

                    foreach (uint bonusListID in _voidStorageItems[i].BonusListIDs)
                        bonusListIDs.AppendFormat("{0} ", bonusListID);

                    stmt.AddValue(9, bonusListIDs.ToString());
                }

                trans.Append(stmt);
            }
        }

        private void _SaveCUFProfiles(SQLTransaction trans)
        {
            PreparedStatement stmt;
            ulong lowGuid = GetGUID().GetCounter();

            for (byte i = 0; i < PlayerConst.MaxCUFProfiles; ++i)
            {
                if (_cUFProfiles[i] == null) // unused profile
                {
                    // DELETE FROM character_cuf_profiles WHERE Guid = ? and Id = ?
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_CUF_PROFILES_BY_ID);
                    stmt.AddValue(0, lowGuid);
                    stmt.AddValue(1, i);
                }
                else
                {
                    // REPLACE INTO character_cuf_profiles (Guid, Id, Name, frameHeight, frameWidth, sortBy, healthText, boolOptions, unk146, unk147, unk148, unk150, unk152, unk154) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_CUF_PROFILES);
                    stmt.AddValue(0, lowGuid);
                    stmt.AddValue(1, i);
                    stmt.AddValue(2, _cUFProfiles[i].ProfileName);
                    stmt.AddValue(3, _cUFProfiles[i].FrameHeight);
                    stmt.AddValue(4, _cUFProfiles[i].FrameWidth);
                    stmt.AddValue(5, _cUFProfiles[i].SortBy);
                    stmt.AddValue(6, _cUFProfiles[i].HealthText);
                    stmt.AddValue(7, (uint)_cUFProfiles[i].GetUlongOptionValue()); // 25 of 32 fields used, fits in an int
                    stmt.AddValue(8, _cUFProfiles[i].TopPoint);
                    stmt.AddValue(9, _cUFProfiles[i].BottomPoint);
                    stmt.AddValue(10, _cUFProfiles[i].LeftPoint);
                    stmt.AddValue(11, _cUFProfiles[i].TopOffset);
                    stmt.AddValue(12, _cUFProfiles[i].BottomOffset);
                    stmt.AddValue(13, _cUFProfiles[i].LeftOffset);
                }

                trans.Append(stmt);
            }
        }

        private void _SaveInstanceTimeRestrictions(SQLTransaction trans)
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

        private void _SaveBGData(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_BGDATA);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PLAYER_BGDATA);
            stmt.AddValue(0, GetGUID().GetCounter());
            stmt.AddValue(1, _bgData.InstanceID);
            stmt.AddValue(2, _bgData.Team);
            stmt.AddValue(3, _bgData.JoinPos.GetPositionX());
            stmt.AddValue(4, _bgData.JoinPos.GetPositionY());
            stmt.AddValue(5, _bgData.JoinPos.GetPositionZ());
            stmt.AddValue(6, _bgData.JoinPos.GetOrientation());
            stmt.AddValue(7, (ushort)_bgData.JoinPos.GetMapId());
            stmt.AddValue(8, _bgData.TaxiPath[0]);
            stmt.AddValue(9, _bgData.TaxiPath[1]);
            stmt.AddValue(10, _bgData.MountSpell);
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
            byte bankSlots = result.Read<byte>(fieldIndex++);
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

            // check Name limitations
            if (ObjectManager.CheckPlayerName(GetName(), GetSession().GetSessionDbcLocale()) != ResponseCodes.CharNameSuccess ||
                (!GetSession().HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(GetName())))
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)AtLoginFlags.Rename);
                stmt.AddValue(1, guid.GetCounter());
                DB.Characters.Execute(stmt);

                return false;
            }


            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.WowAccount), GetSession().GetAccountGUID());
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.BnetAccount), GetSession().GetBattlenetAccountGUID());

            if (gender >= Gender.None)
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong Gender ({1}), can't be loaded.", guid.ToString(), gender);

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

            for (int i = 0; i < exploredZonesStrings.Length && i / 2 < ActivePlayerData.ExploredZonesSize; ++i)
                SetUpdateFieldFlagValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ExploredZones, i / 2), (ulong)((long.Parse(exploredZonesStrings[i])) << (32 * (i % 2))));

            StringArray knownTitlesStrings = new(knownTitles, ' ');

            if ((knownTitlesStrings.Length % 2) == 0)
                for (int i = 0; i < knownTitlesStrings.Length; ++i)
                    SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.KnownTitles, i / 2), (ulong)((long.Parse(knownTitlesStrings[i])) << (32 * (i % 2))));

            SetObjectScale(1.0f);
            SetHoverHeight(1.0f);

            // load achievements before anything else to prevent multiple gains for the same Achievement/criteria on every loading (as loading does call UpdateAchievementCriteria)
            _achievementSys.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Achievements), holder.GetResult(PlayerLoginQueryLoad.CriteriaProgress));
            _questObjectiveCriteriaMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectivesCriteria), holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectivesCriteriaProgress));

            SetMoney(Math.Min(money, PlayerConst.MaxMoneyAmount));

            List<ChrCustomizationChoice> customizations = new();
            SQLResult customizationsResult = holder.GetResult(PlayerLoginQueryLoad.Customizations);

            if (!customizationsResult.IsEmpty())
                do
                {
                    ChrCustomizationChoice choice = new();
                    choice.ChrCustomizationOptionID = customizationsResult.Read<uint>(0);
                    choice.ChrCustomizationChoiceID = customizationsResult.Read<uint>(1);
                    customizations.Add(choice);
                } while (customizationsResult.NextRow());

            SetCustomizations(customizations, false);
            SetInventorySlotCount(inventorySlots);
            SetBankBagSlotCount(bankSlots);
            SetNativeGender(gender);
            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.Inebriation), drunk);
            ReplaceAllPlayerFlags(playerFlags);
            ReplaceAllPlayerFlagsEx(playerFlagsEx);
            SetWatchedFactionIndex(watchedFaction);

            AtLoginFlags = (AtLoginFlags)at_login;

            if (!GetSession().ValidateAppearance(GetRace(), GetClass(), gender, customizations))
            {
                Log.outError(LogFilter.Player, "Player {0} has wrong Appearance values (Hair/Skin/Color), can't be loaded.", guid.ToString());

                return false;
            }

            // set which actionbars the client has active - DO NOT REMOVE EVER AGAIN (can be changed though, if it does change fieldwise)
            SetMultiActionBars(actionBars);

            _fishingSteps = fishingSteps;

            InitDisplayIds();

            //Need to call it to initialize _team (_team can be calculated from race)
            //Other way is to saves _team into characters table.
            SetFactionForRace(GetRace());

            // load home bind and check in same Time class/race pair, it used later for restore broken positions
            if (!_LoadHomeBind(holder.GetResult(PlayerLoginQueryLoad.HomeBind)))
                return false;

            InitializeSkillFields();
            InitPrimaryProfessions(); // to max set before any spell loaded

            // init saved position, and fix it later if problematic
            Relocate(position_x, position_y, position_z, orientation);

            SetDungeonDifficultyID(CheckLoadedDungeonDifficultyID(dungeonDifficulty));
            SetRaidDifficultyID(CheckLoadedRaidDifficultyID(raidDifficulty));
            SetLegacyRaidDifficultyID(CheckLoadedLegacyRaidDifficultyID(legacyRaidDifficulty));

            var RelocateToHomebind = new Action(() =>
                                                {
                                                    mapId = _homebind.GetMapId();
                                                    instance_id = 0;
                                                    Relocate(_homebind);
                                                });

            _LoadGroup(holder.GetResult(PlayerLoginQueryLoad.Group));

            _LoadCurrency(holder.GetResult(PlayerLoginQueryLoad.Currency));
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LifetimeHonorableKills), totalKills);
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TodayHonorableKills), todayKills);
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.YesterdayHonorableKills), yesterdayKills);

            _LoadInstanceTimeRestrictions(holder.GetResult(PlayerLoginQueryLoad.InstanceLockTimes));
            _LoadBGData(holder.GetResult(PlayerLoginQueryLoad.BgData));

            GetSession().SetPlayer(this);

            Map map = null;
            bool player_at_bg = false;
            var mapEntry = CliDB.MapStorage.LookupByKey(mapId);

            if (mapEntry == null ||
                !IsPositionValid())
            {
                Log.outError(LogFilter.Player, "Player (guidlow {0}) have invalid coordinates (_mapId: {1} {2}). Teleport to default race/class locations.", guid.ToString(), mapId, GetPosition());
                RelocateToHomebind();
            }
            else if (mapEntry.IsBattlegroundOrArena())
            {
                Battleground currentBg = null;

                if (_bgData.InstanceID != 0) //saved in Battleground
                    currentBg = Global.BattlegroundMgr.GetBattleground(_bgData.InstanceID, BattlegroundTypeId.None);

                player_at_bg = currentBg != null && currentBg.IsPlayerInBattleground(GetGUID());

                if (player_at_bg && currentBg.GetStatus() != BattlegroundStatus.WaitLeave)
                {
                    map = currentBg.GetBgMap();

                    BattlegroundQueueTypeId bgQueueTypeId = currentBg.GetQueueId();
                    AddBattlegroundQueueId(bgQueueTypeId);

                    _bgData.TypeID = currentBg.GetTypeID();

                    //join player to Battlegroundgroup
                    currentBg.EventPlayerLoggedIn(this);

                    SetInviteForBattlegroundQueueType(bgQueueTypeId, currentBg.GetInstanceID());
                    SetMercenaryForBattlegroundQueueType(bgQueueTypeId, currentBg.IsPlayerMercenaryInBattleground(GetGUID()));
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
                    {
                        Relocate(_loc);
                    }

                    // We are not in BG anymore
                    _bgData.InstanceID = 0;
                }
            }
            // currently we do not support Transport in bg
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
                            InstanceId = 0;
                            transportMap = Global.MapMgr.CreateMap(mapId, this);

                            if (transportMap)
                                transport = transportMap.GetTransport(transGUID);
                        }
                        else
                        {
                            transport = transportOnMap;
                        }
                    }
                }

                if (transport)
                {
                    float x = trans_x;
                    float y = trans_y;
                    float z = trans_z;
                    float o = trans_o;

                    MovementInfo.Transport.Pos = new Position(x, y, z, o);
                    transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);

                    if (!GridDefines.IsValidMapCoord(x, y, z, o) ||
                        // Transport size limited
                        Math.Abs(MovementInfo.Transport.Pos.X) > 250.0f ||
                        Math.Abs(MovementInfo.Transport.Pos.Y) > 250.0f ||
                        Math.Abs(MovementInfo.Transport.Pos.Z) > 250.0f)
                    {
                        Log.outError(LogFilter.Player, "Player (guidlow {0}) have invalid Transport coordinates (X: {1} Y: {2} Z: {3} O: {4}). Teleport to bind location.", guid.ToString(), x, y, z, o);

                        MovementInfo.Transport.Reset();
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
                    Log.outError(LogFilter.Player, "Player (guidlow {0}) have problems with Transport Guid ({1}). Teleport to bind location.", guid.ToString(), transguid);

                    RelocateToHomebind();
                }
            }
            // currently we do not support taxi in instance
            else if (!taxi_path.IsEmpty())
            {
                instance_id = 0;

                // Not finish taxi flight path
                if (_bgData.HasTaxiPath())
                    for (int i = 0; i < 2; ++i)
                        Taxi.AddTaxiDestination(_bgData.TaxiPath[i]);

                if (!Taxi.LoadTaxiDestinationsFromString(taxi_path, GetTeam()))
                {
                    // problems with taxi path loading
                    TaxiNodesRecord nodeEntry = null;
                    uint node_id = Taxi.GetTaxiSource();

                    if (node_id != 0)
                        nodeEntry = CliDB.TaxiNodesStorage.LookupByKey(node_id);

                    if (nodeEntry == null) // don't know taxi start node, to _homebind
                    {
                        Log.outError(LogFilter.Player, "Character {0} have wrong _data in taxi destination list, teleport to _homebind.", GetGUID().ToString());
                        RelocateToHomebind();
                    }
                    else // have start node, to it
                    {
                        Log.outError(LogFilter.Player, "Character {0} have too short taxi destination list, teleport to original node.", GetGUID().ToString());
                        mapId = nodeEntry.ContinentID;
                        Relocate(nodeEntry.Pos.X, nodeEntry.Pos.Y, nodeEntry.Pos.Z, 0.0f);
                    }

                    Taxi.ClearTaxiDestinations();
                }

                uint nodeid = Taxi.GetTaxiSource();

                if (nodeid != 0)
                {
                    // save source node as recall coord to prevent recall and fall from sky
                    var nodeEntry = CliDB.TaxiNodesStorage.LookupByKey(nodeid);

                    if (nodeEntry != null &&
                        nodeEntry.ContinentID == GetMapId())
                    {
                        Cypher.Assert(nodeEntry != null); // checked in Taxi.LoadTaxiDestinationsFromString
                        mapId = nodeEntry.ContinentID;
                        Relocate(nodeEntry.Pos.X, nodeEntry.Pos.Y, nodeEntry.Pos.Z, 0.0f);
                    }

                    // flight will started later
                }
            }
            else if (mapEntry.IsDungeon() &&
                     InstanceId != 0)
            {
                // try finding instance by Id first
                map = Global.MapMgr.FindMap(mapId, InstanceId);
            }

            // Map could be changed before
            mapEntry = CliDB.MapStorage.LookupByKey(mapId);

            // client without expansion support
            if (mapEntry != null)
                if (GetSession().GetExpansion() < mapEntry.Expansion())
                {
                    Log.outDebug(LogFilter.Player, "Player {0} using client without required expansion tried login at non accessible map {1}", GetName(), mapId);
                    RelocateToHomebind();
                }

            // NOW player must have valid map
            // load the player's map here if it's not already loaded
            if (!map)
                map = Global.MapMgr.CreateMap(mapId, this);

            AreaTriggerStruct areaTrigger = null;
            bool check = false;

            if (!map)
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
                else if (instance_id != 0 &&
                         Global.InstanceLockMgr.FindActiveInstanceLock(guid, new MapDb2Entries(mapId, map.GetDifficultyID())) != null) // ... and instance is reseted then look for entrance.
                {
                    areaTrigger = Global.ObjectMgr.GetMapEntranceTrigger(mapId);
                    check = true;
                }
            }

            if (check)                   // in case of special event when creating map...
                if (areaTrigger != null) // ... if we have an areatrigger, then relocate to new map/coordinates.
                {
                    Relocate(areaTrigger.target_X, areaTrigger.target_Y, areaTrigger.target_Z, GetOrientation());

                    if (mapId != areaTrigger.target_mapId)
                    {
                        mapId = areaTrigger.target_mapId;
                        map = Global.MapMgr.CreateMap(mapId, this);
                    }
                }

            if (!map)
            {
                RelocateToHomebind();
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
            if (!CheckInstanceValidity(true) &&
                !IsInstanceLoginGameMasterException())
                InstanceValid = false;

            if (player_at_bg)
                map.ToBattlegroundMap().GetBG().AddPlayer(this);

            // randomize first save Time in range [CONFIG_INTERVAL_SAVE] around [CONFIG_INTERVAL_SAVE]
            // this must help in case next save after mass player load after server startup
            _nextSave = RandomHelper.URand(_nextSave / 2, _nextSave * 3 / 2);

            SaveRecallPosition();

            long now = GameTime.GetGameTime();
            long logoutTime = logout_time;

            SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.LogoutTime), logoutTime);

            // since last logout (in seconds)
            uint time_diff = (uint)(now - logoutTime);

            // set value, including drunk invisibility detection
            // calculate sobering. after 15 minutes logged out, the player will be sober again
            if (time_diff < (uint)GetDrunkValue() * 9)
                SetDrunkValue((byte)(GetDrunkValue() - time_diff / 9));
            else
                SetDrunkValue(0);

            _createTime = createTime;
            _createMode = createMode;
            _cinematic = cinematic;
            _playedTimeTotal = totaltime;
            _playedTimeLevel = leveltime;

            SetTalentResetCost(resettalents_cost);
            SetTalentResetTime(resettalents_time);

            Taxi.LoadTaxiMask(taximask); // must be before InitTaxiNodesForLevel

            _LoadPetStable(summonedPetNumber, holder.GetResult(PlayerLoginQueryLoad.PetSlots));

            // Honor system
            // Update Honor kills _data
            _lastHonorUpdateTime = logoutTime;
            UpdateHonorFields();

            _deathExpireTime = death_expire_time;

            if (_deathExpireTime > now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep)
                _deathExpireTime = now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep - 1;

            RemoveUnitFlag2(UnitFlags2.ForceMovement);

            // make sure the unit is considered out of combat for proper loading
            ClearInCombat();

            // reset Stats before loading any modifiers
            InitStatsForLevel();
            InitTaxiNodesForLevel();
            InitRunes();

            // rest bonus can only be calculated after InitStatsForLevel()
            _restMgr.LoadRestBonus(RestTypes.XP, restState, rest_bonus);

            // load Skills after InitStatsForLevel because it triggering aura apply also
            _LoadSkills(holder.GetResult(PlayerLoginQueryLoad.Skills));
            UpdateSkillsForLevel();

            SetNumRespecs(numRespecs);
            SetPrimarySpecialization(primarySpecialization);
            SetActiveTalentGroup(activeTalentGroup);
            ChrSpecializationRecord primarySpec = CliDB.ChrSpecializationStorage.LookupByKey(GetPrimarySpecialization());

            if (primarySpec == null ||
                primarySpec.ClassID != (byte)GetClass() ||
                GetActiveTalentGroup() >= PlayerConst.MaxSpecializations)
                ResetTalentSpecialization();

            ChrSpecializationRecord chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(lootSpecId);

            if (chrSpec != null)
                if (chrSpec.ClassID == (uint)GetClass())
                    SetLootSpecId(lootSpecId);

            UpdateDisplayPower();
            _LoadTalents(holder.GetResult(PlayerLoginQueryLoad.Talents));
            _LoadPvpTalents(holder.GetResult(PlayerLoginQueryLoad.PvpTalents));
            _LoadSpells(holder.GetResult(PlayerLoginQueryLoad.Spells), holder.GetResult(PlayerLoginQueryLoad.SpellFavorites));
            GetSession().GetCollectionMgr().LoadToys();
            GetSession().GetCollectionMgr().LoadHeirlooms();
            GetSession().GetCollectionMgr().LoadMounts();
            GetSession().GetCollectionMgr().LoadItemAppearances();
            GetSession().GetCollectionMgr().LoadTransmogIllusions();

            LearnSpecializationSpells();

            _LoadGlyphs(holder.GetResult(PlayerLoginQueryLoad.Glyphs));
            _LoadAuras(holder.GetResult(PlayerLoginQueryLoad.Auras), holder.GetResult(PlayerLoginQueryLoad.AuraEffects), time_diff);
            _LoadGlyphAuras();

            // add ghost flag (must be after aura load: PLAYER_FLAGS_GHOST set in aura)
            if (HasPlayerFlag(PlayerFlags.Ghost))
                DeathState = DeathState.Dead;

            // Load spell locations - must be after loading Auras
            _LoadStoredAuraTeleportLocations(holder.GetResult(PlayerLoginQueryLoad.AuraStoredLocations));

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

            _LoadTraits(holder.GetResult(PlayerLoginQueryLoad.TraitConfigs), holder.GetResult(PlayerLoginQueryLoad.TraitEntries)); // must be after loading spells

            // must be before inventory (some items required reputation check)
            _reputationMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Reputation));

            _LoadInventory(holder.GetResult(PlayerLoginQueryLoad.Inventory),
                           holder.GetResult(PlayerLoginQueryLoad.Artifacts),
                           holder.GetResult(PlayerLoginQueryLoad.Azerite),
                           holder.GetResult(PlayerLoginQueryLoad.AzeriteMilestonePowers),
                           holder.GetResult(PlayerLoginQueryLoad.AzeriteUnlockedEssences),
                           holder.GetResult(PlayerLoginQueryLoad.AzeriteEmpowered),
                           time_diff);

            if (IsVoidStorageUnlocked())
                _LoadVoidStorage(holder.GetResult(PlayerLoginQueryLoad.VoidStorage));

            // update items with duration and realtime
            UpdateItemDuration(time_diff, true);

            StartLoadingActionButtons();

            // unread mails and next delivery Time, actual mails not loaded
            _LoadMail(holder.GetResult(PlayerLoginQueryLoad.Mails),
                      holder.GetResult(PlayerLoginQueryLoad.MailItems),
                      holder.GetResult(PlayerLoginQueryLoad.MailItemsArtifact),
                      holder.GetResult(PlayerLoginQueryLoad.MailItemsAzerite),
                      holder.GetResult(PlayerLoginQueryLoad.MailItemsAzeriteMilestonePower),
                      holder.GetResult(PlayerLoginQueryLoad.MailItemsAzeriteUnlockedEssence),
                      holder.GetResult(PlayerLoginQueryLoad.MailItemsAzeriteEmpowered));

            _social = Global.SocialMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.SocialList), GetGUID());

            // check PLAYER_CHOSEN_TITLE compatibility with PLAYER__FIELD_KNOWN_TITLES
            // note: PLAYER__FIELD_KNOWN_TITLES updated at quest status loaded
            if (chosenTitle != 0 &&
                !HasTitle(chosenTitle))
                chosenTitle = 0;

            SetChosenTitle(chosenTitle);

            // has to be called after last Relocate() in Player.LoadFromDB
            SetFallInformation(0, GetPositionZ());

            GetSpellHistory().LoadFromDB<Player>(holder.GetResult(PlayerLoginQueryLoad.SpellCooldowns), holder.GetResult(PlayerLoginQueryLoad.SpellCharges));

            uint savedHealth = health;

            if (savedHealth == 0)
                DeathState = DeathState.Corpse;

            // Spell code allow apply any Auras to dead character in load Time in aura/spell/Item loading
            // Do now before Stats re-calculation cleanup for ghost State unexpected Auras
            if (!IsAlive())
                RemoveAllAurasOnDeath();
            else
                RemoveAllAurasRequiringDeadTarget();

            //apply all stat bonuses from items and Auras
            SetCanModifyStats(true);
            UpdateAllStats();

            // restore remembered power/health values (but not more max values)
            SetHealth(savedHealth > GetMaxHealth() ? GetMaxHealth() : savedHealth);
            int loadedPowers = 0;

            for (PowerType i = 0; i < PowerType.Max; ++i)
                if (Global.DB2Mgr.GetPowerIndexByClass(i, GetClass()) != (int)PowerType.Max)
                {
                    uint savedPower = powers[loadedPowers];
                    uint maxPower = UnitData.MaxPower[loadedPowers];
                    SetPower(i, (int)(savedPower > maxPower ? maxPower : savedPower));

                    if (++loadedPowers >= (int)PowerType.MaxPerClass)
                        break;
                }

            for (; loadedPowers < (int)PowerType.MaxPerClass; ++loadedPowers)
                SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.Power, loadedPowers), 0);

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

            Log.outDebug(LogFilter.Player, "The value of player {0} after load Item and aura is: ", GetName());

            // GM State
            if (GetSession().HasPermission(RBACPermissions.RestoreSavedGmState))
            {
                switch (WorldConfig.GetIntValue(WorldCfg.GmLoginState))
                {
                    default:
                    case 0:
                        break; // disable
                    case 1:
                        SetGameMaster(true);

                        break; // enable
                    case 2:    // save State
                        if (extra_flags.HasAnyFlag(PlayerExtraFlags.GMOn))
                            SetGameMaster(true);

                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmVisibleState))
                {
                    default:
                    case 0:
                        SetGMVisible(false);

                        break; // invisible
                    case 1:
                        break; // visible
                    case 2:    // save State
                        if (extra_flags.HasAnyFlag(PlayerExtraFlags.GMInvisible))
                            SetGMVisible(false);

                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmChat))
                {
                    default:
                    case 0:
                        break; // disable
                    case 1:
                        SetGMChat(true);

                        break; // enable
                    case 2:    // save State
                        if (extra_flags.HasAnyFlag(PlayerExtraFlags.GMChat))
                            SetGMChat(true);

                        break;
                }

                switch (WorldConfig.GetIntValue(WorldCfg.GmWhisperingTo))
                {
                    default:
                    case 0:
                        break; // disable
                    case 1:
                        SetAcceptWhispers(true);

                        break; // enable
                    case 2:    // save State
                        if (extra_flags.HasAnyFlag(PlayerExtraFlags.AcceptWhispers))
                            SetAcceptWhispers(true);

                        break;
                }
            }

            InitPvP();

            // RaF stuff.
            if (GetSession().IsARecruiter() ||
                (GetSession().GetRecruiterId() != 0))
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

            _achievementSys.CheckAllAchievementCriteria(this);
            _questObjectiveCriteriaMgr.CheckAllQuestObjectiveCriteria(this);

            PushQuests();

            foreach (var transmogIllusion in CliDB.TransmogIllusionStorage.Values)
            {
                if (!transmogIllusion.GetFlags().HasFlag(TransmogIllusionFlags.PlayerConditionGrantsOnLogin))
                    continue;

                if (GetSession().GetCollectionMgr().HasTransmogIllusion(transmogIllusion.Id))
                    continue;

                var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(transmogIllusion.UnlockConditionID);

                if (playerCondition != null)
                    if (!ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
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
            _nextSave = WorldConfig.GetUIntValue(WorldCfg.IntervalSave);

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
                Global.ScriptMgr.ForEach<IPlayerOnSave>(p => p.OnSave(this));

            PreparedStatement stmt;
            byte index = 0;

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_FISHINGSTEPS);
            stmt.AddValue(0, GetGUID().GetCounter());
            characterTransaction.Append(stmt);

            static float finiteAlways(float f)
            {
                return float.IsFinite(f) ? f : 0.0f;
            }

            ;

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
                stmt.AddValue(index++, (byte)GetNativeGender()); // save Gender from PLAYER_BYTES_3, UNIT_BYTES_0 changes with every transform effect
                stmt.AddValue(index++, GetLevel());
                stmt.AddValue(index++, GetXP());
                stmt.AddValue(index++, GetMoney());
                stmt.AddValue(index++, GetInventorySlotCount());
                stmt.AddValue(index++, GetBankBagSlotCount());
                stmt.AddValue(index++, ActivePlayerData.RestInfo[(int)RestTypes.XP].StateID);
                stmt.AddValue(index++, PlayerData.PlayerFlags);
                stmt.AddValue(index++, PlayerData.PlayerFlagsEx);
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

                for (int i = 0; i < Taxi._taximask.Length; ++i)
                    ss.Append(Taxi._taximask[i] + " ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, _createTime);
                stmt.AddValue(index++, (byte)_createMode);
                stmt.AddValue(index++, _cinematic);
                stmt.AddValue(index++, _playedTimeTotal);
                stmt.AddValue(index++, _playedTimeLevel);
                stmt.AddValue(index++, finiteAlways(_restMgr.GetRestBonus(RestTypes.XP)));
                stmt.AddValue(index++, GameTime.GetGameTime());
                stmt.AddValue(index++, (HasPlayerFlag(PlayerFlags.Resting) ? 1 : 0));
                //save, far from tavern/city
                //save, but in tavern/city
                stmt.AddValue(index++, GetTalentResetCost());
                stmt.AddValue(index++, GetTalentResetTime());
                stmt.AddValue(index++, GetPrimarySpecialization());
                stmt.AddValue(index++, (ushort)_extraFlags);
                stmt.AddValue(index++, 0); // summonedPetNumber
                stmt.AddValue(index++, (ushort)AtLoginFlags);
                stmt.AddValue(index++, _deathExpireTime);

                ss.Clear();
                ss.Append(Taxi.SaveTaxiDestinationsToString());

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, ActivePlayerData.LifetimeHonorableKills);
                stmt.AddValue(index++, ActivePlayerData.TodayHonorableKills);
                stmt.AddValue(index++, ActivePlayerData.YesterdayHonorableKills);
                stmt.AddValue(index++, PlayerData.PlayerTitle);
                stmt.AddValue(index++, ActivePlayerData.WatchedFactionIndex);
                stmt.AddValue(index++, GetDrunkValue());
                stmt.AddValue(index++, GetHealth());

                int storedPowers = 0;

                for (PowerType powerType = 0; powerType < PowerType.Max; ++powerType)
                    if (GetPowerIndex(powerType) != (int)PowerType.Max)
                    {
                        stmt.AddValue(index++, UnitData.Power[storedPowers]);

                        if (++storedPowers >= (int)PowerType.MaxPerClass)
                            break;
                    }

                for (; storedPowers < (int)PowerType.MaxPerClass; ++storedPowers)
                    stmt.AddValue(index++, 0);

                stmt.AddValue(index++, GetSession().GetLatency());
                stmt.AddValue(index++, GetActiveTalentGroup());
                stmt.AddValue(index++, GetLootSpecId());

                ss.Clear();

                for (int i = 0; i < PlayerConst.ExploredZonesSize; ++i)
                    ss.Append($"{(uint)(ActivePlayerData.ExploredZones[i] & 0xFFFFFFFF)} {(uint)((ActivePlayerData.ExploredZones[i] >> 32) & 0xFFFFFFFF)} ");

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
                    {
                        ss.Append("0 0 0 0 0 ");
                    }
                }

                stmt.AddValue(index++, ss.ToString());

                ss.Clear();

                for (int i = 0; i < ActivePlayerData.KnownTitles.Size(); ++i)
                    ss.Append($"{(uint)(ActivePlayerData.KnownTitles[i] & 0xFFFFFFFF)} {(uint)((ActivePlayerData.KnownTitles[i] >> 32) & 0xFFFFFFFF)} ");

                stmt.AddValue(index++, ss.ToString());

                stmt.AddValue(index++, ActivePlayerData.MultiActionBars);
                stmt.AddValue(index++, Global.RealmMgr.GetMinorMajorBugfixVersionForBuild(Global.WorldMgr.GetRealm().Build));
            }
            else
            {
                // Update query
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER);
                stmt.AddValue(index++, GetName());
                stmt.AddValue(index++, (byte)GetRace());
                stmt.AddValue(index++, (byte)GetClass());
                stmt.AddValue(index++, (byte)GetNativeGender()); // save Gender from PLAYER_BYTES_3, UNIT_BYTES_0 changes with every transform effect
                stmt.AddValue(index++, GetLevel());
                stmt.AddValue(index++, GetXP());
                stmt.AddValue(index++, GetMoney());
                stmt.AddValue(index++, GetInventorySlotCount());
                stmt.AddValue(index++, GetBankBagSlotCount());
                stmt.AddValue(index++, ActivePlayerData.RestInfo[(int)RestTypes.XP].StateID);
                stmt.AddValue(index++, PlayerData.PlayerFlags);
                stmt.AddValue(index++, PlayerData.PlayerFlagsEx);

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
                    stmt.AddValue(index++, (ushort)GetTeleportDest().GetMapId());
                    stmt.AddValue(index++, 0);
                    stmt.AddValue(index++, (byte)GetDungeonDifficultyID());
                    stmt.AddValue(index++, (byte)GetRaidDifficultyID());
                    stmt.AddValue(index++, (byte)GetLegacyRaidDifficultyID());
                    stmt.AddValue(index++, finiteAlways(GetTeleportDest().GetPositionX()));
                    stmt.AddValue(index++, finiteAlways(GetTeleportDest().GetPositionY()));
                    stmt.AddValue(index++, finiteAlways(GetTeleportDest().GetPositionZ()));
                    stmt.AddValue(index++, finiteAlways(GetTeleportDest().GetOrientation()));
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

                for (int i = 0; i < Taxi._taximask.Length; ++i)
                    ss.Append(Taxi._taximask[i] + " ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, _cinematic);
                stmt.AddValue(index++, _playedTimeTotal);
                stmt.AddValue(index++, _playedTimeLevel);
                stmt.AddValue(index++, finiteAlways(_restMgr.GetRestBonus(RestTypes.XP)));
                stmt.AddValue(index++, GameTime.GetGameTime());
                stmt.AddValue(index++, (HasPlayerFlag(PlayerFlags.Resting) ? 1 : 0));
                //save, far from tavern/city
                //save, but in tavern/city
                stmt.AddValue(index++, GetTalentResetCost());
                stmt.AddValue(index++, GetTalentResetTime());
                stmt.AddValue(index++, GetNumRespecs());
                stmt.AddValue(index++, GetPrimarySpecialization());
                stmt.AddValue(index++, (ushort)_extraFlags);
                PetStable petStable = GetPetStable();

                if (petStable != null)
                    stmt.AddValue(index++, petStable.GetCurrentPet() != null && petStable.GetCurrentPet().Health > 0 ? petStable.GetCurrentPet().PetNumber : 0); // summonedPetNumber
                else
                    stmt.AddValue(index++, 0); // summonedPetNumber

                stmt.AddValue(index++, (ushort)AtLoginFlags);
                stmt.AddValue(index++, GetZoneId());
                stmt.AddValue(index++, _deathExpireTime);

                ss.Clear();
                ss.Append(Taxi.SaveTaxiDestinationsToString());

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, ActivePlayerData.LifetimeHonorableKills);
                stmt.AddValue(index++, ActivePlayerData.TodayHonorableKills);
                stmt.AddValue(index++, ActivePlayerData.YesterdayHonorableKills);
                stmt.AddValue(index++, PlayerData.PlayerTitle);
                stmt.AddValue(index++, ActivePlayerData.WatchedFactionIndex);
                stmt.AddValue(index++, GetDrunkValue());
                stmt.AddValue(index++, GetHealth());

                int storedPowers = 0;

                for (PowerType powerType = 0; powerType < PowerType.Max; ++powerType)
                    if (GetPowerIndex(powerType) != (int)PowerType.Max)
                    {
                        stmt.AddValue(index++, UnitData.Power[storedPowers]);

                        if (++storedPowers >= (int)PowerType.MaxPerClass)
                            break;
                    }

                for (; storedPowers < (int)PowerType.MaxPerClass; ++storedPowers)
                    stmt.AddValue(index++, 0);

                stmt.AddValue(index++, GetSession().GetLatency());
                stmt.AddValue(index++, GetActiveTalentGroup());
                stmt.AddValue(index++, GetLootSpecId());

                ss.Clear();

                for (int i = 0; i < PlayerConst.ExploredZonesSize; ++i)
                    ss.Append($"{(uint)(ActivePlayerData.ExploredZones[i] & 0xFFFFFFFF)} {(uint)((ActivePlayerData.ExploredZones[i] >> 32) & 0xFFFFFFFF)} ");

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
                    {
                        ss.Append("0 0 0 0 0 ");
                    }
                }

                stmt.AddValue(index++, ss.ToString());

                ss.Clear();

                for (int i = 0; i < ActivePlayerData.KnownTitles.Size(); ++i)
                    ss.Append($"{(uint)(ActivePlayerData.KnownTitles[i] & 0xFFFFFFFF)} {(uint)((ActivePlayerData.KnownTitles[i] >> 32) & 0xFFFFFFFF)} ");

                stmt.AddValue(index++, ss.ToString());
                stmt.AddValue(index++, ActivePlayerData.MultiActionBars);

                stmt.AddValue(index++, IsInWorld && !GetSession().PlayerLogout() ? 1 : 0);
                stmt.AddValue(index++, ActivePlayerData.Honor);
                stmt.AddValue(index++, GetHonorLevel());
                stmt.AddValue(index++, ActivePlayerData.RestInfo[(int)RestTypes.Honor].StateID);
                stmt.AddValue(index++, finiteAlways(_restMgr.GetRestBonus(RestTypes.Honor)));
                stmt.AddValue(index++, Global.RealmMgr.GetMinorMajorBugfixVersionForBuild(Global.WorldMgr.GetRealm().Build));

                // Index
                stmt.AddValue(index, GetGUID().GetCounter());
            }

            characterTransaction.Append(stmt);

            if (_fishingSteps != 0)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_FISHINGSTEPS);
                index = 0;
                stmt.AddValue(index++, GetGUID().GetCounter());
                stmt.AddValue(index++, _fishingSteps);
                characterTransaction.Append(stmt);
            }

            if (MailUpdated) //save mails only when needed
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
            _achievementSys.SaveToDB(characterTransaction);
            _reputationMgr.SaveToDB(characterTransaction);
            _questObjectiveCriteriaMgr.SaveToDB(characterTransaction);
            _SaveEquipmentSets(characterTransaction);
            GetSession().SaveTutorialsData(characterTransaction); // changed only while character in game
            _SaveInstanceTimeRestrictions(characterTransaction);
            _SaveCurrency(characterTransaction);
            _SaveCUFProfiles(characterTransaction);

            _garrison?.SaveToDB(characterTransaction);

            // check if Stats should only be saved on logout
            // save Stats can be out of transaction
            if (GetSession().IsLogingOut() ||
                !WorldConfig.GetBoolValue(WorldCfg.StatsSaveOnlyOnLogout))
                _SaveStats(characterTransaction);

            // TODO: Move this out
            GetSession().GetCollectionMgr().SaveAccountToys(loginTransaction);
            GetSession().GetBattlePetMgr().SaveToDB(loginTransaction);
            GetSession().GetCollectionMgr().SaveAccountHeirlooms(loginTransaction);
            GetSession().GetCollectionMgr().SaveAccountMounts(loginTransaction);
            GetSession().GetCollectionMgr().SaveAccountItemAppearances(loginTransaction);
            GetSession().GetCollectionMgr().SaveAccountTransmogIllusions(loginTransaction);

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
            stmt.AddValue(6, GameTime.GetGameTime());
            loginTransaction.Append(stmt);

            // save pet (hunter pet level and experience and all Type pets health/mana).
            Pet pet = GetPet();

            if (pet)
                pet.SavePetToDB(PetSaveMode.AsCurrent);
        }

        private void DeleteSpellFromAllPlayers(uint spellId)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_SPELL_SPELLS);
            stmt.AddValue(0, spellId);
            DB.Characters.Execute(stmt);
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

                zone = Global.TerrainMgr.GetZoneId(PhasingHandler.EmptyPhaseShift, map, posx, posy, posz);

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

            // Convert Guid to low GUID for CharacterNameData, but also other methods on success
            ulong guid = playerGuid.GetCounter();
            CharDeleteMethod charDelete_method = (CharDeleteMethod)WorldConfig.GetIntValue(WorldCfg.ChardeleteMethod);
            CharacterCacheEntry characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(playerGuid);
            string name = "<Unknown>";

            if (characterInfo != null)
                name = characterInfo.Name;

            if (deleteFinally)
            {
                charDelete_method = CharDeleteMethod.Remove;
            }
            else if (characterInfo != null) // To avoid a Select, we select loaded _data. If it doesn't exist, return.
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
                            MultiMap<uint, Item> itemsByMail = new();

                            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
                            stmt.AddValue(0, guid);
                            SQLResult resultItems = DB.Characters.Query(stmt);

                            if (!resultItems.IsEmpty())
                            {
                                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_ARTIFACT);
                                stmt.AddValue(0, guid);
                                SQLResult artifactResult = DB.Characters.Query(stmt);

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE);
                                stmt.AddValue(0, guid);
                                SQLResult azeriteResult = DB.Characters.Query(stmt);

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_MILESTONE_POWER);
                                stmt.AddValue(0, guid);
                                SQLResult azeriteItemMilestonePowersResult = DB.Characters.Query(stmt);

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_UNLOCKED_ESSENCE);
                                stmt.AddValue(0, guid);
                                SQLResult azeriteItemUnlockedEssencesResult = DB.Characters.Query(stmt);

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_EMPOWERED);
                                stmt.AddValue(0, guid);
                                SQLResult azeriteEmpoweredItemResult = DB.Characters.Query(stmt);

                                Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
                                ItemAdditionalLoadInfo.Init(additionalData, artifactResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

                                do
                                {
                                    uint mailId = resultItems.Read<uint>(44);
                                    Item mailItem = _LoadMailedItem(playerGuid, null, mailId, null, resultItems.GetFields(), additionalData.LookupByKey(resultItems.Read<ulong>(0)));

                                    if (mailItem != null)
                                        itemsByMail.Add(mailId, mailItem);
                                } while (resultItems.NextRow());
                            }

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

                                MailDraft draft = new(subject, body);

                                if (mailTemplateId != 0)
                                    draft = new MailDraft(mailTemplateId, false); // items are already included

                                var itemsList = itemsByMail.LookupByKey(mail_id);

                                if (itemsList != null)
                                {
                                    foreach (Item item in itemsList)
                                        draft.AddItem(item);

                                    itemsByMail.Remove(mail_id);
                                }

                                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                                stmt.AddValue(0, mail_id);
                                trans.Append(stmt);

                                uint pl_account = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(ObjectGuid.Create(HighGuid.Player, guid));

                                draft.AddMoney(money).SendReturnToSender(pl_account, guid, sender, trans);
                            } while (resultMail.NextRow());

                            // Free remaining items
                            foreach (var pair in itemsByMail)
                                pair.Value.Dispose();
                        }

                        // Unsummon and delete for pets in world is not required: player deleted from CLI or character list with not loaded pet.
                        // NOW we can finally clear other DB _data related to character
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PET_IDS);
                        stmt.AddValue(0, guid);
                        SQLResult resultPets = DB.Characters.Query(stmt);

                        if (!resultPets.IsEmpty())
                            do
                            {
                                uint petguidlow = resultPets.Read<uint>(0);
                                Pet.DeleteFromDB(petguidlow);
                            } while (resultPets.NextRow());

                        // Delete char from social list of online chars
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_SOCIAL);
                        stmt.AddValue(0, guid);
                        SQLResult resultFriends = DB.Characters.Query(stmt);

                        if (!resultFriends.IsEmpty())
                            do
                            {
                                Player playerFriend = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, resultFriends.Read<ulong>(0)));

                                if (playerFriend)
                                {
                                    playerFriend.GetSocial().RemoveFromSocialList(playerGuid, SocialFlag.All);
                                    Global.SocialMgr.SendFriendStatus(playerFriend, FriendsResult.Removed, playerGuid);
                                }
                            } while (resultFriends.NextRow());

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

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_INSTANCE_LOCK_BY_GUID);
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

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATIONS_BY_GUID);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME_BY_OWNER);
                        stmt.AddValue(0, guid);
                        stmt.AddValue(1, Global.WorldMgr.GetRealmId().Index);
                        loginTransaction.Append(stmt);

                        stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BATTLE_PETS_BY_OWNER);
                        stmt.AddValue(0, guid);
                        stmt.AddValue(1, Global.WorldMgr.GetRealmId().Index);
                        loginTransaction.Append(stmt);

                        Corpse.DeleteFromDB(playerGuid, trans);

                        Garrison.DeleteFromDB(guid, trans);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES_BY_CHAR);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS_BY_CHAR);
                        stmt.AddValue(0, guid);
                        trans.Append(stmt);

                        Global.CharacterCacheStorage.DeleteCharacterCacheEntry(playerGuid, name);

                        break;
                    }
                // The character gets unlinked from the account, the Name gets freed up and appears as deleted ingame
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

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_OLD_CHARS);
            stmt.AddValue(0, (uint)(GameTime.GetGameTime() - keepDays * Time.Day));
            SQLResult result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                int count = 0;

                do
                {
                    DeleteFromDB(ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0)), result.Read<uint>(1), true, true);
                    count++;
                } while (result.NextRow());

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

            loc.X = result.Read<float>(0);
            loc.Y = result.Read<float>(1);
            loc.Z = result.Read<float>(2);
            loc.Orientation = result.Read<float>(3);
            loc.SetMapId(result.Read<ushort>(4));
            inFlight = !string.IsNullOrEmpty(result.Read<string>(5));

            return true;
        }
    }

    public enum CharDeleteMethod
    {
        Remove = 0, // Completely remove from the database

        Unlink = 1 // The character gets unlinked from the account,
                   // the Name gets freed up and appears as deleted ingame
    }
}