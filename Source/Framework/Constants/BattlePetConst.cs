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
using System;

namespace Framework.Constants
{
    [Flags]
    public enum BattlePetError
    {
        CantHaveMorePetsOfThatType = 3, // You can't have any more pets of that type.
        CantHaveMorePets = 4, // You can't have any more pets.
        TooHighLevelToUncage = 7, // This pet is too high level for you to uncage.
    }

    public enum FlagsControlType
    {
        Apply = 1,
        Remove = 2
    }

    // taken from BattlePetState.db2 - it seems to store some initial values for battle pets
    // there are only values used in BattlePetSpeciesState.db2 and BattlePetBreedState.db2
    // TODO: expand this enum if needed
    public enum BattlePetState
    {
        MaxHealthBonus = 2,
        InternalInitialLevel = 17,
        StatPower = 18,
        StatStamina = 19,
        StatSpeed = 20,
        ModDamageDealtPercent = 23,
        Gender = 78, // 1 - Male, 2 - Female
        CosmeticWaterBubbled = 85,
        SpecialIsCockroach = 93,
        CosmeticFlyTier = 128,
        CosmeticBigglesworth = 144,
        PassiveElite = 153,
        PassiveBoss = 162,
        CosmeticTreasureGoblin = 176,
        // These Are Not In Battlepetstate.Db2 But Are Used In Battlepetspeciesstate.Db2
        StartWithBuff = 183,
        StartWithBuff2 = 184,
        //
        CosmeticSpectralBlue = 196
    }

    public enum BattlePetSaveInfo
    {
        Unchanged = 0,
        Changed = 1,
        New = 2,
        Removed = 3
    }

    public enum BattlePetBreedQuality
    {
        Poor = 0,
        Common = 1,
        Uncommon = 2,
        Rare = 3,
        Epic = 4,
        Legendary = 5,

        Max
    }

    public enum BattlePetXpSource
    {
        PetBattle = 0,
        SpellEffect = 1,

        Count
    }
}
