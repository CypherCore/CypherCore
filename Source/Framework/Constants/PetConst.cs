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
 */﻿

namespace Framework.Constants
{
    public enum CharmType
    {
        Charm,
        Possess,
        Vehicle,
        Convert
    }

    public enum PetType
    {
        Summon = 0,
        Hunter = 1,
        Max = 4
    }

    public enum PetSaveMode
    {
        AsDeleted = -1,                        // not saved in fact
        AsCurrent = 0,                        // in current slot (with player)
        FirstStableSlot = 1,
        LastStableSlot = 4,          // last in DB stable slot index (including), all higher have same meaning as PET_SAVE_NOT_IN_SLOT
        NotInSlot = 100                       // for avoid conflict with stable size grow will use 100
    }

    public enum HappinessState
    {
        UnHappy = 1,
        Content = 2,
        Happy = 3
    }

    public enum PetSpellState
    {
        Unchanged = 0,
        Changed = 1,
        New = 2,
        Removed = 3
    }

    public enum PetSpellType
    {
        Normal = 0,
        Family = 1,
        Talent = 2
    }

    public enum ActionFeedback
    {
        None = 0,
        PetDead = 1,
        NothingToAtt = 2,
        CantAttTarget = 3
    }

    public enum PetTalk
    {
        SpecialSpell = 0,
        Attack = 1
    }

    public enum CommandStates
    {
        Stay = 0,
        Follow = 1,
        Attack = 2,
        Abandon = 3,
        MoveTo = 4
    }

    public enum PetNameInvalidReason
    {
        // custom, not send
        Success = 0,

        Invalid = 1,
        NoName = 2,
        TooShort = 3,
        TooLong = 4,
        MixedLanguages = 6,
        Profane = 7,
        Reserved = 8,
        ThreeConsecutive = 11,
        InvalidSpace = 12,
        ConsecutiveSpaces = 13,
        RussianConsecutiveSilentCharacters = 14,
        RussianSilentCharacterAtBeginningOrEnd = 15,
        DeclensionDoesntMatchBaseName = 16
    }

    public enum PetStableinfo : byte
    {
        Active = 1,
        Inactive = 2
    }
}
