// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.BattlePets;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.BattlePetRequestJournal)]
        private void HandleBattlePetRequestJournal(BattlePetRequestJournal battlePetRequestJournal)
        {
            GetBattlePetMgr().SendJournal();
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetRequestJournalLock)]
        private void HandleBattlePetRequestJournalLock(BattlePetRequestJournalLock battlePetRequestJournalLock)
        {
            GetBattlePetMgr().SendJournalLockStatus();

            if (GetBattlePetMgr().HasJournalLock())
                GetBattlePetMgr().SendJournal();
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetSetBattleSlot)]
        private void HandleBattlePetSetBattleSlot(BattlePetSetBattleSlot battlePetSetBattleSlot)
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
        private void HandleBattlePetModifyName(BattlePetModifyName battlePetModifyName)
        {
            GetBattlePetMgr().ModifyName(battlePetModifyName.PetGuid, battlePetModifyName.Name, battlePetModifyName.DeclinedNames);
        }

        [WorldPacketHandler(ClientOpcodes.QueryBattlePetName)]
        private void HandleQueryBattlePetName(QueryBattlePetName queryBattlePetName)
        {
            QueryBattlePetNameResponse response = new();
            response.BattlePetID = queryBattlePetName.BattlePetID;

            Creature summonedBattlePet = ObjectAccessor.GetCreatureOrPetOrVehicle(_player, queryBattlePetName.UnitGUID);

            if (!summonedBattlePet ||
                !summonedBattlePet.IsSummon())
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

            BattlePet battlePet = petOwner.ToPlayer().Session.GetBattlePetMgr().GetPet(queryBattlePetName.BattlePetID);

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
        private void HandleBattlePetDeletePet(BattlePetDeletePet battlePetDeletePet)
        {
            GetBattlePetMgr().RemovePet(battlePetDeletePet.PetGuid);
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetSetFlags)]
        private void HandleBattlePetSetFlags(BattlePetSetFlags battlePetSetFlags)
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
        private void HandleBattlePetClearFanfare(BattlePetClearFanfare battlePetClearFanfare)
        {
            GetBattlePetMgr().ClearFanfare(battlePetClearFanfare.PetGuid);
        }

        [WorldPacketHandler(ClientOpcodes.CageBattlePet)]
        private void HandleCageBattlePet(CageBattlePet cageBattlePet)
        {
            GetBattlePetMgr().CageBattlePet(cageBattlePet.PetGuid);
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetSummon, Processing = PacketProcessing.Inplace)]
        private void HandleBattlePetSummon(BattlePetSummon battlePetSummon)
        {
            if (_player.GetSummonedBattlePetGUID() != battlePetSummon.PetGuid)
                GetBattlePetMgr().SummonPet(battlePetSummon.PetGuid);
            else
                GetBattlePetMgr().DismissPet();
        }

        [WorldPacketHandler(ClientOpcodes.BattlePetUpdateNotify)]
        private void HandleBattlePetUpdateNotify(BattlePetUpdateNotify battlePetUpdateNotify)
        {
            GetBattlePetMgr().UpdateBattlePetData(battlePetUpdateNotify.PetGuid);
        }
    }
}