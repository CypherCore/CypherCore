﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        public const int ReputationCap = 42000;
        public const int ReputationBottom = -42000;
        public const int MaxClientMailItems = 12; // max number of items a player is allowed to attach
        public const int MaxMailItems = 16;
        public const int MaxDeclinedNameCases = 5;
        public const int MaxHolidayDurations = 10;
        public const int MaxHolidayDates = 26;
        public const int MaxHolidayFlags = 10;
        public const int DefaultMaxLevel = 70;
        public const int MaxLevel = 123;
        public const int StrongMaxLevel = 255;
        public const int MaxOverrideSpell = 10;
        public const int MaxWorldMapOverlayArea = 4;
        public const int MaxMountCapabilities = 24;
        public const int MaxLockCase = 8;
        public const int MaxAzeriteEmpoweredTier = 5;
        public const int MaxAzeriteEssenceSlot = 4;
        public const int MaxAzeriteEssenceRank = 4;
        public const int AchivementCategoryPetBattles = 15117;
        public const int MaxUnitClasses = 4;

        /// <summary>
        /// BattlePets Const
        /// </summary>
        public const int DefaultMaxBattlePetsPerSpecies = 3;
        public const int BattlePetCageItemId = 82800;
        public const int SpellVisualUncagePet = 222;

        public const int SpellBattlePetTraining = 125610;
        public const int SpellReviveBattlePets = 125439;
        public const int SpellSummonBattlePet = 118301;
        public const int MaxBattlePetLevel = 25;

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
        public const int MaxNRLootItems = 18;
        public const int PlayerCorpseLootEntry = 1;

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
        public const int SmartEventParamCount = 5;
        public const int SmartActionParamCount = 7;
        public const uint SmartSummonCounter = 0xFFFFFF;
        public const uint SmartEscortTargets = 0xFFFFFF;

        /// <summary>
        /// BGs / Arena Const
        /// </summary>
        public const int PvpTeamsCount = 2;
        public const uint CountOfPlayersToAverageWaitTime = 10;
        public const uint MaxPlayerBGQueues = 3;
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
        /// Calendar Const
        /// </summary>
        public const uint CalendarMaxEvents = 30;
        public const uint CalendarMaxGuildEvents = 100;
        public const uint CalendarMaxInvites = 100;
        public const uint CalendarCreateEventCooldown = 5;
        public const uint CalendarOldEventsDeletionTime = 1 * Time.Month;
        public const uint CalendarDefaultResponseTime = 946684800; // 01/01/2000 00:00:00

        /// <summary>
        /// Misc Const
        /// </summary>
        public const Locale DefaultLocale = Locale.enUS;
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
        public const int MaxEquipmentItems = 3;

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
        public const int PetFocusRegenInterval = 4 * Time.InMilliseconds;
        public const int CreatureNoPathEvadeTime = 5 * Time.InMilliseconds;
        public const int BoundaryVisualizeCreature = 15425;
        public const float BoundaryVisualizeCreatureScale = 0.25f;
        public const int BoundaryVisualizeStepSize = 1;
        public const int BoundaryVisualizeFailsafeLimit = 750;
        public const int BoundaryVisualizeSpawnHeight = 5;
        public const uint AIDefaultCooldown = 5000;
        public const uint CreatureTappersSoftCap = 5;

        /// <summary>
        /// GameObject Const
        /// </summary>
        public const int MaxGOData = 35;
        public const uint MaxTransportStopFrames = 9;

        /// <summary>
        /// AreaTrigger Const
        /// </summary>
        public const int MaxAreatriggerEntityData = 8;
        public const int MaxAreatriggerScale = 7;

        /// <summary>
        /// Pet Const
        /// </summary>
        public const int MaxActivePets = 5;
        public const int MaxPetStables = 200;
        public const uint CallPetSpellId = 883;
        public const uint SpellPetSummoningDisorientation = 32752;
        public const float PetFollowDist = 1.0f;
        public const float PetFollowAngle = MathF.PI;
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
        public const float DefaultPlayerBoundingRadius = 0.388999998569489f;      // player size, also currently used (correctly?) for any non Unit world objects
        public const float AttackDistance = 5.0f;
        public const float DefaultPlayerCombatReach = 1.5f;
        public const float DefaultPlayerDisplayScale = 1.0f;
        public const float DefaultPlayerHoverHeight = 1.0f;
        public const float MinMeleeReach = 2.0f;
        public const float NominalMeleeRange = 5.0f;
        public const float MeleeRange = NominalMeleeRange - MinMeleeReach * 2; //center to center for players
        public const float ExtraCellSearchRadius = 40.0f; // We need in some cases increase search radius. Allow to find creatures with huge combat reach in a different nearby cell.
        public const float InspectDistance = 28.0f;
        public const float ContactDistance = 0.5f;
        public const float InteractionDistance = 5.0f;
        public const float MaxVisibilityDistance = MapConst.SizeofGrids;        // max distance for visible objects
        public const float SightRangeUnit = 50.0f;
        public const float VisibilityDistanceGigantic = 400.0f;
        public const float VisibilityDistanceLarge = 200.0f;
        public const float VisibilityDistanceNormal = 100.0f;
        public const float VisibilityDistanceSmall = 50.0f;
        public const float VisibilityDistanceTiny = 25.0f;
        public const float DefaultVisibilityDistance = VisibilityDistanceNormal;  // default visible distance, 100 yards on continents
        public const float DefaultVisibilityInstance = 170.0f;                    // default visible distance in instances, 170 yards
        public const float DefaultVisibilityBGAreans = 533.0f;                    // default visible distance in BG/Arenas, roughly 533 yards
        public const int DefaultVisibilityNotifyPeriod = 1000;
        public static TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5.2);

        public const int WorldTrigger = 12999;

        public const uint DisplayIdHiddenMount = 73200;

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

        public static float[] VisibilityDistances =
        {
            DefaultVisibilityDistance,
            VisibilityDistanceTiny,
            VisibilityDistanceSmall,
            VisibilityDistanceLarge,
            VisibilityDistanceGigantic,
            MaxVisibilityDistance
        };

        static int[] raceBits =
        {
            0, 0, 1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, 21, -1, 23, 24, 25, 26, 27, 28,
            29, 30, 31, -1, 11, 12, 13, 14
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
                case QuestSort.Jewelcrafting:
                    return SkillType.Jewelcrafting;
                case QuestSort.Inscription:
                    return SkillType.Inscription;
                case QuestSort.Archaeology:
                    return SkillType.Archaeology;
            }
            return SkillType.None;
        }

        public static SkillType SkillByLockType(LockType locktype) => locktype switch
        {
            LockType.Herbalism or LockType.ElusiveHerbalism => SkillType.Herbalism,
            LockType.Mining or LockType.Mining2 or LockType.ElusiveMining => SkillType.Mining,
            LockType.Fishing => SkillType.Fishing,
            LockType.Inscription => SkillType.Inscription,
            LockType.Archaeology => SkillType.Archaeology,
            LockType.LumberMill => SkillType.Logging,
            LockType.Skinning => SkillType.Skinning,
            LockType.ClassicHerbalism => SkillType.ClassicHerbalism,
            LockType.OutlandHerbalism => SkillType.OutlandHerbalism,
            LockType.NorthrendHerbalism => SkillType.NorthrendHerbalism,
            LockType.CataclysmHerbalism => SkillType.CataclysmHerbalism,
            LockType.PandariaHerbalism => SkillType.PandariaHerbalism,
            LockType.DraenorHerbalism => SkillType.DraenorHerbalism,
            LockType.LegionHerbalism => SkillType.LegionHerbalism,
            LockType.KulTiranHerbalism => SkillType.KulTiranHerbalism,
            LockType.ClassicMining => SkillType.ClassicMining,
            LockType.OutlandMining => SkillType.OutlandMining,
            LockType.NorthrendMining => SkillType.NorthrendMining,
            LockType.CataclysmMining => SkillType.CataclysmMining,
            LockType.PandariaMining => SkillType.PandariaMining,
            LockType.DraenorMining => SkillType.DraenorMining,
            LockType.LegionMining => SkillType.LegionMining,
            LockType.KulTiranMining => SkillType.KulTiranMining,
            LockType.LegionSkinning => SkillType.LegionSkinning,
            LockType.ShadowlandsHerbalism => SkillType.ShadowlandsHerbalism,
            LockType.ShadowlandsMining => SkillType.ShadowlandsMining,
            LockType.CovenantNightFae => SkillType.CovenantNightFae,
            LockType.CovenantVenthyr => SkillType.CovenantVenthyr,
            LockType.CovenantKyrian => SkillType.CovenantKyrian,
            LockType.CovenantNecrolord => SkillType.CovenantNecrolord,
            LockType.Engineering => SkillType.Engineering,
            LockType.DragonIslesHerbalism or LockType.DragonIslesHerbalism25 => SkillType.DragonIslesHerbalism,
            LockType.Enchanting => SkillType.Enchanting,
            LockType.DragonIslesAlchemy25 => SkillType.DragonIslesAlchemy,
            LockType.DragonIslesBlacksmithing25 => SkillType.DragonIslesBlacksmithing,
            LockType.DragonIslesEnchanting25 => SkillType.DragonIslesEnchanting,
            LockType.DragonIslesEngineering25 => SkillType.DragonIslesEngineering,
            LockType.DragonIslesInscription25 => SkillType.DragonIslesInscription,
            LockType.DragonIslesJewelcrafting25 => SkillType.DragonIslesJewelcrafting,
            LockType.DragonIslesLeatherworking25 => SkillType.DragonIslesLeatherworking,
            LockType.DragonIslesSkinning25 => SkillType.DragonIslesSkinning,
            LockType.DragonIslesTailoring25 => SkillType.DragonIslesTailoring,
            LockType.DragonIslesMining or LockType.DragonIslesMining25 => SkillType.DragonIslesMining,
            _ => SkillType.None
        };

        public static bool IsValidLocale(Locale locale)
        {
            return locale < Locale.Total && locale != Locale.None;
        }

        public static CascLocaleBit[] WowLocaleToCascLocaleBit =
        {
            CascLocaleBit.enUS,
            CascLocaleBit.koKR,
            CascLocaleBit.frFR,
            CascLocaleBit.deDE,
            CascLocaleBit.zhCN,
            CascLocaleBit.zhTW,
            CascLocaleBit.esES,
            CascLocaleBit.esMX,
            CascLocaleBit.ruRU,
            CascLocaleBit.None,
            CascLocaleBit.ptBR,
            CascLocaleBit.itIT
        };

        public static bool IsActivePetSlot(PetSaveMode slot)
        {
            return slot >= PetSaveMode.FirstActiveSlot && slot < PetSaveMode.LastActiveSlot;
        }

        public static bool IsStabledPetSlot(PetSaveMode slot)
        {
            return slot >= PetSaveMode.FirstStableSlot && slot < PetSaveMode.LastStableSlot;
        }

        public static LootType GetLootTypeForClient(LootType lootType)
        {
            switch (lootType)
            {
                case LootType.Prospecting:
                case LootType.Milling:
                    return LootType.Disenchanting;
                case LootType.Insignia:
                    return LootType.Skinning;
                case LootType.Fishinghole:
                case LootType.FishingJunk:
                    return LootType.Fishing;
                default:
                    break;
            }
            return lootType;
        }

        public static int GetOtherTeam(int team) => team switch
        {
            BattleGroundTeamId.Alliance => BattleGroundTeamId.Horde,
            BattleGroundTeamId.Horde => BattleGroundTeamId.Alliance,
            _ => BattleGroundTeamId.Neutral
        };

        public static Team GetOtherTeam(Team team) => team switch
        {
            Team.Horde => Team.Alliance,
            Team.Alliance => Team.Horde,
            Team.PandariaNeutral => Team.PandariaNeutral,
            _ => Team.Other
        };

        public static int GetTeamIdForTeam(Team team) => team switch
        {
            Team.Horde => BattleGroundTeamId.Horde,
            Team.Alliance => BattleGroundTeamId.Alliance,
            _ => BattleGroundTeamId.Neutral
        };
    }

    public enum Locale
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
        Total = 12
    }

    public enum CascLocaleBit
    {
        None = 0,
        enUS = 1,
        koKR = 2,
        Reserved = 3,
        frFR = 4,
        deDE = 5,
        zhCN = 6,
        esES = 7,
        zhTW = 8,
        enGB = 9,
        enCN = 10,
        enTW = 11,
        esMX = 12,
        ruRU = 13,
        ptBR = 14,
        itIT = 15,
        ptPT = 16
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

    public struct BattleGroundTeamId
    {
        public const int Alliance = 0;
        public const int Horde = 1;
        public const int Neutral = 2;
    }

    public enum Team
    {
        Horde = 67,
        Alliance = 469,
        PandariaNeutral = 1249,                             // Starting pandas should have this team
        Other = 0                            // if ReputationListId > 0 && Flags != FACTION_FLAG_TEAM_HEADER
    }

    public enum FactionMasks : byte
    {
        Player = 1,                              // any player
        Alliance = 2,                              // player or creature from alliance team
        Horde = 4,                              // player or creature from horde team
        Monster = 8                               // aggressive creature from monster team
        // if none flags set then non-aggressive creature
    }

    public enum FactionTemplateFlags
    {
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

    public enum FactionTemplates
    {
        None = 0,
        Creature = 7,
        EscorteeANeutralPassive = 10,
        Monster = 14,
        Monster2 = 16,
        TrollBloodscalp = 28,
        Prey = 31,
        EscorteeHNeutralPassive = 33,
        Friendly = 35,
        TrollFrostmane = 37,
        Ogre = 45,
        OrcDragonmaw = 62,
        HordeGeneric = 83,
        AllianceGeneric = 84,
        Demon = 90,
        Elemental = 91,
        DragonflightBlack = 103,
        EscorteeNNeutralPassive = 113,
        Enemy = 168,
        EscorteeANeutralActive = 231,
        EscorteeHNeutralActive = 232,
        EscorteeNNeutralActive = 250,
        EscorteeNFriendPassive = 290,
        Titan = 415,
        EscorteeNFriendActive = 495,
        Ratchet = 637,
        GoblinDarkIronBarPatron = 736,
        DarkIronDwarves = 754,
        EscorteeAPassive = 774,
        EscorteeHPassive = 775,
        UndeadScourge = 974,
        EarthenRing = 1726,
        AllianceGenericWg = 1732,
        HordeGenericWg = 1735,
        Arakkoa = 1738,
        AshtongueDeathsworn = 1820,
        FlayerHunter = 1840,
        MonsterSparBuddy = 1868,
        EscorteeNActive = 1986,
        EscorteeHActive = 2046,
        UndeadScourge2 = 2068,
        UndeadScourge3 = 2084,
        ScarletCrusade = 2089,
        ScarletCrusade2 = 2096
    };

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

    [Flags]
    public enum ReputationFlags : ushort
    {
        None = 0x00,
        Visible = 0x01,                   // makes visible in client (set or can be set at interaction with target of this faction)
        AtWar = 0x02,                   // enable AtWar-button in client. player controlled (except opposition team always war state), Flag only set on initial creation
        Hidden = 0x04,                   // hidden faction from reputation pane in client (player can gain reputation, but this update not sent to client)
        Header = 0x08,                   // Display as header in UI
        Peaceful = 0x10,
        Inactive = 0x20,                   // player controlled (CMSG_SET_FACTION_INACTIVE)
        ShowPropagated = 0x40,
        HeaderShowsBar = 0x80,                   // Header has its own reputation bar
        CapitalCityForRaceChange = 0x100,
        Guild = 0x200,
        GarrisonInvasion = 0x400
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
        Evoker = 13,
        Adventurer = 14,
        Max = 15,

        ClassMaskAllPlayable = ((1 << (Warrior - 1)) | (1 << (Paladin - 1)) | (1 << (Hunter - 1)) |
            (1 << (Rogue - 1)) | (1 << (Priest - 1)) | (1 << (Deathknight - 1)) | (1 << (Shaman - 1)) |
            (1 << (Mage - 1)) | (1 << (Warlock - 1)) | (1 << (Monk - 1)) | (1 << (Druid - 1)) | (1 << (DemonHunter - 1)) | (1 << (Evoker - 1))),

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
        Nightborne = 27,
        HighmountainTauren = 28,
        VoidElf = 29,
        LightforgedDraenei = 30,
        ZandalariTroll = 31,
        KulTiran = 32,
        //RACE_THIN_HUMAN         = 33,
        DarkIronDwarf = 34,
        Vulpera = 35,
        MagharOrc = 36,
        MechaGnome = 37,
        DracthyrAlliance = 52,
        DracthyrHorde = 70,
        //CompanionDrake      = 71,
        //CompanionProtoDragon = 72,
        //CompanionSerpent    = 73,
        //CompanionWyvern     = 74,
        //DracthyrVisageAlliance = 75,
        //DracthyrVisageHorde= 76,
        //CompanionPterrodax  = 77
        Max = 78
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
        BattleForAzeroth = 7,
        ShadowLands = 8,
        Dragonflight = 9,
        Max,

        MaxAccountExpansions
    }
    public enum PowerType : sbyte
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
        Essence = 19,
        RuneBlood = 20,
        RuneFrost = 21,
        RuneUnholy = 22,
        AlternateQuest = 23,
        AlternateEncounter = 24,
        AlternateMount = 25,
        Max = 26,

        All = 127,          // default for class?
        Health = -2,    // (-2 as signed value)
        MaxPerClass = 10
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

    public enum ChatMsg
    {
        Addon = -1,
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
        BnInlineToastAlert = 0x35,
        BnInlineToastBroadcast = 0x36,
        BnInlineToastBroadcastInform = 0x37,
        BnInlineToastConversation = 0x38,
        BnWhisperPlayerOffline = 0x39,
        Currency = 0x3a,
        QuestBossEmote = 0x3b,
        PetBattleCombatLog = 0x3c,
        PetBattleInfo = 0x3d,
        InstanceChat = 0x3e,
        InstanceChatLeader = 0x3f,
        GuildItemLooted = 0x40,
        CommunitiesChannel = 0x41,
        VoiceText = 0x42,
        Max
    }

    public enum ChatRestrictionType
    {
        Restricted = 0,
        Throttled = 1,
        Squelched = 2,
        YellRestricted = 3,
        RaidRestricted = 4
    }

    public enum CurveInterpolationMode
    {
        Linear = 0,
        Cosine = 1,
        CatmullRom = 2,
        Bezier3 = 3,
        Bezier4 = 4,
        Bezier = 5,
        Constant = 6,
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
        NormalIsland = 38,
        HeroicIsland = 39,
        MythicIsland = 40,
        PvpIsland = 45,
        NormalWarfront = 147,
        HeroicWarfront = 149,
        LFR15thAnniversary = 151,

        VisionsOfNzoth = 152,
        TeemingIsland = 153
    }

    public enum DifficultyFlags : ushort
    {
        HeroicStyleLockouts = 0x01,
        Default = 0x02,
        CanSelect = 0x04, // Player can select this difficulty in dropdown menu
        //ChallengeMode = 0x08, // deprecated since Legion expansion
        LfgOnly = 0x10,
        Legacy = 0x20,
        DisplayHeroic = 0x40, // Controls icon displayed on minimap when inside the instance
        DisplayMythic = 0x80  // Controls icon displayed on minimap when inside the instance
    }

    [Flags]
    public enum MapFlags : uint
    {
        Optimize = 0x01,
        DevelopmentMap = 0x02,
        WeightedBlend = 0x04,
        VertexColoring = 0x08,
        SortObjects = 0x10,
        LimitToPlayersFromOneRealm = 0x20,
        EnableLighting = 0x40,
        InvertedTerrain = 0x80,
        DynamicDifficulty = 0x100,
        ObjectFile = 0x200,
        TextureFile = 0x400,
        GenerateNormals = 0x800,
        FixBorderShadowSeams = 0x1000,
        InfiniteOcean = 0x2000,
        UnderwaterMap = 0x4000,
        FlexibleRaidLocking = 0x8000,
        LimitFarclip = 0x10000,
        UseParentMapFlightBounds = 0x20000,
        NoRaceChangeOnThisMap = 0x40000,
        DisabledForNonGMs = 0x80000,
        WeightedNormals1 = 0x100000,
        DisableLowDetailTerrain = 0x200000,
        EnableOrgArenaBlinkRule = 0x400000,
        WeightedHeightBlend = 0x800000,
        CoalescingAreaSharing = 0x1000000,
        ProvingGrounds = 0x2000000,
        Garrison = 0x4000000,
        EnableAINeedSystem = 0x8000000,
        SingleVServer = 0x10000000,
        UseInstancePool = 0x20000000,
        MapUsesRaidGraphics = 0x40000000,
        ForceCustomUIMap = 0x80000000,
    }

    [Flags]
    public enum MapFlags2 : uint
    {
        DontActivateShowMap = 0x01,
        NoVoteKicks = 0x02,
        NoIncomingTransfers = 0x04,
        DontVoxelizePathData = 0x08,
        TerrainLOD = 0x10,
        UnclampedPointLights = 0x20,
        PVP = 0x40,
        IgnoreInstanceFarmLimit = 0x80,
        DontInheritAreaLightsFromParent = 0x100,
        ForceLightBufferOn = 0x200,
        WMOLiquidScale = 0x400,
        SpellClutterOn = 0x800,
        SpellClutterOff = 0x1000,
        ReducedPathMapHeightValidation = 0x2000,
        NewMinimapGeneration = 0x4000,
        AIBotsDetectedLikePlayers = 0x8000,
        LinearlyLitTerrain = 0x10000,
        FogOfWar = 0x20000,
        DisableSharedWeatherSystems = 0x40000,
        HonorSpellAttribute11LosHitsNocamcollide = 0x80000,
        BelongsToLayer = 0x100000,
    }

    public enum StringIdType
    {
        Template = 0,
        Spawn = 1,
        Script = 2
    }

    // values based at Holidays.dbc
    public enum HolidayIds
    {
        None = 0,
        FireworksSpectacular = 62,
        FeastOfWinterVeil = 141,
        Noblegarden = 181,
        ChildrensWeek = 201,
        CallToArmsAvOld = 283,
        CallToArmsWgOld = 284,
        CallToArmsAbOld = 285,
        HarvestFestival = 321,
        HallowsEnd = 324,
        LunarFestival = 327,
        LoveIsInTheAirOld = 335,
        MidsummerFireFestival = 341,
        CallToArmsEsOld = 353,
        Brewfest = 372,
        PiratesDay = 398,
        CallToArmsSaOld = 400,
        PilgrimsBounty = 404,
        LkLaunch = 406,
        DayOfTheDead = 409,
        CallToArmsIcOld = 420,
        LoveIsInTheAir = 423,
        KaluAkFishingDerby = 424,
        CallToArmsBg = 435,
        CallToArmsTp = 436,
        RatedBg15Vs15 = 442,
        RatedBg25Vs25 = 443,
        Wow7thAnniversary = 467,
        DarkmoonFaire = 479,
        Wow8thAnniversary = 484,
        CallToArmsSm = 488,
        CallToArmsTk = 489,
        CallToArmsAv = 490,
        CallToArmsAb = 491,
        CallToArmsEs = 492,
        CallToArmsIc = 493,
        CallToArmsSmOld = 494,
        CallToArmsSa = 495,
        CallToArmsTkOld = 496,
        CallToArmsBgOld = 497,
        CallToArmsTpOld = 498,
        CallToArmsWg = 499,
        Wow9thAnniversary = 509,
        Wow10thAnniversary = 514,
        CallToArmsDg = 515,
        CallToArmsDgOld = 516,
        TimewalkingDungeonEventBcDefault = 559,
        ApexisBonusEventDefault = 560,
        ArenaSkirmishBonusEvent = 561,
        TimewalkingDungeonEventLkDefault = 562,
        BattlegroundBonusEventDefault = 563,
        DraenorDungeonEventDefault = 564,
        PetBattleBonusEventDefault = 565,
        Wow11thAnniversary = 566,
        TimewalkingDungeonEventCataDefault = 587,
        Wow12thAnniversary = 589,
        WowAnniversary = 590,
        LegionDungeonEventDefault = 591,
        WorldQuestBonusEventDefault = 592,
        ApexisBonusEventEu = 593,
        ApexisBonusEventTwCn = 594,
        ApexisBonusEventKr = 595,
        DraenorDungeonEventEu = 596,
        DraenorDungeonEventTwCn = 597,
        DraenorDungeonEventKr = 598,
        PetBattleBonusEventEu = 599,
        PetBattleBonusEventTwCn = 600,
        PetBattleBonusEventKr = 601,
        BattlegroundBonusEventEu = 602,
        BattlegroundBonusEventTwCn = 603,
        BattlegroundBonusEventKr = 604,
        LegionDungeonEventEu = 605,
        LegionDungeonEventTwCn = 606,
        LegionDungeonEventKr = 607,
        ArenaSkirmishBonusEventEu = 610,
        ArenaSkirmishBonusEventTwCn = 611,
        ArenaSkirmishBonusEventKr = 612,
        WorldQuestBonusEventEu = 613,
        WorldQuestBonusEventTwCn = 614,
        WorldQuestBonusEventKr = 615,
        TimewalkingDungeonEventLkEu = 616,
        TimewalkingDungeonEventLkTwCn = 617,
        TimewalkingDungeonEventLkKr = 618,
        TimewalkingDungeonEventBcEu = 622,
        TimewalkingDungeonEventBcTwCn = 623,
        TimewalkingDungeonEventBcKr = 624,
        TimewalkingDungeonEventCataEu = 628,
        TimewalkingDungeonEventCataTwCn = 629,
        TimewalkingDungeonEventCataKr = 630,
        HatchingOfTheHippogryphs = 634,
        VolunteerGuardDay = 635,
        CallOfTheScarab = 638,
        ThousandBoatBash = 642,
        TimewalkingDungeonEventMopDefault = 643,
        UngoroMadness = 644,
        SpringBalloonFestival = 645,
        KirinTorTavernCrawl = 646,
        MarchOfTheTadpoles = 647,
        GlowcapFestival = 648,
        TimewalkingDungeonEventMopEu = 652,
        TimewalkingDungeonEventMopTwCn = 654,
        TimewalkingDungeonEventMopKr = 656,
        FireworksCelebration = 658,
        PvpBrawlGl1984 = 659,
        PvpBrawlSsVsTm1984 = 660,
        PvpBrawlSsVsTmUs = 662,
        PvpBrawlGlUs = 663,
        PvpBrawlWsUs = 664,
        PvpBrawlAbUs = 666,
        PvpBrawlPhUs = 667,
        PvpBrawlSsVsTmEu = 669,
        PvpBrawlGlEu = 670,
        PvpBrawlWsEu = 671,
        PvpBrawlAbEu = 673,
        PvpBrawlPhEu = 674,
        PvpBrawlSsVsTmTwCn = 676,
        PvpBrawlGlTwCn = 677,
        PvpBrawlWsTwCn = 678,
        PvpBrawlAbTwCn = 680,
        PvpBrawlPhTwCn = 681,
        PvpBrawlSsVsTmKr = 683,
        PvpBrawlGlKr = 684,
        PvpBrawlWsKr = 685,
        PvpBrawlAbKr = 687,
        PvpBrawlPhKr = 688,
        TrialOfStyle = 691,
        AuctionHouseDanceParty = 692,
        Wow13thAnniversary = 693,
        MookinFestival = 694,
        TheGreatGnomereganRun = 696,
        PvpBrawlWs1984 = 701,
        PvpBrawlDsUs = 702,
        PvpBrawlDsEu = 704,
        PvpBrawlDsTwCn = 705,
        PvpBrawlDsKr = 706,
        TombOfSargerasNormalHeroicDefault = 710,    // Tomb Of Sargeras: Kil'Jaeden Awaits!
        TombOfSargerasNormalHeroicEu = 711,    // Tomb Of Sargeras: Kil'Jaeden Awaits!
        TombOfSargerasNormalHeroicTwCn = 712,    // Tomb Of Sargeras: Kil'Jaeden Awaits!
        TombOfSargerasNormalHeroicKr = 713,    // Tomb Of Sargeras: Kil'Jaeden Awaits!
        TombOfSargerasRf1SectionDefault = 714,    // Tomb Of Sargeras: The Gates Of Hell.
        TombOfSargerasRf1SectionEu = 715,    // Tomb Of Sargeras: The Gates Of Hell.
        TombOfSargerasRf1SectionTwCn = 716,    // Tomb Of Sargeras: The Gates Of Hell.
        TombOfSargerasRf1SectionKr = 717,    // Tomb Of Sargeras: The Gates Of Hell.
        TombOfSargerasRf2SectionDefault = 718,    // Tomb Of Sargeras: Wailing Halls.
        TombOfSargerasRf2SectionEu = 719,    // Tomb Of Sargeras: Wailing Halls.
        TombOfSargerasRf2SectionTwCn = 720,    // Tomb Of Sargeras: Wailing Halls.
        TombOfSargerasRf2SectionKr = 721,    // Tomb Of Sargeras: Wailing Halls.
        TombOfSargerasRf3SectionDefault = 722,    // Tomb Of Sargeras: Chamber Of The Avatar.
        TombOfSargerasRf3SectionEu = 723,    // Tomb Of Sargeras: Chamber Of The Avatar.
        TombOfSargerasRf3SectionTwCn = 724,    // Tomb Of Sargeras: Chamber Of The Avatar.
        TombOfSargerasRf3SectionKr = 725,    // Tomb Of Sargeras: Chamber Of The Avatar.
        TombOfSargerasFinalEncounterDefault = 726,    // Tomb Of Sargeras: Deceiver'S Fall. Kil'Jaeden Awaits!
        TombOfSargerasFinalEncounterEu = 727,    // Tomb Of Sargeras: Deceiver'S Fall. Kil'Jaeden Awaits!
        TombOfSargerasFinalEncounterTwCn = 728,    // Tomb Of Sargeras: Deceiver'S Fall. Kil'Jaeden Awaits!
        TombOfSargerasFinalEncounterKr = 729,    // Tomb Of Sargeras: Deceiver'S Fall. Kil'Jaeden Awaits!
        TombOfSargerasNormalHeroic768 = 730,    // Tomb Of Sargeras: Kil'Jaeden Awaits!
        PvpBrawlDs1984 = 736,
        PvpBrawlAb1984 = 737,
        ShadowsOfArgusWeek2UnlocksDefault = 744,    // In Part 2 Of Shadows Of Argus, Finish The Story Of Krokuun And Travel To The Ruined Draenei City Of Mac'Aree. Gain Access To Invasion Points And Thwart The Burning Legion'S Plans On Other Worlds. Additional World Quests Become Available.
        ShadowsOfArgusWeek3UnlocksDefault = 745,    // In Part 3 Of Shadows Of Argus, Finish The Shadows Of Argus Storyline, Unlock All World Quests, And Venture Into The New Dungeon, The Seat Of The Triumvirate. Activate Your Netherlight Crucible On The Vindicaar To Begin Forging Relics.
        ShadowsOfArgusWeek2UnlocksKr = 746,    // In Part 2 Of Shadows Of Argus, Finish The Story Of Krokuun And Travel To The Ruined Draenei City Of Mac'Aree. Gain Access To Invasion Points And Thwart The Burning Legion'S Plans On Other Worlds. Additional World Quests Become Available.
        ShadowsOfArgusWeek2UnlocksEu = 747,    // In Part 2 Of Shadows Of Argus, Finish The Story Of Krokuun And Travel To The Ruined Draenei City Of Mac'Aree. Gain Access To Invasion Points And Thwart The Burning Legion'S Plans On Other Worlds. Additional World Quests Become Available.
        ShadowsOfArgusWeek2UnlocksTwCn = 748,    // In Part 2 Of Shadows Of Argus, Finish The Story Of Krokuun And Travel To The Ruined Draenei City Of Mac'Aree. Gain Access To Invasion Points And Thwart The Burning Legion'S Plans On Other Worlds. Additional World Quests Become Available.
        ShadowsOfArgusWeek3UnlocksTwCn = 749,    // In Part 3 Of Shadows Of Argus, Finish The Shadows Of Argus Storyline, Unlock All World Quests, And Venture Into The New Dungeon, The Seat Of The Triumvirate. Activate Your Netherlight Crucible On The Vindicaar To Begin Forging Relics.
        ShadowsOfArgusWeek3UnlocksKr = 750,    // In Part 3 Of Shadows Of Argus, Finish The Shadows Of Argus Storyline, Unlock All World Quests, And Venture Into The New Dungeon, The Seat Of The Triumvirate. Activate Your Netherlight Crucible On The Vindicaar To Begin Forging Relics.
        ShadowsOfArgusWeek3UnlocksEu = 751,    // In Part 3 Of Shadows Of Argus, Finish The Shadows Of Argus Storyline, Unlock All World Quests, And Venture Into The New Dungeon, The Seat Of The Triumvirate. Activate Your Netherlight Crucible On The Vindicaar To Begin Forging Relics.
        AntorusBurningThroneRf2SectionTwCn = 756,    // Antorus, The Burning Throne: Forbidden Descent.
        AntorusBurningThroneRf2SectionEu = 757,    // Antorus, The Burning Throne: Forbidden Descent.
        AntorusBurningThroneRf2SectionKr = 758,    // Antorus, The Burning Throne: Forbidden Descent.
        AntorusBurningThroneRf2SectionDefault = 759,    // Antorus, The Burning Throne: Forbidden Descent.
        AntorusBurningThroneRf3SectionTwCn = 760,    // Antorus, The Burning Throne: Hope'S End.
        AntorusBurningThroneRf3SectionEu = 761,    // Antorus, The Burning Throne: Hope'S End.
        AntorusBurningThroneRf3SectionKr = 762,    // Antorus, The Burning Throne: Hope'S End.
        AntorusBurningThroneRf3SectionDefault = 763,    // Antorus, The Burning Throne: Hope'S End.
        AntorusBurningThroneFinalSectionTwCn = 764,    // Antorus, The Burning Throne: Seat Of The Pantheon.
        AntorusBurningThroneFinalSectionEu = 765,    // Antorus, The Burning Throne: Seat Of The Pantheon.
        AntorusBurningThroneFinalSectionKr = 766,    // Antorus, The Burning Throne: Seat Of The Pantheon.
        AntorusBurningThroneFinalSectionDefault = 767,    // Antorus, The Burning Throne: Seat Of The Pantheon.
        AntorusBurningThroneRf1SectionTwCn = 768,    // Antorus, The Burning Throne: Light'S Breach.
        AntorusBurningThroneRf1SectionEu = 769,    // Antorus, The Burning Throne: Light'S Breach.
        AntorusBurningThroneRf1SectionKr = 770,    // Antorus, The Burning Throne: Light'S Breach.
        AntorusBurningThroneRf1SectionDefault = 771,    // Antorus, The Burning Throne: Light'S Breach.
        AntorusBurningThroneNormalHeroicTwCn = 772,    // Antorus, The Burning Throne: Argus Awaits!
        AntorusBurningThroneNormalHeroicEu = 773,    // Antorus, The Burning Throne: Argus Awaits!
        AntorusBurningThroneNormalHeroicKr = 774,    // Antorus, The Burning Throne: Argus Awaits!
        AntorusBurningThroneNormalHeroicDefault = 775,    // Antorus, The Burning Throne: Argus Awaits!
        AntorusBurningThroneNormalHeroic768 = 776,    // Antorus, The Burning Throne: Argus Awaits!
        Wow14thAnniversary = 807,
        Wow15thAnniversary = 808,
        WarOfTheThorns = 918,    // Conflict Emerges In Darkshore As The Horde And Alliance Battle For Control Over Teldrassil In This Limited Time Event!
        UldirNormalHeroic768 = 920,    // Uldir: G'Huun Awaits!
        UldirNormalHeroicDefault = 921,    // Uldir: G'Huun Awaits!
        UldirNormalHeroicKr = 922,    // Uldir: G'Huun Awaits!
        UldirNormalHeroicEu = 923,    // Uldir: G'Huun Awaits!
        UldirNormalHeroicTwCn = 924,    // Uldir: G'Huun Awaits!
        UldirRf1SectionDefault = 925,    // Uldir: Halls Of Containment.
        UldirRf1SectionKr = 926,    // Uldir: Halls Of Containment.
        UldirRf1SectionEu = 927,    // Uldir: Halls Of Containment.
        UldirRf1SectionTwCn = 928,    // Uldir: Halls Of Containment.
        UldirRf2SectionDefault = 929,    // Uldir: Crimson Descent.
        UldirRf2SectionKr = 930,    // Uldir: Crimson Descent.
        UldirRf2SectionEu = 931,    // Uldir: Crimson Descent.
        UldirRf2SectionTwCn = 932,    // Uldir: Crimson Descent.
        UldirFinalSectionDefault = 933,    // Uldir: Heart Of Corruption.
        UldirFinalSectionKr = 934,    // Uldir: Heart Of Corruption.
        UldirFinalSectionEu = 935,    // Uldir: Heart Of Corruption.
        UldirFinalSectionTwCn = 936,    // Uldir: Heart Of Corruption.
        BattleForAzerothDungeonEventEu = 938,
        BattleForAzerothDungeonEventTwCn = 939,
        BattleForAzerothDungeonEventKr = 940,
        BattleForAzerothDungeonEventDefault = 941,
        WarOfTheThornsEu = 956,    // Conflict Emerges In Darkshore As The Horde And Alliance Battle For Control Over Teldrassil In This Limited Time Event!
        WarOfTheThornsTwCn = 957,    // Conflict Emerges In Darkshore As The Horde And Alliance Battle For Control Over Teldrassil In This Limited Time Event!
        WarOfTheThornsKr = 958,    // Conflict Emerges In Darkshore As The Horde And Alliance Battle For Control Over Teldrassil In This Limited Time Event!
        WarOfTheThorns320 = 959,    // Conflict Emerges In Darkshore As The Horde And Alliance Battle For Control Over Teldrassil In This Limited Time Event!
        WarOfTheThornsUs = 965,    // Conflict Emerges In Darkshore As The Horde And Alliance Battle For Control Over Teldrassil In This Limited Time Event!
        WarOfTheThorns512 = 967,    // Conflict Emerges In Darkshore As The Horde And Alliance Battle For Control Over Teldrassil In This Limited Time Event!
        WarOfTheThorns128 = 973,    // Conflict Emerges In Darkshore As The Horde And Alliance Battle For Control Over Teldrassil In This Limited Time Event!
        UldirNormalHeroic = 979,    // Uldir: G'Huun Awaits!
        BattleOfDazaralorNormalHeroicDefault = 1025,    // Battle Of Dazar'Alor Raid
        BattleOfDazaralorNormalHeroicKr = 1026,    // Battle Of Dazar'Alor Raid
        BattleOfDazaralorNormalHeroicEu = 1027,    // Battle Of Dazar'Alor Raid
        BattleOfDazaralorNormalHeroicTwCn = 1028,    // Battle Of Dazar'Alor Raid
        BattleOfDazaralorNormalHeroic768 = 1029,    // Battle Of Dazar'Alor Raid
        BattleOfDazaralorRf1SectionDefault = 1030,    // Battle Of Dazar'Alor: Siege Of Dazar'Alor.
        BattleOfDazaralorRf1SectionKr = 1031,    // Battle Of Dazar'Alor: Siege Of Dazar'Alor.
        BattleOfDazaralorRf1SectionEu = 1032,    // Battle Of Dazar'Alor: Siege Of Dazar'Alor.
        BattleOfDazaralorRf1SectionTwCn = 1033,    // Battle Of Dazar'Alor: Siege Of Dazar'Alor.
        BattleOfDazaralorRf2SectionDefault = 1034,    // Battle Of Dazar'Alor: Empire'S Fall.
        BattleOfDazaralorRf2SectionKr = 1035,    // Battle Of Dazar'Alor: Empire'S Fall.
        BattleOfDazaralorRf2SectionEu = 1036,    // Battle Of Dazar'Alor: Empire'S Fall.
        BattleOfDazaralorRf2SectionTwCn = 1037,    // Battle Of Dazar'Alor: Empire'S Fall.
        BattleOfDazaralorRf3SectionDefault = 1038,    // Battle Of Dazar'Alor: Might Of The Alliance For Alliance Players, And Victory Or Death For Horde Players.
        BattleOfDazaralorRf3SectionKr = 1039,    // Battle Of Dazar'Alor: Might Of The Alliance For Alliance Players, And Victory Or Death For Horde Players.
        BattleOfDazaralorRf3SectionEu = 1040,    // Battle Of Dazar'Alor: Might Of The Alliance For Alliance Players, And Victory Or Death For Horde Players.
        BattleOfDazaralorRf3SectionTwCn = 1041,    // Battle Of Dazar'Alor: Might Of The Alliance For Alliance Players, And Victory Or Death For Horde Players.
        PvpBrawlCookingImpossibleUs = 1047,
        PvpBrawlCookingImpossibleKr = 1048,
        PvpBrawlCookingImpossibleEu = 1049,
        PvpBrawlCookingImpossible1984 = 1050,
        PvpBrawlCookingImpossibleTwCn = 1051,
        WanderersFestival = 1052,
        FreeTshirtDay = 1053,
        LuminousLuminaries = 1054,
        TimewalkingDungeonEventWodDefault = 1056,
        LuminousLuminaries64 = 1062,
        TimewalkingDungeonEventWodEu = 1063,
        TimewalkingDungeonEventWodKr = 1065,
        TimewalkingDungeonEventWodTwCn = 1068,
        CrucibleOfStormsNormalHeroicDefault = 1069,    // Delve Into The Chambers Beneath Stormsong Valley To Uncover The Source Of The Shadow Spreading Across The Land, Now Available On Normal Or Heroic Difficulty.
        CrucibleOfStormsNormalHeroicKr = 1070,    // Delve Into The Chambers Beneath Stormsong Valley To Uncover The Source Of The Shadow Spreading Across The Land, Now Available On Normal Or Heroic Difficulty.
        CrucibleOfStormsNormalHeroicEu = 1071,    // Delve Into The Chambers Beneath Stormsong Valley To Uncover The Source Of The Shadow Spreading Across The Land, Now Available On Normal Or Heroic Difficulty.
        CrucibleOfStormsNormalHeroicTwCn = 1072,    // Delve Into The Chambers Beneath Stormsong Valley To Uncover The Source Of The Shadow Spreading Across The Land, Now Available On Normal Or Heroic Difficulty.
        CrucibleOfStormsNormalHeroic = 1073,    // Delve Into The Chambers Beneath Stormsong Valley To Uncover The Source Of The Shadow Spreading Across The Land, Now Available On Normal Or Heroic Difficulty.
        CrucibleOfStormsRaidFinderDefault = 1074,    // Mythic Difficulty Of The Crucible Of Storms Raid Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        CrucibleOfStormsRaidFinderEu = 1075,    // Mythic Difficulty Of The Crucible Of Storms Raid Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        CrucibleOfStormsRaidFinderKr = 1076,    // Mythic Difficulty Of The Crucible Of Storms Raid Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        CrucibleOfStormsRaidFinderTwCn = 1077,    // Mythic Difficulty Of The Crucible Of Storms Raid Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        CrucibleOfStormsRaidFinder = 1078,    // Mythic Difficulty Of The Crucible Of Storms Raid Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        TheEternalPalaceDefault = 1098,    // The Dangers Of Nazjatar Were Merely Preamble. Breach The Palace Gates And Descend Into Azshara'S Deadly Domain.
        TheEternalPalaceKr = 1099,    // The Dangers Of Nazjatar Were Merely Preamble. Breach The Palace Gates And Descend Into Azshara'S Deadly Domain.
        TheEternalPalaceEu = 1100,    // The Dangers Of Nazjatar Were Merely Preamble. Breach The Palace Gates And Descend Into Azshara'S Deadly Domain.
        TheEternalPalaceTwCn = 1101,    // The Dangers Of Nazjatar Were Merely Preamble. Breach The Palace Gates And Descend Into Azshara'S Deadly Domain.
        TheEternalPalaceRaidFinderDefault = 1102,    // Mythic Difficulty Of The Eternal Palace Raid Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        TheEternalPalaceRaidFinderKr = 1103,    // Mythic Difficulty Of The Eternal Palace Raid Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        TheEternalPalaceRaidFinderEu = 1104,    // Mythic Difficulty Of The Eternal Palace Raid Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        TheEternalPalaceRaidFinderTwCn = 1105,    // Mythic Difficulty Of The Eternal Palace Raid Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        TheEternalPalaceRf2SectionEu = 1106,    // The Eternal Palace: Depths Of The Devoted.
        TheEternalPalaceRf2SectionTwCn = 1107,    // The Eternal Palace: Depths Of The Devoted.
        TheEternalPalaceFinalSectionDefault = 1108,    // The Eternal Palace: The Circle Of Stars.
        TheEternalPalaceFinalSectionKr = 1109,    // The Eternal Palace: The Circle Of Stars.
        TheEternalPalaceFinalSectionEu = 1110,    // The Eternal Palace: The Circle Of Stars.
        TheEternalPalaceFinalSectionTwCn = 1111,    // The Eternal Palace: The Circle Of Stars.
        TheEternalPalaceRf2SectionKr = 1112,    // The Eternal Palace: Depths Of The Devoted.
        TheEternalPalaceRf2SectionDefault = 1113,    // The Eternal Palace: Depths Of The Devoted.
        PvpBrawlClassicAshranUs = 1120,
        PvpBrawlClassicAshranKr = 1121,
        PvpBrawlClassicAshranEu = 1122,
        PvpBrawlClassicAshran1984 = 1123,
        PvpBrawlClassicAshranTwCn = 1124,
        NyalothaWalkingCityDefault = 1140,    // Descend Into Ny'Alotha, The Waking City And Face N'Zoth In His Own Realm.
        NyalothaWalkingCityKr = 1141,    // Descend Into Ny'Alotha, The Waking City And Face N'Zoth In His Own Realm.
        NyalothaWalkingCityEu = 1142,    // Descend Into Ny'Alotha, The Waking City And Face N'Zoth In His Own Realm.
        NyalothaWalkingCityTwCn = 1143,    // Descend Into Ny'Alotha, The Waking City And Face N'Zoth In His Own Realm.
        NyalothaWalkingCityRaidFinderDefault = 1144,    // Mythic Difficulty Of Ny'Alotha, The Waking City Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        NyalothaWalkingCityRaidFinderKr = 1145,    // Mythic Difficulty Of Ny'Alotha, The Waking City Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        NyalothaWalkingCityRaidFinderEu = 1146,    // Mythic Difficulty Of Ny'Alotha, The Waking City Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        NyalothaWalkingCityRaidFinderTwCn = 1147,    // Mythic Difficulty Of Ny'Alotha, The Waking City Awaits The Boldest Of Adventurers, And Players May Now Use The Raid Finder To Access The Raid.
        NyalothaWalkingCityRf2SectionDefault = 1148,    // Ny'Alotha, The Waking City: Halls Of Devotion.
        NyalothaWalkingCityRf2SectionKr = 1149,    // Ny'Alotha, The Waking City: Halls Of Devotion.
        NyalothaWalkingCityRf2SectionEu = 1150,    // Ny'Alotha, The Waking City: Halls Of Devotion.
        NyalothaWalkingCityRf2SectionTwCn = 1151,    // Ny'Alotha, The Waking City: Halls Of Devotion.
        NyalothaWalkingCityRf3SectionDefault = 1152,    // Ny'Alotha, The Waking City: Gift Of Flesh.
        NyalothaWalkingCityRf3SectionKr = 1153,    // Ny'Alotha, The Waking City: Gift Of Flesh.
        NyalothaWalkingCityRf3SectionEu = 1154,    // Ny'Alotha, The Waking City: Gift Of Flesh.
        NyalothaWalkingCityRf3SectionTwCn = 1155,    // Ny'Alotha, The Waking City: Gift Of Flesh.
        NyalothaWalkingCityFinalSectionDefault = 1156,    // Ny'Alotha, The Waking City: The Waking Dream.
        NyalothaWalkingCityFinalSectionKr = 1157,    // Ny'Alotha, The Waking City: The Waking Dream.
        NyalothaWalkingCityFinalSectionEu = 1158,    // Ny'Alotha, The Waking City: The Waking Dream.
        NyalothaWalkingCityFinalSectionTwCn = 1159,    // Ny'Alotha, The Waking City: The Waking Dream.
        PvpBrawlTh1984 = 1166,
        PvpBrawlThKr = 1167,
        PvpBrawlThTwCn = 1168,
        PvpBrawlThEu = 1169,
        PvpBrawlThUs = 1170,
        Wow16thAnniversary = 1181,
        CastleNathriaDefault = 1194,    // Enter Castle Nathria And Confront Sire Denathrius In His Citadel.
        CastleNathriaRf1SectionDefault = 1195,    // Castle Nathria: The Leeching Vaults.
        CastleNathriaRf2SectionDefault = 1196,    // Castle Nathria: Reliquary Of Opulence.
        CastleNathriaRf3SectionDefault = 1197,    // Castle Nathria: Blood From Stone.
        CastleNathriaEu = 1198,    // Enter Castle Nathria And Confront Sire Denathrius In His Citadel.
        CastleNathriaRf1SectionEu = 1199,    // Castle Nathria: The Leeching Vaults.
        CastleNathriaRf2SectionEu = 1200,    // Castle Nathria: Reliquary Of Opulence.
        CastleNathriaRf3SectionEu = 1201,    // Castle Nathria: Blood From Stone.
        CastleNathriaKr = 1202,    // Enter Castle Nathria And Confront Sire Denathrius In His Citadel.
        CastleNathriaRf1SectionKr = 1203,    // Castle Nathria: The Leeching Vaults.
        CastleNathriaRf2SectionKr = 1204,    // Castle Nathria: Reliquary Of Opulence.
        CastleNathriaRf3SectionKr = 1205,    // Castle Nathria: Blood From Stone.
        CastleNathriaTwCn = 1206,    // Enter Castle Nathria And Confront Sire Denathrius In His Citadel.
        CastleNathriaRf1SectionTwCn = 1207,    // Castle Nathria: The Leeching Vaults.
        CastleNathriaRf2SectionTwCn = 1208,    // Castle Nathria: Reliquary Of Opulence.
        CastleNathriaRf3SectionTwCn = 1209,    // Castle Nathria: Blood From Stone.
        CastleNathriaFinalSectionDefault = 1210,    // Castle Nathria: An Audience With Arrogance.
        CastleNathriaFinalSectionEu = 1211,    // Castle Nathria: An Audience With Arrogance.
        CastleNathriaFinalSectionKr = 1212,    // Castle Nathria: An Audience With Arrogance.
        CastleNathriaFinalSectionTwCn = 1213,    // Castle Nathria: An Audience With Arrogance.
        TorghastBeastsOfProdigum = 1214,
        TorghastUnbridledDarkness = 1215,
        TorghastChorusOfDeadSouls = 1216,
        ShadowlandsDungeonEventDefault = 1217,
        ShadowlandsDungeonEventEu = 1218,
        ShadowlandsDungeonEventTwCn = 1219,
        ShadowlandsDungeonEventKr = 1220,
        PvpBrawlWs19842 = 1221,
        CastleNathria32 = 1222    // Enter Castle Nathria And Confront Sire Denathrius In His Citadel.
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
        Talk = 0,                // Source/Target = Creature, Target = Any, Datalong = Talk Type (see ChatType enum), datalong2 & 1 = player talk (instead of creature), dataint = string_id
        Emote = 1,                // Source/Target = Creature, Datalong = Emote Id, Datalong2 = 0: Set Emote State; > 0: Play Emote State
        FieldSetDeprecated = 2,
        MoveTo = 3,                // Source/Target = Creature, Datalong2 = Time To Reach, X/Y/Z = Destination
        FlagSetDeprecated = 4,
        FlagRemoveDeprecated = 5,
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
        Movement = 35,               // Source = Creature, datalong = MovementType, datalong2 = MovementDistance (wander_distance f.ex.), dataint = pathid
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
        Notfound,
        Exists
    }

    public enum WorldCfg
    {
        AccPasschangesec,
        AddonChannel,
        AhbotUpdateInterval,
        AllTaxiPaths,
        AllowGmGroup,
        AllowLogginIpAddressesInDatabase,
        AllowTwoSideInteractionAuction,
        AllowTwoSideInteractionCalendar,
        AllowTwoSideInteractionChannel,
        AllowTwoSideInteractionGroup,
        AllowTwoSideInteractionGuild,
        AllowTwoSideTrade,
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
        AuctionLevelReq,
        AuctionReplicateDelay,
        AuctionSearchDelay,
        AuctionTaintedSearchDelay,
        AutoBroadcast,
        AutoBroadcastCenter,
        AutoBroadcastInterval,
        BasemapLoadGrids,
        BattlegroundCastDeserter,
        BattlegroundInvitationType,
        BattlegroundMapLoadGrids,
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
        CacheDataQueries,
        CalculateCreatureZoneAreaData,
        CalculateGameobjectZoneAreaData,
        CalendarDeleteOldEventsHour,
        CallToArms5Pct,
        CallToArms10Pct,
        CallToArms20Pct,
        CastUnstuck,
        CharacterCreatingDisableAlliedRaceAchievementRequirement,
        CharacterCreatingDisabled,
        CharacterCreatingDisabledClassmask,
        CharacterCreatingDisabledRacemask,
        CharacterCreatingEvokersPerRealm,
        CharacterCreatingMinLevelForDemonHunter,
        CharacterCreatingMinLevelForEvoker,
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
        ChatEmoteLevelReq,
        ChatFakeMessagePreventing,
        ChatFloodMessageCount,
        ChatFloodMessageDelay,
        ChatFloodMuteTime,
        ChatPartyRaidWarnings,
        ChatSayLevelReq,
        ChatStrictLinkCheckingKick,
        ChatStrictLinkCheckingSeverity,
        ChatWhisperLevelReq,
        ChatYellLevelReq,
        CheckGobjectLos,
        CleanCharacterDb,
        CleanOldMailTime,
        ClientCacheVersion,
        CommunityAllowBnetClubTypeEnabled,
        CommunityAllowCharacterClubTypeEnabled,
        CommunityChatKeepDays,
        CommunityClubFinderEnabled,
        CommunityClubsEnabled,
        Compression,
        CorpseDecayElite,
        CorpseDecayMinusMob,
        CorpseDecayNormal,
        CorpseDecayObsolete,
        CorpseDecayRare,
        CorpseDecayRareelite,
        CorpseDecayTrivial,
        CreatureCheckInvalidPostion,
        CreatureFamilyAssistanceDelay,
        CreatureFamilyAssistanceRadius,
        CreatureFamilyFleeAssistanceRadius,
        CreatureFamilyFleeDelay,
        CreaturePickpocketRefill,
        CreatureStopForPlayer,
        CurrencyResetDay,
        CurrencyResetHour,
        CurrencyResetInterval,
        DailyQuestResetTimeHour,
        DbcEnforceItemAttributes,
        DeathBonesBgOrArena,
        DeathBonesWorld,
        DeathCorpseReclaimDelayPve,
        DeathCorpseReclaimDelayPvp,
        DeathSicknessLevel,
        DeclinedNamesUsed,
        DetectPosCollision,
        DieCommandMode,
        DisableBreathing,
        DurabilityLossInPvp,
        EnableAeLoot,
        EnableMmaps,
        EnableSinfoLogin,
        EventAnnounce,
        Expansion,
        FactionBalanceLevelCheckDiff,
        FeatureSystemBpayStoreEnabled,
        FeatureSystemCharacterUndeleteCooldown,
        FeatureSystemCharacterUndeleteEnabled,
        FeatureSystemWarModeEnabled,
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
        IntervalMapupdate,
        IntervalSave,
        IpBasedActionLogging,
        LfgOptionsmask,
        ListenRangeSay,
        ListenRangeTextemote,
        ListenRangeYell,
        LoadLocales,
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
        MinCreatureScaledXpRatio,
        MinDiscoveredScaledXpRatio,
        MinDualspecLevel,
        MinLevelStatSave,
        MinPetName,
        MinPetitionSigns,
        MinPlayerName,
        MinQuestScaledXpRatio,
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
        PreserveCustomChannelInterval,
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
        RateCreatureDamageElite,
        RateCreatureDamageMinusmob,
        RateCreatureDamageNormal,
        RateCreatureDamageObsolete,
        RateCreatureDamageRare,
        RateCreatureDamageRareelite,
        RateCreatureDamageTrivial,
        RateCreatureHpElite,
        RateCreatureHpMinusmob,
        RateCreatureHpNormal,
        RateCreatureHpObsolete,
        RateCreatureHpRare,
        RateCreatureHpRareelite,
        RateCreatureHpTrivial,
        RateCreatureSpelldamageElite,
        RateCreatureSpelldamageMinusmob,
        RateCreatureSpelldamageNormal,
        RateCreatureSpelldamageObsolete,
        RateCreatureSpelldamageRare,
        RateCreatureSpelldamageRareelite,
        RateCreatureSpelldamageTrivial,
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
        RatePowerEssence,
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
        RateXpExplore,
        RateXpGuildModifier,
        RateXpKill,
        RateXpBgKill,
        RateXpBoost,
        RateXpQuest,
        RealmZone,
        RegenHpCannotReachTargetInRaid,
        ResetDuelCooldowns,
        ResetDuelHealthMana,
        ResetScheduleWeekDay,
        ResetScheduleHour,
        RespawnDynamicEscortNpc,
        RespawnDynamicMinimumCreature,
        RespawnDynamicMinimumGameObject,
        RespawnDynamicMode,
        RespawnDynamicRateCreature,
        RespawnDynamicRateGameobject,
        RespawnGuidAlertLevel,
        RespawnGuidWarnLevel,
        RespawnGuidWarningFrequency,
        RespawnMinCheckIntervalMs,
        RespawnRestartQuietTime,
        RestrictedLfgChannel,
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
        SocketTimeoutTime,
        SocketTimeoutTimeActive,
        StartAlliedRaceLevel,
        StartAllExplored,
        StartAllRep,
        StartAllSpells,
        StartDeathKnightPlayerLevel,
        StartDemonHunterPlayerLevel,
        StartEvokerPlayerLevel,
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
        TolbaradBattleTime,
        TolbaradBonusTime,
        TolbaradEnable,
        TolbaradNoBattleTime,
        TolbaradPlrMax,
        TolbaradPlrMin,
        TolbaradPlrMinLvl,
        TolbaradRestartAfterCrash,
        TradeLevelReq,
        UptimeUpdate,
        VmapIndoorCheck,
        WardenClientResponseDelay,
        WardenClientCheckHoldoff,
        WardenClientFailAction,
        WardenClientBanDuration,
        WardenEnabled,
        WardenNumInjectChecks,
        WardenNumLuaChecks,
        WardenNumClientModChecks,
        Weather,
        WeeklyQuestResetTimeWDay,
        WintergraspBattletime,
        WintergraspEnable,
        WintergraspNobattletime,
        WintergraspPlrMax,
        WintergraspPlrMin,
        WintergraspPlrMinLvl,
        WintergraspRestartAfterCrash,
        WorldBossLevelDiff,
        XpBoostDaymask,
        Max
    }

    public enum GameError : uint
    {
        System = 0,
        InternalError = 1,
        InvFull = 2,
        BankFull = 3,
        CantEquipLevelI = 4,
        CantEquipSkill = 5,
        CantEquipEver = 6,
        CantEquipRank = 7,
        CantEquipRating = 8,
        CantEquipReputation = 9,
        ProficiencyNeeded = 10,
        WrongSlot = 11,
        CantEquipNeedTalent = 12,
        BagFull = 13,
        InternalBagError = 14,
        DestroyNonemptyBag = 15,
        BagInBag = 16,
        TooManySpecialBags = 17,
        TradeEquippedBag = 18,
        AmmoOnly = 19,
        NoSlotAvailable = 20,
        WrongBagType = 21,
        ReagentbagWrongSlot = 22,
        SlotOnlyReagentbag = 23,
        ReagentbagItemType = 24,
        ItemMaxCount = 25,
        NotEquippable = 26,
        CantStack = 27,
        CantSwap = 28,
        SlotEmpty = 29,
        ItemNotFound = 30,
        TooFewToSplit = 31,
        SplitFailed = 32,
        NotABag = 33,
        NotOwner = 34,
        OnlyOneQuiver = 35,
        NoBankSlot = 36,
        NoBankHere = 37,
        NoAccountBankHere = 38,
        ItemLocked = 39,
        Handed2Equipped = 40,
        VendorNotInterested = 41,
        VendorRefuseScrappableAzerite = 42,
        VendorHatesYou = 43,
        VendorSoldOut = 44,
        VendorTooFar = 45,
        VendorDoesntBuy = 46,
        NotEnoughMoney = 47,
        ReceiveItemS = 48,
        DropBoundItem = 49,
        TradeBoundItem = 50,
        TradeQuestItem = 51,
        TradeTempEnchantBound = 52,
        TradeGroundItem = 53,
        TradeBag = 54,
        TradeFactionSpecific = 55,
        SpellFailedS = 56,
        ItemCooldown = 57,
        PotionCooldown = 58,
        FoodCooldown = 59,
        SpellCooldown = 60,
        AbilityCooldown = 61,
        SpellAlreadyKnownS = 62,
        PetSpellAlreadyKnownS = 63,
        ProficiencyGainedS = 64,
        SkillGainedS = 65,
        SkillUpSi = 66,
        LearnSpellS = 67,
        LearnAbilityS = 68,
        LearnPassiveS = 69,
        LearnRecipeS = 70,
        ProfessionsRecipeDiscoveryS = 71,
        LearnCompanionS = 72,
        LearnMountS = 73,
        LearnToyS = 74,
        LearnHeirloomS = 75,
        LearnTransmogS = 76,
        CompletedTransmogSetS = 77,
        AppearanceAlreadyLearned = 78,
        RevokeTransmogS = 79,
        InvitePlayerS = 80,
        SuggestInvitePlayerS = 81,
        InformSuggestInviteS = 82,
        InformSuggestInviteSs = 83,
        RequestJoinPlayerS = 84,
        InviteSelf = 85,
        InvitedToGroupSs = 86,
        InvitedAlreadyInGroupSs = 87,
        AlreadyInGroupS = 88,
        RequestedInviteToGroupSs = 89,
        CrossRealmRaidInvite = 90,
        PlayerBusyS = 91,
        NewLeaderS = 92,
        NewLeaderYou = 93,
        NewGuideS = 94,
        NewGuideYou = 95,
        LeftGroupS = 96,
        LeftGroupYou = 97,
        GroupDisbanded = 98,
        DeclineGroupS = 99,
        DeclineGroupRequestS = 100,
        JoinedGroupS = 101,
        UninviteYou = 102,
        BadPlayerNameS = 103,
        NotInGroup = 104,
        TargetNotInGroupS = 105,
        TargetNotInInstanceS = 106,
        NotInInstanceGroup = 107,
        GroupFull = 108,
        NotLeader = 109,
        PlayerDiedS = 110,
        GuildCreateS = 111,
        GuildInviteS = 112,
        InvitedToGuildSss = 113,
        AlreadyInGuildS = 114,
        AlreadyInvitedToGuildS = 115,
        InvitedToGuild = 116,
        AlreadyInGuild = 117,
        GuildAccept = 118,
        GuildDeclineS = 119,
        GuildDeclineAutoS = 120,
        GuildPermissions = 121,
        GuildJoinS = 122,
        GuildFounderS = 123,
        GuildPromoteSss = 124,
        GuildDemoteSs = 125,
        GuildDemoteSss = 126,
        GuildInviteSelf = 127,
        GuildQuitS = 128,
        GuildLeaveS = 129,
        GuildRemoveSs = 130,
        GuildRemoveSelf = 131,
        GuildDisbandS = 132,
        GuildDisbandSelf = 133,
        GuildLeaderS = 134,
        GuildLeaderSelf = 135,
        GuildPlayerNotFoundS = 136,
        GuildPlayerNotInGuildS = 137,
        GuildPlayerNotInGuild = 138,
        GuildBankNotAvailable = 139,
        GuildCantPromoteS = 140,
        GuildCantDemoteS = 141,
        GuildNotInAGuild = 142,
        GuildInternal = 143,
        GuildLeaderIsS = 144,
        GuildLeaderChangedSs = 145,
        GuildDisbanded = 146,
        GuildNotAllied = 147,
        GuildNewLeaderNotAllied = 148,
        GuildLeaderLeave = 149,
        GuildRanksLocked = 150,
        GuildRankInUse = 151,
        GuildRankTooHighS = 152,
        GuildRankTooLowS = 153,
        GuildNameExistsS = 154,
        GuildWithdrawLimit = 155,
        GuildNotEnoughMoney = 156,
        GuildTooMuchMoney = 157,
        GuildBankConjuredItem = 158,
        GuildBankEquippedItem = 159,
        GuildBankBoundItem = 160,
        GuildBankQuestItem = 161,
        GuildBankWrappedItem = 162,
        GuildBankFull = 163,
        GuildBankWrongTab = 164,
        GuildNewLeaderWrongRealm = 165,
        NoGuildCharter = 166,
        OutOfRange = 167,
        PlayerDead = 168,
        ClientLockedOut = 169,
        ClientOnTransport = 170,
        KilledByS = 171,
        LootLocked = 172,
        LootTooFar = 173,
        LootDidntKill = 174,
        LootBadFacing = 175,
        LootNotstanding = 176,
        LootStunned = 177,
        LootNoUi = 178,
        LootWhileInvulnerable = 179,
        NoLoot = 180,
        QuestAcceptedS = 181,
        QuestCompleteS = 182,
        QuestFailedS = 183,
        QuestFailedBagFullS = 184,
        QuestFailedMaxCountS = 185,
        QuestFailedLowLevel = 186,
        QuestFailedMissingItems = 187,
        QuestFailedWrongRace = 188,
        QuestFailedNotEnoughMoney = 189,
        QuestFailedExpansion = 190,
        QuestOnlyOneTimed = 191,
        QuestNeedPrereqs = 192,
        QuestNeedPrereqsCustom = 193,
        QuestAlreadyOn = 194,
        QuestAlreadyDone = 195,
        QuestAlreadyDoneDaily = 196,
        QuestHasInProgress = 197,
        QuestRewardExpI = 198,
        QuestRewardMoneyS = 199,
        QuestMustChoose = 200,
        QuestLogFull = 201,
        CombatDamageSsi = 202,
        InspectS = 203,
        CantUseItem = 204,
        CantUseItemInArena = 205,
        CantUseItemInRatedBattleground = 206,
        MustEquipItem = 207,
        PassiveAbility = 208,
        H2SkillNotFound = 209,
        NoAttackTarget = 210,
        InvalidAttackTarget = 211,
        AttackPvpTargetWhileUnflagged = 212,
        AttackStunned = 213,
        AttackPacified = 214,
        AttackMounted = 215,
        AttackFleeing = 216,
        AttackConfused = 217,
        AttackCharmed = 218,
        AttackDead = 219,
        AttackPreventedByMechanicS = 220,
        AttackChannel = 221,
        Taxisamenode = 222,
        Taxinosuchpath = 223,
        Taxiunspecifiedservererror = 224,
        Taxinotenoughmoney = 225,
        Taxitoofaraway = 226,
        Taxinovendornearby = 227,
        Taxinotvisited = 228,
        Taxiplayerbusy = 229,
        Taxiplayeralreadymounted = 230,
        Taxiplayershapeshifted = 231,
        Taxiplayermoving = 232,
        Taxinopaths = 233,
        Taxinoteligible = 234,
        Taxinotstanding = 235,
        Taxiincombat = 236,
        NoReplyTarget = 237,
        GenericNoTarget = 238,
        InitiateTradeS = 239,
        TradeRequestS = 240,
        TradeBlockedS = 241,
        TradeTargetDead = 242,
        TradeTooFar = 243,
        TradeCancelled = 244,
        TradeComplete = 245,
        TradeBagFull = 246,
        TradeTargetBagFull = 247,
        TradeMaxCountExceeded = 248,
        TradeTargetMaxCountExceeded = 249,
        InventoryTradeTooManyUniqueItem = 250,
        AlreadyTrading = 251,
        MountInvalidmountee = 252,
        MountToofaraway = 253,
        MountAlreadymounted = 254,
        MountNotmountable = 255,
        MountNotyourpet = 256,
        MountOther = 257,
        MountLooting = 258,
        MountRacecantmount = 259,
        MountShapeshifted = 260,
        MountNoFavorites = 261,
        MountNoMounts = 262,
        DismountNopet = 263,
        DismountNotmounted = 264,
        DismountNotyourpet = 265,
        SpellFailedTotems = 266,
        SpellFailedReagents = 267,
        SpellFailedReagentsGeneric = 268,
        SpellFailedOptionalReagents = 269,
        CantTradeGold = 270,
        SpellFailedEquippedItem = 271,
        SpellFailedEquippedItemClassS = 272,
        SpellFailedShapeshiftFormS = 273,
        SpellFailedAnotherInProgress = 274,
        Badattackfacing = 275,
        Badattackpos = 276,
        ChestInUse = 277,
        UseCantOpen = 278,
        UseLocked = 279,
        DoorLocked = 280,
        ButtonLocked = 281,
        UseLockedWithItemS = 282,
        UseLockedWithSpellS = 283,
        UseLockedWithSpellKnownSi = 284,
        UseTooFar = 285,
        UseBadAngle = 286,
        UseObjectMoving = 287,
        UseSpellFocus = 288,
        UseDestroyed = 289,
        SetLootFreeforall = 290,
        SetLootRoundrobin = 291,
        SetLootMaster = 292,
        SetLootGroup = 293,
        SetLootThresholdS = 294,
        NewLootMasterS = 295,
        SpecifyMasterLooter = 296,
        LootSpecChangedS = 297,
        TameFailed = 298,
        ChatWhileDead = 299,
        ChatPlayerNotFoundS = 300,
        Newtaxipath = 301,
        NoPet = 302,
        Notyourpet = 303,
        PetNotRenameable = 304,
        QuestObjectiveCompleteS = 305,
        QuestUnknownComplete = 306,
        QuestAddKillSii = 307,
        QuestAddFoundSii = 308,
        QuestAddItemSii = 309,
        QuestAddPlayerKillSii = 310,
        Cannotcreatedirectory = 311,
        Cannotcreatefile = 312,
        PlayerWrongFaction = 313,
        PlayerIsNeutral = 314,
        BankslotFailedTooMany = 315,
        BankslotInsufficientFunds = 316,
        BankslotNotbanker = 317,
        FriendDbError = 318,
        FriendListFull = 319,
        FriendAddedS = 320,
        BattletagFriendAddedS = 321,
        FriendOnlineSs = 322,
        FriendOfflineS = 323,
        FriendNotFound = 324,
        FriendWrongFaction = 325,
        FriendRemovedS = 326,
        BattletagFriendRemovedS = 327,
        FriendError = 328,
        FriendAlreadyS = 329,
        FriendSelf = 330,
        FriendDeleted = 331,
        IgnoreFull = 332,
        IgnoreSelf = 333,
        IgnoreNotFound = 334,
        IgnoreAlreadyS = 335,
        IgnoreAddedS = 336,
        IgnoreRemovedS = 337,
        IgnoreAmbiguous = 338,
        IgnoreDeleted = 339,
        OnlyOneBolt = 340,
        OnlyOneAmmo = 341,
        SpellFailedEquippedSpecificItem = 342,
        WrongBagTypeSubclass = 343,
        CantWrapStackable = 344,
        CantWrapEquipped = 345,
        CantWrapWrapped = 346,
        CantWrapBound = 347,
        CantWrapUnique = 348,
        CantWrapBags = 349,
        OutOfMana = 350,
        OutOfRage = 351,
        OutOfFocus = 352,
        OutOfEnergy = 353,
        OutOfChi = 354,
        OutOfHealth = 355,
        OutOfRunes = 356,
        OutOfRunicPower = 357,
        OutOfSoulShards = 358,
        OutOfLunarPower = 359,
        OutOfHolyPower = 360,
        OutOfMaelstrom = 361,
        OutOfComboPoints = 362,
        OutOfInsanity = 363,
        OutOfEssence = 364,
        OutOfArcaneCharges = 365,
        OutOfFury = 366,
        OutOfPain = 367,
        OutOfPowerDisplay = 368,
        LootGone = 369,
        MountForceddismount = 370,
        AutofollowTooFar = 371,
        UnitNotFound = 372,
        InvalidFollowTarget = 373,
        InvalidFollowPvpCombat = 374,
        InvalidFollowTargetPvpCombat = 375,
        InvalidInspectTarget = 376,
        GuildemblemSuccess = 377,
        GuildemblemInvalidTabardColors = 378,
        GuildemblemNoguild = 379,
        GuildemblemNotguildmaster = 380,
        GuildemblemNotenoughmoney = 381,
        GuildemblemInvalidvendor = 382,
        EmblemerrorNotabardgeoset = 383,
        SpellOutOfRange = 384,
        CommandNeedsTarget = 385,
        NoammoS = 386,
        Toobusytofollow = 387,
        DuelRequested = 388,
        DuelCancelled = 389,
        Deathbindalreadybound = 390,
        DeathbindSuccessS = 391,
        Noemotewhilerunning = 392,
        ZoneExplored = 393,
        ZoneExploredXp = 394,
        InvalidItemTarget = 395,
        InvalidQuestTarget = 396,
        IgnoringYouS = 397,
        FishNotHooked = 398,
        FishEscaped = 399,
        SpellFailedNotunsheathed = 400,
        PetitionOfferedS = 401,
        PetitionSigned = 402,
        PetitionSignedS = 403,
        PetitionDeclinedS = 404,
        PetitionAlreadySigned = 405,
        PetitionRestrictedAccountTrial = 406,
        PetitionAlreadySignedOther = 407,
        PetitionInGuild = 408,
        PetitionCreator = 409,
        PetitionNotEnoughSignatures = 410,
        PetitionNotSameServer = 411,
        PetitionFull = 412,
        PetitionAlreadySignedByS = 413,
        GuildNameInvalid = 414,
        SpellUnlearnedS = 415,
        PetSpellRooted = 416,
        PetSpellAffectingCombat = 417,
        PetSpellOutOfRange = 418,
        PetSpellNotBehind = 419,
        PetSpellTargetsDead = 420,
        PetSpellDead = 421,
        PetSpellNopath = 422,
        ItemCantBeDestroyed = 423,
        TicketAlreadyExists = 424,
        TicketCreateError = 425,
        TicketUpdateError = 426,
        TicketDbError = 427,
        TicketNoText = 428,
        TicketTextTooLong = 429,
        ObjectIsBusy = 430,
        ExhaustionWellrested = 431,
        ExhaustionRested = 432,
        ExhaustionNormal = 433,
        ExhaustionTired = 434,
        ExhaustionExhausted = 435,
        NoItemsWhileShapeshifted = 436,
        CantInteractShapeshifted = 437,
        RealmNotFound = 438,
        MailQuestItem = 439,
        MailBoundItem = 440,
        MailConjuredItem = 441,
        MailBag = 442,
        MailToSelf = 443,
        MailTargetNotFound = 444,
        MailDatabaseError = 445,
        MailDeleteItemError = 446,
        MailWrappedCod = 447,
        MailCantSendRealm = 448,
        MailTempReturnOutage = 449,
        MailRecepientCantReceiveMail = 450,
        MailSent = 451,
        MailTargetIsTrial = 452,
        NotHappyEnough = 453,
        UseCantImmune = 454,
        CantBeDisenchanted = 455,
        CantUseDisarmed = 456,
        AuctionDatabaseError = 457,
        AuctionHigherBid = 458,
        AuctionAlreadyBid = 459,
        AuctionOutbidS = 460,
        AuctionWonS = 461,
        AuctionRemovedS = 462,
        AuctionBidPlaced = 463,
        LogoutFailed = 464,
        QuestPushSuccessS = 465,
        QuestPushInvalidS = 466,
        QuestPushInvalidToRecipientS = 467,
        QuestPushAcceptedS = 468,
        QuestPushDeclinedS = 469,
        QuestPushBusyS = 470,
        QuestPushDeadS = 471,
        QuestPushDeadToRecipientS = 472,
        QuestPushLogFullS = 473,
        QuestPushLogFullToRecipientS = 474,
        QuestPushOnquestS = 475,
        QuestPushOnquestToRecipientS = 476,
        QuestPushAlreadyDoneS = 477,
        QuestPushAlreadyDoneToRecipientS = 478,
        QuestPushNotDailyS = 479,
        QuestPushTimerExpiredS = 480,
        QuestPushNotInPartyS = 481,
        QuestPushDifferentServerDailyS = 482,
        QuestPushDifferentServerDailyToRecipientS = 483,
        QuestPushNotAllowedS = 484,
        QuestPushPrerequisiteS = 485,
        QuestPushPrerequisiteToRecipientS = 486,
        QuestPushLowLevelS = 487,
        QuestPushLowLevelToRecipientS = 488,
        QuestPushHighLevelS = 489,
        QuestPushHighLevelToRecipientS = 490,
        QuestPushClassS = 491,
        QuestPushClassToRecipientS = 492,
        QuestPushRaceS = 493,
        QuestPushRaceToRecipientS = 494,
        QuestPushLowFactionS = 495,
        QuestPushLowFactionToRecipientS = 496,
        QuestPushHighFactionS = 497,
        QuestPushHighFactionToRecipientS = 498,
        QuestPushExpansionS = 499,
        QuestPushExpansionToRecipientS = 500,
        QuestPushNotGarrisonOwnerS = 501,
        QuestPushNotGarrisonOwnerToRecipientS = 502,
        QuestPushWrongCovenantS = 503,
        QuestPushWrongCovenantToRecipientS = 504,
        QuestPushNewPlayerExperienceS = 505,
        QuestPushNewPlayerExperienceToRecipientS = 506,
        QuestPushWrongFactionS = 507,
        QuestPushWrongFactionToRecipientS = 508,
        QuestPushCrossFactionRestrictedS = 509,
        RaidGroupLowlevel = 510,
        RaidGroupOnly = 511,
        RaidGroupFull = 512,
        RaidGroupRequirementsUnmatch = 513,
        CorpseIsNotInInstance = 514,
        PvpKillHonorable = 515,
        PvpKillDishonorable = 516,
        SpellFailedAlreadyAtFullHealth = 517,
        SpellFailedAlreadyAtFullMana = 518,
        SpellFailedAlreadyAtFullPowerS = 519,
        AutolootMoneyS = 520,
        GenericStunned = 521,
        GenericThrottle = 522,
        ClubFinderSearchingTooFast = 523,
        TargetStunned = 524,
        MustRepairDurability = 525,
        RaidYouJoined = 526,
        RaidYouLeft = 527,
        InstanceGroupJoinedWithParty = 528,
        InstanceGroupJoinedWithRaid = 529,
        RaidMemberAddedS = 530,
        RaidMemberRemovedS = 531,
        InstanceGroupAddedS = 532,
        InstanceGroupRemovedS = 533,
        ClickOnItemToFeed = 534,
        TooManyChatChannels = 535,
        LootRollPending = 536,
        LootPlayerNotFound = 537,
        NotInRaid = 538,
        LoggingOut = 539,
        TargetLoggingOut = 540,
        NotWhileMounted = 541,
        NotWhileShapeshifted = 542,
        NotInCombat = 543,
        NotWhileDisarmed = 544,
        PetBroken = 545,
        TalentWipeError = 546,
        SpecWipeError = 547,
        GlyphWipeError = 548,
        PetSpecWipeError = 549,
        FeignDeathResisted = 550,
        MeetingStoneInQueueS = 551,
        MeetingStoneLeftQueueS = 552,
        MeetingStoneOtherMemberLeft = 553,
        MeetingStonePartyKickedFromQueue = 554,
        MeetingStoneMemberStillInQueue = 555,
        MeetingStoneSuccess = 556,
        MeetingStoneInProgress = 557,
        MeetingStoneMemberAddedS = 558,
        MeetingStoneGroupFull = 559,
        MeetingStoneNotLeader = 560,
        MeetingStoneInvalidLevel = 561,
        MeetingStoneTargetNotInParty = 562,
        MeetingStoneTargetInvalidLevel = 563,
        MeetingStoneMustBeLeader = 564,
        MeetingStoneNoRaidGroup = 565,
        MeetingStoneNeedParty = 566,
        MeetingStoneNotFound = 567,
        MeetingStoneTargetInVehicle = 568,
        GuildemblemSame = 569,
        EquipTradeItem = 570,
        PvpToggleOn = 571,
        PvpToggleOff = 572,
        GroupJoinBattlegroundDeserters = 573,
        GroupJoinBattlegroundDead = 574,
        GroupJoinBattlegroundS = 575,
        GroupJoinBattlegroundFail = 576,
        GroupJoinBattlegroundTooMany = 577,
        SoloJoinBattlegroundS = 578,
        JoinSingleScenarioS = 579,
        BattlegroundTooManyQueues = 580,
        BattlegroundCannotQueueForRated = 581,
        BattledgroundQueuedForRated = 582,
        BattlegroundTeamLeftQueue = 583,
        BattlegroundNotInBattleground = 584,
        AlreadyInArenaTeamS = 585,
        InvalidPromotionCode = 586,
        BgPlayerJoinedSs = 587,
        BgPlayerLeftS = 588,
        RestrictedAccount = 589,
        RestrictedAccountTrial = 590,
        NotEnoughPurchasedGameTime = 591,
        PlayTimeExceeded = 592,
        ApproachingPartialPlayTime = 593,
        ApproachingPartialPlayTime2 = 594,
        ApproachingNoPlayTime = 595,
        ApproachingNoPlayTime2 = 596,
        UnhealthyTime = 597,
        ChatRestrictedTrial = 598,
        ChatThrottled = 599,
        MailReachedCap = 600,
        InvalidRaidTarget = 601,
        RaidLeaderReadyCheckStartS = 602,
        ReadyCheckInProgress = 603,
        ReadyCheckThrottled = 604,
        DungeonDifficultyFailed = 605,
        DungeonDifficultyChangedS = 606,
        TradeWrongRealm = 607,
        TradeNotOnTaplist = 608,
        ChatPlayerAmbiguousS = 609,
        LootCantLootThatNow = 610,
        LootMasterInvFull = 611,
        LootMasterUniqueItem = 612,
        LootMasterOther = 613,
        FilteringYouS = 614,
        UsePreventedByMechanicS = 615,
        ItemUniqueEquippable = 616,
        LfgLeaderIsLfmS = 617,
        LfgPending = 618,
        CantSpeakLangage = 619,
        VendorMissingTurnins = 620,
        BattlegroundNotInTeam = 621,
        NotInBattleground = 622,
        NotEnoughHonorPoints = 623,
        NotEnoughArenaPoints = 624,
        SocketingRequiresMetaGem = 625,
        SocketingMetaGemOnlyInMetaslot = 626,
        SocketingRequiresHydraulicGem = 627,
        SocketingHydraulicGemOnlyInHydraulicslot = 628,
        SocketingRequiresCogwheelGem = 629,
        SocketingCogwheelGemOnlyInCogwheelslot = 630,
        SocketingItemTooLowLevel = 631,
        ItemMaxCountSocketed = 632,
        SystemDisabled = 633,
        QuestFailedTooManyDailyQuestsI = 634,
        ItemMaxCountEquippedSocketed = 635,
        ItemUniqueEquippableSocketed = 636,
        UserSquelched = 637,
        AccountSilenced = 638,
        PartyMemberSilenced = 639,
        PartyMemberSilencedLfgDelist = 640,
        TooMuchGold = 641,
        NotBarberSitting = 642,
        QuestFailedCais = 643,
        InviteRestrictedTrial = 644,
        VoiceIgnoreFull = 645,
        VoiceIgnoreSelf = 646,
        VoiceIgnoreNotFound = 647,
        VoiceIgnoreAlreadyS = 648,
        VoiceIgnoreAddedS = 649,
        VoiceIgnoreRemovedS = 650,
        VoiceIgnoreAmbiguous = 651,
        VoiceIgnoreDeleted = 652,
        UnknownMacroOptionS = 653,
        NotDuringArenaMatch = 654,
        NotInRatedBattleground = 655,
        PlayerSilenced = 656,
        PlayerUnsilenced = 657,
        ComsatDisconnect = 658,
        ComsatReconnectAttempt = 659,
        ComsatConnectFail = 660,
        MailInvalidAttachmentSlot = 661,
        MailTooManyAttachments = 662,
        MailInvalidAttachment = 663,
        MailAttachmentExpired = 664,
        VoiceChatParentalDisableMic = 665,
        ProfaneChatName = 666,
        PlayerSilencedEcho = 667,
        PlayerUnsilencedEcho = 668,
        LootCantLootThat = 669,
        ArenaExpiredCais = 670,
        GroupActionThrottled = 671,
        AlreadyPickpocketed = 672,
        NameInvalid = 673,
        NameNoName = 674,
        NameTooShort = 675,
        NameTooLong = 676,
        NameMixedLanguages = 677,
        NameProfane = 678,
        NameReserved = 679,
        NameThreeConsecutive = 680,
        NameInvalidSpace = 681,
        NameConsecutiveSpaces = 682,
        NameRussianConsecutiveSilentCharacters = 683,
        NameRussianSilentCharacterAtBeginningOrEnd = 684,
        NameDeclensionDoesntMatchBaseName = 685,
        RecruitAFriendNotLinked = 686,
        RecruitAFriendNotNow = 687,
        RecruitAFriendSummonLevelMax = 688,
        RecruitAFriendSummonCooldown = 689,
        RecruitAFriendSummonOffline = 690,
        RecruitAFriendInsufExpanLvl = 691,
        RecruitAFriendMapIncomingTransferNotAllowed = 692,
        NotSameAccount = 693,
        BadOnUseEnchant = 694,
        TradeSelf = 695,
        TooManySockets = 696,
        ItemMaxLimitCategoryCountExceededIs = 697,
        TradeTargetMaxLimitCategoryCountExceededIs = 698,
        ItemMaxLimitCategorySocketedExceededIs = 699,
        ItemMaxLimitCategoryEquippedExceededIs = 700,
        ShapeshiftFormCannotEquip = 701,
        ItemInventoryFullSatchel = 702,
        ScalingStatItemLevelExceeded = 703,
        ScalingStatItemLevelTooLow = 704,
        PurchaseLevelTooLow = 705,
        GroupSwapFailed = 706,
        InviteInCombat = 707,
        InvalidGlyphSlot = 708,
        GenericNoValidTargets = 709,
        CalendarEventAlertS = 710,
        PetLearnSpellS = 711,
        PetLearnAbilityS = 712,
        PetSpellUnlearnedS = 713,
        InviteUnknownRealm = 714,
        InviteNoPartyServer = 715,
        InvitePartyBusy = 716,
        InvitePartyBusyPendingRequest = 717,
        InvitePartyBusyPendingSuggest = 718,
        PartyTargetAmbiguous = 719,
        PartyLfgInviteRaidLocked = 720,
        PartyLfgBootLimit = 721,
        PartyLfgBootCooldownS = 722,
        PartyLfgBootNotEligibleS = 723,
        PartyLfgBootInpatientTimerS = 724,
        PartyLfgBootInProgress = 725,
        PartyLfgBootTooFewPlayers = 726,
        PartyLfgBootVoteSucceeded = 727,
        PartyLfgBootVoteFailed = 728,
        PartyLfgBootDisallowedByMap = 729,
        PartyLfgBootDungeonComplete = 730,
        PartyLfgBootLootRolls = 731,
        PartyLfgBootVoteRegistered = 732,
        PartyPrivateGroupOnly = 733,
        PartyLfgTeleportInCombat = 734,
        PartyTimeRunningSeasonIdMustMatch = 735,
        RaidDisallowedByLevel = 736,
        RaidDisallowedByCrossRealm = 737,
        PartyRoleNotAvailable = 738,
        JoinLfgObjectFailed = 739,
        LfgRemovedLevelup = 740,
        LfgRemovedXpToggle = 741,
        LfgRemovedFactionChange = 742,
        BattlegroundInfoThrottled = 743,
        BattlegroundAlreadyIn = 744,
        ArenaTeamChangeFailedQueued = 745,
        ArenaTeamPermissions = 746,
        NotWhileFalling = 747,
        NotWhileMoving = 748,
        NotWhileFatigued = 749,
        MaxSockets = 750,
        MultiCastActionTotemS = 751,
        BattlegroundJoinLevelup = 752,
        RemoveFromPvpQueueXpGain = 753,
        BattlegroundJoinXpGain = 754,
        BattlegroundJoinMercenary = 755,
        BattlegroundJoinTooManyHealers = 756,
        BattlegroundJoinRatedTooManyHealers = 757,
        BattlegroundJoinTooManyTanks = 758,
        BattlegroundJoinTooManyDamage = 759,
        RaidDifficultyFailed = 760,
        RaidDifficultyChangedS = 761,
        LegacyRaidDifficultyChangedS = 762,
        RaidLockoutChangedS = 763,
        RaidConvertedToParty = 764,
        PartyConvertedToRaid = 765,
        PlayerDifficultyChangedS = 766,
        GmresponseDbError = 767,
        BattlegroundJoinRangeIndex = 768,
        ArenaJoinRangeIndex = 769,
        RemoveFromPvpQueueFactionChange = 770,
        BattlegroundJoinFailed = 771,
        BattlegroundJoinNoValidSpecForRole = 772,
        BattlegroundJoinRespec = 773,
        BattlegroundInvitationDeclined = 774,
        BattlegroundInvitationDeclinedBy = 775,
        BattlegroundJoinTimedOut = 776,
        BattlegroundDupeQueue = 777,
        BattlegroundJoinMustCompleteQuest = 778,
        InBattlegroundRespec = 779,
        MailLimitedDurationItem = 780,
        YellRestrictedTrial = 781,
        ChatRaidRestrictedTrial = 782,
        LfgRoleCheckFailed = 783,
        LfgRoleCheckFailedTimeout = 784,
        LfgRoleCheckFailedNotViable = 785,
        LfgReadyCheckFailed = 786,
        LfgReadyCheckFailedTimeout = 787,
        LfgGroupFull = 788,
        LfgNoLfgObject = 789,
        LfgNoSlotsPlayer = 790,
        LfgNoSlotsParty = 791,
        LfgNoSpec = 792,
        LfgMismatchedSlots = 793,
        LfgMismatchedSlotsLocalXrealm = 794,
        LfgPartyPlayersFromDifferentRealms = 795,
        LfgMembersNotPresent = 796,
        LfgGetInfoTimeout = 797,
        LfgInvalidSlot = 798,
        LfgDeserterPlayer = 799,
        LfgDeserterParty = 800,
        LfgDead = 801,
        LfgRandomCooldownPlayer = 802,
        LfgRandomCooldownParty = 803,
        LfgTooManyMembers = 804,
        LfgTooFewMembers = 805,
        LfgProposalFailed = 806,
        LfgProposalDeclinedSelf = 807,
        LfgProposalDeclinedParty = 808,
        LfgNoSlotsSelected = 809,
        LfgNoRolesSelected = 810,
        LfgRoleCheckInitiated = 811,
        LfgReadyCheckInitiated = 812,
        LfgPlayerDeclinedRoleCheck = 813,
        LfgPlayerDeclinedReadyCheck = 814,
        LfgJoinedQueue = 815,
        LfgJoinedFlexQueue = 816,
        LfgJoinedRfQueue = 817,
        LfgJoinedScenarioQueue = 818,
        LfgJoinedWorldPvpQueue = 819,
        LfgJoinedBattlefieldQueue = 820,
        LfgJoinedList = 821,
        LfgLeftQueue = 822,
        LfgLeftList = 823,
        LfgRoleCheckAborted = 824,
        LfgReadyCheckAborted = 825,
        LfgCantUseBattleground = 826,
        LfgCantUseDungeons = 827,
        LfgReasonTooManyLfg = 828,
        LfgFarmLimit = 829,
        LfgNoCrossFactionParties = 830,
        InvalidTeleportLocation = 831,
        TooFarToInteract = 832,
        BattlegroundPlayersFromDifferentRealms = 833,
        DifficultyChangeCooldownS = 834,
        DifficultyChangeCombatCooldownS = 835,
        DifficultyChangeWorldstate = 836,
        DifficultyChangeEncounter = 837,
        DifficultyChangeCombat = 838,
        DifficultyChangePlayerBusy = 839,
        DifficultyChangePlayerOnVehicle = 840,
        DifficultyChangeAlreadyStarted = 841,
        DifficultyChangeOtherHeroicS = 842,
        DifficultyChangeHeroicInstanceAlreadyRunning = 843,
        ArenaTeamPartySize = 844,
        SoloShuffleWargameGroupSize = 845,
        SoloShuffleWargameGroupComp = 846,
        SoloRbgWargameGroupSize = 847,
        SoloRbgWargameGroupComp = 848,
        SoloMinItemLevel = 849,
        PvpPlayerAbandoned = 850,
        BattlegroundJoinGroupQueueWithoutHealer = 851,
        QuestForceRemovedS = 852,
        AttackNoActions = 853,
        InRandomBg = 854,
        InNonRandomBg = 855,
        BnFriendSelf = 856,
        BnFriendAlready = 857,
        BnFriendBlocked = 858,
        BnFriendListFull = 859,
        BnFriendRequestSent = 860,
        BnBroadcastThrottle = 861,
        BgDeveloperOnly = 862,
        CurrencySpellSlotMismatch = 863,
        CurrencyNotTradable = 864,
        RequiresExpansionS = 865,
        QuestFailedSpell = 866,
        TalentFailedUnspentTalentPoints = 867,
        TalentFailedNotEnoughTalentsInPrimaryTree = 868,
        TalentFailedNoPrimaryTreeSelected = 869,
        TalentFailedCantRemoveTalent = 870,
        TalentFailedUnknown = 871,
        TalentFailedInCombat = 872,
        TalentFailedInPvpMatch = 873,
        TalentFailedInMythicPlus = 874,
        WargameRequestFailure = 875,
        RankRequiresAuthenticator = 876,
        GuildBankVoucherFailed = 877,
        WargameRequestSent = 878,
        RequiresAchievementI = 879,
        RefundResultExceedMaxCurrency = 880,
        CantBuyQuantity = 881,
        ItemIsBattlePayLocked = 882,
        PartyAlreadyInBattlegroundQueue = 883,
        PartyConfirmingBattlegroundQueue = 884,
        BattlefieldTeamPartySize = 885,
        InsuffTrackedCurrencyIs = 886,
        NotOnTournamentRealm = 887,
        GuildTrialAccountTrial = 888,
        GuildTrialAccountVeteran = 889,
        GuildUndeletableDueToLevel = 890,
        CantDoThatInAGroup = 891,
        GuildLeaderReplaced = 892,
        TransmogrifyCantEquip = 893,
        TransmogrifyInvalidItemType = 894,
        TransmogrifyNotSoulbound = 895,
        TransmogrifyInvalidSource = 896,
        TransmogrifyInvalidDestination = 897,
        TransmogrifyMismatch = 898,
        TransmogrifyLegendary = 899,
        TransmogrifySameItem = 900,
        TransmogrifySameAppearance = 901,
        TransmogrifyNotEquipped = 902,
        VoidDepositFull = 903,
        VoidWithdrawFull = 904,
        VoidStorageWrapped = 905,
        VoidStorageStackable = 906,
        VoidStorageUnbound = 907,
        VoidStorageRepair = 908,
        VoidStorageCharges = 909,
        VoidStorageQuest = 910,
        VoidStorageConjured = 911,
        VoidStorageMail = 912,
        VoidStorageBag = 913,
        VoidTransferStorageFull = 914,
        VoidTransferInvFull = 915,
        VoidTransferInternalError = 916,
        VoidTransferItemInvalid = 917,
        DifficultyDisabledInLfg = 918,
        VoidStorageUnique = 919,
        VoidStorageLoot = 920,
        VoidStorageHoliday = 921,
        VoidStorageDuration = 922,
        VoidStorageLoadFailed = 923,
        VoidStorageInvalidItem = 924,
        VoidStorageAccountItem = 925,
        ParentalControlsChatMuted = 926,
        SorStartExperienceIncomplete = 927,
        SorInvalidEmail = 928,
        SorInvalidComment = 929,
        ChallengeModeResetCooldownS = 930,
        ChallengeModeResetKeystone = 931,
        PetJournalAlreadyInLoadout = 932,
        ReportSubmittedSuccessfully = 933,
        ReportSubmissionFailed = 934,
        SuggestionSubmittedSuccessfully = 935,
        BugSubmittedSuccessfully = 936,
        ChallengeModeEnabled = 937,
        ChallengeModeDisabled = 938,
        PetbattleCreateFailed = 939,
        PetbattleNotHere = 940,
        PetbattleNotHereOnTransport = 941,
        PetbattleNotHereUnevenGround = 942,
        PetbattleNotHereObstructed = 943,
        PetbattleNotWhileInCombat = 944,
        PetbattleNotWhileDead = 945,
        PetbattleNotWhileFlying = 946,
        PetbattleTargetInvalid = 947,
        PetbattleTargetOutOfRange = 948,
        PetbattleTargetNotCapturable = 949,
        PetbattleNotATrainer = 950,
        PetbattleDeclined = 951,
        PetbattleInBattle = 952,
        PetbattleInvalidLoadout = 953,
        PetbattleAllPetsDead = 954,
        PetbattleNoPetsInSlots = 955,
        PetbattleNoAccountLock = 956,
        PetbattleWildPetTapped = 957,
        PetbattleRestrictedAccount = 958,
        PetbattleOpponentNotAvailable = 959,
        PetbattleNotWhileInMatchedBattle = 960,
        CantHaveMorePetsOfThatType = 961,
        CantHaveMorePets = 962,
        PvpMapNotFound = 963,
        PvpMapNotSet = 964,
        PetbattleQueueQueued = 965,
        PetbattleQueueAlreadyQueued = 966,
        PetbattleQueueJoinFailed = 967,
        PetbattleQueueJournalLock = 968,
        PetbattleQueueRemoved = 969,
        PetbattleQueueProposalDeclined = 970,
        PetbattleQueueProposalTimeout = 971,
        PetbattleQueueOpponentDeclined = 972,
        PetbattleQueueRequeuedInternal = 973,
        PetbattleQueueRequeuedRemoved = 974,
        PetbattleQueueSlotLocked = 975,
        PetbattleQueueSlotEmpty = 976,
        PetbattleQueueSlotNoTracker = 977,
        PetbattleQueueSlotNoSpecies = 978,
        PetbattleQueueSlotCantBattle = 979,
        PetbattleQueueSlotRevoked = 980,
        PetbattleQueueSlotDead = 981,
        PetbattleQueueSlotNoPet = 982,
        PetbattleQueueNotWhileNeutral = 983,
        PetbattleGameTimeLimitWarning = 984,
        PetbattleGameRoundsLimitWarning = 985,
        HasRestriction = 986,
        ItemUpgradeItemTooLowLevel = 987,
        ItemUpgradeNoPath = 988,
        ItemUpgradeNoMoreUpgrades = 989,
        BonusRollEmpty = 990,
        ChallengeModeFull = 991,
        ChallengeModeInProgress = 992,
        ChallengeModeIncorrectKeystone = 993,
        BattletagFriendNotFound = 994,
        BattletagFriendNotValid = 995,
        BattletagFriendNotAllowed = 996,
        BattletagFriendThrottled = 997,
        BattletagFriendSuccess = 998,
        PetTooHighLevelToUncage = 999,
        PetbattleInternal = 1000,
        CantCagePetYet = 1001,
        NoLootInChallengeMode = 1002,
        QuestPetBattleVictoriesPvpIi = 1003,
        RoleCheckAlreadyInProgress = 1004,
        RecruitAFriendAccountLimit = 1005,
        RecruitAFriendFailed = 1006,
        SetLootPersonal = 1007,
        SetLootMethodFailedCombat = 1008,
        ReagentBankFull = 1009,
        ReagentBankLocked = 1010,
        GarrisonBuildingExists = 1011,
        GarrisonInvalidPlot = 1012,
        GarrisonInvalidBuildingid = 1013,
        GarrisonInvalidPlotBuilding = 1014,
        GarrisonRequiresBlueprint = 1015,
        GarrisonNotEnoughCurrency = 1016,
        GarrisonNotEnoughGold = 1017,
        GarrisonCompleteMissionWrongFollowerType = 1018,
        AlreadyUsingLfgList = 1019,
        RestrictedAccountLfgListTrial = 1020,
        ToyUseLimitReached = 1021,
        ToyAlreadyKnown = 1022,
        TransmogSetAlreadyKnown = 1023,
        NotEnoughCurrency = 1024,
        SpecIsDisabled = 1025,
        FeatureRestrictedTrial = 1026,
        CantBeObliterated = 1027,
        CantBeScrapped = 1028,
        CantBeRecrafted = 1029,
        ArtifactRelicDoesNotMatchArtifact = 1030,
        MustEquipArtifact = 1031,
        CantDoThatRightNow = 1032,
        AffectingCombat = 1033,
        EquipmentManagerCombatSwapS = 1034,
        EquipmentManagerBagsFull = 1035,
        EquipmentManagerMissingItemS = 1036,
        MovieRecordingWarningPerf = 1037,
        MovieRecordingWarningDiskFull = 1038,
        MovieRecordingWarningNoMovie = 1039,
        MovieRecordingWarningRequirements = 1040,
        MovieRecordingWarningCompressing = 1041,
        NoChallengeModeReward = 1042,
        ClaimedChallengeModeReward = 1043,
        ChallengeModePeriodResetSs = 1044,
        CantDoThatChallengeModeActive = 1045,
        TalentFailedRestArea = 1046,
        CannotAbandonLastPet = 1047,
        TestCvarSetSss = 1048,
        QuestTurnInFailReason = 1049,
        ClaimedChallengeModeRewardOld = 1050,
        TalentGrantedByAura = 1051,
        ChallengeModeAlreadyComplete = 1052,
        GlyphTargetNotAvailable = 1053,
        PvpWarmodeToggleOn = 1054,
        PvpWarmodeToggleOff = 1055,
        SpellFailedLevelRequirement = 1056,
        SpellFailedCantFlyHere = 1057,
        BattlegroundJoinRequiresLevel = 1058,
        BattlegroundJoinDisqualified = 1059,
        BattlegroundJoinDisqualifiedNoName = 1060,
        VoiceChatGenericUnableToConnect = 1061,
        VoiceChatServiceLost = 1062,
        VoiceChatChannelNameTooShort = 1063,
        VoiceChatChannelNameTooLong = 1064,
        VoiceChatChannelAlreadyExists = 1065,
        VoiceChatTargetNotFound = 1066,
        VoiceChatTooManyRequests = 1067,
        VoiceChatPlayerSilenced = 1068,
        VoiceChatParentalDisableAll = 1069,
        VoiceChatDisabled = 1070,
        NoPvpReward = 1071,
        ClaimedPvpReward = 1072,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1073,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1074,
        AzeriteEssenceSelectionFailedConditionFailed = 1075,
        AzeriteEssenceSelectionFailedRestArea = 1076,
        AzeriteEssenceSelectionFailedSlotLocked = 1077,
        AzeriteEssenceSelectionFailedNotAtForge = 1078,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1079,
        AzeriteEssenceSelectionFailedNotEquipped = 1080,
        SocketingRequiresPunchcardredGem = 1081,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1082,
        SocketingRequiresPunchcardyellowGem = 1083,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1084,
        SocketingRequiresPunchcardblueGem = 1085,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1086,
        SocketingRequiresDominationShard = 1087,
        SocketingDominationShardOnlyInDominationslot = 1088,
        SocketingRequiresCypherGem = 1089,
        SocketingCypherGemOnlyInCypherslot = 1090,
        SocketingRequiresTinkerGem = 1091,
        SocketingTinkerGemOnlyInTinkerslot = 1092,
        SocketingRequiresPrimordialGem = 1093,
        SocketingPrimordialGemOnlyInPrimordialslot = 1094,
        SocketingRequiresFragranceGem = 1095,
        SocketingFragranceGemOnlyInFragranceslot = 1096,
        LevelLinkingResultLinked = 1097,
        LevelLinkingResultUnlinked = 1098,
        ClubFinderErrorPostClub = 1099,
        ClubFinderErrorApplyClub = 1100,
        ClubFinderErrorRespondApplicant = 1101,
        ClubFinderErrorCancelApplication = 1102,
        ClubFinderErrorTypeAcceptApplication = 1103,
        ClubFinderErrorTypeNoInvitePermissions = 1104,
        ClubFinderErrorTypeNoPostingPermissions = 1105,
        ClubFinderErrorTypeApplicantList = 1106,
        ClubFinderErrorTypeApplicantListNoPerm = 1107,
        ClubFinderErrorTypeFinderNotAvailable = 1108,
        ClubFinderErrorTypeGetPostingIds = 1109,
        ClubFinderErrorTypeJoinApplication = 1110,
        ClubFinderErrorTypeRealmNotEligible = 1111,
        ClubFinderErrorTypeFlaggedRename = 1112,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1113,
        ItemInteractionNotEnoughGold = 1114,
        ItemInteractionNotEnoughCurrency = 1115,
        ItemInteractionNoConversionOutput = 1116,
        PlayerChoiceErrorPendingChoice = 1117,
        SoulbindInvalidConduit = 1118,
        SoulbindInvalidConduitItem = 1119,
        SoulbindInvalidTalent = 1120,
        SoulbindDuplicateConduit = 1121,
        ActivateSoulbindS = 1122,
        ActivateSoulbindFailedRestArea = 1123,
        CantUseProfanity = 1124,
        NotInPetBattle = 1125,
        NotInNpe = 1126,
        NoSpec = 1127,
        NoDominationshardOverwrite = 1128,
        UseWeeklyRewardsDisabled = 1129,
        CrossFactionGroupJoined = 1130,
        CantTargetUnfriendlyInOverworld = 1131,
        EquipablespellsSlotsFull = 1132,
        ItemModAppearanceGroupAlreadyKnown = 1133,
        CantBulkSellItemWithRefund = 1134,
        NoSoulboundItemInAccountBank = 1135,
        NoRefundableItemInAccountBank = 1136,
        CantDeleteInAccountBank = 1137,
        NoImmediateContainerInAccountBank = 1138,
        NoOpenImmediateContainerInAccountBank = 1139,
        NoAccountInventoryLock = 1140,
        TooManyAccountBankTabs = 1141,
        AccountBankTabNotUnlocked = 1142,
        BankTabInvalidName = 1143,
        BankTabInvalidText = 1144,
        WowLabsPartyErrorTypePartyIsFull = 1145,
        WowLabsPartyErrorTypeMaxInviteSent = 1146,
        WowLabsPartyErrorTypePlayerAlreadyInvited = 1147,
        WowLabsPartyErrorTypePartyInviteInvalid = 1148,
        WowLabsLobbyMatchmakerErrorEnterQueueFailed = 1149,
        WowLabsLobbyMatchmakerErrorLeaveQueueFailed = 1150,
        TargetIsSelfFoundCannotTrade = 1151,
        PlayerIsSelfFoundCannotTrade = 1152,
        MailRecepientIsSelfFoundCannotReceiveMail = 1153,
        PlayerIsSelfFoundCannotSendMail = 1154,
        PlayerIsSelfFoundCannotUseAuctionHouse = 1155,
        MailTargetCannotReceiveMail = 1156,
        RemixInvalidTransferRequest = 1157,
        CurrencyTransferInvalidCharacter = 1158,
        CurrencyTransferInvalidCurrency = 1159,
        CurrencyTransferInsufficientCurrency = 1160,
        CurrencyTransferMaxQuantity = 1161,
        CurrencyTransferNoValidSource = 1162,
        CurrencyTransferCharacterLoggedIn = 1163,
        CurrencyTransferServerError = 1164,
        CurrencyTransferUnmetRequirements = 1165,
    }

    public enum SceneFlags
    {
        None = 0x00,
        PlayerNonInteractablePhased = 0x01, // Sets UNIT_FLAG_IMMUNE_TO_PC + UNIT_FLAG_IMMUNE_TO_NPC + UNIT_FLAG_PACIFIED
        FadeToBlackscreenOnComplete = 0x02,
        NotCancelable = 0x04,
        FadeToBlackscreenOnCancel = 0x08,

        IgnoreTransport = 0x20
    }

    [Flags]
    public enum VignetteFlags
    {
        InfiniteAOI = 0x000001,
        ShowOnMap = 0x000002,
        PingMinimap = 0x000004,
        TestVisibilityRules = 0x000008,
        VerticalRangeIsAbsolute = 0x000010,
        Unique = 0x000020,
        ZoneInfiniteAOI = 0x000040,
        PersistsThroughDeath = 0x000080,

        DontShowOnMinimap = 0x000200,
        HasTooltip = 0x000400,

        AdditionalHeightReq = 0x008000, // Must be within 10 yards of vignette Z coord (hardcoded in client)
        HideOnContinentMaps = 0x010000,
        NoPaddingAboveUiWidgets = 0x020000
    }

    public enum WorldMapTransformsFlags
    {
        Dungeon = 0x04
    }

    public enum VisibilityDistanceType
    {
        Normal = 0,
        Tiny = 1,
        Small = 2,
        Large = 3,
        Gigantic = 4,
        Infinite = 5,

        Max
    }

    public enum ConversationActorType
    {
        WorldObject = 0,
        TalkingHead = 1
    }

    public enum CorpseDynFlags
    {
        Lootable = 0x1
    }

    public enum QueryDataGroup
    {
        Creatures = 0x01,
        Gameobjects = 0x02,
        Items = 0x04,
        Quests = 0x08,
        POIs = 0x10,

        All = 0xFF
    }

    public enum BattlePetDbFlags : ushort
    {
        None = 0x00,
        Favorite = 0x01,
        Converted = 0x02,
        Revoked = 0x04,
        LockedForConvert = 0x08,
        Ability0Selection = 0x10,
        Ability1Selection = 0x20,
        Ability2Selection = 0x40,
        FanfareNeeded = 0x80,
        DisplayOverridden = 0x100
    }

    public enum BattlePetSlots
    {
        Slot0 = 0,
        Slot1 = 1,
        Slot2 = 2,

        Count
    }

    public enum SceneType
    {
        Normal = 0,
        PetBattle = 1
    }

    public enum ComplaintStatus
    {
        Disabled = 0,
        EnabledWithoutAutoIgnore = 1,
        EnabledWithAutoIgnore = 2
    }

    public enum AreaId
    {
        Wintergrasp = 4197,
        TheSunkenRing = 4538,
        TheBrokenTemplate = 4539,
        WintergraspFortress = 4575,
        TheChilledQuagmire = 4589,
        WestparkWorkshop = 4611,
        EastparkWorkshop = 4612,
    }

    public enum WorldStates
    {
        CurrentPvpSeasonId = 3191,
        PreviousPvpSeasonId = 3901,

        TeamInInstanceAlliance = 4485,
        TeamInInstanceHorde = 4486,

        BattlefieldWgVehicleH = 3490,
        BattlefieldWgMaxVehicleH = 3491,
        BattlefieldWgVehicleA = 3680,
        BattlefieldWgMaxVehicleA = 3681,
        BattlefieldWgWorkshopKW = 3698,
        BattlefieldWgWorkshopKE = 3699,
        BattlefieldWgWorkshopNw = 3700,
        BattlefieldWgWorkshopNe = 3701,
        BattlefieldWgWorkshopSw = 3702,
        BattlefieldWgWorkshopSe = 3703,
        BattlefieldWgShowTimeBattleEnd = 3710,
        BattlefieldWgTimeBattleEnd = 3781,
        BattlefieldWgShowTimeNextBattle = 3801,
        BattlefieldWgDefender = 3802,
        BattlefieldWgAttacker = 3803,
        BattlefieldWgAttackedH = 4022,
        BattlefieldWgAttackedA = 4023,
        BattlefieldWgDefendedH = 4024,
        BattlefieldWgDefendedA = 4025,
        BattlefieldWgTimeNextBattle = 4354,

        BattlefieldTbAllianceControlsShow = 5385,
        BattlefieldTbHordeControlsShow = 5384,
        BattlefieldTbAllianceAttackingShow = 5546,
        BattlefieldTbHordeAttackingShow = 5547,

        BattlefieldTbBuildingsCaptured = 5348,
        BattlefieldTbBuildingsCapturedShow = 5349,
        BattlefieldTbTowersDestroyed = 5347,
        BattlefieldTbTowersDestroyedShow = 5350,

        BattlefieldTbFactionControlling = 5334, // 1 -> Alliance, 2 -> Horde

        BattlefieldTbTimeNextBattle = 5332,
        BattlefieldTbTimeNextBattleShow = 5387,
        BattlefieldTbTimeBattleEnd = 5333,
        BattlefieldTbTimeBattleEndShow = 5346,

        BattlefieldTbStatePreparations = 5684,
        BattlefieldTbStateBattle = 5344,

        BattlefieldTbKeepHorde = 5469,
        BattlefieldTbKeepAlliance = 5470,

        BattlefieldTbGarrisonHordeControlled = 5418,
        BattlefieldTbGarrisonHordeCapturing = 5419,
        BattlefieldTbGarrisonNeutral = 5420, // Unused
        BattlefieldTbGarrisonAllianceCapturing = 5421,
        BattlefieldTbGarrisonAllianceControlled = 5422,

        BattlefieldTbVigilHordeControlled = 5423,
        BattlefieldTbVigilHordeCapturing = 5424,
        BattlefieldTbVigilNeutral = 5425, // Unused
        BattlefieldTbVigilAllianceCapturing = 5426,
        BattlefieldTbVigilAllianceControlled = 5427,

        BattlefieldTbSlagworksHordeControlled = 5428,
        BattlefieldTbSlagworksHordeCapturing = 5429,
        BattlefieldTbSlagworksNeutral = 5430, // Unused
        BattlefieldTbSlagworksAllianceCapturing = 5431,
        BattlefieldTbSlagworksAllianceControlled = 5432,

        BattlefieldTbWestIntactHorde = 5433,
        BattlefieldTbWestDamagedHorde = 5434,
        BattlefieldTbWestDestroyedNeutral = 5435,
        BattlefieldTbWestIntactAlliance = 5436,
        BattlefieldTbWestDamagedAlliance = 5437,
        BattlefieldTbWestIntactNeutral = 5453, // Unused
        BattlefieldTbWestDamagedNeutral = 5454, // Unused

        BattlefieldTbSouthIntactHorde = 5438,
        BattlefieldTbSouthDamagedHorde = 5439,
        BattlefieldTbSouthDestroyedNeutral = 5440,
        BattlefieldTbSouthIntactAlliance = 5441,
        BattlefieldTbSouthDamagedAlliance = 5442,
        BattlefieldTbSouthIntactNeutral = 5455, // Unused
        BattlefieldTbSouthDamagedNeutral = 5456, // Unused

        BattlefieldTbEastIntactHorde = 5443,
        BattlefieldTbEastDamagedHorde = 5444,
        BattlefieldTbEastDestroyedNeutral = 5445,
        BattlefieldTbEastIntactAlliance = 5446,
        BattlefieldTbEastDamagedAlliance = 5447,
        BattlefieldTbEastIntactNeutral = 5451,
        BattlefieldTbEastDamagedNeutral = 5452,

        WarModeHordeBuffValue = 17042,
        WarModeAllianceBuffValue = 17043,
    }
}
