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

namespace Framework.Constants
{
    public enum FlagsControlType
    {
        Apply = 1,
        Remove = 2
    }

    public enum BattlePetError
    {
        CantHaveMorePetsOfThatType = 3,
        CantHaveMorePets = 4,
        TooHighLevelToUncage = 7,

        // TODO: find correct values if possible and needed (also wrong order)
        DuplicateConvertedPet,
        NeedToUnlock,
        BadParam,
        LockedPetAlreadyExists,
        Ok,
        Uncapturable,
        CantInvalidCharacterGuid
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
}
