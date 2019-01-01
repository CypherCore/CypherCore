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
        SuspendedQueue = 14,
        ProposalBegin = 15,
        UpdateStatus = 16,
        GroupMemberOffline = 17,
        GroupDisbandUnk16 = 18,     // FIXME: Sometimes at group disband
        JoinQueueInitial = 25,
        DungeonFinished = 26,
        PartyRoleNotAvailable = 46,
        JoinLfgObjectFailed = 48,
        RemovedLevelup = 49,
        RemovedXpToggle = 50,
        RemovedFactionChange = 51
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

    public enum LfgQueueType
    {
        Dungeon = 1,
        LRF = 2,
        Scenario = 3,
        Flex = 4,
        WorldPvP = 5
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
        None = 0,      // Internal Use
        Dead = 1,
        Falling = 2,
        OnTransport = 3,
        Exhaustion = 4,
        NoReturnLocation = 6,
        ImmuneToSummons = 8       // Fixme - It Can Be 7 Or 8 (Need Proper Data)

        // unknown values
        //LFG_TELEPORT_RESULT_NOT_IN_DUNGEON,
        //LFG_TELEPORT_RESULT_NOT_ALLOWED,
        //LFG_TELEPORT_RESULT_ALREADY_IN_DUNGEON
    }

    public enum LfgJoinResult
    {
        // 3 = No client reaction | 18 = "Rolecheck failed"
        Ok = 0x00, // Joined (No Client Msg)
        GroupFull = 0x1f, // Your Group Is Already Full.
        NoLfgObject = 0x21, // Internal Lfg Error.
        NoSlotsPlayer = 0x22, // You Do Not Meet The Requirements For The Chosen Dungeons.
        MismatchedSlots = 0x23, // You Cannot Mix Dungeons, Raids, And Random When Picking Dungeons.
        PartyPlayersFromDifferentRealms = 0x24, // The Dungeon You Chose Does Not Support Players From Multiple Realms.
        MembersNotPresent = 0x25, // One Or More Group Members Are Pending Invites Or Disconnected.
        GetInfoTimeout = 0x26, // Could Not Retrieve Information About Some Party Members.
        InvalidSlot = 0x27, // One Or More Dungeons Was Not Valid.
        DeserterPlayer = 0x28, // You Can Not Queue For Dungeons Until Your Deserter Debuff Wears Off.
        DeserterParty = 0x29, // One Or More Party Members Has A Deserter Debuff.
        RandomCooldownPlayer = 0x2a, // You Can Not Queue For Random Dungeons While On Random Dungeon Cooldown.
        RandomCooldownParty = 0x2b, // One Or More Party Members Are On Random Dungeon Cooldown.
        TooManyMembers = 0x2c, // You Have Too Many Group Members To Queue For That.
        CantUseDungeons = 0x2d, // You Cannot Queue For A Dungeon Or Raid While Using Battlegrounds Or Arenas.
        RoleCheckFailed = 0x2e, // The Role Check Has Failed.
        TooFewMembers = 0x34, // You Do Not Have Enough Group Members To Queue For That.
        ReasonTooManyLfg = 0x35, // You Are Queued For Too Many Instances.
        MismatchedSlotsLocalXrealm = 0x37, // You Cannot Mix Realm-Only And X-Realm Entries When Listing Your Name In Other Raids.
        AlreadyUsingLfgList = 0x3f, // You Can'T Do That While Using Premade Groups.
        NotLeader = 0x45, // You Are Not The Party Leader.
        Dead = 0x49,

        PartyNotMeetReqs = 6,      // One Or More Party Members Do Not Meet The Requirements For The Chosen Dungeons (Fixme)
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
