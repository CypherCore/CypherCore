// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.﻿

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
        public const uint AutocloseBattleground = 120000;       // Ms
        public const uint MaxOfflineTime = 300;                 // Secs
        public const uint RespawnOneDay = 86400;                // Secs
        public const uint RespawnImmediately = 0;               // Secs
        public const uint BuffRespawnTime = 180;                // Secs
        public const uint BattlegroundCountdownMax = 120;       // Secs
        public const uint ArenaCountdownMax = 60;               // Secs
        public const uint PlayerPositionUpdateInterval = 5000;  // Ms

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
        public const uint SpellSpiritHealChannelAoE = 22011;                // used for AoE resurrections
        public const uint SpellSpiritHealPlayerAura = 156758;               // individual player timers for resurrection
        public const uint SpellSpiritHealChannelSelf = 305122;               // channel visual for individual area spirit healers
        public const uint SpellWaitingForResurrect = 2584;                 // Waiting To Resurrect
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
        public const uint SpellMercenaryContractHorde = 193472;
        public const uint SpellMercenaryContractAlliance = 193475;
        public const uint SpellMercenaryHorde1 = 193864;
        public const uint SpellMercenaryHordeReactions = 195838;
        public const uint SpellMercenaryAlliance1 = 193863;
        public const uint SpellMercenaryAllianceReactions = 195843;
        public const uint SpellMercenaryShapeshift = 193970;
        public const uint SpellPetSummoned = 6962; // used after resurrection
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
        BrawlTbg = 846, // Brawl - The Battle For Gilneas (Old City Map)
        BrawlAbw = 847, // Brawl - Arathi Basin Winter
                        // 848 = "Ai Test - Arathi Basin"
        BrawlDd = 849, // Brawl - Deepwind Dunk
        BrawlSps = 853, // Brawl - Shadow-Pan Showdown
                        // 856 = "[Temp] Racetrackbg"
        Br = 857, // Blackrock
        BrawlTh = 858, // Brawl - Temple Of Hotmogu
        BrawlGl = 859, // Brawl - Gravity Lapse
        BrawlDd2 = 860, // Brawl - Deepwind Dunk
        BrawlWs = 861, // Brawl - Warsong Scramble
        BrawlEh = 862, // Brawl - Eye Of The Horn
        BrawlAa = 866, // Brawl - All Arenas
        Rl2 = 868, // Ruins Of Lordaeron
        Ds2 = 869, // Dalaran Sewers
        Tva2 = 870, // Tol'Viron Arena
        Ttp2 = 871, // The Tiger'S Peak
        Brha2 = 872, // Black Rook Hold Arena
        Na2 = 873, // Nagrand Arena
        Af2 = 874, // Ashamane'S Fall
        Bea2 = 875, // Blade'S Edge Arena
                    // 878 = "Ai Test - Warsong Gulch"
        BrawlDs = 879, // Brawl - Deep Six
        BrawlAb = 880, // Brawl - Arathi Basin
        BrawlDg = 881, // Brawl - Deepwind Gorge
        BrawlEs = 882, // Brawl - Eye Of The Storm
        BrawlSm = 883, // Brawl - Silvershard Mines
        BrawlTk = 884, // Brawl - Temple Of Kotmogue
        BrawlTbg2 = 885, // Brawl - The Battle For Gilneas
        BrawlWg = 886, // Brawl - Warsong Gulch
        Ci = 887, // Cooking: Impossible
        DomSs = 890, // Domination - Seething Strand
                     // 893 = "8.0 Bg Temp"
        Ss = 894, // Seething Shore
        Hp = 897, // Hooking Point
        RandomEpic = 901, // Random Epic Battleground
        Ttp3 = 902, // The Tiger'S Peak
        Mb = 903, // Mugambala
        BrawlAa2 = 904, // Brawl - All Arenas
        BrawlAash = 905, // Brawl - All Arenas - Stocked House
        Af3 = 906, // Ashamane'S Fall
        Bea3 = 907, // Blade'S Edge Arena
        Be2 = 908, // Blade'S Edge
        Ds3 = 909, // Dalaran Sewers
        Na3 = 910, // Nagrand Arena
        Rl3 = 911, // Ruins Of Lordaeron
        Tva3 = 912, // Tol'Viron Arena
        Brha3 = 913, // Black Rook Hold Arena
        WgCtf = 1014,     // Warsong Gulch Capture The Flag
        EbBw = 1017,     // Epic Battleground - Battle For Wintergrasp
        DomAb = 1018,     // Domination - Arathi Basin
        AbCs = 1019,     // Arathi Basin Comp Stomp
        EbA = 1020,     // Epic Battleground - Ashran
        Ca = 1021,     // Classic Ashran (Endless)
        BrawlAb2 = 1022,     // Brawl - Arathi Basin
        Tr = 1025,     // The Robodrome (Arena)
        RandomBg = 1029,     // Random Battleground
        EbBw2 = 1030,     // Epic Battleground - Battle For Wintergrasp
                          // 1031 = "Programmer Map - Battlefield"
        Kr = 1033,     // Korrak'S Revenge
        EpicBgWf = 1036,     // Epic Battleground - Warfront Arathi (Pvp)
        DomDg = 1037,     // Domination - Deepwind Gorge
        DomDg2 = 1039,     // Domination - Deepwind Gorge
        Ed = 1041,     // Empyrean Domain
        Max = 902
    }

    public enum BattlegroundQueueIdType
    {
        Battleground = 0,
        Arena = 1,
        Wargame = 2,
        Cheat = 3,
        ArenaSkirmish = 4
    }

    public enum BattlegroundPointCaptureStatus
    {
        AllianceControlled,
        AllianceCapturing,
        Neutral,
        HordeCapturing,
        HordeControlled
    }

    public enum BattlegroundQueueInvitationType
    {
        NoBalance = 0, // no balance: N+M vs N players
        Balanced = 1, // teams balanced: N+1 vs N players
        Even = 2  // teams even: N vs N players
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

    public enum PvPTeamId
    {
        Horde = 0, // Battleground: Horde,    Arena: Green
        Alliance = 1, // Battleground: Alliance, Arena: Gold
        Neutral = 2  // Battleground: Neutral,  Arena: None
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
        JoinXpGain = 9,        // Cannot join as a group unless all the members of your party have the same XP gain setting.
        JoinRangeIndex = 10,       // Cannot Join The Queue Unless All Members Of Your Party Are In The Same BattlegroundLevel Range.
        JoinTimedOut = 11,       // %S Was Unavailable To Join The Queue. (Uint64 Guid Exist In Client Cache)
                                 //JoinTimedOut               = 12,       // Same As 11
                                 //TeamLeftQueue              = 13,       // Same As 7
        LfgCantUseBattleground= 14,       // You Cannot Queue For A BattlegroundOr Arena While Using The Dungeon System.
        InRandomBg = 15,       // Can'T Do That While In A Random BattlegroundQueue.
        InNonRandomBg = 16,       // Can'T Queue For Random BattlegroundWhile In Another BattlegroundQueue.
        BgDeveloperOnly = 17,       // This Battleground Is Only Available For Developer Testing At This Time.
        BattlegroundInvitationDeclined = 18,       // Your War Game Invitation Has Been Declined
        MeetingStoneNotFound = 19,       // Player Not Found.
        WargameRequestFailure = 20,       // War Game Request Failed
        BattlefieldTeamPartySize = 22,       // Incorrect Party Size For This Battlefield.
        NotOnTournamentRealm = 23,       // Not Available On A Tournament Realm.
        BattlegroundPlayersFromDifferentRealms = 24,       // You Cannot Queue For A Battleground While Players From Different Realms Are In Your Party.
        BattlegroundJoinLevelup = 33,       // You Have Been Removed From A Pvp Queue Because You Have Gained A Level.
        RemoveFromPvpQueueFactionChange = 34,       // You Have Been Removed From A Pvp Queue Because You Changed Your Faction.
        BattlegroundJoinFailed = 35,       // Join As A Group Failed
        BattlegroundDupeQueue = 43,       // Someone In Your Group Is Already Queued For That.
        BattlegroundJoinNoValidSpecForRole = 44,       // Role Check Failed Because One Of Your Party Members Selected An Invalid Role.
        BattlegroundJoinRespec = 45,       // You Have Been Removed From A Pvp Queue Because Your Specialization Changed.
        AlreadyUsingLfgList = 46,       // You Can'T Do That While Using Premade Groups.
        BattlegroundJoinMustCompleteQuest = 47,       // You Have Been Removed From A Pvp Queue Because Someone Is Missing Required Quest Completion.
        BattlergoundRestrictedAccount = 48,       // Free Trial Accounts Cannot Perform That Action
        BattlegroundJoinMercenary = 49,       // Cannot Join As A Group Unless All The Members Of Your Party Are Flagged As A Mercenary.
        BattlegroundJoinTooManyHealers = 51,       // You Can Not Enter This Bracket Of Arena With More Than One Healer. / You Can Not Enter A Rated Battleground With More Than Three Healers.
        BattlegroundJoinTooManyTanks = 52,       // You Can Not Enter This Bracket Of Arena With More Than One Tank.
        BattlegroundJoinTooManyDamage = 53,       // You Can Not Enter This Bracket Of Arena With More Than Two Damage Dealers.
        GroupJoinBattlegroundDead = 57,       // You Cannot Join The Battleground Because You Or One Of Your Party Members Is Dead.
        BattlegroundJoinRequiresLevel = 58,       // Tournament Rules Requires All Participants To Be Max Level.
        BattlegroundJoinDisqualified = 59,       // %S Has Been Disqualified From Ranked Play In This Bracket.
        ArenaExpiredCais = 60,       // You May Not Queue While One Or More Of Your Team Members Is Under The Effect Of Restricted Play.
        SoloShuffleWargameGroupSize = 64,       // Exactly 6 Non-Spectator Players Must Be Present To Begin A Solo Shuffle Wargame.
        SoloShuffleWargameGroupComp = 65,       // Exactly 4 Dps, And Either 2 Tanks Or 2 Healers, Must Be Present To Begin A Solo Shuffle Wargame.
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

    public enum BattlegroundCapturePointState
    {
        Neutral = 1,
        ContestedHorde = 2,
        ContestedAlliance = 3,
        HordeCaptured = 4,
        AllianceCaptured = 5
    }

    public enum PVPMatchState : byte
    {
        Waiting = 0,
        StartUp = 1,
        Engaged = 2,
        PostRound = 3,
        Inactive = 4,
        Complete = 5
    }
}
