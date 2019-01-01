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
using Game.BattlePets;
using Game.Network;
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.BattlePetRequestJournal)]
        void HandleBattlePetRequestJournal(BattlePetRequestJournal battlePetRequestJournal)
        {
            GetBattlePetMgr().SendJournal();
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetSetBattleSlot)]
        void HandleBattlePetSetBattleSlot(BattlePetSetBattleSlot battlePetSetBattleSlot)
        {
            BattlePetMgr.BattlePet pet = GetBattlePetMgr().GetPet(battlePetSetBattleSlot.PetGuid);
            if (pet != null)
                GetBattlePetMgr().GetSlot(battlePetSetBattleSlot.Slot).Pet = pet.PacketInfo;
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetModifyName)]
        void HandleBattlePetModifyName(BattlePetModifyName battlePetModifyName)
        {
            BattlePetMgr.BattlePet pet = GetBattlePetMgr().GetPet(battlePetModifyName.PetGuid);
            if (pet != null)
            {
                pet.PacketInfo.Name = battlePetModifyName.Name;

                if (pet.SaveInfo != BattlePetSaveInfo.New)
                    pet.SaveInfo = BattlePetSaveInfo.Changed;
            }
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetDeletePet)]
        void HandleBattlePetDeletePet(BattlePetDeletePet battlePetDeletePet)
        {
            GetBattlePetMgr().RemovePet(battlePetDeletePet.PetGuid);
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetSetFlags)]
        void HandleBattlePetSetFlags(BattlePetSetFlags battlePetSetFlags)
        {
            var pet = GetBattlePetMgr().GetPet(battlePetSetFlags.PetGuid);
            if (pet != null)
            {
                if (battlePetSetFlags.ControlType == FlagsControlType.Apply)
                    pet.PacketInfo.Flags |= (ushort)battlePetSetFlags.Flags;
                else
                    pet.PacketInfo.Flags &= (ushort)~battlePetSetFlags.Flags;

                if (pet.SaveInfo != BattlePetSaveInfo.New)
                    pet.SaveInfo = BattlePetSaveInfo.Changed;
            }
        }

        [WorldPacketHandler(ClientOpcodes.CageBattlePet)]
        void HandleCageBattlePet(CageBattlePet cageBattlePet)
        {
            GetBattlePetMgr().CageBattlePet(cageBattlePet.PetGuid);
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetSummon, Processing = PacketProcessing.Inplace)]
        void HandleBattlePetSummon(BattlePetSummon battlePetSummon)
        {
            if (_player.GetGuidValue(ActivePlayerFields.SummonedBattlePetId) != battlePetSummon.PetGuid)
                GetBattlePetMgr().SummonPet(battlePetSummon.PetGuid);
            else
                GetBattlePetMgr().DismissPet();
        }
    }
}
