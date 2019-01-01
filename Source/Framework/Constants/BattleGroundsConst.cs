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
    public struct BattlegroundConst
    {
        //Time Intervals
        public const uint CheckPlayerPositionInverval = 1000;   // Ms
        public const uint ResurrectionInterval = 30000;         // Ms
        //RemindInterval                 = 10000,               // Ms
        public const uint InvitationRemindTime = 20000;         // Ms
        public const uint InviteAcceptWaitTime = 90000;         // Ms
        public const uint AutocloseBattleground = 120000;        // Ms
        public const uint MaxOfflineTime = 300;                 // Secs
        public const uint RespawnOneDay = 86400;                // Secs
        public const uint RespawnImmediately = 0;               // Secs
        public const uint BuffRespawnTime = 180;                // Secs
        public const uint BattlegroundCountdownMax = 120;       // Secs
        public const uint ArenaCountdownMax = 60;               // Secs
        public const uint PlayerPositionUpdateInterval = 5;     // secs

        //EventIds
        public const int EventIdFirst = 0;
        public const int EventIdSecond = 1;
        public const int EventIdThird = 2;
        public const int EventIdFourth = 3;
        public const int EventIdCount = 4;

        //Quests
        public const uint WsQuestReward = 43483;
        public const uint AbQuestReward = 43484;
        public const uint AvQuestReward = 43475;
        public const uint AvQuestKilledBoss = 23658;
        public const uint EyQuestReward = 43477;
        public const uint SaQuestReward = 61213;
        public const uint AbQuestReward4Bases = 24061;
        public const uint AbQuestReward5Bases = 24064;

        //BuffObjects
        public const uint SpeedBuff = 179871;
        public const uint RegenBuff = 179904;
        public const uint BerserkerBuff = 179905;

        //QueueGroupTypes
        public const uint BgQueuePremadeAlliance = 0;
        public const uint BgQueuePremadeHorde = 1;
        public const uint BgQueueNormalAlliance = 2;
        public const uint BgQueueNormalHorde = 3;
        public const int BgQueueTypesCount = 4;

        //PlayerPosition
        public const sbyte PlayerPositionIconNone = 0;
        public const sbyte PlayerPositionIconHordeFlag = 1;
        public const sbyte PlayerPositionIconAllianceFlag = 2;

        public const sbyte PlayerPositionArenaSlotNone = 1;
        public const sbyte PlayerPositionArenaSlot1 = 2;
        public const sbyte PlayerPositionArenaSlot2 = 3;
        public const sbyte PlayerPositionArenaSlot3 = 4;
        public const sbyte PlayerPositionArenaSlot4 = 5;
        public const sbyte PlayerPositionArenaSlot5 = 6;

        //Spells
        public const uint SpellWaitingForResurrect = 2584;                 // Waiting To Resurrect
        public const uint SpellSpiritHealChannel = 22011;                // Spirit Heal Channel
        public const uint SpellSpiritHealChannelVisual = 3060;
        public const uint SpellSpiritHeal = 22012;                // Spirit Heal
        public const uint SpellResurrectionVisual = 24171;                // Resurrection Impact Visual
        public const uint SpellArenaPreparation = 32727;                // Use This One, 32728 Not Correct
        public const uint SpellPreparation = 44521;                // Preparation
        public const uint SpellSpiritHealMana = 44535;                // Spirit Heal
        public const uint SpellRecentlyDroppedFlag = 42792;                // Recently Dropped Flag
        public const uint SpellAuraPlayerInactive = 43681;                // Inactive
        public const uint SpellHonorableDefender25y = 68652;                // +50% Honor When Standing At A Capture Point That You Control, 25yards Radius (Added In 3.2)
        public const uint SpellHonorableDefender60y = 66157;              // +50% Honor When Standing At A Capture Point That You Control, 60yards Radius (Added In 3.2), Probably For 40+ Player Battlegrounds
    }

    public enum BattlegroundEventFlags
    {
        None = 0x00,
        Event1 = 0x01,
        Event2 = 0x02,
        Event3 = 0x04,
        Event4 = 0x08
    }

    // indexes of BattlemasterList.dbc
    public enum BattlegroundTypeId
    {
        None = 0,   // None
        AV = 1,   // Alterac Valley
        WS = 2,   // Warsong Gulch
        AB = 3,   // Arathi Basin
        NA = 4,   // Nagrand Arena
        BE = 5,   // Blade'S Edge Arena
        AA = 6,   // All Arenas
        EY = 7,   // Eye Of The Storm
        RL = 8,   // Ruins Of Lordaernon
        SA = 9,   // Strand Of The Ancients
        DS = 10,  // Dalaran Sewers
        RV = 11,  // The Ring Of Valor
        IC = 30,  // Isle Of Conquest
        RB = 32,  // Random Battleground
        Rated10Vs10 = 100, // Rated Battleground 10 Vs 10
        Rated15Vs15 = 101, // Rated Battleground 15 Vs 15
        Rated25Vs25 = 102, // Rated Battleground 25 Vs 25
        TP = 108, // Twin Peaks
        BFG = 120, // Battle For Gilneas
                   // 656 = "Rated Eye Of The Storm"
        Tk = 699, // Temple Of Kotmogu
                  // 706 = "Ctf3"
        SM = 708, // Silvershard Mines
        TVA = 719, // Tol'Viron Arena
        DG = 754, // Deepwind Gorge
        TTP = 757, // The Tiger'S Peak
        SSvsTM = 789, // Southshore Vs. Tarren Mill
        SmallD = 803, // Small Battleground D
        BRH = 808, // Black Rook Hold Arena
                   // 809 = "New Nagrand Arena (Legion)"
        AF = 816, // Ashamane'S Fall
                  // 844 = "New Blade'S Edge Arena (Legion)"
        Max = 845
    }

    public enum BattlegroundQueueTypeId
    {
        None = 0,
        AV = 1,
        WS = 2,
        AB = 3,
        EY = 4,
        SA = 5,
        IC = 6,
        TP = 7,
        BFG = 8,
        RB = 9,
        Arena2v2 = 10,
        Arena3v3 = 11,
        Arena5v5 = 12,
        Max
    }

    public enum BattlegroundQueueInvitationType
    {
        NoBalance = 0, // no balance: N+M vs N players
        Balanced = 1, // teams balanced: N+1 vs N players
        Even = 2  // teams even: N vs N players
    }

    public enum BattlegroundCriteriaId
    {
        ResilientVictory,
        SaveTheDay,
        EverythingCounts,
        AvPerfection,
        DefenseOfTheAncients,
        NotEvenAScratch,
    }

    public struct BattlegroundBroadcastTexts
    {
        public const uint AllianceWins = 10633;
        public const uint HordeWins = 10634;

        public const uint StartTwoMinutes = 18193;
        public const uint StartOneMinute = 18194;
        public const uint StartHalfMinute = 18195;
        public const uint HasBegun = 18196;
    }

    public enum BattlegroundSounds
    {
        HordeWins = 8454,
        AllianceWins = 8455,
        BgStart = 3439,
        BgStartL70etc = 11803
    }

    public enum BattlegroundTeamId
    {
        Horde = 0, // Battleground: Horde,    Arena: Green
        Alliance = 1, // Battleground: Alliance, Arena: Gold
        Neutral = 2  // Battleground: Neutral,  Arena: None
    }

    public enum BattlegroundMarks
    {
        SpellWsMarkLoser = 24950,
        SpellWsMarkWinner = 24951,
        SpellAbMarkLoser = 24952,
        SpellAbMarkWinner = 24953,
        SpellAvMarkLoser = 24954,
        SpellAvMarkWinner = 24955,
        SpellSaMarkWinner = 61160,
        SpellSaMarkLoser = 61159,
        ItemAvMarkOfHonor = 20560,
        ItemWsMarkOfHonor = 20558,
        ItemAbMarkOfHonor = 20559,
        ItemEyMarkOfHonor = 29024,
        ItemSaMarkOfHonor = 42425
    }

    public enum BattlegroundMarksCount
    {
        WinnterCount = 3,
        LoserCount = 1
    }

    public enum BattlegroundCreatures
    {
        A_SpiritGuide = 13116,           // alliance
        H_SpiritGuide = 13117            // horde
    }

    public enum BattlegroundStartTimeIntervals
    {
        Delay2m = 120000,               // Ms (2 Minutes)
        Delay1m = 60000,                // Ms (1 Minute)
        Delay30s = 30000,                // Ms (30 Seconds)
        Delay15s = 15000,                // Ms (15 Seconds) Used Only In Arena
        None = 0                     // Ms
    }

    public enum BattlegroundStatus
    {
        None = 0,                                // first status, should mean bg is not instance
        WaitQueue = 1,                                // means bg is empty and waiting for queue
        WaitJoin = 2,                                // this means, that BG has already started and it is waiting for more players
        InProgress = 3,                                // means bg is running
        WaitLeave = 4                                 // means some faction has won BG and it is ending
    }

    public enum BGHonorMode
    {
        Normal = 0,
        Holiday,
        HonorModeNum
    }

    public enum GroupJoinBattlegroundResult
    {
        None = 0,
        Deserters = 2,        // You Cannot Join The BattlegroundYet Because You Or One Of Your Party Members Is Flagged As A Deserter.
        ArenaTeamPartySize = 3,        // Incorrect Party Size For This Arena.
        TooManyQueues = 4,        // You Can Only Be Queued For 2 Battles At Once
        CannotQueueForRated = 5,        // You Cannot Queue For A Rated Match While Queued For Other Battles
        BattledgroundQueuedForRated = 6,        // You Cannot Queue For Another Battle While Queued For A Rated Arena Match
        TeamLeftQueue = 7,        // Your Team Has Left The Arena Queue
        NotInBattleground= 8,        // You Can'T Do That In A Battleground.
        JoinXpGain = 9,        // Wtf, Doesn'T Exist In Client...
        JoinRangeIndex = 10,       // Cannot Join The Queue Unless All Members Of Your Party Are In The Same BattlegroundLevel Range.
        JoinTimedOut = 11,       // %S Was Unavailable To Join The Queue. (Uint64 Guid Exist In Client Cache)
                                 //JoinTimedOut               = 12,       // Same As 11
                                 //TeamLeftQueue              = 13,       // Same As 7
        LfgCantUseBattleground= 14,       // You Cannot Queue For A BattlegroundOr Arena While Using The Dungeon System.
        InRandomBg = 15,       // Can'T Do That While In A Random BattlegroundQueue.
        InNonRandomBg = 16,       // Can'T Queue For Random BattlegroundWhile In Another BattlegroundQueue.
        BgDeveloperOnly = 17,
        InvitationDeclined = 18,
        MeetingStoneNotFound = 19,
        WargameRequestFailure = 20,
        BattlefieldTeamPartySize = 22,
        NotOnTournamentRealm = 23,
        PlayersFromDifferentRealms = 24,
        RemoveFromPvpQueueGrantLevel = 33,
        RemoveFromPvpQueueFactionChange = 34,
        JoinFailed = 35,
        DupeQueue = 43,
        JoinNoValidSpecForRole = 44,
        JoinRespec = 45,
        AlreadyUsingLFGList = 46,
        JoinMustCompleteQuest = 47
    }

    public enum ScoreType
    {
        KillingBlows = 1,
        Deaths = 2,
        HonorableKills = 3,
        BonusHonor = 4,
        DamageDone = 5,
        HealingDone = 6,

        // Ws And Ey
        FlagCaptures = 7,
        FlagReturns = 8,

        // Ab And Ic
        BasesAssaulted = 9,
        BasesDefended = 10,

        // Av
        GraveyardsAssaulted = 11,
        GraveyardsDefended = 12,
        TowersAssaulted = 13,
        TowersDefended = 14,
        MinesCaptured = 15,

        // Sota
        DestroyedDemolisher = 16,
        DestroyedWall = 17
    }

    //Arenas
    public struct ArenaBroadcastTexts
    {
        public const uint OneMinute = 15740;
        public const uint ThirtySeconds = 15741;
        public const uint FifteenSeconds = 15739;
        public const uint HasBegun = 15742;
    }

    public struct ArenaSpellIds
    {
        public const uint AllianceGoldFlag = 32724;
        public const uint AllianceGreenFlag = 32725;
        public const uint HordeGoldFlag = 35774;
        public const uint HordeGreenFlag = 35775;
        public const uint LastManStanding = 26549;            // Arena Achievement Related
    }

    public enum ArenaTeamCommandTypes
    {
        Create_S = 0x00,
        Invite_SS = 0x01,
        Quit_S = 0x03,
        Founder_S = 0x0e
    }

    public enum ArenaTeamCommandErrors
    {
        ArenaTeamCreated = 0x00,
        ArenaTeamInternal = 0x01,
        AlreadyInArenaTeam = 0x02,
        AlreadyInArenaTeamS = 0x03,
        InvitedToArenaTeam = 0x04,
        AlreadyInvitedToArenaTeamS = 0x05,
        ArenaTeamNameInvalid = 0x06,
        ArenaTeamNameExistsS = 0x07,
        ArenaTeamLeaderLeaveS = 0x08,
        ArenaTeamPermissions = 0x08,
        ArenaTeamPlayerNotInTeam = 0x09,
        ArenaTeamPlayerNotInTeamSs = 0x0a,
        ArenaTeamPlayerNotFoundS = 0x0b,
        ArenaTeamNotAllied = 0x0c,
        ArenaTeamIgnoringYouS = 0x13,
        ArenaTeamTargetTooLowS = 0x15,
        ArenaTeamTargetTooHighS = 0x16,
        ArenaTeamTooManyMembersS = 0x17,
        ArenaTeamNotFound = 0x1b,
        ArenaTeamsLocked = 0x1e,
        ArenaTeamTooManyCreate = 0x21,
    }

    public enum ArenaTeamEvents
    {
        JoinSs = 3,            // Player Name + Arena Team Name
        LeaveSs = 4,            // Player Name + Arena Team Name
        RemoveSss = 5,            // Player Name + Arena Team Name + Captain Name
        LeaderIsSs = 6,            // Player Name + Arena Team Name
        LeaderChangedSss = 7,            // Old Captain + New Captain + Arena Team Name
        DisbandedS = 8             // Captain Name + Arena Team Name
    }

    public enum ArenaTypes
    {
        BG = 0,
        Team2v2 = 2,
        Team3v3 = 3,
        Team5v5 = 5
    }

    public enum ArenaErrorType
    {
        NoTeam = 0,
        ExpiredCAIS = 1,
        CantUseBattleground = 2
    }

    public enum ArenaTeamInfoType
    {
        Id = 0,
        Type = 1,                       // new in 3.2 - team type?
        Member = 2,                       // 0 - captain, 1 - member
        GamesWeek = 3,
        GamesSeason = 4,
        WinsSeason = 5,
        PersonalRating = 6,
        End = 7
    }
}
