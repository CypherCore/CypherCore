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
using Game.DataStorage;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public class CollectionMgr
    {
        public static void LoadMountDefinitions()
        {
            uint oldMSTime = Time.GetMSTime();

            SQLResult result = DB.World.Query("SELECT spellId, otherFactionSpellId FROM mount_definitions");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 mount definitions. DB table `mount_definitions` is empty.");
                return;
            }

            do
            {
                uint spellId = result.Read<uint>(0);
                uint otherFactionSpellId = result.Read<uint>(1);

                if (Global.DB2Mgr.GetMount(spellId) == null)
                {
                    Log.outError(LogFilter.Sql, "Mount spell {0} defined in `mount_definitions` does not exist in Mount.db2, skipped", spellId);
                    continue;
                }

                if (otherFactionSpellId != 0 && Global.DB2Mgr.GetMount(otherFactionSpellId) == null)
                {
                    Log.outError(LogFilter.Sql, "otherFactionSpellId {0} defined in `mount_definitions` for spell {1} does not exist in Mount.db2, skipped", otherFactionSpellId, spellId);
                    continue;
                }

                FactionSpecificMounts[spellId] = otherFactionSpellId;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} mount definitions in {1} ms", FactionSpecificMounts.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public CollectionMgr(WorldSession owner)
        {
            _owner = owner;
            _appearances = new System.Collections.BitSet(0);
        }

        public void LoadToys()
        {
            foreach (var value in _toys.Keys)
                _owner.GetPlayer().AddDynamicValue(ActivePlayerDynamicFields.Toys, value);
        }

        public bool AddToy(uint itemId, bool isFavourite = false)
        {
            if (UpdateAccountToys(itemId, isFavourite))
            {
                _owner.GetPlayer().AddDynamicValue(ActivePlayerDynamicFields.Toys, itemId);
                return true;
            }

            return false;
        }

        public void LoadAccountToys(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                uint itemId = result.Read<uint>(0);
                bool isFavourite = result.Read<bool>(1);

                _toys[itemId] = isFavourite;
            } while (result.NextRow());
        }

        public void SaveAccountToys(SQLTransaction trans)
        {
            PreparedStatement stmt = null;
            foreach (var pair in _toys)
            {
                stmt = DB.Login.GetPreparedStatement(LoginStatements.REP_ACCOUNT_TOYS);
                stmt.AddValue(0, _owner.GetBattlenetAccountId());
                stmt.AddValue(1, pair.Key);
                stmt.AddValue(2, pair.Value);
                trans.Append(stmt);
            }
        }

        bool UpdateAccountToys(uint itemId, bool isFavourite = false)
        {
            if (_toys.ContainsKey(itemId))
                return false;

            _toys.Add(itemId, isFavourite);
            return true;
        }

        public void ToySetFavorite(uint itemId, bool favorite)
        {
            if (!_toys.ContainsKey(itemId))
                return;

            _toys[itemId] = favorite;
        }

        public void OnItemAdded(Item item)
        {
            if (Global.DB2Mgr.GetHeirloomByItemId(item.GetEntry()) != null)
                AddHeirloom(item.GetEntry(), 0);

            AddItemAppearance(item);
        }

        public void LoadAccountHeirlooms(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                uint itemId = result.Read<uint>(0);
                HeirloomPlayerFlags flags = (HeirloomPlayerFlags)result.Read<uint>(1);

                HeirloomRecord heirloom = Global.DB2Mgr.GetHeirloomByItemId(itemId);
                if (heirloom == null)
                    continue;

                uint bonusId = 0;

                if (flags.HasAnyFlag(HeirloomPlayerFlags.BonusLevel110))
                    bonusId = heirloom.UpgradeItemBonusListID[2];
                else if (flags.HasAnyFlag(HeirloomPlayerFlags.BonusLevel100))
                    bonusId = heirloom.UpgradeItemBonusListID[1];
                else if (flags.HasAnyFlag(HeirloomPlayerFlags.BonusLevel90))
                    bonusId = heirloom.UpgradeItemBonusListID[0];

                _heirlooms[itemId] = new HeirloomData(flags, bonusId);
            } while (result.NextRow());
        }

        public void SaveAccountHeirlooms(SQLTransaction trans)
        {
            PreparedStatement stmt;
            foreach (var heirloom in _heirlooms)
            {
                stmt = DB.Login.GetPreparedStatement(LoginStatements.REP_ACCOUNT_HEIRLOOMS);
                stmt.AddValue(0, _owner.GetBattlenetAccountId());
                stmt.AddValue(1, heirloom.Key);
                stmt.AddValue(2, heirloom.Value.flags);
                trans.Append(stmt);
            }
        }

        bool UpdateAccountHeirlooms(uint itemId, HeirloomPlayerFlags flags)
        {
            if (_heirlooms.ContainsKey(itemId))
                return false;

            _heirlooms.Add(itemId, new HeirloomData(flags, 0));
            return true;
        }

        public uint GetHeirloomBonus(uint itemId)
        {
            var data = _heirlooms.LookupByKey(itemId);
            if (data != null)
                return data.bonusId;

            return 0;
        }

        public void LoadHeirlooms()
        {
            foreach (var item in _heirlooms)
            {
                _owner.GetPlayer().AddDynamicValue(ActivePlayerDynamicFields.Heirlooms, item.Key);
                _owner.GetPlayer().AddDynamicValue(ActivePlayerDynamicFields.HeirloomFlags, (uint)item.Value.flags);
            }
        }

        public void AddHeirloom(uint itemId, HeirloomPlayerFlags flags)
        {
            if (UpdateAccountHeirlooms(itemId, flags))
            {
                _owner.GetPlayer().AddDynamicValue(ActivePlayerDynamicFields.Heirlooms, itemId);
                _owner.GetPlayer().AddDynamicValue(ActivePlayerDynamicFields.HeirloomFlags, (uint)flags);
            }
        }

        public void UpgradeHeirloom(uint itemId, uint castItem)
        {
            Player player = _owner.GetPlayer();
            if (!player)
                return;

            HeirloomRecord heirloom = Global.DB2Mgr.GetHeirloomByItemId(itemId);
            if (heirloom == null)
                return;

            var data = _heirlooms.LookupByKey(itemId);
            if (data == null)
                return;

            HeirloomPlayerFlags flags = data.flags;
            uint bonusId = 0;

            if (heirloom.UpgradeItemID[0] == castItem)
            {
                flags |= HeirloomPlayerFlags.BonusLevel90;
                bonusId = heirloom.UpgradeItemBonusListID[0];
            }
            if (heirloom.UpgradeItemID[1] == castItem)
            {
                flags |= HeirloomPlayerFlags.BonusLevel100;
                bonusId = heirloom.UpgradeItemBonusListID[1];
            }
            if (heirloom.UpgradeItemID[2] == castItem)
            {
                flags |= HeirloomPlayerFlags.BonusLevel110;
                bonusId = heirloom.UpgradeItemBonusListID[2];
            }

            foreach (Item item in player.GetItemListByEntry(itemId, true))
                item.AddBonuses(bonusId);

            // Get heirloom offset to update only one part of dynamic field
            var fields = player.GetDynamicValues(ActivePlayerDynamicFields.Heirlooms);
            ushort offset = (ushort)Array.IndexOf(fields, itemId);

            player.SetDynamicValue(ActivePlayerDynamicFields.HeirloomFlags, offset, (uint)flags);
            data.flags = flags;
            data.bonusId = bonusId;
        }

        public void CheckHeirloomUpgrades(Item item)
        {
            Player player = _owner.GetPlayer();
            if (!player)
                return;

            // Check already owned heirloom for upgrade kits
            HeirloomRecord heirloom = Global.DB2Mgr.GetHeirloomByItemId(item.GetEntry());
            if (heirloom != null)
            {
                var data = _heirlooms.LookupByKey(item.GetEntry());
                if (data == null)
                    return;

                // Check for heirloom pairs (normal - heroic, heroic - mythic)
                uint heirloomItemId = heirloom.StaticUpgradedItemID;
                uint newItemId = 0;
                HeirloomRecord heirloomDiff;
                while ((heirloomDiff = Global.DB2Mgr.GetHeirloomByItemId(heirloomItemId)) != null)
                {
                    if (player.GetItemByEntry(heirloomDiff.ItemID))
                        newItemId = heirloomDiff.ItemID;

                    HeirloomRecord heirloomSub = Global.DB2Mgr.GetHeirloomByItemId(heirloomDiff.StaticUpgradedItemID);
                    if (heirloomSub != null)
                    {
                        heirloomItemId = heirloomSub.ItemID;
                        continue;
                    }

                    break;
                }

                if (newItemId != 0)
                {
                    var heirloomFields = player.GetDynamicValues(ActivePlayerDynamicFields.Heirlooms);
                    ushort offset = (ushort)Array.IndexOf(heirloomFields, item.GetEntry());

                    player.SetDynamicValue(ActivePlayerDynamicFields.Heirlooms, offset, newItemId);
                    player.SetDynamicValue(ActivePlayerDynamicFields.HeirloomFlags, offset, 0);

                    _heirlooms.Remove(item.GetEntry());
                    _heirlooms[newItemId] = null;

                    return;
                }

                var fields = item.GetDynamicValues(ItemDynamicFields.BonusListIds);
                foreach (uint bonusId in fields)
                    if (bonusId != data.bonusId)
                        item.ClearDynamicValue(ItemDynamicFields.BonusListIds);

                if (!fields.Contains(data.bonusId))
                    item.AddBonuses(data.bonusId);
            }
        }

        public bool CanApplyHeirloomXpBonus(uint itemId, uint level)
        {
            if (Global.DB2Mgr.GetHeirloomByItemId(itemId) == null)
                return false;

            var data = _heirlooms.LookupByKey(itemId);
            if (data == null)
                return false;

            if (data.flags.HasAnyFlag(HeirloomPlayerFlags.BonusLevel110))
                return level <= 110;
            if (data.flags.HasAnyFlag(HeirloomPlayerFlags.BonusLevel100))
                return level <= 100;
            if (data.flags.HasAnyFlag(HeirloomPlayerFlags.BonusLevel90))
                return level <= 90;

            return level <= 60;
        }

        public void LoadMounts()
        {
            foreach (var m in _mounts.ToList())
                AddMount(m.Key, m.Value, false, false);
        }

        public void LoadAccountMounts(SQLResult result)
        {
            if (result.IsEmpty())
                return;

            do
            {
                uint mountSpellId = result.Read<uint>(0);
                MountStatusFlags flags = (MountStatusFlags)result.Read<byte>(1);

                if (Global.DB2Mgr.GetMount(mountSpellId) == null)
                    continue;

                _mounts[mountSpellId] = flags;
            } while (result.NextRow());
        }

        public void SaveAccountMounts(SQLTransaction trans)
        {
            foreach (var mount in _mounts)
            {
                PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.REP_ACCOUNT_MOUNTS);
                stmt.AddValue(0, _owner.GetBattlenetAccountId());
                stmt.AddValue(1, mount.Key);
                stmt.AddValue(2, mount.Value);
                trans.Append(stmt);
            }
        }

        public bool AddMount(uint spellId, MountStatusFlags flags, bool factionMount = false, bool learned = false)
        {
            Player player = _owner.GetPlayer();
            if (!player)
                return false;

            MountRecord mount = Global.DB2Mgr.GetMount(spellId);
            if (mount == null)
                return false;

            var value = FactionSpecificMounts.LookupByKey(spellId);
            if (value != 0 && !factionMount)
                AddMount(value, flags, true, learned);

            _mounts[spellId] = flags;

            // Mount condition only applies to using it, should still learn it.
            if (mount.PlayerConditionID != 0)
            {
                PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(mount.PlayerConditionID);
                if (playerCondition != null && !ConditionManager.IsPlayerMeetingCondition(player, playerCondition))
                    return false;
            }

            if (!learned)
            {
                if (!factionMount)
                    SendSingleMountUpdate(spellId, flags);
                if (!player.HasSpell(spellId))
                    player.LearnSpell(spellId, true);
            }

            return true;
        }

        public void MountSetFavorite(uint spellId, bool favorite)
        {
            if (!_mounts.ContainsKey(spellId))
                return;

            if (favorite)
                _mounts[spellId] |= MountStatusFlags.IsFavorite;
            else
                _mounts[spellId] &= ~MountStatusFlags.IsFavorite;

            SendSingleMountUpdate(spellId, _mounts[spellId]);
        }

        void SendSingleMountUpdate(uint spellId, MountStatusFlags mountStatusFlags)
        {
            Player player = _owner.GetPlayer();
            if (!player)
                return;

            AccountMountUpdate mountUpdate = new AccountMountUpdate();
            mountUpdate.IsFullUpdate = false;
            mountUpdate.Mounts.Add(spellId, mountStatusFlags);
            player.SendPacket(mountUpdate);
        }

        public void LoadItemAppearances()
        {
            foreach (uint blockValue in _appearances.ToBlockRange())
            {
                _owner.GetPlayer().AddDynamicValue(ActivePlayerDynamicFields.Transmog, blockValue);
            }

            foreach (var value in _temporaryAppearances.Keys)
                _owner.GetPlayer().AddDynamicValue(ActivePlayerDynamicFields.ConditionalTransmog, value);
        }

        public void LoadAccountItemAppearances(SQLResult knownAppearances, SQLResult favoriteAppearances)
        {
            if (!knownAppearances.IsEmpty())
            {
                uint[] blocks = new uint[1];
                do
                {
                    ushort blobIndex = knownAppearances.Read<ushort>(0);
                    if (blobIndex >= blocks.Length)
                        Array.Resize(ref blocks, blobIndex + 1);

                    blocks[blobIndex] = knownAppearances.Read<uint>(1);

                } while (knownAppearances.NextRow());

                _appearances = new System.Collections.BitSet(blocks);
            }

            if (!favoriteAppearances.IsEmpty())
            {
                do
                {
                    _favoriteAppearances[favoriteAppearances.Read<uint>(0)] = FavoriteAppearanceState.Unchanged;
                } while (favoriteAppearances.NextRow());
            }

            // Static item appearances known by every player
            uint[] hiddenAppearanceItems =
            {
                134110, // Hidden Helm
                134111, // Hidden Cloak
                134112, // Hidden Shoulder
                142503, // Hidden Shirt
                142504, // Hidden Tabard
                143539  // Hidden Belt
            };

            foreach (uint hiddenItem in hiddenAppearanceItems)
            {
               ItemModifiedAppearanceRecord hiddenAppearance = Global.DB2Mgr.GetItemModifiedAppearance(hiddenItem, 0);
                //ASSERT(hiddenAppearance);
                if (_appearances.Length <= hiddenAppearance.Id)
                    _appearances.Length = (int)hiddenAppearance.Id + 1;

                _appearances.Set((int)hiddenAppearance.Id, true);
            }
        }

        public void SaveAccountItemAppearances(SQLTransaction trans)
        {
            PreparedStatement stmt;
            ushort blockIndex = 0;
            foreach (uint blockValue in _appearances.ToBlockRange())
            {
                if (blockValue != 0) // this table is only appended/bits are set (never cleared) so don't save empty blocks
                {
                    stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_ITEM_APPEARANCES);
                    stmt.AddValue(0, _owner.GetBattlenetAccountId());
                    stmt.AddValue(1, blockIndex);
                    stmt.AddValue(2, blockValue);
                    trans.Append(stmt);
                }

                ++blockIndex;
            }

            foreach (var key in _favoriteAppearances.Keys)
            {
                var appearanceState = _favoriteAppearances[key];
                switch (appearanceState)
                {
                    case FavoriteAppearanceState.New:
                        stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_ITEM_FAVORITE_APPEARANCE);
                        stmt.AddValue(0, _owner.GetBattlenetAccountId());
                        stmt.AddValue(1, key);
                        trans.Append(stmt);
                        appearanceState = FavoriteAppearanceState.Unchanged;
                        break;
                    case FavoriteAppearanceState.Removed:
                        stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BNET_ITEM_FAVORITE_APPEARANCE);
                        stmt.AddValue(0, _owner.GetBattlenetAccountId());
                        stmt.AddValue(1, key);
                        trans.Append(stmt);
                        _favoriteAppearances.Remove(key);
                        break;
                    case FavoriteAppearanceState.Unchanged:
                        break;
                }
            }
        }

        uint[] PlayerClassByArmorSubclass =
        {
            (int)Class.ClassMaskAllPlayable,                                                                                        //ITEM_SUBCLASS_ARMOR_MISCELLANEOUS
            (1 << ((int)Class.Priest - 1)) | (1 << ((int)Class.Mage - 1)) | (1 << ((int)Class.Warlock - 1)),                                       //ITEM_SUBCLASS_ARMOR_CLOTH
            (1 << ((int)Class.Rogue - 1)) | (1 << ((int)Class.Monk - 1)) | (1 << ((int)Class.Druid - 1)) | (1 << ((int)Class.DemonHunter - 1)),        //ITEM_SUBCLASS_ARMOR_LEATHER
            (1 << ((int)Class.Hunter - 1)) | (1 << ((int)Class.Shaman - 1)),                                                                  //ITEM_SUBCLASS_ARMOR_MAIL
            (1 << ((int)Class.Warrior - 1)) | (1 << ((int)Class.Paladin - 1)) | (1 << ((int)Class.Deathknight - 1)),                              //ITEM_SUBCLASS_ARMOR_PLATE
            (int)Class.ClassMaskAllPlayable,                                                                                                 //ITEM_SUBCLASS_ARMOR_BUCKLER
            (1 << ((int)Class.Warrior - 1)) | (1 << ((int)Class.Paladin - 1)) | (1 << ((int)Class.Shaman - 1)),                                    //ITEM_SUBCLASS_ARMOR_SHIELD
            1 << ((int)Class.Paladin - 1),                                                                                               //ITEM_SUBCLASS_ARMOR_LIBRAM
            1 << ((int)Class.Druid - 1),                                                                                                 //ITEM_SUBCLASS_ARMOR_IDOL
            1 << ((int)Class.Shaman - 1),                                                                                                //ITEM_SUBCLASS_ARMOR_TOTEM
            1 << ((int)Class.Deathknight - 1),                                                                                          //ITEM_SUBCLASS_ARMOR_SIGIL
            (1 << ((int)Class.Paladin - 1)) | (1 << ((int)Class.Deathknight - 1)) | (1 << ((int)Class.Shaman - 1)) | (1 << ((int)Class.Druid - 1)),    //ITEM_SUBCLASS_ARMOR_RELIC
        };

        public void AddItemAppearance(Item item)
        {
            if (!item.IsSoulBound())
                return;

            ItemModifiedAppearanceRecord itemModifiedAppearance = item.GetItemModifiedAppearance();
            if (!CanAddAppearance(itemModifiedAppearance))
                return;

            if (Convert.ToBoolean(item.GetUInt32Value(ItemFields.Flags) & (uint)(ItemFieldFlags.BopTradeable | ItemFieldFlags.Refundable)))
            {
                AddTemporaryAppearance(item.GetGUID(), itemModifiedAppearance);
                return;
            }

            AddItemAppearance(itemModifiedAppearance);
        }

        public void AddItemAppearance(uint itemId, uint appearanceModId = 0)
        {
            ItemModifiedAppearanceRecord itemModifiedAppearance = Global.DB2Mgr.GetItemModifiedAppearance(itemId, appearanceModId);
            if (!CanAddAppearance(itemModifiedAppearance))
                return;

            AddItemAppearance(itemModifiedAppearance);
        }

        bool CanAddAppearance(ItemModifiedAppearanceRecord itemModifiedAppearance)
        {
            if (itemModifiedAppearance == null)
                return false;

            if (itemModifiedAppearance.TransmogSourceTypeEnum == 6 || itemModifiedAppearance.TransmogSourceTypeEnum == 9)
                return false;

            if (!CliDB.ItemSearchNameStorage.ContainsKey(itemModifiedAppearance.ItemID))
                return false;

            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemModifiedAppearance.ItemID);
            if (itemTemplate == null)
                return false;

            if (_owner.GetPlayer().CanUseItem(itemTemplate) != InventoryResult.Ok)
                return false;

            if (itemTemplate.GetFlags2().HasAnyFlag(ItemFlags2.NoSourceForItemVisual) || itemTemplate.GetQuality() == ItemQuality.Artifact)
                return false;

            switch (itemTemplate.GetClass())
            {
                case ItemClass.Weapon:
                    {
                        if (!Convert.ToBoolean(_owner.GetPlayer().GetWeaponProficiency() & (1 << (int)itemTemplate.GetSubClass())))
                            return false;
                        if (itemTemplate.GetSubClass() == (int)ItemSubClassWeapon.Exotic ||
                            itemTemplate.GetSubClass() == (int)ItemSubClassWeapon.Exotic2 ||
                            itemTemplate.GetSubClass() == (int)ItemSubClassWeapon.Miscellaneous ||
                            itemTemplate.GetSubClass() == (int)ItemSubClassWeapon.Thrown ||
                            itemTemplate.GetSubClass() == (int)ItemSubClassWeapon.Spear ||
                            itemTemplate.GetSubClass() == (int)ItemSubClassWeapon.FishingPole)
                            return false;
                        break;
                    }
                case ItemClass.Armor:
                    {
                        switch (itemTemplate.GetInventoryType())
                        {
                            case InventoryType.Body:
                            case InventoryType.Shield:
                            case InventoryType.Cloak:
                            case InventoryType.Tabard:
                            case InventoryType.Holdable:
                                break;
                            case InventoryType.Head:
                            case InventoryType.Shoulders:
                            case InventoryType.Chest:
                            case InventoryType.Waist:
                            case InventoryType.Legs:
                            case InventoryType.Feet:
                            case InventoryType.Wrists:
                            case InventoryType.Hands:
                            case InventoryType.Robe:
                                if ((ItemSubClassArmor)itemTemplate.GetSubClass() == ItemSubClassArmor.Miscellaneous)
                                    return false;
                                break;
                            default:
                                return false;
                        }
                        if (itemTemplate.GetInventoryType() != InventoryType.Cloak)
                            if (!Convert.ToBoolean(PlayerClassByArmorSubclass[itemTemplate.GetSubClass()] & _owner.GetPlayer().getClassMask()))
                                return false;
                        break;
                    }
                default:
                    return false;
            }

            if (itemTemplate.GetQuality() < ItemQuality.Uncommon)
                if (!itemTemplate.GetFlags2().HasAnyFlag(ItemFlags2.IgnoreQualityForItemVisualSource) || !itemTemplate.GetFlags3().HasAnyFlag(ItemFlags3.ActsAsTransmogHiddenVisualOption))
                    return false;

            if (itemModifiedAppearance.Id < _appearances.Count && _appearances.Get((int)itemModifiedAppearance.Id))
                return false;

            return true;
        }
        //todo  check this
        void AddItemAppearance(ItemModifiedAppearanceRecord itemModifiedAppearance)
        {
            if (_appearances.Count <= itemModifiedAppearance.Id)
            {
                uint numBlocks = (uint)(_appearances.Count << 2);
                _appearances.Length = (int)itemModifiedAppearance.Id + 1;
                numBlocks = (uint)(_appearances.Count << 2) - numBlocks;
                while (numBlocks-- != 0)
                    _owner.GetPlayer().AddDynamicValue(ActivePlayerDynamicFields.Transmog, 0);
            }

            _appearances.Set((int)itemModifiedAppearance.Id, true);
            uint blockIndex = itemModifiedAppearance.Id / 32;
            uint bitIndex = itemModifiedAppearance.Id % 32;
            uint currentMask = _owner.GetPlayer().GetDynamicValue(ActivePlayerDynamicFields.Transmog, (ushort)blockIndex);
            _owner.GetPlayer().SetDynamicValue(ActivePlayerDynamicFields.Transmog, (ushort)blockIndex, (uint)((int)currentMask | (1 << (int)bitIndex)));
            var temporaryAppearance = _temporaryAppearances.LookupByKey(itemModifiedAppearance.Id);
            if (!temporaryAppearance.Empty())
            {
                _owner.GetPlayer().RemoveDynamicValue(ActivePlayerDynamicFields.ConditionalTransmog, itemModifiedAppearance.Id);
                _temporaryAppearances.Remove(itemModifiedAppearance.Id);
            }

            ItemRecord item = CliDB.ItemStorage.LookupByKey(itemModifiedAppearance.ItemID);
            if (item != null)
            {
                int transmogSlot = Item.ItemTransmogrificationSlots[(int)item.inventoryType];
                if (transmogSlot >= 0)
                    _owner.GetPlayer().UpdateCriteria(CriteriaTypes.AppearanceUnlockedBySlot, (ulong)transmogSlot, 1);
            }

            var sets = Global.DB2Mgr.GetTransmogSetsForItemModifiedAppearance(itemModifiedAppearance.Id);
            foreach (TransmogSetRecord set in sets)
                if (IsSetCompleted(set.Id))
                    _owner.GetPlayer().UpdateCriteria(CriteriaTypes.TransmogSetUnlocked, set.TransmogSetGroupID);
        }

        void AddTemporaryAppearance(ObjectGuid itemGuid, ItemModifiedAppearanceRecord itemModifiedAppearance)
        {
            var itemsWithAppearance = _temporaryAppearances[itemModifiedAppearance.Id];
            if (itemsWithAppearance.Empty())
                _owner.GetPlayer().AddDynamicValue(ActivePlayerDynamicFields.ConditionalTransmog, itemModifiedAppearance.Id);

            itemsWithAppearance.Add(itemGuid);
        }

        public void RemoveTemporaryAppearance(Item item)
        {
            ItemModifiedAppearanceRecord itemModifiedAppearance = item.GetItemModifiedAppearance();
            if (itemModifiedAppearance == null)
                return;

            var guid = _temporaryAppearances.LookupByKey(itemModifiedAppearance.Id);
            if (guid.Empty())
                return;

            guid.Remove(item.GetGUID());
            if (guid.Empty())
            {
                _owner.GetPlayer().RemoveDynamicValue(ActivePlayerDynamicFields.ConditionalTransmog, itemModifiedAppearance.Id);
                _temporaryAppearances.Remove(itemModifiedAppearance.Id);
            }
        }

        public Tuple<bool, bool> HasItemAppearance(uint itemModifiedAppearanceId)
        {
            if (itemModifiedAppearanceId < _appearances.Count && _appearances.Get((int)itemModifiedAppearanceId))
                return Tuple.Create(true, false);

            if (_temporaryAppearances.ContainsKey(itemModifiedAppearanceId))
                return Tuple.Create(true, true);

            return Tuple.Create(false, false);
        }

        public List<ObjectGuid> GetItemsProvidingTemporaryAppearance(uint itemModifiedAppearanceId)
        {
            return _temporaryAppearances.LookupByKey(itemModifiedAppearanceId);
        }

        public void SetAppearanceIsFavorite(uint itemModifiedAppearanceId, bool apply)
        {
            var apperanceState = _favoriteAppearances.LookupByKey(itemModifiedAppearanceId);
            if (apply)
            {
                if (!_favoriteAppearances.ContainsKey(itemModifiedAppearanceId))
                    _favoriteAppearances[itemModifiedAppearanceId] = FavoriteAppearanceState.New;
                else if (apperanceState == FavoriteAppearanceState.Removed)
                    apperanceState = FavoriteAppearanceState.Unchanged;
                else
                    return;
            }
            else if (_favoriteAppearances.ContainsKey(itemModifiedAppearanceId))
            {
                if (apperanceState == FavoriteAppearanceState.New)
                    _favoriteAppearances.Remove(itemModifiedAppearanceId);
                else
                    apperanceState = FavoriteAppearanceState.Removed;
            }
            else
                return;

            _favoriteAppearances[itemModifiedAppearanceId] = apperanceState;

            TransmogCollectionUpdate transmogCollectionUpdate = new TransmogCollectionUpdate();
            transmogCollectionUpdate.IsFullUpdate = false;
            transmogCollectionUpdate.IsSetFavorite = apply;
            transmogCollectionUpdate.FavoriteAppearances.Add(itemModifiedAppearanceId);

            _owner.SendPacket(transmogCollectionUpdate);
        }

        public void SendFavoriteAppearances()
        {
            TransmogCollectionUpdate transmogCollectionUpdate = new TransmogCollectionUpdate();
            transmogCollectionUpdate.IsFullUpdate = true;
            foreach (var pair in _favoriteAppearances)
                if (pair.Value != FavoriteAppearanceState.Removed)
                    transmogCollectionUpdate.FavoriteAppearances.Add(pair.Key);

            _owner.SendPacket(transmogCollectionUpdate);
        }

        public void AddTransmogSet(uint transmogSetId)
        {
            var items = Global.DB2Mgr.GetTransmogSetItems(transmogSetId);
            if (items.Empty())
                return;

            foreach (TransmogSetItemRecord item in items)
            {
                ItemModifiedAppearanceRecord itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(item.ItemModifiedAppearanceID);
                if (itemModifiedAppearance == null)
                    continue;

                AddItemAppearance(itemModifiedAppearance);
            }
        }

        bool IsSetCompleted(uint transmogSetId)
        {
            var transmogSetItems = Global.DB2Mgr.GetTransmogSetItems(transmogSetId);
            if (transmogSetItems.Empty())
                return false;

            int[] knownPieces = new int[EquipmentSlot.End];
            for (var i = 0; i < EquipmentSlot.End; ++i)
                knownPieces[i] = -1;

            foreach (TransmogSetItemRecord transmogSetItem in transmogSetItems)
            {
                ItemModifiedAppearanceRecord itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(transmogSetItem.ItemModifiedAppearanceID);
                if (itemModifiedAppearance == null)
                    continue;

                ItemRecord item = CliDB.ItemStorage.LookupByKey(itemModifiedAppearance.ItemID);
                if (item == null)
                    continue;

                int transmogSlot = Item.ItemTransmogrificationSlots[(int)item.inventoryType];
                if (transmogSlot < 0 || knownPieces[transmogSlot] == 1)
                    continue;

                (var hasAppearance, bool isTemporary) = HasItemAppearance(transmogSetItem.ItemModifiedAppearanceID);

                knownPieces[transmogSlot] = (hasAppearance && !isTemporary) ? 1 : 0;
            }

            return !knownPieces.Contains(0);
        }

        public bool HasToy(uint itemId) { return _toys.ContainsKey(itemId); }
        public Dictionary<uint, bool> GetAccountToys() { return _toys; }
        public Dictionary<uint, HeirloomData> GetAccountHeirlooms() { return _heirlooms; }
        public Dictionary<uint, MountStatusFlags> GetAccountMounts() { return _mounts; }

        WorldSession _owner;

        Dictionary<uint, bool> _toys = new Dictionary<uint, bool>();
        Dictionary<uint, HeirloomData> _heirlooms = new Dictionary<uint, HeirloomData>();
        Dictionary<uint, MountStatusFlags> _mounts = new Dictionary<uint, MountStatusFlags>();
        System.Collections.BitSet _appearances;
        MultiMap<uint, ObjectGuid> _temporaryAppearances = new MultiMap<uint, ObjectGuid>();
        Dictionary<uint, FavoriteAppearanceState> _favoriteAppearances = new Dictionary<uint, FavoriteAppearanceState>();

        static Dictionary<uint, uint> FactionSpecificMounts = new Dictionary<uint, uint>();
    }

    enum FavoriteAppearanceState
    {
        New,
        Removed,
        Unchanged
    }

    public class HeirloomData
    {
        public HeirloomData(HeirloomPlayerFlags _flags = 0, uint _bonusId = 0)
        {
            flags = _flags;
            bonusId = _bonusId;
        }

        public HeirloomPlayerFlags flags;
        public uint bonusId;
    }
}
