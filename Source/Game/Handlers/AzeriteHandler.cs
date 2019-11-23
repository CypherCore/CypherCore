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
using Game.Entities;
using Game.Network;
using Game.DataStorage;
using System.Collections.Generic;
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AzeriteEssenceUnlockMilestone)]
        void HandleAzeriteEssenceUnlockMilestone(AzeriteEssenceUnlockMilestone azeriteEssenceUnlockMilestone)
        {
            if (!AzeriteItem.FindHeartForge(_player))
                return;

            Item item = _player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth);
            if (!item)
                return;

            AzeriteItem azeriteItem = item.ToAzeriteItem();
            if (!azeriteItem || !azeriteItem.CanUseEssences())
                return;

            AzeriteItemMilestonePowerRecord milestonePower = CliDB.AzeriteItemMilestonePowerStorage.LookupByKey(azeriteEssenceUnlockMilestone.AzeriteItemMilestonePowerID);
            if (milestonePower == null || milestonePower.RequiredLevel > azeriteItem.GetLevel())
                return;

            // check that all previous milestones are unlocked
            foreach (AzeriteItemMilestonePowerRecord previousMilestone in Global.DB2Mgr.GetAzeriteItemMilestonePowers())
            {
                if (previousMilestone == milestonePower)
                    break;

                if (!azeriteItem.HasUnlockedEssenceMilestone(previousMilestone.ID))
                    return;
            }

            azeriteItem.AddUnlockedEssenceMilestone(milestonePower.ID);
            _player.ApplyAzeriteItemMilestonePower(azeriteItem, milestonePower, true);
            azeriteItem.SetState(ItemUpdateState.Changed, _player);
        }

        [WorldPacketHandler(ClientOpcodes.AzeriteEssenceActivateEssence)]
        void HandleAzeriteEssenceActivateEssence(AzeriteEssenceActivateEssence azeriteEssenceActivateEssence)
        {
            AzeriteEssenceSelectionResult activateEssenceResult = new AzeriteEssenceSelectionResult();
            activateEssenceResult.AzeriteEssenceID = azeriteEssenceActivateEssence.AzeriteEssenceID;

            Item item = _player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth);
            if (!item || !item.IsEquipped())
            {
                activateEssenceResult.Reason = AzeriteEssenceActivateResult.NotEquipped;
                activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
                SendPacket(activateEssenceResult);
                return;
            }

            AzeriteItem azeriteItem = item.ToAzeriteItem();
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

            SelectedAzeriteEssences selectedEssences = azeriteItem.GetSelectedAzeriteEssences();
            // essence is already in that slot, nothing to do
            if (selectedEssences.AzeriteEssenceID[azeriteEssenceActivateEssence.Slot] == azeriteEssenceActivateEssence.AzeriteEssenceID)
                return;

            uint rank = azeriteItem.GetEssenceRank(azeriteEssenceActivateEssence.AzeriteEssenceID);
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

            // need to remove selected essence from another slot if selected
            int removeEssenceFromSlot = -1;
            for (int slot = 0; slot < SharedConst.MaxAzeriteEssenceSlot; ++slot)
                if (azeriteEssenceActivateEssence.Slot != slot && selectedEssences.AzeriteEssenceID[slot] == azeriteEssenceActivateEssence.AzeriteEssenceID)
                    removeEssenceFromSlot = slot;

            // check cooldown of major essence slot
            if (selectedEssences.AzeriteEssenceID[0] != 0 && (azeriteEssenceActivateEssence.Slot == 0 || removeEssenceFromSlot == 0))
            {
                for (uint essenceRank = 1; essenceRank <= rank; ++essenceRank)
                {
                    AzeriteEssencePowerRecord azeriteEssencePower = Global.DB2Mgr.GetAzeriteEssencePower(selectedEssences.AzeriteEssenceID[0], essenceRank);
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

            _player.ApplyAzeriteEssence(azeriteItem, azeriteEssenceActivateEssence.AzeriteEssenceID, rank,
                (AzeriteItemMilestoneType)Global.DB2Mgr.GetAzeriteItemMilestonePower(azeriteEssenceActivateEssence.Slot).Type == AzeriteItemMilestoneType.MajorEssence, true);

            azeriteItem.SetSelectedAzeriteEssence(azeriteEssenceActivateEssence.Slot, azeriteEssenceActivateEssence.AzeriteEssenceID);
            azeriteItem.SetState(ItemUpdateState.Changed, _player);
        }
    }
}
