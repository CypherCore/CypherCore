/*
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
using Game.BattlePets;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using System;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.BattlePetRequestJournal)]
        void HandleBattlePetRequestJournal(BattlePetRequestJournal battlePetRequestJournal)
        {
            GetBattlePetMgr().SendJournal();
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetRequestJournalLock)]
        void HandleBattlePetRequestJournalLock(BattlePetRequestJournalLock battlePetRequestJournalLock)
        {
            GetBattlePetMgr().SendJournalLockStatus();

            if (GetBattlePetMgr().HasJournalLock())
                GetBattlePetMgr().SendJournal();
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetSetBattleSlot)]
        void HandleBattlePetSetBattleSlot(BattlePetSetBattleSlot battlePetSetBattleSlot)
        {
            BattlePet pet = GetBattlePetMgr().GetPet(battlePetSetBattleSlot.PetGuid);
            if (pet != null)
            {
                BattlePetSlot slot = GetBattlePetMgr().GetSlot((BattlePetSlots)battlePetSetBattleSlot.Slot);
                if (slot != null)
                    slot.Pet = pet.PacketInfo;
            }
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetModifyName)]
        void HandleBattlePetModifyName(BattlePetModifyName battlePetModifyName)
        {
            GetBattlePetMgr().ModifyName(battlePetModifyName.PetGuid, battlePetModifyName.Name, battlePetModifyName.DeclinedNames);
        }

        [WorldPacketHandler(ClientOpcodes.QueryBattlePetName)]
        void HandleQueryBattlePetName(QueryBattlePetName queryBattlePetName)
        {
            QueryBattlePetNameResponse response = new();
            response.BattlePetID = queryBattlePetName.BattlePetID;

            Creature summonedBattlePet = ObjectAccessor.GetCreatureOrPetOrVehicle(_player, queryBattlePetName.UnitGUID);
            if (!summonedBattlePet || !summonedBattlePet.IsSummon())
            {
                SendPacket(response);
                return;
            }

            response.CreatureID = summonedBattlePet.GetEntry();
            response.Timestamp = summonedBattlePet.GetBattlePetCompanionNameTimestamp();

            Unit petOwner = summonedBattlePet.ToTempSummon().GetSummonerUnit();
            if (!petOwner.IsPlayer())
            {
                SendPacket(response);
                return;
            }

            BattlePet battlePet = petOwner.ToPlayer().GetSession().GetBattlePetMgr().GetPet(queryBattlePetName.BattlePetID);
            if (battlePet == null)
            {
                SendPacket(response);
                return;
            }

            response.Name = battlePet.PacketInfo.Name;
            if (battlePet.DeclinedName != null)
            {
                response.HasDeclined = true;
                response.DeclinedNames = battlePet.DeclinedName;
            }

            response.Allow = !response.Name.IsEmpty();

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetDeletePet)]
        void HandleBattlePetDeletePet(BattlePetDeletePet battlePetDeletePet)
        {
            GetBattlePetMgr().RemovePet(battlePetDeletePet.PetGuid);
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetSetFlags)]
        void HandleBattlePetSetFlags(BattlePetSetFlags battlePetSetFlags)
        {
            if (!GetBattlePetMgr().HasJournalLock())
                return;

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

        [WorldPacketHandler(ClientOpcodes.BattlePetClearFanfare)]
        void HandleBattlePetClearFanfare(BattlePetClearFanfare battlePetClearFanfare)
        {
            GetBattlePetMgr().ClearFanfare(battlePetClearFanfare.PetGuid);
        }

        [WorldPacketHandler(ClientOpcodes.CageBattlePet)]
        void HandleCageBattlePet(CageBattlePet cageBattlePet)
        {
            GetBattlePetMgr().CageBattlePet(cageBattlePet.PetGuid);
        }        
    }
}
