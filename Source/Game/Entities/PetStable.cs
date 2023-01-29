// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities
{
    public class PetStable
    {
        private static readonly uint _unslottedPetIndexMask = 0x80000000;
        public PetInfo[] ActivePets { get; set; } = new PetInfo[SharedConst.MaxActivePets]; // PET_SAVE_FIRST_ACTIVE_SLOT - PET_SAVE_LAST_ACTIVE_SLOT

        public uint? CurrentPetIndex { get; set; }                                          // index into ActivePets or UnslottedPets if highest bit is set
        public PetInfo[] StabledPets { get; set; } = new PetInfo[SharedConst.MaxPetStables]; // PET_SAVE_FIRST_STABLE_SLOT - PET_SAVE_LAST_STABLE_SLOT
        public List<PetInfo> UnslottedPets { get; set; } = new();                            // PET_SAVE_NOT_IN_SLOT

        public PetInfo GetCurrentPet()
        {
            if (!CurrentPetIndex.HasValue)
                return null;

            uint? activePetIndex = GetCurrentActivePetIndex();

            if (activePetIndex.HasValue)
                return ActivePets[activePetIndex.Value] != null ? ActivePets[activePetIndex.Value] : null;

            uint? unslottedPetIndex = GetCurrentUnslottedPetIndex();

            if (unslottedPetIndex.HasValue)
                return unslottedPetIndex < UnslottedPets.Count ? UnslottedPets[(int)unslottedPetIndex.Value] : null;

            return null;
        }

        public uint? GetCurrentActivePetIndex()
        {
            return CurrentPetIndex.HasValue && ((CurrentPetIndex & _unslottedPetIndexMask) == 0) ? CurrentPetIndex : null;
        }

        public void SetCurrentActivePetIndex(uint index)
        {
            CurrentPetIndex = index;
        }

        private uint? GetCurrentUnslottedPetIndex()
        {
            return CurrentPetIndex.HasValue && ((CurrentPetIndex & _unslottedPetIndexMask) != 0) ? (CurrentPetIndex & ~_unslottedPetIndexMask) : null;
        }

        public void SetCurrentUnslottedPetIndex(uint index)
        {
            CurrentPetIndex = index | _unslottedPetIndexMask;
        }

        public class PetInfo
        {
            public string ActionBar;
            public uint CreatedBySpellId;
            public uint CreatureId;
            public uint DisplayId;
            public uint Experience;
            public uint Health;
            public uint LastSaveTime;
            public byte Level = 0;
            public uint Mana;
            public string Name;
            public uint PetNumber;
            public ReactStates ReactState;
            public ushort SpecializationId;
            public PetType Type = PetType.Max;
            public bool WasRenamed;
        }
    }
}