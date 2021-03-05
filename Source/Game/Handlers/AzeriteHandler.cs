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

using Framework.Constants;
using Game.Entities;
using Game.Networking;
using Game.DataStorage;
using System.Collections.Generic;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AzeriteEssenceUnlockMilestone)]
        private void HandleAzeriteEssenceUnlockMilestone(AzeriteEssenceUnlockMilestone azeriteEssenceUnlockMilestone)
        {
            if (!AzeriteItem.FindHeartForge(_player))
                return;

            Item item = _player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
            if (!item)
                return;

            var azeriteItem = item.ToAzeriteItem();
            if (!azeriteItem || !azeriteItem.CanUseEssences())
                return;

            var milestonePower = CliDB.AzeriteItemMilestonePowerStorage.LookupByKey(azeriteEssenceUnlockMilestone.AzeriteItemMilestonePowerID);
            if (milestonePower == null || milestonePower.RequiredLevel > azeriteItem.GetLevel())
                return;

            // check that all previous milestones are unlocked
            foreach (var previousMilestone in Global.DB2Mgr.GetAzeriteItemMilestonePowers())
            {
                if (previousMilestone == milestonePower)
                    break;

                if (!azeriteItem.HasUnlockedEssenceMilestone(previousMilestone.Id))
                    return;
            }

            azeriteItem.AddUnlockedEssenceMilestone(milestonePower.Id);
            _player.ApplyAzeriteItemMilestonePower(azeriteItem, milestonePower, true);
            azeriteItem.SetState(ItemUpdateState.Changed, _player);
        }

        [WorldPacketHandler(ClientOpcodes.AzeriteEssenceActivateEssence)]
        private void HandleAzeriteEssenceActivateEssence(AzeriteEssenceActivateEssence azeriteEssenceActivateEssence)
        {
            var activateEssenceResult = new ActivateEssenceFailed();
            activateEssenceResult.AzeriteEssenceID = azeriteEssenceActivateEssence.AzeriteEssenceID;

            Item item = _player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.InEquipment);
            if (item == null)
            {
                activateEssenceResult.Reason = AzeriteEssenceActivateResult.NotEquipped;
                activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
                SendPacket(activateEssenceResult);
                return;
            }

            var azeriteItem = item.ToAzeriteItem();
            if (azeriteItem == null || !azeriteItem.CanUseEssences())
            {
                activateEssenceResult.Reason = AzeriteEssenceActivateResult.ConditionFailed;
                SendPacket(activateEssenceResult);
                return;
            }

            if (azeriteEssenceActivateEssence.Slot >= SharedConst.MaxAzeriteEssenceSlot || !azeriteItem.HasUnlockedEssenceSlot(azeriteEssenceActivateEssence.Slot))
            {
                activateEssenceResult.Reason = AzeriteEssenceActivateResult.SlotLocked;
                SendPacket(activateEssenceResult);
                return;
            }

            var selectedEssences = azeriteItem.GetSelectedAzeriteEssences();
            // essence is already in that slot, nothing to do
            if (selectedEssences != null && selectedEssences.AzeriteEssenceID[azeriteEssenceActivateEssence.Slot] == azeriteEssenceActivateEssence.AzeriteEssenceID)
                return;

            var rank = azeriteItem.GetEssenceRank(azeriteEssenceActivateEssence.AzeriteEssenceID);
            if (rank == 0)
            {
                activateEssenceResult.Reason = AzeriteEssenceActivateResult.EssenceNotUnlocked;
                activateEssenceResult.Arg = azeriteEssenceActivateEssence.AzeriteEssenceID;
                SendPacket(activateEssenceResult);
                return;
            }

            if (_player.IsInCombat())
            {
                activateEssenceResult.Reason = AzeriteEssenceActivateResult.AffectingCombat;
                activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
                SendPacket(activateEssenceResult);
                return;
            }

            if (_player.IsDead())
            {
                activateEssenceResult.Reason = AzeriteEssenceActivateResult.CantDoThatRightNow;
                activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
                SendPacket(activateEssenceResult);
                return;
            }

            if (!_player.HasPlayerFlag(PlayerFlags.Resting) && !_player.HasUnitFlag2(UnitFlags2.AllowChangingTalents))
            {
                activateEssenceResult.Reason = AzeriteEssenceActivateResult.NotInRestArea;
                activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
                SendPacket(activateEssenceResult);
                return;
            }

            if (selectedEssences != null)
            {
                // need to remove selected essence from another slot if selected
                var removeEssenceFromSlot = -1;
                for (var slot = 0; slot < SharedConst.MaxAzeriteEssenceSlot; ++slot)
                    if (azeriteEssenceActivateEssence.Slot != slot && selectedEssences.AzeriteEssenceID[slot] == azeriteEssenceActivateEssence.AzeriteEssenceID)
                        removeEssenceFromSlot = slot;

                // check cooldown of major essence slot
                if (selectedEssences.AzeriteEssenceID[0] != 0 && (azeriteEssenceActivateEssence.Slot == 0 || removeEssenceFromSlot == 0))
                {
                    for (uint essenceRank = 1; essenceRank <= rank; ++essenceRank)
                    {
                        var azeriteEssencePower = Global.DB2Mgr.GetAzeriteEssencePower(selectedEssences.AzeriteEssenceID[0], essenceRank);
                        if (_player.GetSpellHistory().HasCooldown(azeriteEssencePower.MajorPowerDescription))
                        {
                            activateEssenceResult.Reason = AzeriteEssenceActivateResult.CantRemoveEssence;
                            activateEssenceResult.Arg = azeriteEssencePower.MajorPowerDescription;
                            activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
                            SendPacket(activateEssenceResult);
                            return;
                        }
                    }
                }


                if (removeEssenceFromSlot != -1)
                {
                    _player.ApplyAzeriteEssence(azeriteItem, selectedEssences.AzeriteEssenceID[removeEssenceFromSlot], SharedConst.MaxAzeriteEssenceRank,
                        (AzeriteItemMilestoneType)Global.DB2Mgr.GetAzeriteItemMilestonePower(removeEssenceFromSlot).Type == AzeriteItemMilestoneType.MajorEssence, false);
                    azeriteItem.SetSelectedAzeriteEssence(removeEssenceFromSlot, 0);
                }

                if (selectedEssences.AzeriteEssenceID[azeriteEssenceActivateEssence.Slot] != 0)
                {
                    _player.ApplyAzeriteEssence(azeriteItem, selectedEssences.AzeriteEssenceID[azeriteEssenceActivateEssence.Slot], SharedConst.MaxAzeriteEssenceRank,
                        (AzeriteItemMilestoneType)Global.DB2Mgr.GetAzeriteItemMilestonePower(azeriteEssenceActivateEssence.Slot).Type == AzeriteItemMilestoneType.MajorEssence, false);
                }
            }
            else
                azeriteItem.CreateSelectedAzeriteEssences(_player.GetPrimarySpecialization());

            azeriteItem.SetSelectedAzeriteEssence(azeriteEssenceActivateEssence.Slot, azeriteEssenceActivateEssence.AzeriteEssenceID);

            _player.ApplyAzeriteEssence(azeriteItem, azeriteEssenceActivateEssence.AzeriteEssenceID, rank,
                (AzeriteItemMilestoneType)Global.DB2Mgr.GetAzeriteItemMilestonePower(azeriteEssenceActivateEssence.Slot).Type == AzeriteItemMilestoneType.MajorEssence, true);

            azeriteItem.SetState(ItemUpdateState.Changed, _player);
        }

        [WorldPacketHandler(ClientOpcodes.AzeriteEmpoweredItemViewed)]
        private void HandleAzeriteEmpoweredItemViewed(AzeriteEmpoweredItemViewed azeriteEmpoweredItemViewed)
        {
            Item item = _player.GetItemByGuid(azeriteEmpoweredItemViewed.ItemGUID);
            if (item == null || !item.IsAzeriteEmpoweredItem())
                return;

            item.AddItemFlag(ItemFieldFlags.AzeriteEmpoweredItemViewed);
            item.SetState(ItemUpdateState.Changed, _player);
        }

        [WorldPacketHandler(ClientOpcodes.AzeriteEmpoweredItemSelectPower)]
        private void HandleAzeriteEmpoweredItemSelectPower(AzeriteEmpoweredItemSelectPower azeriteEmpoweredItemSelectPower)
        {
            Item item = _player.GetItemByPos(azeriteEmpoweredItemSelectPower.ContainerSlot, azeriteEmpoweredItemSelectPower.Slot);
            if (item == null)
                return;

            var azeritePower = CliDB.AzeritePowerStorage.LookupByKey(azeriteEmpoweredItemSelectPower.AzeritePowerID);
            if (azeritePower == null)
                return;

            var azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();
            if (azeriteEmpoweredItem == null)
                return;

            // Validate tier
            var actualTier = azeriteEmpoweredItem.GetTierForAzeritePower(_player.GetClass(), azeriteEmpoweredItemSelectPower.AzeritePowerID);
            if (azeriteEmpoweredItemSelectPower.Tier > SharedConst.MaxAzeriteEmpoweredTier || azeriteEmpoweredItemSelectPower.Tier != actualTier)
                return;

            uint azeriteLevel = 0;
            Item heartOfAzeroth = _player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
            if (heartOfAzeroth == null)
                return;

            var azeriteItem = heartOfAzeroth.ToAzeriteItem();
            if (azeriteItem != null)
                azeriteLevel = azeriteItem.GetEffectiveLevel();

            // Check required heart of azeroth level
            if (azeriteLevel < azeriteEmpoweredItem.GetRequiredAzeriteLevelForTier((uint)actualTier))
                return;

            // tiers are ordered backwards, you first select the highest one
            for (var i = actualTier + 1; i < azeriteEmpoweredItem.GetMaxAzeritePowerTier(); ++i)
                if (azeriteEmpoweredItem.GetSelectedAzeritePower(i) == 0)
                    return;

            var activateAzeritePower = azeriteEmpoweredItem.IsEquipped() && heartOfAzeroth.IsEquipped();
            if (azeritePower.ItemBonusListID != 0 && activateAzeritePower)
                _player._ApplyItemMods(azeriteEmpoweredItem, azeriteEmpoweredItem.GetSlot(), false);

            azeriteEmpoweredItem.SetSelectedAzeritePower(actualTier, azeriteEmpoweredItemSelectPower.AzeritePowerID);

            if (activateAzeritePower)
            {
                // apply all item mods when azerite power grants a bonus, item level changes and that affects stats and auras that scale with item level
                if (azeritePower.ItemBonusListID != 0)
                    _player._ApplyItemMods(azeriteEmpoweredItem, azeriteEmpoweredItem.GetSlot(), true);
                else
                    _player.ApplyAzeritePower(azeriteEmpoweredItem, azeritePower, true);
            }

            azeriteEmpoweredItem.SetState(ItemUpdateState.Changed, _player);
        }
    }
}
