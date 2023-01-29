// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.BattlePets
{
    public class BattlePet
    {
        public DeclinedName DeclinedName { get; set; }
        public long NameTimestamp { get; set; }

        public BattlePetStruct PacketInfo;
        public BattlePetSaveInfo SaveInfo { get; set; }

        public void CalculateStats()
        {
            // get base breed Stats
            var breedState = BattlePetMgr.BattlePetBreedStates.LookupByKey(PacketInfo.Breed);

            if (breedState == null) // non existing breed Id
                return;

            float health = breedState[BattlePetState.StatStamina];
            float power = breedState[BattlePetState.StatPower];
            float speed = breedState[BattlePetState.StatSpeed];

            // modify Stats depending on species - not all pets have this
            var speciesState = BattlePetMgr.BattlePetSpeciesStates.LookupByKey(PacketInfo.Species);

            if (speciesState != null)
            {
                health += speciesState[BattlePetState.StatStamina];
                power += speciesState[BattlePetState.StatPower];
                speed += speciesState[BattlePetState.StatSpeed];
            }

            // modify Stats by quality
            foreach (var battlePetBreedQuality in CliDB.BattlePetBreedQualityStorage.Values)
                if (battlePetBreedQuality.QualityEnum == PacketInfo.Quality)
                {
                    health *= battlePetBreedQuality.StateMultiplier;
                    power *= battlePetBreedQuality.StateMultiplier;
                    speed *= battlePetBreedQuality.StateMultiplier;

                    break;
                }

            // TOOD: add check if pet has existing quality
            // scale Stats depending on level
            health *= PacketInfo.Level;
            power *= PacketInfo.Level;
            speed *= PacketInfo.Level;

            // set Stats
            // round, ceil or floor? verify this
            PacketInfo.MaxHealth = (uint)((Math.Round(health / 20) + 100));
            PacketInfo.Power = (uint)(Math.Round(power / 100));
            PacketInfo.Speed = (uint)(Math.Round(speed / 100));
        }
    }
}