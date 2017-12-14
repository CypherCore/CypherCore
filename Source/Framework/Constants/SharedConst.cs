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
    public class SharedConst
    {
        /// <summary>
        /// CliDB Const
        /// </summary>
        public const int GTMaxLevel = 100; // All Gt* DBC store data for 100 levels, some by 100 per class/race
        public const int GTMaxRating = 32; // gtOCTClassCombatRatingScalar.dbc stores data for 32 ratings, look at MAX_COMBAT_RATING for real used amount
        public const int ReputationCap = 42999;
        public const int ReputationBottom = -42000;
        public const int MaxMailItems = 12;
        public const int MaxDeclinedNameCases = 5;
        public const int MaxHolidayDurations = 10;
        public const int MaxHolidayDates = 16;
        public const int MaxHolidayFlags = 10;
        public const int DefaultMaxLevel = 110;
        public const int MaxLevel = 110;
        public const int StrongMaxLevel = 255;
        public const int MaxOverrideSpell = 10;
        public const int MaxWorldMapOverlayArea = 4;
        public const int MaxMountCapabilities = 24;
        public const int MaxLockCase = 8;

        /// <summary>
        /// BattlePets Const
        /// </summary>
        public const int MaxPetBattleSlots = 3;
        public const int MaxBattlePetsPerSpecies = 3;
        public const int BattlePetCageItemId = 82800;
        public const int DefaultSummonBattlePetSpell = 118301;

        /// <summary>
        /// Lfg Const
        /// </summary>
        public const uint LFGTimeRolecheck = 45;
        public const uint LFGTimeBoot = 120;
        public const uint LFGTimeProposal = 45;
        public const uint LFGQueueUpdateInterval = 15 * Time.InMilliseconds;
        public const uint LFGSpellDungeonCooldown = 71328;
        public const uint LFGSpellDungeonDeserter = 71041;
        public const uint LFGSpellLuckOfTheDraw = 72221;
        public const uint LFGKickVotesNeeded = 3;
        public const byte LFGMaxKicks = 3;
        public const int LFGTanksNeeded = 1;
        public const int LFGHealersNeeded = 1;
        public const int LFGDPSNeeded = 3;


        /// <summary>
        /// Loot Const
        /// </summary>
        public const int MaxNRLootItems = 16;
        public const int MaxNRQuestItems = 32;

        /// <summary>
        /// Movement Const
        /// </summary>
        public const double gravity = 19.29110527038574;
        public const float terminalVelocity = 60.148003f;
        public const float terminalSafefallVelocity = 7.0f;
        public const float terminal_length = (float)((terminalVelocity * terminalVelocity) / (2.0f * gravity));
        public const float terminal_safeFall_length = (float)((terminalSafefallVelocity * terminalSafefallVelocity) / (2.0f * gravity));
        public const float terminal_fallTime = (float)(terminalVelocity / gravity); // the time that needed to reach terminalVelocity
        public const float terminal_safeFall_fallTime = (float)(terminalSafefallVelocity / gravity); // the time that needed to reach terminalVelocity with safefall


        /// <summary>
        /// Vehicle Const
        /// </summary>
        public const int MaxSpellVehicle = 6;
        public const int VehicleSpellRideHardcoded = 46598;
        public const int VehicleSpellParachute = 45472;

        /// <summary>
        /// Quest Const
        /// </summary>
        public const int MaxQuestLogSize = 25;
        public const int MaxQuestCounts = 24;

        public const int QuestItemDropCount = 4;
        public const int QuestRewardChoicesCount = 6;
        public const int QuestRewardItemCount = 4;
        public const int QuestDeplinkCount = 10;
        public const int QuestRewardReputationsCount = 5;
        public const int QuestEmoteCount = 4;
        public const int QuestRewardCurrencyCount = 4;
        public const int QuestRewardDisplaySpellCount = 3;

        /// <summary>
        /// Smart AI Const
        /// </summary>
        public const int SmartEventParamCount = 4;
        public const int SmartActionParamCount = 6;
        public const uint SmartSummonCounter = 0xFFFFFF;
        public const uint SmartEscortTargets = 0xFFFFFF;

        /// <summary>
        /// BGs / Arena Const
        /// </summary>
        public const int BGTeamsCount = 2;
        public const uint CountOfPlayersToAverageWaitTime = 10;
        public const uint MaxPlayerBGQueues = 2;
        public const uint BGAwardArenaPointsMinLevel = 71;
        public const int ArenaTimeLimitPointsLoss = -16;
        public const int MaxArenaSlot = 3;

        /// <summary>
        /// Void Storage Const
        /// </summary>
        public const uint VoidStorageUnlockCost = 100 * MoneyConstants.Gold;
        public const uint VoidStorageStoreItemCost = 10 * MoneyConstants.Gold;
        public const uint VoidStorageMaxDeposit = 9;
        public const uint VoidStorageMaxWithdraw = 9;
        public const byte VoidStorageMaxSlot = 160;

        /// <summary>
        /// Misc Const
        /// </summary>
        public const uint CalendarMaxInvites = 100;
        public const uint CalendarDefaultResponseTime = 946684800; // 01/01/2000 00:00:00
        public const LocaleConstant DefaultLocale = LocaleConstant.enUS;
        public const int MaxAccountTutorialValues = 8;
        public const int MinAuctionTime = (12 * Time.Hour);
        public const int MaxConditionTargets = 3;

        /// <summary>
        /// Unit Const
        /// </summary>
        public const float BaseMinDamage = 1.0f;
        public const float BaseMaxDamage = 2.0f;
        public const int BaseAttackTime = 2000;
        public const int MaxSummonSlot = 7;
        public const int MaxTotemSlot = 5;
        public const int MaxGameObjectSlot = 4;
        public const float MaxAggroRadius = 45.0f;  // yards
        public const int MaxAggroResetTime = 10;
        public const int MaxVehicleSeats = 8;
        public const int AttackDisplayDelay = 200;
        public const float MaxPlayerStealthDetectRange = 30.0f;               // max distance for detection targets by player
        public const uint MaxEquipmentItems = 3;

        /// <summary>
        /// Creature Const
        /// </summary>
        public const int MaxGossipMenuItems = 64;                            // client supported items unknown, but provided number must be enough
        public const int DefaultGossipMessage = 0xFFFFFF;
        public const int MaxGossipTextEmotes = 3;
        public const int MaxNpcTextOptions = 8;
        public const int MaxCreatureBaseHp = 4;
        public const int MaxCreatureSpells = 8;
        public const byte MaxVendorItems = 150;
        public const int CreatureAttackRangeZ = 3;
        public const int MaxCreatureKillCredit = 2;
        public const int MaxCreatureDifficulties = 3;
        public const int MaxCreatureSpellDataSlots = 4;
        public const int MaxCreatureNames = 4;
        public const int MaxCreatureModelIds = 4;
        public const int MaxTrainerspellAbilityReqs = 3;
        public const int CreatureRegenInterval = 2 * Time.InMilliseconds;
        public const int CreatureNoPathEvadeTime = 5 * Time.InMilliseconds;
        public const int PetFocusRegenInterval = 4 * Time.InMilliseconds;
        public const int BoundaryVisualizeCreature = 15425;
        public const float BoundaryVisualizeCreatureScale = 0.25f;
        public const int BoundaryVisualizeStepSize = 1;
        public const int BoundaryVisualizeFailsafeLimit = 750;
        public const int BoundaryVisualizeSpawnHeight = 5;

        /// <summary>
        /// GameObject Const
        /// </summary>
        public const int MaxGOData = 33;
        public const uint MaxTransportStopFrames = 9;

        /// <summary>
        /// AreaTrigger Const
        /// </summary>
        public const int MaxAreatriggerEntityData = 6;
        public const int MaxAreatriggerScale = 7;

        /// <summary>
        /// Pet Const
        /// </summary>
        public const int MaxPetStables = 4;
        public const float PetFollowDist = 1.0f;
        public const float PetFollowAngle = MathFunctions.PiOver2;
        public const int MaxSpellCharm = 4;
        public const int ActionBarIndexStart = 0;
        public const byte ActionBarIndexPetSpellStart = 3;
        public const int ActionBarIndexPetSpellEnd = 7;
        public const int ActionBarIndexEnd = 10;
        public const int MaxSpellControlBar = 10;
        public const int MaxPetTalentRank = 3;
        public const int ActionBarIndexMax = (ActionBarIndexEnd - ActionBarIndexStart);


        /// <summary>
        /// Object Const
        /// </summary>
        public const float DefaultWorldObjectSize = 0.388999998569489f;      // player size, also currently used (correctly?) for any non Unit world objects
        public const float AttackDistance = 5.0f;
        public const float DefaultCombatReach = 1.5f;
        public const float MinMeleeReach = 2.0f;
        public const float NominalMeleeRange = 5.0f;
        public const float MeleeRange = NominalMeleeRange - MinMeleeReach * 2; //center to center for players
        public const float InspectDistance = 28.0f;
        public const float ContactDistance = 0.5f;
        public const float InteractionDistance = 5.0f;
        public const float MaxVisibilityDistance = MapConst.SizeofGrids;        // max distance for visible objects
        public const float SightRangeUnit = 50.0f;
        public const float DefaultVisibilityDistance = 90.0f;                  // default visible distance, 90 yards on continents
        public const float DefaultVisibilityInstance = 170.0f;                 // default visible distance in instances, 170 yards
        public const float DefaultVisibilityBGAreans = 533.0f;             // default visible distance in BG/Arenas, roughly 533 yards
        public const int DefaultVisibilityNotifyPeriod = 1000;

        public const int WorldTrigger = 12999;

        public static float[] baseMoveSpeed =
        {
            2.5f,                  // MOVE_WALK
            7.0f,                  // MOVE_RUN
            4.5f,                  // MOVE_RUN_BACK
            4.722222f,             // MOVE_SWIM
            2.5f,                  // MOVE_SWIM_BACK
            3.141594f,             // MOVE_TURN_RATE
            7.0f,                  // MOVE_FLIGHT
            4.5f,                  // MOVE_FLIGHT_BACK
            3.14f                  // MOVE_PITCH_RATE
        };

        public static float[] playerBaseMoveSpeed =
        {
            2.5f,                  // MOVE_WALK
            7.0f,                  // MOVE_RUN
            4.5f,                  // MOVE_RUN_BACK
            4.722222f,             // MOVE_SWIM
            2.5f,                  // MOVE_SWIM_BACK
            3.141594f,             // MOVE_TURN_RATE
            7.0f,                  // MOVE_FLIGHT
            4.5f,                  // MOVE_FLIGHT_BACK
            3.14f                  // MOVE_PITCH_RATE
        };

        //Todo move these else where
        /// <summary>
        /// Method Const
        /// </summary>
        public static SpellSchools GetFirstSchoolInMask(SpellSchoolMask mask)
        {
            for (SpellSchools i = 0; i < SpellSchools.Max; ++i)
                if (mask.HasAnyFlag((SpellSchoolMask)(1 << (int)i)))
                    return i;

            return SpellSchools.Normal;
        }
        public static SkillType SkillByQuestSort(int sort)
        {
            switch ((QuestSort)sort)
            {
                case QuestSort.Herbalism:
                    return SkillType.Herbalism;
                case QuestSort.Fishing:
                    return SkillType.Fishing;
                case QuestSort.Blacksmithing:
                    return SkillType.Blacksmithing;
                case QuestSort.Alchemy:
                    return SkillType.Alchemy;
                case QuestSort.Leatherworking:
                    return SkillType.Leatherworking;
                case QuestSort.Engineering:
                    return SkillType.Engineering;
                case QuestSort.Tailoring:
                    return SkillType.Tailoring;
                case QuestSort.Cooking:
                    return SkillType.Cooking;
                case QuestSort.FirstAid:
                    return SkillType.FirstAid;
                case QuestSort.Jewelcrafting:
                    return SkillType.Jewelcrafting;
                case QuestSort.Inscription:
                    return SkillType.Inscription;
                case QuestSort.Archaeology:
                    return SkillType.Archaeology;
            }
            return SkillType.None;
        }
        public static SkillType SkillByLockType(LockType locktype)
        {
            switch (locktype)
            {
                case LockType.Herbalism:
                    return SkillType.Herbalism;
                case LockType.Mining:
                    return SkillType.Mining;
                case LockType.Fishing:
                    return SkillType.Fishing;
                case LockType.Inscription:
                    return SkillType.Inscription;
                case LockType.Archaelogy:
                    return SkillType.Archaeology;
                case LockType.LumberMill:
                    return SkillType.Logging;
            }
            return SkillType.None;
        }
    }

    public struct PhaseMasks
    {
        public const uint Normal = 0x00000001;
        public const uint Anywhere = 0xFFFFFFFF;
    }

    public enum LocaleConstant
    {
        enUS = 0,
        koKR = 1,
        frFR = 2,
        deDE = 3,
        zhCN = 4,
        zhTW = 5,
        esES = 6,
        esMX = 7,
        ruRU = 8,
        None = 9,
        ptBR = 10,
        itIT = 11,
        Total = 12,

        Max = 11,
        OldTotal = 9// @todo convert in simple system
    }

    public enum ComparisionType
    {
        EQ = 0,
        High,
        Low,
        HighEQ,
        LowEQ,
        Max
    }

    public enum XPColorChar
    {
        Red,
        Orange,
        Yellow,
        Green,
        Gray
    }
    public enum ContentLevels
    {
        Content_1_60 = 0,
        Content_61_70 = 1,
        Content_71_80 = 2,
        Content_81_85 = 3,
        Max
    }

    public struct TeamId
    {
        public const int Alliance = 0;
        public const int Horde = 1;
        public const int Neutral = 2;
    }

    public enum Team
    {
        Horde = 67,
        Alliance = 469,
        //TEAM_STEAMWHEEDLE_CARTEL = 169,                       // not used in code
        //TEAM_ALLIANCE_FORCES     = 891,
        //TEAM_HORDE_FORCES        = 892,
        //TEAM_SANCTUARY           = 936,
        //TEAM_OUTLAND             = 980,
        Other = 0                            // if ReputationListId > 0 && Flags != FACTION_FLAG_TEAM_HEADER
    }

    public enum FactionMasks
    {
        Player = 1,                              // any player
        Alliance = 2,                              // player or creature from alliance team
        Horde = 4,                              // player or creature from horde team
        Monster = 8                               // aggressive creature from monster team
        // if none flags set then non-aggressive creature
    }
    public enum FactionTemplateFlags
    {
        EnemySpar = 0x20,   // guessed, sparring with enemies?
        PVP = 0x800,   // flagged for PvP
        ContestedGuard = 0x1000,   // faction will attack players that were involved in PvP combats
        HostileByDefault = 0x2000
    }
    public enum ReputationRank
    {
        None = -1,
        Hated = 0,
        Hostile = 1,
        Unfriendly = 2,
        Neutral = 3,
        Friendly = 4,
        Honored = 5,
        Revered = 6,
        Exalted = 7,
        Max = 8,
        Min = Hated
    }
    public enum ReputationSource
    {
        Kill,
        Quest,
        DailyQuest,
        WeeklyQuest,
        MonthlyQuest,
        RepeatableQuest,
        Spell
    }
    public enum FactionFlags
    {
        None = 0x00,                 // no faction flag
        Visible = 0x01,                 // makes visible in client (set or can be set at interaction with target of this faction)
        AtWar = 0x02,                 // enable AtWar-button in client. player controlled (except opposition team always war state), Flag only set on initial creation
        Hidden = 0x04,                 // hidden faction from reputation pane in client (player can gain reputation, but this update not sent to client)
        InvisibleForced = 0x08,                 // always overwrite FACTION_FLAG_VISIBLE and hide faction in rep.list, used for hide opposite team factions
        PeaceForced = 0x10,                 // always overwrite FACTION_FLAG_AT_WAR, used for prevent war with own team factions
        Inactive = 0x20,                 // player controlled, state stored in characters.data (CMSG_SET_FACTION_INACTIVE)
        Rival = 0x40,                 // flag for the two competing outland factions
        Special = 0x80                  // horde and alliance home cities and their northrend allies have this flag
    }

    public enum Gender : sbyte
    {
        Unknown = -1,
        Male = 0,
        Female = 1,
        None = 2
    }

    public enum Class
    {
        None = 0,
        Warrior = 1,
        Paladin = 2,
        Hunter = 3,
        Rogue = 4,
        Priest = 5,
        Deathknight = 6,
        Shaman = 7,
        Mage = 8,
        Warlock = 9,
        Monk = 10,
        Druid = 11,
        DemonHunter = 12,
        Max = 13,

        ClassMaskAllPlayable = ((1 << (Warrior - 1)) | (1 << (Paladin - 1)) | (1 << (Hunter - 1)) |
            (1 << (Rogue - 1)) | (1 << (Priest - 1)) | (1 << (Deathknight - 1)) | (1 << (Shaman - 1)) |
            (1 << (Mage - 1)) | (1 << (Warlock - 1)) | (1 << (Monk - 1)) | (1 << (Druid - 1)) | (1 << (DemonHunter - 1))),

        ClassMaskAllCreatures = ((1 << (Warrior - 1)) | (1 << (Paladin - 1)) | (1 << (Rogue - 1)) | (1 << (Mage - 1))),

        ClassMaskWandUsers = ((1 << (Priest - 1)) | (1 << (Mage - 1)) | (1 << (Warlock - 1)))
    }

    public enum Race
    {
        None = 0,
        Human = 1,
        Orc = 2,
        Dwarf = 3,
        NightElf = 4,
        Undead = 5,
        Tauren = 6,
        Gnome = 7,
        Troll = 8,
        Goblin = 9,
        BloodElf = 10,
        Draenei = 11,
        //FelOrc = 12,
        //Naga = 13,
        //Broken = 14,
        //Skeleton = 15,
        //Vrykul = 16,
        //Tuskarr = 17,
        //ForestTroll = 18,
        //Taunka = 19,
        //NorthrendSkeleton = 20,
        //IceTroll = 21,
        Worgen = 22,
        //HumanGilneas = 23,
        PandarenNeutral = 24,
        PandarenAlliance = 25,
        PandarenHorde = 26,
        Max = 27,

        RaceMaskAllPlayable = ((1 << (Human - 1)) | (1 << (Orc - 1)) | (1 << (Dwarf - 1)) | (1 << (NightElf - 1)) | (1 << (Undead - 1))
            | (1 << (Tauren - 1)) | (1 << (Gnome - 1)) | (1 << (Troll - 1)) | (1 << (BloodElf - 1)) | (1 << (Draenei - 1))
            | (1 << (Goblin - 1)) | (1 << (Worgen - 1)) | (1 << (PandarenNeutral - 1)) | (1 << (PandarenAlliance - 1)) | (1 << (PandarenHorde - 1))),

        RaceMaskAlliance = ((1 << (Human - 1)) | (1 << (Dwarf - 1)) | (1 << (NightElf - 1)) |
            (1 << (Gnome - 1)) | (1 << (Draenei - 1)) | (1 << (Worgen - 1)) | (1 << (PandarenAlliance - 1))),

        RaceMaskHorde = RaceMaskAllPlayable & ~RaceMaskAlliance
    }
    public enum Expansion
    {
        LevelCurrent = -1,
        Classic = 0,
        BurningCrusade = 1,
        WrathOfTheLichKing = 2,
        Cataclysm = 3,
        MistsOfPandaria = 4,
        WarlordsOfDraenor = 5,
        Legion = 6,
        Max
    }
    public enum PowerType : byte
    {
        Mana = 0,
        Rage = 1,
        Focus = 2,
        Energy = 3,
        ComboPoints = 4,
        Runes = 5,
        RunicPower = 6,
        SoulShards = 7,
        LunarPower = 8,
        HolyPower = 9,
        AlternatePower = 10,           // Used in some quests
        Maelstrom = 11,
        Chi = 12,
        Insanity = 13,
        BurningEmbers = 14,
        DemonicFury = 15,
        ArcaneCharges = 16,
        Fury = 17,
        Pain = 18,
        Max = 19,
        All = 127,          // default for class?
        Health = 0xFE,    // (-2 as signed value)
        MaxPerClass = 6
    }

    public enum Stats
    {
        Strength = 0,
        Agility = 1,
        Stamina = 2,
        Intellect = 3,
        Max = 4
    }

    public enum TrainerType
    {
        None = 0,
        Talent = 1,
        Tradeskills = 2,
        Pets = 3
    }
    public enum TrainerSpellState
    {
        Known = 0,
        Available = 1,
        Unavailable = 2,
    }

    public enum TrainerFailReason
    {
        Unavailable = 0,
        NotEnoughMoney = 1
    }

    public enum ChatMsg : uint
    {
        Addon = 0xffffffff, // -1
        System = 0x00,
        Say = 0x01,
        Party = 0x02,
        Raid = 0x03,
        Guild = 0x04,
        Officer = 0x05,
        Yell = 0x06,
        Whisper = 0x07,
        WhisperForeign = 0x08,
        WhisperInform = 0x09,
        Emote = 0x0a,
        TextEmote = 0x0b,
        MonsterSay = 0x0c,
        MonsterParty = 0x0d,
        MonsterYell = 0x0e,
        MonsterWhisper = 0x0f,
        MonsterEmote = 0x10,
        Channel = 0x11,
        ChannelJoin = 0x12,
        ChannelLeave = 0x13,
        ChannelList = 0x14,
        ChannelNotice = 0x15,
        ChannelNoticeUser = 0x16,
        Afk = 0x17,
        Dnd = 0x18,
        Ignored = 0x19,
        Skill = 0x1a,
        Loot = 0x1b,
        Money = 0x1c,
        Opening = 0x1d,
        Tradeskills = 0x1e,
        PetInfo = 0x1f,
        CombatMiscInfo = 0x20,
        CombatXpGain = 0x21,
        CombatHonorGain = 0x22,
        CombatFactionChange = 0x23,
        BgSystemNeutral = 0x24,
        BgSystemAlliance = 0x25,
        BgSystemHorde = 0x26,
        RaidLeader = 0x27,
        RaidWarning = 0x28,
        RaidBossEmote = 0x29,
        RaidBossWhisper = 0x2a,
        Filtered = 0x2b,
        Restricted = 0x2c,
        Battlenet = 0x2d,
        Achievement = 0x2e,
        GuildAchievement = 0x2f,
        ArenaPoints = 0x30,
        PartyLeader = 0x31,
        Targeticons = 0x32,
        BnWhisper = 0x33,
        BnWhisperInform = 0x34,
        BnConversation = 0x35,
        BnConversationNotice = 0x36,
        BnConversationList = 0x37,
        BnInlineToastAlert = 0x38,
        BnInlineToastBroadcast = 0x39,
        BnInlineToastBroadcastInform = 0x3a,
        BnInlineToastConversation = 0x3b,
        BnWhisperPlayerOffline = 0x3c,
        CombatGuildXpGain = 0x3d,
        Currency = 0x3e,
        QuestBossEmote = 0x3f,
        PetBattleCombatLog = 0x40,
        PetBattleInfo = 0x41,
        InstanceChat = 0x42,
        InstanceChatLeader = 0x43,
        Max = 0x44,
    }

    public enum ChatRestrictionType
    {
        Restricted = 0,
        Throttled = 1,
        Squelched = 2,
        YellRestricted = 3,
        RaidRestricted = 4
    }

    public enum Difficulty : byte
    {
        None = 0,
        Normal = 1,
        Heroic = 2,
        Raid10N = 3,
        Raid25N = 4,
        Raid10HC = 5,
        Raid25HC = 6,
        LFR = 7,
        MythicKeystone = 8,
        Raid40 = 9,
        Scenario3ManHC = 11,
        Scenario3ManN = 12,
        NormalRaid = 14,
        HeroicRaid = 15,
        MythicRaid = 16,
        LFRNew = 17,
        EventRaid = 18,
        EventDungeon = 19,
        EventScenario = 20,
        Mythic = 23,
        Timewalking = 24,
        WorldPvPScenario = 25,
        Scenario5ManN = 26,
        Scenario20ManN = 27,
        PvEvPScenario = 29,
        EventScenario6 = 30,
        WorldPvPScenario2 = 32,
        TimewalkingRaid = 33,
        Pvp = 34,

        Max
    }

    public enum DifficultyFlags : byte
    {
        Heroic = 0x01,
        Default = 0x02,
        CanSelect = 0x04, // Player can select this difficulty in dropdown menu
        ChallengeMode = 0x08,

        Legacy = 0x20,
        DisplayHeroic = 0x40, // Controls icon displayed on minimap when inside the instance
        DisplayMythic = 0x80  // Controls icon displayed on minimap when inside the instance
    }

    public enum SpawnMask
    {
        Continent = (1 << Difficulty.None), // Any Maps Without Spawn Modes

        DungeonNormal = (1 << Difficulty.Normal),
        DungeonHeroic = (1 << Difficulty.Heroic),
        DungeonAll = (DungeonNormal | DungeonHeroic),

        Raid10Normal = (1 << Difficulty.Raid10N),
        Raid25Normal = (1 << Difficulty.Raid25N),
        RaidNormalAll = (Raid10Normal | Raid25Normal),

        Raid10Heroic = (1 << Difficulty.Raid10HC),
        Raid25Heroic = (1 << Difficulty.Raid25HC),
        RaidHeroicAll = (Raid10Heroic | Raid25Heroic),

        RaidAll = (RaidNormalAll | RaidHeroicAll)
    }

    public enum MapFlags
    {
        CanToggleDifficulty = 0x0100,
        FlexLocking = 0x8000, // All difficulties share completed encounters lock, not bound to a single instance id
                              // heroic difficulty flag overrides it and uses instance id bind
        Garrison = 0x4000000
    }

    public enum WorldStates
    {
        CurrencyResetTime = 20001,          // Next currency reset time
        WeeklyQuestResetTime = 20002,       // Next weekly reset time
        BGDailyResetTime = 20003,           // Next daily BG reset time
        CleaningFlags = 20004,              // Cleaning Flags
        GuildDailyResetTime = 20006,        // Next guild cap reset time
        MonthlyQuestResetTime = 20007,      // Next monthly reset time
        // Cata specific custom worldstates
        GuildWeeklyResetTime = 20050,       // Next guild week reset time
    }

    // values based at Holidays.dbc
    public enum HolidayIds
    {
        None = 0,

        FireworksSpectacular = 62,
        FeastOfWinterVeil = 141,
        NobleGarden = 181,
        ChildrensWeek = 201,
        CallToArmsAv = 283,
        CallToArmsWs = 284,
        CallToArmsAb = 285,
        FishingExtravaganza = 301,
        HarvestFestival = 321,
        HallowsEnd = 324,
        LunarFestival = 327,
        LoveIsInTheAir = 335,
        FireFestival = 341,
        CallToArmsEy = 353,
        Brewfest = 372,
        DarkmoonFaireElwynn = 374,
        DarkmoonFaireThunder = 375,
        DarkmoonFaireShattrath = 376,
        PiratesDay = 398,
        CallToArmsSa = 400,
        PilgrimsBounty = 404,
        WotlkLaunch = 406,
        DayOfDead = 409,
        CallToArmsIc = 420,
        //LoveIsInTheAir = 423,
        KaluAkFishingDerby = 424,
        CallToArmsBfg = 435,
        CallToArmsTp = 436,
        RatedBg15Vs15 = 442,
        RatedBg25Vs25 = 443,
        Anniversary7Years = 467,
        DarkmoonFaireTerokkar = 479,
        Anniversary8Years = 484,
        CallToArmsSm = 488,
        CallToArmsTk = 489,
        //CallToArmsAv        = 490,
        //CallToArmsAb        = 491,
        //CallToArmsEy        = 492,
        //CallToArmsAv        = 493,
        //CallToArmsSm        = 494,
        //CallToArmsSa        = 495,
        //CallToArmsTk        = 496,
        //CallToArmsBfg       = 497,
        //CallToArmsTp        = 498,
        //CallToArmsWs        = 499,
        Anniversary9Years = 509,
        Anniversary10Years = 514,
        CallToArmsDg = 515,
        //CallToArmsDg        = 516
        TimewalkingOutlands = 559,
        ApexisBonusEvent = 560,
        ArenaSkirmishBonusEvent = 561,
        TimewalkingNorthrend = 562,
        BattlegroundBonusEvent = 563,
        DraenorDungeonEvent = 564,
        PetBattleBonusEvent = 565,
        Anniversary11Years = 566,
        TimewalkingCataclysm = 587
    }

    public enum VoidTransferError
    {
        Ok = 0,
        InternalError1 = 1,
        InternalError2 = 2,
        Full = 3,
        InternalError3 = 4,
        InternalError4 = 5,
        NotEnoughMoney = 6,
        InventoryFull = 7,
        ItemInvalid = 8,
        TransferUnknown = 9
    }

    public enum eScriptFlags
    {
        // Talk Flags
        TalkUsePlayer = 0x1,

        // Emote Flags
        EmoteUseState = 0x1,

        // Teleportto Flags
        TeleportUseCreature = 0x1,

        // Killcredit Flags
        KillcreditRewardGroup = 0x1,

        // Removeaura Flags
        RemoveauraReverse = 0x1,

        // Castspell Flags
        CastspellSourceToTarget = 0,
        CastspellSourceToSource = 1,
        CastspellTargetToTarget = 2,
        CastspellTargetToSource = 3,
        CastspellSearchCreature = 4,
        CastspellTriggered = 0x1,

        // Playsound Flags
        PlaysoundTargetPlayer = 0x1,
        PlaysoundDistanceSound = 0x2,

        // Orientation Flags
        OrientationFaceTarget = 0x1
    }

    public enum ScriptsType
    {
        First = 1,

        Spell = First,
        Event,
        Waypoint,

        Last
    }

    // Db Scripting Commands
    public enum ScriptCommands
    {
        Talk = 0,                // Source/Target = Creature, Target = Any, Datalong = Talk Type (0=Say, 1=Whisper, 2=Yell, 3=Emote Text, 4=Boss Emote Text), Datalong2 & 1 = Player Talk (Instead Of Creature), Dataint = StringId
        Emote = 1,                // Source/Target = Creature, Datalong = Emote Id, Datalong2 = 0: Set Emote State; > 0: Play Emote State
        FieldSet = 2,                // Source/Target = Creature, Datalong = Field Id, Datalog2 = Value
        MoveTo = 3,                // Source/Target = Creature, Datalong2 = Time To Reach, X/Y/Z = Destination
        FlagSet = 4,                // Source/Target = Creature, Datalong = Field Id, Datalog2 = Bitmask
        FlagRemove = 5,                // Source/Target = Creature, Datalong = Field Id, Datalog2 = Bitmask
        TeleportTo = 6,                // Source/Target = Creature/Player (See Datalong2), Datalong = MapId, Datalong2 = 0: Player; 1: Creature, X/Y/Z = Destination, O = Orientation
        QuestExplored = 7,                // Target/Source = Player, Target/Source = Go/Creature, Datalong = Quest Id, Datalong2 = Distance Or 0
        KillCredit = 8,                // Target/Source = Player, Datalong = Creature Entry, Datalong2 = 0: Personal Credit, 1: Group Credit
        RespawnGameobject = 9,                // Source = Worldobject (Summoner), Datalong = Go Guid, Datalong2 = Despawn Delay
        TempSummonCreature = 10,               // Source = Worldobject (Summoner), Datalong = Creature Entry, Datalong2 = Despawn Delay, X/Y/Z = Summon Position, O = Orientation
        OpenDoor = 11,               // Source = Unit, Datalong = Go Guid, Datalong2 = Reset Delay (Min 15)
        CloseDoor = 12,               // Source = Unit, Datalong = Go Guid, Datalong2 = Reset Delay (Min 15)
        ActivateObject = 13,               // Source = Unit, Target = Go
        RemoveAura = 14,               // Source (Datalong2 != 0) Or Target (Datalong2 == 0) = Unit, Datalong = Spell Id
        CastSpell = 15,               // Source And/Or Target = Unit, Datalong2 = Cast Direction (0: S.T 1: S.S 2: T.T 3: T.S 4: S.Creature With Dataint Entry), Dataint & 1 = Triggered Flag
        PlaySound = 16,               // Source = Worldobject, Target = None/Player, Datalong = Sound Id, Datalong2 (Bitmask: 0/1=Anyone/Player, 0/2=Without/With Distance Dependency, So 1|2 = 3 Is Target With Distance Dependency)
        CreateItem = 17,               // Target/Source = Player, Datalong = Item Entry, Datalong2 = Amount
        DespawnSelf = 18,               // Target/Source = Creature, Datalong = Despawn Delay

        LoadPath = 20,               // Source = Unit, Datalong = Path Id, Datalong2 = Is Repeatable
        CallscriptToUnit = 21,               // Source = Worldobject (If Present Used As A Search Center), Datalong = Script Id, Datalong2 = Unit Lowguid, Dataint = Script Table To Use (See Scriptstype)
        Kill = 22,               // Source/Target = Creature, Dataint = Remove Corpse Attribute

        // Cyphercore Only
        Orientation = 30,               // Source = Unit, Target (Datalong > 0) = Unit, Datalong = > 0 Turn Source To Face Target, O = Orientation
        Equip = 31,               // Soucre = Creature, Datalong = Equipment Id
        Model = 32,               // Source = Creature, Datalong = Model Id
        CloseGossip = 33,               // Source = Player
        Playmovie = 34,                // Source = Player, Datalong = Movie Id
        Movement = 35,               // Source = Creature, datalong = MovementType, datalong2 = MovementDistance (spawndist f.ex.), dataint = pathid
        PlayAnimkit = 36                // Source = Creature, datalong = AnimKit id
    }

    // Custom values
    public enum SpellClickUserTypes
    {
        Any = 0,
        Friend = 1,
        Raid = 2,
        Party = 3,
        Max = 4
    }

    public enum TokenResult
    {
        Success = 1,
        Disabled = 2,
        Other = 3,
        NoneForSale = 4,
        TooManyTokens = 5,
        SuccessNo = 6,
        TransactionInProgress = 7,
        AuctionableTokenOwned = 8,
        TrialRestricted = 9
    }

    public enum BanMode
    {
        Account,
        Character,
        IP
    }

    public enum BanReturn
    {
        Success,
        SyntaxError,
        Notfound
    }

    public enum WorldCfg
    {
        AccPasschangesec,
        AddonChannel,
        AhbotUpdateInterval,
        AllTaxiPaths,
        AllowGmGroup,
        AllowPlayerCommands,
        AllowTrackBothResources,
        AllowTwoSideInteractionAuction,
        AllowTwoSideInteractionCalendar,
        AllowTwoSideInteractionChannel,
        AllowTwoSideInteractionGroup,
        AllowTwoSideInteractionGuild,
        AllowTwoSideTrade,
        AlwaysMaxskill,
        ArenaLogExtendedInfo,
        ArenaMaxRatingDifference,
        ArenaQueueAnnouncerEnable,
        ArenaRatedUpdateTimer,
        ArenaRatingDiscardTimer,
        ArenaSeasonId,
        ArenaSeasonInProgress,
        ArenaStartMatchmakerRating,
        ArenaStartPersonalRating,
        ArenaStartRating,
        ArenaWinRatingModifier1,
        ArenaWinRatingModifier2,
        ArenaLoseRatingModifier,
        ArenaMatchmakerRatingModifier,
        AuctionGetallDelay,
        AuctionLevelReq,
        AuctionSearchDelay,
        AutoBroadcast,
        AutoBroadcastCenter,
        AutoBroadcastInterval,
        BasemapLoadGrids,
        BattlegroundCastDeserter,
        BattlegroundInvitationType,
        BattlegroundPremadeGroupWaitForMatch,
        BattlegroundPrematureFinishTimer,
        BattlegroundQueueAnnouncerEnable,
        BattlegroundQueueAnnouncerPlayeronly,
        BattlegroundReportAfk,
        BattlegroundStoreStatisticsEnable,
        BgRewardLoserHonorFirst,
        BgRewardLoserHonorLast,
        BgRewardWinnerConquestFirst,
        BgRewardWinnerConquestLast,
        BgRewardWinnerHonorFirst,
        BgRewardWinnerHonorLast,
        BgXpForKill,
        BlackmarketEnabled,
        BlackmarketMaxAuctions,
        BlackmarketUpdatePeriod,
        CalculateCreatureZoneAreaData,
        CalculateGameobjectZoneAreaData,
        CastUnstuck,
        CharacterCreatingDisabled,
        CharacterCreatingDisabledClassmask,
        CharacterCreatingDisabledRacemask,
        CharacterCreatingMinLevelForDemonHunter,
        CharactersPerAccount,
        CharactersPerRealm,
        ChardeleteDeathKnightMinLevel,
        ChardeleteDemonHunterMinLevel,
        ChardeleteKeepDays,
        ChardeleteMethod,
        ChardeleteMinLevel,
        CharterCostArena2v2,
        CharterCostArena3v3,
        CharterCostArena5v5,
        CharterCostGuild,
        ChatChannelLevelReq,
        ChatFakeMessagePreventing,
        ChatSayLevelReq,
        ChatStrictLinkCheckingKick,
        ChatStrictLinkCheckingSeverity,
        ChatWhisperLevelReq,
        ChatEmoteLevelReq,
        ChatYellLevelReq,
        ChatfloodMessageCount,
        ChatfloodMessageDelay,
        ChatfloodMuteTime,
        CleanCharacterDb,
        ClientCacheVersion,
        Compression,
        CorpseDecayElite,
        CorpseDecayNormal,
        CorpseDecayRare,
        CorpseDecayRareelite,
        CorpseDecayWorldboss,
        CreatureCheckInvalidPostion,
        CreatureFamilyAssistanceDelay,
        CreatureFamilyAssistanceRadius,
        CreatureFamilyFleeAssistanceRadius,
        CreatureFamilyFleeDelay,
        CreaturePickpocketRefill,
        CreatureStopForPlayer,
        CurrencyMaxApexisCrystals,
        CurrencyMaxJusticePoints,
        CurrencyResetDay,
        CurrencyResetHour,
        CurrencyResetInterval,
        CurrencyStartApexisCrystals,
        CurrencyStartJusticePoints,
        DailyQuestResetTimeHour,
        DbcEnforceItemAttributes,
        DeathBonesBgOrArena,
        DeathBonesWorld,
        DeathCorpseReclaimDelayPve,
        DeathCorpseReclaimDelayPvp,
        DeathSicknessLevel,
        DeclinedNamesUsed,
        DemonHuntersPerRealm,
        DetectPosCollision,
        DieCommandMode,
        DisableBreathing,
        DurabilityLossInPvp,
        EnableMmaps,
        EnableSinfoLogin,
        EventAnnounce,
        Expansion,
        FeatureSystemBpayStoreEnabled,
        FeatureSystemCharacterUndeleteCooldown,
        FeatureSystemCharacterUndeleteEnabled,
        ForceShutdownThreshold,
        GameobjectCheckInvalidPostion,
        GameType,
        GmChat,
        GmFreezeDuration,
        GmLevelInGmList,
        GmLevelInWhoList,
        GmLoginState,
        GmLowerSecurity,
        GmVisibleState,
        GmWhisperingTo,
        GridUnload,
        GroupVisibility,
        GroupXpDistance,
        GuildBankEventLogCount,
        GuildEventLogCount,
        GuildNewsLogCount,
        GuildResetHour,
        GuildSaveInterval,
        HonorAfterDuel,
        HotfixCacheVersion,
        InstanceIgnoreLevel,
        InstanceIgnoreRaid,
        InstancemapLoadGrids,
        InstanceResetTimeHour,
        InstanceUnloadDelay,
        InstancesResetAnnounce,
        InstantLogout,
        InstantTaxi,
        IntervalChangeweather,
        IntervalDisconnectTolerance,
        IntervalGridclean,
        IntervalLogUpdate,
        IntervalMapupdate,
        IntervalSave,
        IpBasedActionLogging,
        LfgOptionsmask,
        ListenRangeSay,
        ListenRangeTextemote,
        ListenRangeYell,
        LogdbClearinterval,
        LogdbCleartime,
        MailDeliveryDelay,
        MailLevelReq,
        MaxInstancesPerHour,
        MaxOverspeedPings,
        MaxPlayerLevel,
        MaxPrimaryTradeSkill,
        MaxRecruitAFriendBonusPlayerLevel,
        MaxRecruitAFriendBonusPlayerLevelDifference,
        MaxRecruitAFriendDistance,
        MaxResultsLookupCommands,
        MaxWho,
        MinCharterName,
        MinDualspecLevel,
        MinLevelStatSave,
        MinLogUpdate,
        MinPetName,
        MinPetitionSigns,
        MinPlayerName,
        NoGrayAggroAbove,
        NoGrayAggroBelow,
        NoResetTalentCost,
        Numthreads,
        OffhandCheckAtSpellUnlearn,
        PacketSpoofBanduration,
        PacketSpoofBanmode,
        PacketSpoofPolicy,
        PartyLevelReq,
        PdumpNoOverwrite,
        PdumpNoPaths,
        PersistentCharacterCleanFlags,
        PlayerAllowCommands,
        PortInstance,
        PortWorld,
        PreserveCustomChannelDuration,
        PreserveCustomChannels,
        PreventRenameCustomization,
        PvpTokenCount,
        PvpTokenEnable,
        PvpTokenId,
        PvpTokenMapType,
        QuestEnableQuestTracker,
        QuestHighLevelHideDiff,
        QuestIgnoreAutoAccept,
        QuestIgnoreAutoComplete,
        QuestIgnoreRaid,
        QuestLowLevelHideDiff,
        RandomBgResetHour,
        RateAuctionCut,
        RateAuctionDeposit,
        RateAuctionTime,
        RateCorpseDecayLooted,
        RateCreatureAggro,
        RateCreatureEliteEliteDamage,
        RateCreatureEliteEliteHp,
        RateCreatureEliteEliteSpelldamage,
        RateCreatureEliteRareDamage,
        RateCreatureEliteRareHp,
        RateCreatureEliteRareSpelldamage,
        RateCreatureEliteRareeliteDamage,
        RateCreatureEliteRareeliteHp,
        RateCreatureEliteRareeliteSpelldamage,
        RateCreatureEliteWorldbossDamage,
        RateCreatureEliteWorldbossHp,
        RateCreatureEliteWorldbossSpelldamage,
        RateCreatureNormalDamage,
        RateCreatureNormalHp,
        RateCreatureNormalSpelldamage,
        RateDamageFall,
        RateDropItemArtifact,
        RateDropItemEpic,
        RateDropItemLegendary,
        RateDropItemNormal,
        RateDropItemPoor,
        RateDropItemRare,
        RateDropItemReferenced,
        RateDropItemReferencedAmount,
        RateDropItemUncommon,
        RateDropMoney,
        RateDurabilityLossAbsorb,
        RateDurabilityLossBlock,
        RateDurabilityLossDamage,
        RateDurabilityLossOnDeath,
        RateDurabilityLossParry,
        RateHealth,
        RateHonor,
        RateInstanceResetTime,
        RateMoneyMaxLevelQuest,
        RateMoneyQuest,
        RateMovespeed,
        RatePowerArcaneCharges,
        RatePowerChi,
        RatePowerComboPointsLoss,
        RatePowerEnergy,
        RatePowerFocus,
        RatePowerFury,
        RatePowerHolyPower,
        RatePowerInsanity,
        RatePowerLunarPower,
        RatePowerMaelstrom,
        RatePowerMana,
        RatePowerPain,
        RatePowerRageIncome,
        RatePowerRageLoss,
        RatePowerRunicPowerIncome,
        RatePowerRunicPowerLoss,
        RatePowerSoulShards,
        RateRepaircost,
        RateReputationGain,
        RateReputationLowLevelKill,
        RateReputationLowLevelQuest,
        RateReputationRecruitAFriendBonus,
        RateRestIngame,
        RateRestOfflineInTavernOrCity,
        RateRestOfflineInWilderness,
        RateSkillDiscovery,
        RateTalent,
        RateTargetPosRecalculationRange,
        RateXpExplore,
        RateXpGuildModifier,
        RateXpKill,
        RateXpBgKill,
        RateXpQuest,
        RealmZone,
        ResetDuelCooldowns,
        ResetDuelHealthMana,
        RestrictedLfgChannel,
        SaveRespawnTimeImmediately,
        SessionAddDelay,
        ShowBanInWorld,
        ShowKickInWorld,
        ShowMuteInWorld,
        SightMonster,
        SkillChanceGreen,
        SkillChanceGrey,
        SkillChanceMiningSteps,
        SkillChanceOrange,
        SkillChanceSkinningSteps,
        SkillChanceYellow,
        SkillGainCrafting,
        SkillGainGathering,
        SkillMilling,
        SkillProspecting,
        SkipCinematics,
        SocketTimeouttime,
        StartAllExplored,
        StartAllRep,
        StartAllSpells,
        StartDeathKnightPlayerLevel,
        StartDemonHunterPlayerLevel,
        StartGmLevel,
        StartPlayerLevel,
        StartPlayerMoney,
        StatsLimitsBlock,
        StatsLimitsCrit,
        StatsLimitsDodge,
        StatsLimitsEnable,
        StatsLimitsParry,
        StatsSaveOnlyOnLogout,
        StrictCharterNames,
        StrictPetNames,
        StrictPlayerNames,
        SupportBugsEnabled,
        SupportComplaintsEnabled,
        SupportEnabled,
        SupportSuggestionsEnabled,
        SupportTicketsEnabled,
        TalentsInspecting,
        ThreatRadius,
        TradeLevelReq,
        UiQuestLevelsInDialogs,     // Should We Add Quest Levels To The Title In The Npc Dialogs?
        UptimeUpdate,
        VmapIndoorCheck,
        WardenClientBanDuration,
        WardenClientCheckHoldoff,
        WardenClientFailAction,
        WardenClientResponseDelay,
        WardenEnabled,
        WardenNumMemChecks,
        WardenNumOtherChecks,
        Weather,
        WintergraspBattletime,
        WintergraspEnable,
        WintergraspNobattletime,
        WintergraspPlrMax,
        WintergraspPlrMin,
        WintergraspPlrMinLvl,
        WintergraspRestartAfterCrash,
        WorldBossLevelDiff
    }

    public enum TimerType
    {
        Pvp = 0,
        ChallengerMode = 1
    }

    public enum GameError : uint
    {
        System = 0,
        InvFull = 1,
        BankFull = 2,
        CantEquipLevelI = 3,
        CantEquipSkill = 4,
        CantEquipEver = 5,
        CantEquipRank = 6,
        CantEquipRating = 7,
        CantEquipReputation = 8,
        ProficiencyNeeded = 9,
        WrongSlot = 10,
        CantEquipNeedTalent = 11,
        BagFull = 12,
        InternalBagError = 13,
        DestroyNonemptyBag = 14,
        BagInBag = 15,
        TooManySpecialBags = 16,
        TradeEquippedBag = 17,
        AmmoOnly = 18,
        NoSlotAvailable = 19,
        WrongBagType = 20,
        ItemMaxCount = 21,
        NotEquippable = 22,
        CantStack = 23,
        CantSwap = 24,
        SlotEmpty = 25,
        ItemNotFound = 26,
        TooFewToSplit = 27,
        SplitFailed = 28,
        NotABag = 29,
        NotOwner = 30,
        OnlyOneQuiver = 31,
        NoBankSlot = 32,
        NoBankHere = 33,
        ItemLocked = 34,
        TwoHandedEquipped = 35,
        VendorNotInterested = 36,
        VendorHatesYou = 37,
        VendorSoldOut = 38,
        VendorTooFar = 39,
        VendorDoesntBuy = 40,
        NotEnoughMoney = 41,
        ReceiveItemS = 42,
        DropBoundItem = 43,
        TradeBoundItem = 44,
        TradeQuestItem = 45,
        TradeTempEnchantBound = 46,
        TradeGroundItem = 47,
        TradeBag = 48,
        SpellFailedS = 49,
        ItemCooldown = 50,
        PotionCooldown = 51,
        FoodCooldown = 52,
        SpellCooldown = 53,
        AbilityCooldown = 54,
        SpellAlreadyKnownS = 55,
        PetSpellAlreadyKnownS = 56,
        ProficiencyGainedS = 57,
        SkillGainedS = 58,
        SkillUpSi = 59,
        LearnSpellS = 60,
        LearnAbilityS = 61,
        LearnPassiveS = 62,
        LearnRecipeS = 63,
        LearnCompanionS = 64,
        LearnMountS = 65,
        LearnToyS = 66,
        LearnHeirloomS = 67,
        LearnTransmogS = 68,
        CompletedTransmogSetS = 69,
        RevokeTransmogS = 70,
        InvitePlayerS = 71,
        InviteSelf = 72,
        InvitedToGroupSs = 73,
        InvitedAlreadyInGroupSs = 74,
        AlreadyInGroupS = 75,
        CrossRealmRaidInvite = 76,
        PlayerBusyS = 77,
        NewLeaderS = 78,
        NewLeaderYou = 79,
        NewGuideS = 80,
        NewGuideYou = 81,
        LeftGroupS = 82,
        LeftGroupYou = 83,
        GroupDisbanded = 84,
        DeclineGroupS = 85,
        JoinedGroupS = 86,
        UninviteYou = 87,
        BadPlayerNameS = 88,
        NotInGroup = 89,
        TargetNotInGroupS = 90,
        TargetNotInInstanceS = 91,
        NotInInstanceGroup = 92,
        GroupFull = 93,
        NotLeader = 94,
        PlayerDiedS = 95,
        GuildCreateS = 96,
        GuildInviteS = 97,
        InvitedToGuildSss = 98,
        AlreadyInGuildS = 99,
        AlreadyInvitedToGuildS = 100,
        InvitedToGuild = 101,
        AlreadyInGuild = 102,
        GuildAccept = 103,
        GuildDeclineS = 104,
        GuildDeclineAutoS = 105,
        GuildPermissions = 106,
        GuildJoinS = 107,
        GuildFounderS = 108,
        GuildPromoteSss = 109,
        GuildDemoteSs = 110,
        GuildDemoteSss = 111,
        GuildInviteSelf = 112,
        GuildQuitS = 113,
        GuildLeaveS = 114,
        GuildRemoveSs = 115,
        GuildRemoveSelf = 116,
        GuildDisbandS = 117,
        GuildDisbandSelf = 118,
        GuildLeaderS = 119,
        GuildLeaderSelf = 120,
        GuildPlayerNotFoundS = 121,
        GuildPlayerNotInGuildS = 122,
        GuildPlayerNotInGuild = 123,
        GuildCantPromoteS = 124,
        GuildCantDemoteS = 125,
        GuildNotInAGuild = 126,
        GuildInternal = 127,
        GuildLeaderIsS = 128,
        GuildLeaderChangedSs = 129,
        GuildDisbanded = 130,
        GuildNotAllied = 131,
        GuildLeaderLeave = 132,
        GuildRanksLocked = 133,
        GuildRankInUse = 134,
        GuildRankTooHighS = 135,
        GuildRankTooLowS = 136,
        GuildNameExistsS = 137,
        GuildWithdrawLimit = 138,
        GuildNotEnoughMoney = 139,
        GuildTooMuchMoney = 140,
        GuildBankConjuredItem = 141,
        GuildBankEquippedItem = 142,
        GuildBankBoundItem = 143,
        GuildBankQuestItem = 144,
        GuildBankWrappedItem = 145,
        GuildBankFull = 146,
        GuildBankWrongTab = 147,
        NoGuildCharter = 148,
        OutOfRange = 149,
        PlayerDead = 150,
        ClientLockedOut = 151,
        KilledByS = 152,
        LootLocked = 153,
        LootTooFar = 154,
        LootDidntKill = 155,
        LootBadFacing = 156,
        LootNotstanding = 157,
        LootStunned = 158,
        LootNoUi = 159,
        LootWhileInvulnerable = 160,
        NoLoot = 161,
        QuestAcceptedS = 162,
        QuestCompleteS = 163,
        QuestFailedS = 164,
        QuestFailedBagFullS = 165,
        QuestFailedMaxCountS = 166,
        QuestFailedLowLevel = 167,
        QuestFailedMissingItems = 168,
        QuestFailedWrongRace = 169,
        QuestFailedNotEnoughMoney = 170,
        QuestFailedExpansion = 171,
        QuestOnlyOneTimed = 172,
        QuestNeedPrereqs = 173,
        QuestNeedPrereqsCustom = 174,
        QuestAlreadyOn = 175,
        QuestAlreadyDone = 176,
        QuestAlreadyDoneDaily = 177,
        QuestHasInProgress = 178,
        QuestRewardExpI = 179,
        QuestRewardMoneyS = 180,
        QuestMustChoose = 181,
        QuestLogFull = 182,
        CombatDamageSsi = 183,
        InspectS = 184,
        CantUseItem = 185,
        CantUseItemInArena = 186,
        CantUseItemInRatedBattleground = 187,
        MustEquipItem = 188,
        PassiveAbility = 189,
        Hand2SkillNotFound = 190,
        NoAttackTarget = 191,
        InvalidAttackTarget = 192,
        AttackPvpTargetWhileUnflagged = 193,
        AttackStunned = 194,
        AttackPacified = 195,
        AttackMounted = 196,
        AttackFleeing = 197,
        AttackConfused = 198,
        AttackCharmed = 199,
        AttackDead = 200,
        AttackPreventedByMechanicS = 201,
        AttackChannel = 202,
        Taxisamenode = 203,
        Taxinosuchpath = 204,
        Taxiunspecifiedservererror = 205,
        Taxinotenoughmoney = 206,
        Taxitoofaraway = 207,
        Taxinovendornearby = 208,
        Taxinotvisited = 209,
        Taxiplayerbusy = 210,
        Taxiplayeralreadymounted = 211,
        Taxiplayershapeshifted = 212,
        Taxiplayermoving = 213,
        Taxinopaths = 214,
        Taxinoteligible = 215,
        Taxinotstanding = 216,
        NoReplyTarget = 217,
        GenericNoTarget = 218,
        InitiateTradeS = 219,
        TradeRequestS = 220,
        TradeBlockedS = 221,
        TradeTargetDead = 222,
        TradeTooFar = 223,
        TradeCancelled = 224,
        TradeComplete = 225,
        TradeBagFull = 226,
        TradeTargetBagFull = 227,
        TradeMaxCountExceeded = 228,
        TradeTargetMaxCountExceeded = 229,
        AlreadyTrading = 230,
        MountInvalidmountee = 231,
        MountToofaraway = 232,
        MountAlreadymounted = 233,
        MountNotmountable = 234,
        MountNotyourpet = 235,
        MountOther = 236,
        MountLooting = 237,
        MountRacecantmount = 238,
        MountShapeshifted = 239,
        MountNoFavorites = 240,
        DismountNopet = 241,
        DismountNotmounted = 242,
        DismountNotyourpet = 243,
        SpellFailedTotems = 244,
        SpellFailedReagents = 245,
        SpellFailedReagentsGeneric = 246,
        SpellFailedEquippedItem = 247,
        SpellFailedEquippedItemClassS = 248,
        SpellFailedShapeshiftFormS = 249,
        SpellFailedAnotherInProgress = 250,
        Badattackfacing = 251,
        Badattackpos = 252,
        ChestInUse = 253,
        UseCantOpen = 254,
        UseLocked = 255,
        DoorLocked = 256,
        ButtonLocked = 257,
        UseLockedWithItemS = 258,
        UseLockedWithSpellS = 259,
        UseLockedWithSpellKnownSi = 260,
        UseTooFar = 261,
        UseBadAngle = 262,
        UseObjectMoving = 263,
        UseSpellFocus = 264,
        UseDestroyed = 265,
        SetLootFreeforall = 266,
        SetLootRoundrobin = 267,
        SetLootMaster = 268,
        SetLootGroup = 269,
        SetLootThresholdS = 270,
        NewLootMasterS = 271,
        SpecifyMasterLooter = 272,
        LootSpecChangedS = 273,
        TameFailed = 274,
        ChatWhileDead = 275,
        ChatPlayerNotFoundS = 276,
        Newtaxipath = 277,
        NoPet = 278,
        Notyourpet = 279,
        PetNotRenameable = 280,
        QuestObjectiveCompleteS = 281,
        QuestUnknownComplete = 282,
        QuestAddKillSii = 283,
        QuestAddFoundSii = 284,
        QuestAddItemSii = 285,
        QuestAddPlayerKillSii = 286,
        Cannotcreatedirectory = 287,
        Cannotcreatefile = 288,
        PlayerWrongFaction = 289,
        PlayerIsNeutral = 290,
        BankslotFailedTooMany = 291,
        BankslotInsufficientFunds = 292,
        BankslotNotbanker = 293,
        FriendDbError = 294,
        FriendListFull = 295,
        FriendAddedS = 296,
        BattletagFriendAddedS = 297,
        FriendOnlineSs = 298,
        FriendOfflineS = 299,
        FriendNotFound = 300,
        FriendWrongFaction = 301,
        FriendRemovedS = 302,
        BattletagFriendRemovedS = 303,
        FriendError = 304,
        FriendAlreadyS = 305,
        FriendSelf = 306,
        FriendDeleted = 307,
        IgnoreFull = 308,
        IgnoreSelf = 309,
        IgnoreNotFound = 310,
        IgnoreAlreadyS = 311,
        IgnoreAddedS = 312,
        IgnoreRemovedS = 313,
        IgnoreAmbiguous = 314,
        IgnoreDeleted = 315,
        OnlyOneBolt = 316,
        OnlyOneAmmo = 317,
        SpellFailedEquippedSpecificItem = 318,
        WrongBagTypeSubclass = 319,
        CantWrapStackable = 320,
        CantWrapEquipped = 321,
        CantWrapWrapped = 322,
        CantWrapBound = 323,
        CantWrapUnique = 324,
        CantWrapBags = 325,
        OutOfMana = 326,
        OutOfRage = 327,
        OutOfFocus = 328,
        OutOfEnergy = 329,
        OutOfChi = 330,
        OutOfHealth = 331,
        OutOfRunes = 332,
        OutOfRunicPower = 333,
        OutOfSoulShards = 334,
        OutOfLunarPower = 335,
        OutOfHolyPower = 336,
        OutOfMaelstrom = 337,
        OutOfComboPoints = 338,
        OutOfInsanity = 339,
        OutOfArcaneCharges = 340,
        OutOfFury = 341,
        OutOfPain = 342,
        OutOfPowerDisplay = 343,
        LootGone = 344,
        MountForceddismount = 345,
        AutofollowTooFar = 346,
        UnitNotFound = 347,
        InvalidFollowTarget = 348,
        InvalidInspectTarget = 349,
        GuildemblemSuccess = 350,
        GuildemblemInvalidTabardColors = 351,
        GuildemblemNoguild = 352,
        GuildemblemNotguildmaster = 353,
        GuildemblemNotenoughmoney = 354,
        GuildemblemInvalidvendor = 355,
        EmblemerrorNotabardgeoset = 356,
        SpellOutOfRange = 357,
        CommandNeedsTarget = 358,
        NoammoS = 359,
        Toobusytofollow = 360,
        DuelRequested = 361,
        DuelCancelled = 362,
        Deathbindalreadybound = 363,
        DeathbindSuccessS = 364,
        Noemotewhilerunning = 365,
        ZoneExplored = 366,
        ZoneExploredXp = 367,
        InvalidItemTarget = 368,
        InvalidQuestTarget = 369,
        IgnoringYouS = 370,
        FishNotHooked = 371,
        FishEscaped = 372,
        SpellFailedNotunsheathed = 373,
        PetitionOfferedS = 374,
        PetitionSigned = 375,
        PetitionSignedS = 376,
        PetitionDeclinedS = 377,
        PetitionAlreadySigned = 378,
        PetitionRestrictedAccountTrial = 379,
        PetitionAlreadySignedOther = 380,
        PetitionInGuild = 381,
        PetitionCreator = 382,
        PetitionNotEnoughSignatures = 383,
        PetitionNotSameServer = 384,
        PetitionFull = 385,
        PetitionAlreadySignedByS = 386,
        GuildNameInvalid = 387,
        SpellUnlearnedS = 388,
        PetSpellRooted = 389,
        PetSpellAffectingCombat = 390,
        PetSpellOutOfRange = 391,
        PetSpellNotBehind = 392,
        PetSpellTargetsDead = 393,
        PetSpellDead = 394,
        PetSpellNopath = 395,
        ItemCantBeDestroyed = 396,
        TicketAlreadyExists = 397,
        TicketCreateError = 398,
        TicketUpdateError = 399,
        TicketDbError = 400,
        TicketNoText = 401,
        TicketTextTooLong = 402,
        ObjectIsBusy = 403,
        ExhaustionWellrested = 404,
        ExhaustionRested = 405,
        ExhaustionNormal = 406,
        ExhaustionTired = 407,
        ExhaustionExhausted = 408,
        NoItemsWhileShapeshifted = 409,
        CantInteractShapeshifted = 410,
        RealmNotFound = 411,
        MailQuestItem = 412,
        MailBoundItem = 413,
        MailConjuredItem = 414,
        MailBag = 415,
        MailToSelf = 416,
        MailTargetNotFound = 417,
        MailDatabaseError = 418,
        MailDeleteItemError = 419,
        MailWrappedCod = 420,
        MailCantSendRealm = 421,
        MailSent = 422,
        NotHappyEnough = 423,
        UseCantImmune = 424,
        CantBeDisenchanted = 425,
        CantUseDisarmed = 426,
        AuctionQuestItem = 427,
        AuctionBoundItem = 428,
        AuctionConjuredItem = 429,
        AuctionLimitedDurationItem = 430,
        AuctionWrappedItem = 431,
        AuctionLootItem = 432,
        AuctionBag = 433,
        AuctionEquippedBag = 434,
        AuctionDatabaseError = 435,
        AuctionBidOwn = 436,
        AuctionBidIncrement = 437,
        AuctionHigherBid = 438,
        AuctionMinBid = 439,
        AuctionRepairItem = 440,
        AuctionUsedCharges = 441,
        AuctionAlreadyBid = 442,
        AuctionStarted = 443,
        AuctionRemoved = 444,
        AuctionOutbidS = 445,
        AuctionWonS = 446,
        AuctionSoldS = 447,
        AuctionExpiredS = 448,
        AuctionRemovedS = 449,
        AuctionBidPlaced = 450,
        LogoutFailed = 451,
        QuestPushSuccessS = 452,
        QuestPushInvalidS = 453,
        QuestPushAcceptedS = 454,
        QuestPushDeclinedS = 455,
        QuestPushBusyS = 456,
        QuestPushDeadS = 457,
        QuestPushLogFullS = 458,
        QuestPushOnquestS = 459,
        QuestPushAlreadyDoneS = 460,
        QuestPushNotDailyS = 461,
        QuestPushTimerExpiredS = 462,
        QuestPushNotInPartyS = 463,
        QuestPushDifferentServerDailyS = 464,
        QuestPushNotAllowedS = 465,
        RaidGroupLowlevel = 466,
        RaidGroupOnly = 467,
        RaidGroupFull = 468,
        RaidGroupRequirementsUnmatch = 469,
        CorpseIsNotInInstance = 470,
        PvpKillHonorable = 471,
        PvpKillDishonorable = 472,
        SpellFailedAlreadyAtFullHealth = 473,
        SpellFailedAlreadyAtFullMana = 474,
        SpellFailedAlreadyAtFullPowerS = 475,
        AutolootMoneyS = 476,
        GenericStunned = 477,
        TargetStunned = 478,
        MustRepairDurability = 479,
        RaidYouJoined = 480,
        RaidYouLeft = 481,
        InstanceGroupJoinedWithParty = 482,
        InstanceGroupJoinedWithRaid = 483,
        RaidMemberAddedS = 484,
        RaidMemberRemovedS = 485,
        InstanceGroupAddedS = 486,
        InstanceGroupRemovedS = 487,
        ClickOnItemToFeed = 488,
        TooManyChatChannels = 489,
        LootRollPending = 490,
        LootPlayerNotFound = 491,
        NotInRaid = 492,
        LoggingOut = 493,
        TargetLoggingOut = 494,
        NotWhileMounted = 495,
        NotWhileShapeshifted = 496,
        NotInCombat = 497,
        NotWhileDisarmed = 498,
        PetBroken = 499,
        TalentWipeError = 500,
        SpecWipeError = 501,
        GlyphWipeError = 502,
        PetSpecWipeError = 503,
        FeignDeathResisted = 504,
        MeetingStoneInQueueS = 505,
        MeetingStoneLeftQueueS = 506,
        MeetingStoneOtherMemberLeft = 507,
        MeetingStonePartyKickedFromQueue = 508,
        MeetingStoneMemberStillInQueue = 509,
        MeetingStoneSuccess = 510,
        MeetingStoneInProgress = 511,
        MeetingStoneMemberAddedS = 512,
        MeetingStoneGroupFull = 513,
        MeetingStoneNotLeader = 514,
        MeetingStoneInvalidLevel = 515,
        MeetingStoneTargetNotInParty = 516,
        MeetingStoneTargetInvalidLevel = 517,
        MeetingStoneMustBeLeader = 518,
        MeetingStoneNoRaidGroup = 519,
        MeetingStoneNeedParty = 520,
        MeetingStoneNotFound = 521,
        GuildemblemSame = 522,
        EquipTradeItem = 523,
        PvpToggleOn = 524,
        PvpToggleOff = 525,
        GroupJoinBattlegroundDeserters = 526,
        GroupJoinBattlegroundDead = 527,
        GroupJoinBattlegroundS = 528,
        GroupJoinBattlegroundFail = 529,
        GroupJoinBattlegroundTooMany = 530,
        SoloJoinBattlegroundS = 531,
        BattlegroundTooManyQueues = 532,
        BattlegroundCannotQueueForRated = 533,
        BattledgroundQueuedForRated = 534,
        BattlegroundTeamLeftQueue = 535,
        BattlegroundNotInBattleground = 536,
        AlreadyInArenaTeamS = 537,
        InvalidPromotionCode = 538,
        BgPlayerJoinedSs = 539,
        BgPlayerLeftS = 540,
        RestrictedAccount = 541,
        RestrictedAccountTrial = 542,
        PlayTimeExceeded = 543,
        ApproachingPartialPlayTime = 544,
        ApproachingPartialPlayTime2 = 545,
        ApproachingNoPlayTime = 546,
        ApproachingNoPlayTime2 = 547,
        UnhealthyTime = 548,
        ChatRestrictedTrial = 549,
        ChatThrottled = 550,
        MailReachedCap = 551,
        InvalidRaidTarget = 552,
        RaidLeaderReadyCheckStartS = 553,
        ReadyCheckInProgress = 554,
        ReadyCheckThrottled = 555,
        DungeonDifficultyFailed = 556,
        DungeonDifficultyChangedS = 557,
        TradeWrongRealm = 558,
        TradeNotOnTaplist = 559,
        ChatPlayerAmbiguousS = 560,
        LootCantLootThatNow = 561,
        LootMasterInvFull = 562,
        LootMasterUniqueItem = 563,
        LootMasterOther = 564,
        FilteringYouS = 565,
        UsePreventedByMechanicS = 566,
        ItemUniqueEquippable = 567,
        LfgLeaderIsLfmS = 568,
        LfgPending = 569,
        CantSpeakLangage = 570,
        VendorMissingTurnins = 571,
        BattlegroundNotInTeam = 572,
        NotInBattleground = 573,
        NotEnoughHonorPoints = 574,
        NotEnoughArenaPoints = 575,
        SocketingRequiresMetaGem = 576,
        SocketingMetaGemOnlyInMetaslot = 577,
        SocketingRequiresHydraulicGem = 578,
        SocketingHydraulicGemOnlyInHydraulicslot = 579,
        SocketingRequiresCogwheelGem = 580,
        SocketingCogwheelGemOnlyInCogwheelslot = 581,
        SocketingItemTooLowLevel = 582,
        ItemMaxCountSocketed = 583,
        SystemDisabled = 584,
        QuestFailedTooManyDailyQuestsI = 585,
        ItemMaxCountEquippedSocketed = 586,
        ItemUniqueEquippableSocketed = 587,
        UserSquelched = 588,
        TooMuchGold = 589,
        NotBarberSitting = 590,
        QuestFailedCais = 591,
        InviteRestrictedTrial = 592,
        VoiceIgnoreFull = 593,
        VoiceIgnoreSelf = 594,
        VoiceIgnoreNotFound = 595,
        VoiceIgnoreAlreadyS = 596,
        VoiceIgnoreAddedS = 597,
        VoiceIgnoreRemovedS = 598,
        VoiceIgnoreAmbiguous = 599,
        VoiceIgnoreDeleted = 600,
        UnknownMacroOptionS = 601,
        NotDuringArenaMatch = 602,
        PlayerSilenced = 603,
        PlayerUnsilenced = 604,
        ComsatDisconnect = 605,
        ComsatReconnectAttempt = 606,
        ComsatConnectFail = 607,
        MailInvalidAttachmentSlot = 608,
        MailTooManyAttachments = 609,
        MailInvalidAttachment = 610,
        MailAttachmentExpired = 611,
        VoiceChatParentalDisableAll = 612,
        VoiceChatParentalDisableMic = 613,
        ProfaneChatName = 614,
        PlayerSilencedEcho = 615,
        PlayerUnsilencedEcho = 616,
        VoicesessionFull = 617,
        LootCantLootThat = 618,
        ArenaExpiredCais = 619,
        GroupActionThrottled = 620,
        AlreadyPickpocketed = 621,
        NameInvalid = 622,
        NameNoName = 623,
        NameTooShort = 624,
        NameTooLong = 625,
        NameMixedLanguages = 626,
        NameProfane = 627,
        NameReserved = 628,
        NameThreeConsecutive = 629,
        NameInvalidSpace = 630,
        NameConsecutiveSpaces = 631,
        NameRussianConsecutiveSilentCharacters = 632,
        NameRussianSilentCharacterAtBeginningOrEnd = 633,
        NameDeclensionDoesntMatchBaseName = 634,
        ReferAFriendNotReferredBy = 635,
        ReferAFriendTargetTooHigh = 636,
        ReferAFriendInsufficientGrantableLevels = 637,
        ReferAFriendTooFar = 638,
        ReferAFriendDifferentFaction = 639,
        ReferAFriendNotNow = 640,
        ReferAFriendGrantLevelMaxI = 641,
        ReferAFriendSummonLevelMaxI = 642,
        ReferAFriendSummonCooldown = 643,
        ReferAFriendSummonOfflineS = 644,
        ReferAFriendInsufExpanLvl = 645,
        ReferAFriendNotInLfg = 646,
        ReferAFriendNoXrealm = 647,
        ReferAFriendMapIncomingTransferNotAllowed = 648,
        NotSameAccount = 649,
        BadOnUseEnchant = 650,
        TradeSelf = 651,
        TooManySockets = 652,
        ItemMaxLimitCategoryCountExceededIs = 653,
        TradeTargetMaxLimitCategoryCountExceededIs = 654,
        ItemMaxLimitCategorySocketedExceededIs = 655,
        ItemMaxLimitCategoryEquippedExceededIs = 656,
        ShapeshiftFormCannotEquip = 657,
        ItemInventoryFullSatchel = 658,
        ScalingStatItemLevelExceeded = 659,
        ScalingStatItemLevelTooLow = 660,
        PurchaseLevelTooLow = 661,
        GroupSwapFailed = 662,
        InviteInCombat = 663,
        InvalidGlyphSlot = 664,
        GenericNoValidTargets = 665,
        CalendarEventAlertS = 666,
        PetLearnSpellS = 667,
        PetLearnAbilityS = 668,
        PetSpellUnlearnedS = 669,
        InviteUnknownRealm = 670,
        InviteNoPartyServer = 671,
        InvitePartyBusy = 672,
        PartyTargetAmbiguous = 673,
        PartyLfgInviteRaidLocked = 674,
        PartyLfgBootLimit = 675,
        PartyLfgBootCooldownS = 676,
        PartyLfgBootNotEligibleS = 677,
        PartyLfgBootInpatientTimerS = 678,
        PartyLfgBootInProgress = 679,
        PartyLfgBootTooFewPlayers = 680,
        PartyLfgBootVoteSucceeded = 681,
        PartyLfgBootVoteFailed = 682,
        PartyLfgBootInCombat = 683,
        PartyLfgBootDungeonComplete = 684,
        PartyLfgBootLootRolls = 685,
        PartyLfgBootVoteRegistered = 686,
        PartyPrivateGroupOnly = 687,
        PartyLfgTeleportInCombat = 688,
        RaidDisallowedByLevel = 689,
        RaidDisallowedByCrossRealm = 690,
        PartyRoleNotAvailable = 691,
        JoinLfgObjectFailed = 692,
        LfgRemovedLevelup = 693,
        LfgRemovedXpToggle = 694,
        LfgRemovedFactionChange = 695,
        BattlegroundInfoThrottled = 696,
        BattlegroundAlreadyIn = 697,
        ArenaTeamChangeFailedQueued = 698,
        ArenaTeamPermissions = 699,
        NotWhileFalling = 700,
        NotWhileMoving = 701,
        NotWhileFatigued = 702,
        MaxSockets = 703,
        MultiCastActionTotemS = 704,
        BattlegroundJoinLevelup = 705,
        RemoveFromPvpQueueXpGain = 706,
        BattlegroundJoinXpGain = 707,
        BattlegroundJoinMercenary = 708,
        BattlegroundJoinTooManyHealers = 709,
        BattlegroundJoinTooManyTanks = 710,
        BattlegroundJoinTooManyDamage = 711,
        RaidDifficultyFailed = 712,
        RaidDifficultyChangedS = 713,
        LegacyRaidDifficultyChangedS = 714,
        RaidLockoutChangedS = 715,
        RaidConvertedToParty = 716,
        PartyConvertedToRaid = 717,
        PlayerDifficultyChangedS = 718,
        GmresponseDbError = 719,
        BattlegroundJoinRangeIndex = 720,
        ArenaJoinRangeIndex = 721,
        RemoveFromPvpQueueFactionChange = 722,
        BattlegroundJoinFailed = 723,
        BattlegroundJoinNoValidSpecForRole = 724,
        BattlegroundJoinRespec = 725,
        BattlegroundInvitationDeclined = 726,
        BattlegroundJoinTimedOut = 727,
        BattlegroundDupeQueue = 728,
        BattlegroundJoinMustCompleteQuest = 729,
        InBattlegroundRespec = 730,
        MailLimitedDurationItem = 731,
        YellRestrictedTrial = 732,
        ChatRaidRestrictedTrial = 733,
        LfgRoleCheckFailed = 734,
        LfgRoleCheckFailedTimeout = 735,
        LfgRoleCheckFailedNotViable = 736,
        LfgReadyCheckFailed = 737,
        LfgReadyCheckFailedTimeout = 738,
        LfgGroupFull = 739,
        LfgNoLfgObject = 740,
        LfgNoSlotsPlayer = 741,
        LfgNoSlotsParty = 742,
        LfgNoSpec = 743,
        LfgMismatchedSlots = 744,
        LfgMismatchedSlotsLocalXrealm = 745,
        LfgPartyPlayersFromDifferentRealms = 746,
        LfgMembersNotPresent = 747,
        LfgGetInfoTimeout = 748,
        LfgInvalidSlot = 749,
        LfgDeserterPlayer = 750,
        LfgDeserterParty = 751,
        LfgDead = 752,
        LfgRandomCooldownPlayer = 753,
        LfgRandomCooldownParty = 754,
        LfgTooManyMembers = 755,
        LfgTooFewMembers = 756,
        LfgProposalFailed = 757,
        LfgProposalDeclinedSelf = 758,
        LfgProposalDeclinedParty = 759,
        LfgNoSlotsSelected = 760,
        LfgNoRolesSelected = 761,
        LfgRoleCheckInitiated = 762,
        LfgReadyCheckInitiated = 763,
        LfgPlayerDeclinedRoleCheck = 764,
        LfgPlayerDeclinedReadyCheck = 765,
        LfgJoinedQueue = 766,
        LfgJoinedFlexQueue = 767,
        LfgJoinedRfQueue = 768,
        LfgJoinedScenarioQueue = 769,
        LfgJoinedWorldPvpQueue = 770,
        ErrLfgJoinedBattlefieldQueue = 771,
        LfgJoinedList = 772,
        LfgLeftQueue = 773,
        LfgLeftList = 774,
        LfgRoleCheckAborted = 775,
        LfgReadyCheckAborted = 776,
        LfgCantUseBattleground = 777,
        LfgCantUseDungeons = 778,
        LfgReasonTooManyLfg = 779,
        InvalidTeleportLocation = 780,
        TooFarToInteract = 781,
        BattlegroundPlayersFromDifferentRealms = 782,
        DifficultyChangeCooldownS = 783,
        DifficultyChangeCombatCooldownS = 784,
        DifficultyChangeWorldstate = 785,
        DifficultyChangeEncounter = 786,
        DifficultyChangeCombat = 787,
        DifficultyChangePlayerBusy = 788,
        DifficultyChangeAlreadyStarted = 789,
        DifficultyChangeOtherHeroicS = 790,
        DifficultyChangeHeroicInstanceAlreadyRunning = 791,
        ArenaTeamPartySize = 792,
        QuestForceRemovedS = 793,
        AttackNoActions = 794,
        InRandomBg = 795,
        InNonRandomBg = 796,
        AuctionEnoughItems = 797,
        BnFriendSelf = 798,
        BnFriendAlready = 799,
        BnFriendBlocked = 800,
        BnFriendListFull = 801,
        BnFriendRequestSent = 802,
        BnBroadcastThrottle = 803,
        BgDeveloperOnly = 804,
        CurrencySpellSlotMismatch = 805,
        CurrencyNotTradable = 806,
        RequiresExpansionS = 807,
        QuestFailedSpell = 808,
        TalentFailedNotEnoughTalentsInPrimaryTree = 809,
        TalentFailedNoPrimaryTreeSelected = 810,
        TalentFailedCantRemoveTalent = 811,
        TalentFailedUnknown = 812,
        WargameRequestFailure = 813,
        RankRequiresAuthenticator = 814,
        GuildBankVoucherFailed = 815,
        WargameRequestSent = 816,
        RequiresAchievementI = 817,
        RefundResultExceedMaxCurrency = 818,
        CantBuyQuantity = 819,
        ItemIsBattlePayLocked = 820,
        PartyAlreadyInBattlegroundQueue = 821,
        PartyConfirmingBattlegroundQueue = 822,
        BattlefieldTeamPartySize = 823,
        InsuffTrackedCurrencyIs = 824,
        NotOnTournamentRealm = 825,
        GuildTrialAccountTrial = 826,
        GuildTrialAccountVeteran = 827,
        GuildUndeletableDueToLevel = 828,
        CantDoThatInAGroup = 829,
        GuildLeaderReplaced = 830,
        TransmogrifyCantEquip = 831,
        TransmogrifyInvalidItemType = 832,
        TransmogrifyNotSoulbound = 833,
        TransmogrifyInvalidSource = 834,
        TransmogrifyInvalidDestination = 835,
        TransmogrifyMismatch = 836,
        TransmogrifyLegendary = 837,
        TransmogrifySameItem = 838,
        TransmogrifySameAppearance = 839,
        TransmogrifyNotEquipped = 840,
        VoidDepositFull = 841,
        VoidWithdrawFull = 842,
        VoidStorageWrapped = 843,
        VoidStorageStackable = 844,
        VoidStorageUnbound = 845,
        VoidStorageRepair = 846,
        VoidStorageCharges = 847,
        VoidStorageQuest = 848,
        VoidStorageConjured = 849,
        VoidStorageMail = 850,
        VoidStorageBag = 851,
        VoidTransferStorageFull = 852,
        VoidTransferInvFull = 853,
        VoidTransferInternalError = 854,
        VoidTransferItemInvalid = 855,
        DifficultyDisabledInLfg = 856,
        VoidStorageUnique = 857,
        VoidStorageLoot = 858,
        VoidStorageHoliday = 859,
        VoidStorageDuration = 860,
        VoidStorageLoadFailed = 861,
        VoidStorageInvalidItem = 862,
        ParentalControlsChatMuted = 863,
        SorStartExperienceIncomplete = 864,
        SorInvalidEmail = 865,
        SorInvalidComment = 866,
        ChallengeModeResetCooldownS = 867,
        ChallengeModeResetKeystone = 868,
        PetJournalAlreadyInLoadout = 869,
        ReportSubmittedSuccessfully = 870,
        ReportSubmissionFailed = 871,
        SuggestionSubmittedSuccessfully = 872,
        BugSubmittedSuccessfully = 873,
        ChallengeModeEnabled = 874,
        ChallengeModeDisabled = 875,
        PetbattleCreateFailed = 876,
        PetbattleNotHere = 877,
        PetbattleNotHereOnTransport = 878,
        PetbattleNotHereUnevenGround = 879,
        PetbattleNotHereObstructed = 880,
        PetbattleNotWhileInCombat = 881,
        PetbattleNotWhileDead = 882,
        PetbattleNotWhileFlying = 883,
        PetbattleTargetInvalid = 884,
        PetbattleTargetOutOfRange = 885,
        PetbattleTargetNotCapturable = 886,
        PetbattleNotATrainer = 887,
        PetbattleDeclined = 888,
        PetbattleInBattle = 889,
        PetbattleInvalidLoadout = 890,
        PetbattleAllPetsDead = 891,
        PetbattleNoPetsInSlots = 892,
        PetbattleNoAccountLock = 893,
        PetbattleWildPetTapped = 894,
        PetbattleRestrictedAccount = 895,
        PetbattleNotWhileInMatchedBattle = 896,
        CantHaveMorePetsOfThatType = 897,
        CantHaveMorePets = 898,
        PvpMapNotFound = 899,
        PvpMapNotSet = 900,
        PetbattleQueueQueued = 901,
        PetbattleQueueAlreadyQueued = 902,
        PetbattleQueueJoinFailed = 903,
        PetbattleQueueJournalLock = 904,
        PetbattleQueueRemoved = 905,
        PetbattleQueueProposalDeclined = 906,
        PetbattleQueueProposalTimeout = 907,
        PetbattleQueueOpponentDeclined = 908,
        PetbattleQueueRequeuedInternal = 909,
        PetbattleQueueRequeuedRemoved = 910,
        PetbattleQueueSlotLocked = 911,
        PetbattleQueueSlotEmpty = 912,
        PetbattleQueueSlotNoTracker = 913,
        PetbattleQueueSlotNoSpecies = 914,
        PetbattleQueueSlotCantBattle = 915,
        PetbattleQueueSlotRevoked = 916,
        PetbattleQueueSlotDead = 917,
        PetbattleQueueSlotNoPet = 918,
        PetbattleQueueNotWhileNeutral = 919,
        PetbattleGameTimeLimitWarning = 920,
        PetbattleGameRoundsLimitWarning = 921,
        HasRestriction = 922,
        ItemUpgradeItemTooLowLevel = 923,
        ItemUpgradeNoPath = 924,
        ItemUpgradeNoMoreUpgrades = 925,
        BonusRollEmpty = 926,
        ChallengeModeFull = 927,
        ChallengeModeInProgress = 928,
        ChallengeModeIncorrectKeystone = 929,
        BattletagFriendNotFound = 930,
        BattletagFriendNotValid = 931,
        BattletagFriendNotAllowed = 932,
        BattletagFriendThrottled = 933,
        BattletagFriendSuccess = 934,
        PetTooHighLevelToUncage = 935,
        PetbattleInternal = 936,
        CantCagePetYet = 937,
        NoLootInChallengeMode = 938,
        QuestPetBattleVictoriesPvpIi = 939,
        RoleCheckAlreadyInProgress = 940,
        RecruitAFriendAccountLimit = 941,
        RecruitAFriendFailed = 942,
        SetLootPersonal = 943,
        SetLootMethodFailedCombat = 944,
        ReagentBankFull = 945,
        ReagentBankLocked = 946,
        GarrisonBuildingExists = 947,
        GarrisonInvalidPlot = 948,
        GarrisonInvalidBuildingid = 949,
        GarrisonInvalidPlotBuilding = 950,
        GarrisonRequiresBlueprint = 951,
        GarrisonNotEnoughCurrency = 952,
        GarrisonNotEnoughGold = 953,
        GarrisonCompleteMissionWrongFollowerType = 954,
        AlreadyUsingLfgList = 955,
        RestrictedAccountLfgListTrial = 956,
        ToyUseLimitReached = 957,
        ToyAlreadyKnown = 958,
        TransmogSetAlreadyKnown = 959,
        NotEnoughCurrency = 960,
        SpecIsDisabled = 961,
        FeatureRestrictedTrial = 962,
        CantBeObliterated = 963,
        ArtifactRelicDoesNotMatchArtifact = 964,
        MustEquipArtifact = 965,
        CantDoThatRightNow = 966,
        AffectingCombat = 967,
        EquipmentManagerCombatSwapS = 968,
        EquipmentManagerBagsFull = 969,
        EquipmentManagerMissingItemS = 970,
        MovieRecordingWarningPerf = 971,
        MovieRecordingWarningDiskFull = 972,
        MovieRecordingWarningNoMovie = 973,
        MovieRecordingWarningRequirements = 974,
        MovieRecordingWarningCompressing = 975,
        NoChallengeModeReward = 976,
        ClaimedChallengeModeReward = 977,
        ChallengeModePeriodResetSs = 978,
        CantDoThatChallengeModeActive = 979,
        TalentFailedRestArea = 980,
        CannotAbandonLastPet = 981,
        TestCvarSetSss = 982,
        QuestTurnInFailReason = 983,
        ClaimedChallengeModeRewardOld = 984,
        TalentGrantedByAura = 985,
        ChallengeModeAlreadyComplete = 986
    }

    public enum SceneFlags
    {
        Unk1 = 0x01,
        CancelAtEnd = 0x02,
        NotCancelable = 0x04,
        Unk8 = 0x08,
        Unk16 = 0x10, // 16, most common value
        Unk32 = 0x20
    }

    public enum WorldMapTransformsFlags
    {
        Dundeon = 0x04
    }
}
