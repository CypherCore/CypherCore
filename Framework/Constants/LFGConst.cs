/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
    public enum LfgRoles
    {
        None = 0x00,
        Leader = 0x01,
        Tank = 0x02,
        Healer = 0x04,
        Damage = 0x08
    }

    public enum LfgUpdateType
    {
        Default = 0,      // Internal Use
        LeaderUnk1 = 1,      // Fixme: At Group Leave
        RolecheckAborted = 4,
        JoinQueue = 6,
        RolecheckFailed = 7,
        RemovedFromQueue = 8,
        ProposalFailed = 9,
        ProposalDeclined = 10,
        GroupFound = 11,
        AddedToQueue = 13,
        ProposalBegin = 14,
        UpdateStatus = 15,
        GroupMemberOffline = 16,
        GroupDisbandUnk16 = 17,     // Fixme: Sometimes At Group Disband
        JoinQueueInitial = 24,
        DungeonFinished = 25,
        PartyRoleNotAvailable = 43,
        JoinLfgObjectFailed = 45,
    }

    public enum LfgState
    {
        None,
        Rolecheck,
        Queued,
        Proposal,
        //Boot,
        Dungeon = 5,
        FinishedDungeon,
        Raidbrowser
    }

    public enum LfgLockStatusType
    {
        InsufficientExpansion = 1,
        TooLowLevel = 2,
        TooHighLevel = 3,
        TooLowGearScore = 4,
        TooHighGearScore = 5,
        RaidLocked = 6,
        AttunementTooLowLevel = 1001,
        AttunementTooHighLevel = 1002,
        QuestNotCompleted = 1022,
        MissingItem = 1025,
        NotInSeason = 1031,
        MissingAchievement = 1034
    }

    public enum LfgOptions
    {
        EnableDungeonFinder = 0x01,
        EnableRaidBrowser = 0x02,
    }

    public enum LfgFlags
    {
        Unk1 = 0x1,
        Unk2 = 0x2,
        Seasonal = 0x4,
        Unk3 = 0x8
    }

    public enum LfgType : byte
    {
        None = 0,
        Dungeon = 1,
        Raid = 2,
        Zone = 4,
        Quest = 5,
        RandomDungeon = 6
    }

    public enum LfgProposalState
    {
        Initiating = 0,
        Failed = 1,
        Success = 2
    }

    public enum LfgTeleportResult
    {
        // 7 = "You Can'T Do That Right Now" | 5 = No Client Reaction
        Ok = 0,      // Internal Use
        PlayerDead = 1,
        Falling = 2,
        InVehicle = 3,
        Fatigue = 4,
        InvalidLocation = 6,
        Charming = 8       // Fixme - It Can Be 7 Or 8 (Need Proper Data)
    }

    public enum LfgJoinResult
    {
        // 3 = No Client Reaction | 18 = "Rolecheck Failed"
        Ok = 0x00,   // Joined (No Client Msg)
        Failed = 0x1b,   // Rolecheck Failed
        Groupfull = 0x1c,   // Your Group Is Full
        InternalError = 0x1e,   // Internal Lfg Error
        NotMeetReqs = 0x1f,   // You Do Not Meet The Requirements For The Chosen Dungeons
        PartyNotMeetReqs = 6,      // One Or More Party Members Do Not Meet The Requirements For The Chosen Dungeons
        MixedRaidDungeon = 0x20,   // You Cannot Mix Dungeons, Raids, And Random When Picking Dungeons
        MultiRealm = 0x21,   // The Dungeon You Chose Does Not Support Players From Multiple Realms
        Disconnected = 0x22,   // One Or More Party Members Are Pending Invites Or Disconnected
        PartyInfoFailed = 0x23,   // Could Not Retrieve Information About Some Party Members
        DungeonInvalid = 0x24,   // One Or More Dungeons Was Not Valid
        Deserter = 0x25,   // You Can Not Queue For Dungeons Until Your Deserter Debuff Wears Off
        PartyDeserter = 0x26,   // One Or More Party Members Has A Deserter Debuff
        RandomCooldown = 0x27,   // You Can Not Queue For Random Dungeons While On Random Dungeon Cooldown
        PartyRandomCooldown = 0x28,   // One Or More Party Members Are On Random Dungeon Cooldown
        TooMuchMembers = 0x29,   // You Can Not Enter Dungeons With More That 5 Party Members
        UsingBgSystem = 0x2a,   // You Can Not Use The Dungeon System While In Bg Or Arenas
        RoleCheckFailed = 0x2b    // Role Check Failed, Client Shows Special Error
    }

    public enum LfgRoleCheckState
    {
        Default = 0,      // Internal Use = Not Initialized.
        Finished = 1,      // Role Check Finished
        Initialiting = 2,      // Role Check Begins
        MissingRole = 3,      // Someone Didn'T Selected A Role After 2 Mins
        WrongRoles = 4,      // Can'T Form A Group With That Role Selection
        Aborted = 5,      // Someone Leave The Group
        NoRole = 6       // Someone Selected No Role
    }

    public enum LfgAnswer
    {
        Pending = -1,
        Deny = 0,
        Agree = 1
    }

    public enum LfgCompatibility
    {
        Pending,
        WrongGroupSize,
        TooMuchPlayers,
        MultipleLfgGroups,
        HasIgnores,
        NoRoles,
        NoDungeons,
        WithLessPlayers,                     // Values Under This = Not Compatible (Do Not Modify Order)
        BadStates,
        Match                                  // Must Be The Last One
    }
}
