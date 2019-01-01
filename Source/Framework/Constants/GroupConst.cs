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
    public enum RemoveMethod
    {
        Default = 0,
        Kick = 1,
        Leave = 2,
        KickLFG = 3
    }

    public enum GroupMemberOnlineStatus
    {
        Offline = 0x00,
        Online = 0x01,      // Lua_UnitIsConnected
        PVP = 0x02,         // Lua_UnitIsPVP
        Dead = 0x04,        // Lua_UnitIsDead
        Ghost = 0x08,       // Lua_UnitIsGhost
        PVPFFA = 0x10,      // Lua_UnitIsPVPFreeForAll
        Unk3 = 0x20,        // used in calls from Lua_GetPlayerMapPosition/Lua_GetBattlefieldFlagPosition
        AFK = 0x40,         // Lua_UnitIsAFK
        DND = 0x80,         // Lua_UnitIsDND
        RAF = 0x100,
        Vehicle = 0x200,    // Lua_UnitInVehicle
    }

    public enum GroupMemberFlags
    {
        Assistant = 0x01,
        MainTank = 0x02,
        MainAssist = 0x04
    }

    public enum GroupMemberAssignment
    {
        MainTank = 0,
        MainAssist = 1
    }

    public enum GroupType
    {
        None = 0,
        Normal = 1,
        WorldPvp = 4,
    }

    public enum GroupFlags
    {
        None = 0x00,
        FakeRaid = 0x01,
        Raid = 0x02,
        LfgRestricted = 0x04, // Script_HasLFGRestrictions()
        Lfg = 0x08,
        Destroyed = 0x10,
        OnePersonParty = 0x020, // Script_IsOnePersonParty()
        EveryoneAssistant = 0x040, // Script_IsEveryoneAssistant()
        GuildGroup = 0x100,

        MaskBgRaid = FakeRaid | Raid
    }

    public enum GroupUpdateFlags
    {
        None = 0x00,       // nothing
        Unk704 = 0x01,       // Uint8[2] (Unk)
        Status = 0x02,       // public ushort (Groupmemberstatusflag)
        PowerType = 0x04,       // Uint8 (Powertype)
        Unk322 = 0x08,       // public ushort (Unk)
        CurHp = 0x10,       // Uint32 (Hp)
        MaxHp = 0x20,       // Uint32 (Max Hp)
        CurPower = 0x40,       // Int16 (Power Value)
        MaxPower = 0x80,       // Int16 (Max Power Value)
        Level = 0x100,       // public ushort (Level Value)
        Unk200000 = 0x200,       // Int16 (Unk)
        Zone = 0x400,       // public ushort (Zone Id)
        Unk2000000 = 0x800,       // Int16 (Unk)
        Unk4000000 = 0x1000,       // Int32 (Unk)
        Position = 0x2000,       // public ushort (X), public ushort (Y), public ushort (Z)
        VehicleSeat = 0x4000,       // Int32 (Vehicle Seat Id)
        Auras = 0x8000,       // Uint8 (Unk), Uint64 (Mask), Uint32 (Count), For Each Bit Set: Uint32 (Spell Id) + public ushort (Auraflags)  (If Has Flags Scalable -> 3x Int32 (Bps))
        Pet = 0x10000,       // Complex (Pet)
        Phase = 0x20000,       // Int32 (Unk), Uint32 (Phase Count), For (Count) Uint16(Phaseid)

        Full = Unk704 | Status | PowerType | Unk322 | CurHp | MaxHp |
            CurPower | MaxPower | Level | Unk200000 | Zone | Unk2000000 |
            Unk4000000 | Position | VehicleSeat | Auras | Pet | Phase // All Known Flags
    }

    public enum GroupUpdatePetFlags
    {
        None = 0x00000000,       // nothing
        GUID = 0x00000001,       // ObjectGuid (pet guid)
        Name = 0x00000002,       // cstring (name, NULL terminated string)
        ModelId = 0x00000004,       // public ushort (model id)
        CurHp = 0x00000008,       // uint32 (HP)
        MaxHp = 0x00000010,       // uint32 (max HP)
        Auras = 0x00000020,       // [see GROUP_UPDATE_FLAG_AURAS]

        Full = GUID | Name | ModelId | CurHp | MaxHp | Auras // all pet flags
    }

    public enum PartyResult
    {
        Ok = 0,
        BadPlayerNameS = 1,
        TargetNotInGroupS = 2,
        TargetNotInInstanceS = 3,
        GroupFull = 4,
        AlreadyInGroupS = 5,
        NotInGroup = 6,
        NotLeader = 7,
        PlayerWrongFaction = 8,
        IgnoringYouS = 9,
        LfgPending = 12,
        InviteRestricted = 13,
        GroupSwapFailed = 14,               // If (Partyoperation == PartyOpSwap) GroupSwapFailed Else InviteInCombat
        InviteUnknownRealm = 15,
        InviteNoPartyServer = 16,
        InvitePartyBusy = 17,
        PartyTargetAmbiguous = 18,
        PartyLfgInviteRaidLocked = 19,
        PartyLfgBootLimit = 20,
        PartyLfgBootCooldownS = 21,
        PartyLfgBootInProgress = 22,
        PartyLfgBootTooFewPlayers = 23,
        PartyLfgBootNotEligibleS = 24,
        RaidDisallowedByLevel = 25,
        PartyLfgBootInCombat = 26,
        VoteKickReasonNeeded = 27,
        PartyLfgBootDungeonComplete = 28,
        PartyLfgBootLootRolls = 29,
        PartyLfgTeleportInCombat = 30
    }

    public enum PartyOperation
    {
        Invite = 0,
        UnInvite = 1,
        Leave = 2,
        Swap = 4
    }

    public enum GroupCategory
    {
        Home = 0,
        Instance = 1,

        Max
    }
}
