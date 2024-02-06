// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
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
        public const int SmartEventParamCount = 4;
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

    public struct BatttleGroundTeamId
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

    public enum TimerType
    {
        Pvp = 0,
        ChallengerMode = 1,
        PlayerCountdown = 2
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
        ItemLocked = 38,
        Handed2Equipped = 39,
        VendorNotInterested = 40,
        VendorRefuseScrappableAzerite = 41,
        VendorHatesYou = 42,
        VendorSoldOut = 43,
        VendorTooFar = 44,
        VendorDoesntBuy = 45,
        NotEnoughMoney = 46,
        ReceiveItemS = 47,
        DropBoundItem = 48,
        TradeBoundItem = 49,
        TradeQuestItem = 50,
        TradeTempEnchantBound = 51,
        TradeGroundItem = 52,
        TradeBag = 53,
        TradeFactionSpecific = 54,
        SpellFailedS = 55,
        ItemCooldown = 56,
        PotionCooldown = 57,
        FoodCooldown = 58,
        SpellCooldown = 59,
        AbilityCooldown = 60,
        SpellAlreadyKnownS = 61,
        PetSpellAlreadyKnownS = 62,
        ProficiencyGainedS = 63,
        SkillGainedS = 64,
        SkillUpSi = 65,
        LearnSpellS = 66,
        LearnAbilityS = 67,
        LearnPassiveS = 68,
        LearnRecipeS = 69,
        ProfessionsRecipeDiscoveryS = 70,
        LearnCompanionS = 71,
        LearnMountS = 72,
        LearnToyS = 73,
        LearnHeirloomS = 74,
        LearnTransmogS = 75,
        CompletedTransmogSetS = 76,
        AppearanceAlreadyLearned = 77,
        RevokeTransmogS = 78,
        InvitePlayerS = 79,
        SuggestInvitePlayerS = 80,
        InformSuggestInviteS = 81,
        InformSuggestInviteSs = 82,
        RequestJoinPlayerS = 83,
        InviteSelf = 84,
        InvitedToGroupSs = 85,
        InvitedAlreadyInGroupSs = 86,
        AlreadyInGroupS = 87,
        RequestedInviteToGroupSs = 88,
        CrossRealmRaidInvite = 89,
        PlayerBusyS = 90,
        NewLeaderS = 91,
        NewLeaderYou = 92,
        NewGuideS = 93,
        NewGuideYou = 94,
        LeftGroupS = 95,
        LeftGroupYou = 96,
        GroupDisbanded = 97,
        DeclineGroupS = 98,
        DeclineGroupRequestS = 99,
        JoinedGroupS = 100,
        UninviteYou = 101,
        BadPlayerNameS = 102,
        NotInGroup = 103,
        TargetNotInGroupS = 104,
        TargetNotInInstanceS = 105,
        NotInInstanceGroup = 106,
        GroupFull = 107,
        NotLeader = 108,
        PlayerDiedS = 109,
        GuildCreateS = 110,
        GuildInviteS = 111,
        InvitedToGuildSss = 112,
        AlreadyInGuildS = 113,
        AlreadyInvitedToGuildS = 114,
        InvitedToGuild = 115,
        AlreadyInGuild = 116,
        GuildAccept = 117,
        GuildDeclineS = 118,
        GuildDeclineAutoS = 119,
        GuildPermissions = 120,
        GuildJoinS = 121,
        GuildFounderS = 122,
        GuildPromoteSss = 123,
        GuildDemoteSs = 124,
        GuildDemoteSss = 125,
        GuildInviteSelf = 126,
        GuildQuitS = 127,
        GuildLeaveS = 128,
        GuildRemoveSs = 129,
        GuildRemoveSelf = 130,
        GuildDisbandS = 131,
        GuildDisbandSelf = 132,
        GuildLeaderS = 133,
        GuildLeaderSelf = 134,
        GuildPlayerNotFoundS = 135,
        GuildPlayerNotInGuildS = 136,
        GuildPlayerNotInGuild = 137,
        GuildCantPromoteS = 138,
        GuildCantDemoteS = 139,
        GuildNotInAGuild = 140,
        GuildInternal = 141,
        GuildLeaderIsS = 142,
        GuildLeaderChangedSs = 143,
        GuildDisbanded = 144,
        GuildNotAllied = 145,
        GuildNewLeaderNotAllied = 146,
        GuildLeaderLeave = 147,
        GuildRanksLocked = 148,
        GuildRankInUse = 149,
        GuildRankTooHighS = 150,
        GuildRankTooLowS = 151,
        GuildNameExistsS = 152,
        GuildWithdrawLimit = 153,
        GuildNotEnoughMoney = 154,
        GuildTooMuchMoney = 155,
        GuildBankConjuredItem = 156,
        GuildBankEquippedItem = 157,
        GuildBankBoundItem = 158,
        GuildBankQuestItem = 159,
        GuildBankWrappedItem = 160,
        GuildBankFull = 161,
        GuildBankWrongTab = 162,
        NoGuildCharter = 163,
        OutOfRange = 164,
        PlayerDead = 165,
        ClientLockedOut = 166,
        ClientOnTransport = 167,
        KilledByS = 168,
        LootLocked = 169,
        LootTooFar = 170,
        LootDidntKill = 171,
        LootBadFacing = 172,
        LootNotstanding = 173,
        LootStunned = 174,
        LootNoUi = 175,
        LootWhileInvulnerable = 176,
        NoLoot = 177,
        QuestAcceptedS = 178,
        QuestCompleteS = 179,
        QuestFailedS = 180,
        QuestFailedBagFullS = 181,
        QuestFailedMaxCountS = 182,
        QuestFailedLowLevel = 183,
        QuestFailedMissingItems = 184,
        QuestFailedWrongRace = 185,
        QuestFailedNotEnoughMoney = 186,
        QuestFailedExpansion = 187,
        QuestOnlyOneTimed = 188,
        QuestNeedPrereqs = 189,
        QuestNeedPrereqsCustom = 190,
        QuestAlreadyOn = 191,
        QuestAlreadyDone = 192,
        QuestAlreadyDoneDaily = 193,
        QuestHasInProgress = 194,
        QuestRewardExpI = 195,
        QuestRewardMoneyS = 196,
        QuestMustChoose = 197,
        QuestLogFull = 198,
        CombatDamageSsi = 199,
        InspectS = 200,
        CantUseItem = 201,
        CantUseItemInArena = 202,
        CantUseItemInRatedBattleground = 203,
        MustEquipItem = 204,
        PassiveAbility = 205,
        Skill2Hnotfound = 206,
        NoAttackTarget = 207,
        InvalidAttackTarget = 208,
        AttackPvpTargetWhileUnflagged = 209,
        AttackStunned = 210,
        AttackPacified = 211,
        AttackMounted = 212,
        AttackFleeing = 213,
        AttackConfused = 214,
        AttackCharmed = 215,
        AttackDead = 216,
        AttackPreventedByMechanicS = 217,
        AttackChannel = 218,
        Taxisamenode = 219,
        Taxinosuchpath = 220,
        Taxiunspecifiedservererror = 221,
        Taxinotenoughmoney = 222,
        Taxitoofaraway = 223,
        Taxinovendornearby = 224,
        Taxinotvisited = 225,
        Taxiplayerbusy = 226,
        Taxiplayeralreadymounted = 227,
        Taxiplayershapeshifted = 228,
        Taxiplayermoving = 229,
        Taxinopaths = 230,
        Taxinoteligible = 231,
        Taxinotstanding = 232,
        Taxiincombat = 233,
        NoReplyTarget = 234,
        GenericNoTarget = 235,
        InitiateTradeS = 236,
        TradeRequestS = 237,
        TradeBlockedS = 238,
        TradeTargetDead = 239,
        TradeTooFar = 240,
        TradeCancelled = 241,
        TradeComplete = 242,
        TradeBagFull = 243,
        TradeTargetBagFull = 244,
        TradeMaxCountExceeded = 245,
        TradeTargetMaxCountExceeded = 246,
        InventoryTradeTooManyUniqueItem = 247,
        AlreadyTrading = 248,
        MountInvalidmountee = 249,
        MountToofaraway = 250,
        MountAlreadymounted = 251,
        MountNotmountable = 252,
        MountNotyourpet = 253,
        MountOther = 254,
        MountLooting = 255,
        MountRacecantmount = 256,
        MountShapeshifted = 257,
        MountNoFavorites = 258,
        MountNoMounts = 259,
        DismountNopet = 260,
        DismountNotmounted = 261,
        DismountNotyourpet = 262,
        SpellFailedTotems = 263,
        SpellFailedReagents = 264,
        SpellFailedReagentsGeneric = 265,
        SpellFailedOptionalReagents = 266,
        CantTradeGold = 267,
        SpellFailedEquippedItem = 268,
        SpellFailedEquippedItemClassS = 269,
        SpellFailedShapeshiftFormS = 270,
        SpellFailedAnotherInProgress = 271,
        Badattackfacing = 272,
        Badattackpos = 273,
        ChestInUse = 274,
        UseCantOpen = 275,
        UseLocked = 276,
        DoorLocked = 277,
        ButtonLocked = 278,
        UseLockedWithItemS = 279,
        UseLockedWithSpellS = 280,
        UseLockedWithSpellKnownSi = 281,
        UseTooFar = 282,
        UseBadAngle = 283,
        UseObjectMoving = 284,
        UseSpellFocus = 285,
        UseDestroyed = 286,
        SetLootFreeforall = 287,
        SetLootRoundrobin = 288,
        SetLootMaster = 289,
        SetLootGroup = 290,
        SetLootThresholdS = 291,
        NewLootMasterS = 292,
        SpecifyMasterLooter = 293,
        LootSpecChangedS = 294,
        TameFailed = 295,
        ChatWhileDead = 296,
        ChatPlayerNotFoundS = 297,
        Newtaxipath = 298,
        NoPet = 299,
        Notyourpet = 300,
        PetNotRenameable = 301,
        QuestObjectiveCompleteS = 302,
        QuestUnknownComplete = 303,
        QuestAddKillSii = 304,
        QuestAddFoundSii = 305,
        QuestAddItemSii = 306,
        QuestAddPlayerKillSii = 307,
        Cannotcreatedirectory = 308,
        Cannotcreatefile = 309,
        PlayerWrongFaction = 310,
        PlayerIsNeutral = 311,
        BankslotFailedTooMany = 312,
        BankslotInsufficientFunds = 313,
        BankslotNotbanker = 314,
        FriendDbError = 315,
        FriendListFull = 316,
        FriendAddedS = 317,
        BattletagFriendAddedS = 318,
        FriendOnlineSs = 319,
        FriendOfflineS = 320,
        FriendNotFound = 321,
        FriendWrongFaction = 322,
        FriendRemovedS = 323,
        BattletagFriendRemovedS = 324,
        FriendError = 325,
        FriendAlreadyS = 326,
        FriendSelf = 327,
        FriendDeleted = 328,
        IgnoreFull = 329,
        IgnoreSelf = 330,
        IgnoreNotFound = 331,
        IgnoreAlreadyS = 332,
        IgnoreAddedS = 333,
        IgnoreRemovedS = 334,
        IgnoreAmbiguous = 335,
        IgnoreDeleted = 336,
        OnlyOneBolt = 337,
        OnlyOneAmmo = 338,
        SpellFailedEquippedSpecificItem = 339,
        WrongBagTypeSubclass = 340,
        CantWrapStackable = 341,
        CantWrapEquipped = 342,
        CantWrapWrapped = 343,
        CantWrapBound = 344,
        CantWrapUnique = 345,
        CantWrapBags = 346,
        OutOfMana = 347,
        OutOfRage = 348,
        OutOfFocus = 349,
        OutOfEnergy = 350,
        OutOfChi = 351,
        OutOfHealth = 352,
        OutOfRunes = 353,
        OutOfRunicPower = 354,
        OutOfSoulShards = 355,
        OutOfLunarPower = 356,
        OutOfHolyPower = 357,
        OutOfMaelstrom = 358,
        OutOfComboPoints = 359,
        OutOfInsanity = 360,
        OutOfEssence = 361,
        OutOfArcaneCharges = 362,
        OutOfFury = 363,
        OutOfPain = 364,
        OutOfPowerDisplay = 365,
        LootGone = 366,
        MountForceddismount = 367,
        AutofollowTooFar = 368,
        UnitNotFound = 369,
        InvalidFollowTarget = 370,
        InvalidFollowPvpCombat = 371,
        InvalidFollowTargetPvpCombat = 372,
        InvalidInspectTarget = 373,
        GuildemblemSuccess = 374,
        GuildemblemInvalidTabardColors = 375,
        GuildemblemNoguild = 376,
        GuildemblemNotguildmaster = 377,
        GuildemblemNotenoughmoney = 378,
        GuildemblemInvalidvendor = 379,
        EmblemerrorNotabardgeoset = 380,
        SpellOutOfRange = 381,
        CommandNeedsTarget = 382,
        NoammoS = 383,
        Toobusytofollow = 384,
        DuelRequested = 385,
        DuelCancelled = 386,
        Deathbindalreadybound = 387,
        DeathbindSuccessS = 388,
        Noemotewhilerunning = 389,
        ZoneExplored = 390,
        ZoneExploredXp = 391,
        InvalidItemTarget = 392,
        InvalidQuestTarget = 393,
        IgnoringYouS = 394,
        FishNotHooked = 395,
        FishEscaped = 396,
        SpellFailedNotunsheathed = 397,
        PetitionOfferedS = 398,
        PetitionSigned = 399,
        PetitionSignedS = 400,
        PetitionDeclinedS = 401,
        PetitionAlreadySigned = 402,
        PetitionRestrictedAccountTrial = 403,
        PetitionAlreadySignedOther = 404,
        PetitionInGuild = 405,
        PetitionCreator = 406,
        PetitionNotEnoughSignatures = 407,
        PetitionNotSameServer = 408,
        PetitionFull = 409,
        PetitionAlreadySignedByS = 410,
        GuildNameInvalid = 411,
        SpellUnlearnedS = 412,
        PetSpellRooted = 413,
        PetSpellAffectingCombat = 414,
        PetSpellOutOfRange = 415,
        PetSpellNotBehind = 416,
        PetSpellTargetsDead = 417,
        PetSpellDead = 418,
        PetSpellNopath = 419,
        ItemCantBeDestroyed = 420,
        TicketAlreadyExists = 421,
        TicketCreateError = 422,
        TicketUpdateError = 423,
        TicketDbError = 424,
        TicketNoText = 425,
        TicketTextTooLong = 426,
        ObjectIsBusy = 427,
        ExhaustionWellrested = 428,
        ExhaustionRested = 429,
        ExhaustionNormal = 430,
        ExhaustionTired = 431,
        ExhaustionExhausted = 432,
        NoItemsWhileShapeshifted = 433,
        CantInteractShapeshifted = 434,
        RealmNotFound = 435,
        MailQuestItem = 436,
        MailBoundItem = 437,
        MailConjuredItem = 438,
        MailBag = 439,
        MailToSelf = 440,
        MailTargetNotFound = 441,
        MailDatabaseError = 442,
        MailDeleteItemError = 443,
        MailWrappedCod = 444,
        MailCantSendRealm = 445,
        MailTempReturnOutage = 446,
        MailRecepientCantReceiveMail = 447,
        MailSent = 448,
        MailTargetIsTrial = 449,
        NotHappyEnough = 450,
        UseCantImmune = 451,
        CantBeDisenchanted = 452,
        CantUseDisarmed = 453,
        AuctionDatabaseError = 454,
        AuctionHigherBid = 455,
        AuctionAlreadyBid = 456,
        AuctionOutbidS = 457,
        AuctionWonS = 458,
        AuctionRemovedS = 459,
        AuctionBidPlaced = 460,
        LogoutFailed = 461,
        QuestPushSuccessS = 462,
        QuestPushInvalidS = 463,
        QuestPushInvalidToRecipientS = 464,
        QuestPushAcceptedS = 465,
        QuestPushDeclinedS = 466,
        QuestPushBusyS = 467,
        QuestPushDeadS = 468,
        QuestPushDeadToRecipientS = 469,
        QuestPushLogFullS = 470,
        QuestPushLogFullToRecipientS = 471,
        QuestPushOnquestS = 472,
        QuestPushOnquestToRecipientS = 473,
        QuestPushAlreadyDoneS = 474,
        QuestPushAlreadyDoneToRecipientS = 475,
        QuestPushNotDailyS = 476,
        QuestPushTimerExpiredS = 477,
        QuestPushNotInPartyS = 478,
        QuestPushDifferentServerDailyS = 479,
        QuestPushDifferentServerDailyToRecipientS = 480,
        QuestPushNotAllowedS = 481,
        QuestPushPrerequisiteS = 482,
        QuestPushPrerequisiteToRecipientS = 483,
        QuestPushLowLevelS = 484,
        QuestPushLowLevelToRecipientS = 485,
        QuestPushHighLevelS = 486,
        QuestPushHighLevelToRecipientS = 487,
        QuestPushClassS = 488,
        QuestPushClassToRecipientS = 489,
        QuestPushRaceS = 490,
        QuestPushRaceToRecipientS = 491,
        QuestPushLowFactionS = 492,
        QuestPushLowFactionToRecipientS = 493,
        QuestPushExpansionS = 494,
        QuestPushExpansionToRecipientS = 495,
        QuestPushNotGarrisonOwnerS = 496,
        QuestPushNotGarrisonOwnerToRecipientS = 497,
        QuestPushWrongCovenantS = 498,
        QuestPushWrongCovenantToRecipientS = 499,
        QuestPushNewPlayerExperienceS = 500,
        QuestPushNewPlayerExperienceToRecipientS = 501,
        QuestPushWrongFactionS = 502,
        QuestPushWrongFactionToRecipientS = 503,
        QuestPushCrossFactionRestrictedS = 504,
        RaidGroupLowlevel = 505,
        RaidGroupOnly = 506,
        RaidGroupFull = 507,
        RaidGroupRequirementsUnmatch = 508,
        CorpseIsNotInInstance = 509,
        PvpKillHonorable = 510,
        PvpKillDishonorable = 511,
        SpellFailedAlreadyAtFullHealth = 512,
        SpellFailedAlreadyAtFullMana = 513,
        SpellFailedAlreadyAtFullPowerS = 514,
        AutolootMoneyS = 515,
        GenericStunned = 516,
        GenericThrottle = 517,
        ClubFinderSearchingTooFast = 518,
        TargetStunned = 519,
        MustRepairDurability = 520,
        RaidYouJoined = 521,
        RaidYouLeft = 522,
        InstanceGroupJoinedWithParty = 523,
        InstanceGroupJoinedWithRaid = 524,
        RaidMemberAddedS = 525,
        RaidMemberRemovedS = 526,
        InstanceGroupAddedS = 527,
        InstanceGroupRemovedS = 528,
        ClickOnItemToFeed = 529,
        TooManyChatChannels = 530,
        LootRollPending = 531,
        LootPlayerNotFound = 532,
        NotInRaid = 533,
        LoggingOut = 534,
        TargetLoggingOut = 535,
        NotWhileMounted = 536,
        NotWhileShapeshifted = 537,
        NotInCombat = 538,
        NotWhileDisarmed = 539,
        PetBroken = 540,
        TalentWipeError = 541,
        SpecWipeError = 542,
        GlyphWipeError = 543,
        PetSpecWipeError = 544,
        FeignDeathResisted = 545,
        MeetingStoneInQueueS = 546,
        MeetingStoneLeftQueueS = 547,
        MeetingStoneOtherMemberLeft = 548,
        MeetingStonePartyKickedFromQueue = 549,
        MeetingStoneMemberStillInQueue = 550,
        MeetingStoneSuccess = 551,
        MeetingStoneInProgress = 552,
        MeetingStoneMemberAddedS = 553,
        MeetingStoneGroupFull = 554,
        MeetingStoneNotLeader = 555,
        MeetingStoneInvalidLevel = 556,
        MeetingStoneTargetNotInParty = 557,
        MeetingStoneTargetInvalidLevel = 558,
        MeetingStoneMustBeLeader = 559,
        MeetingStoneNoRaidGroup = 560,
        MeetingStoneNeedParty = 561,
        MeetingStoneNotFound = 562,
        MeetingStoneTargetInVehicle = 563,
        GuildemblemSame = 564,
        EquipTradeItem = 565,
        PvpToggleOn = 566,
        PvpToggleOff = 567,
        GroupJoinBattlegroundDeserters = 568,
        GroupJoinBattlegroundDead = 569,
        GroupJoinBattlegroundS = 570,
        GroupJoinBattlegroundFail = 571,
        GroupJoinBattlegroundTooMany = 572,
        SoloJoinBattlegroundS = 573,
        JoinSingleScenarioS = 574,
        BattlegroundTooManyQueues = 575,
        BattlegroundCannotQueueForRated = 576,
        BattledgroundQueuedForRated = 577,
        BattlegroundTeamLeftQueue = 578,
        BattlegroundNotInBattleground = 579,
        AlreadyInArenaTeamS = 580,
        InvalidPromotionCode = 581,
        BgPlayerJoinedSs = 582,
        BgPlayerLeftS = 583,
        RestrictedAccount = 584,
        RestrictedAccountTrial = 585,
        PlayTimeExceeded = 586,
        ApproachingPartialPlayTime = 587,
        ApproachingPartialPlayTime2 = 588,
        ApproachingNoPlayTime = 589,
        ApproachingNoPlayTime2 = 590,
        UnhealthyTime = 591,
        ChatRestrictedTrial = 592,
        ChatThrottled = 593,
        MailReachedCap = 594,
        InvalidRaidTarget = 595,
        RaidLeaderReadyCheckStartS = 596,
        ReadyCheckInProgress = 597,
        ReadyCheckThrottled = 598,
        DungeonDifficultyFailed = 599,
        DungeonDifficultyChangedS = 600,
        TradeWrongRealm = 601,
        TradeNotOnTaplist = 602,
        ChatPlayerAmbiguousS = 603,
        LootCantLootThatNow = 604,
        LootMasterInvFull = 605,
        LootMasterUniqueItem = 606,
        LootMasterOther = 607,
        FilteringYouS = 608,
        UsePreventedByMechanicS = 609,
        ItemUniqueEquippable = 610,
        LfgLeaderIsLfmS = 611,
        LfgPending = 612,
        CantSpeakLangage = 613,
        VendorMissingTurnins = 614,
        BattlegroundNotInTeam = 615,
        NotInBattleground = 616,
        NotEnoughHonorPoints = 617,
        NotEnoughArenaPoints = 618,
        SocketingRequiresMetaGem = 619,
        SocketingMetaGemOnlyInMetaslot = 620,
        SocketingRequiresHydraulicGem = 621,
        SocketingHydraulicGemOnlyInHydraulicslot = 622,
        SocketingRequiresCogwheelGem = 623,
        SocketingCogwheelGemOnlyInCogwheelslot = 624,
        SocketingItemTooLowLevel = 625,
        ItemMaxCountSocketed = 626,
        SystemDisabled = 627,
        QuestFailedTooManyDailyQuestsI = 628,
        ItemMaxCountEquippedSocketed = 629,
        ItemUniqueEquippableSocketed = 630,
        UserSquelched = 631,
        AccountSilenced = 632,
        PartyMemberSilenced = 633,
        PartyMemberSilencedLfgDelist = 634,
        TooMuchGold = 635,
        NotBarberSitting = 636,
        QuestFailedCais = 637,
        InviteRestrictedTrial = 638,
        VoiceIgnoreFull = 639,
        VoiceIgnoreSelf = 640,
        VoiceIgnoreNotFound = 641,
        VoiceIgnoreAlreadyS = 642,
        VoiceIgnoreAddedS = 643,
        VoiceIgnoreRemovedS = 644,
        VoiceIgnoreAmbiguous = 645,
        VoiceIgnoreDeleted = 646,
        UnknownMacroOptionS = 647,
        NotDuringArenaMatch = 648,
        NotInRatedBattleground = 649,
        PlayerSilenced = 650,
        PlayerUnsilenced = 651,
        ComsatDisconnect = 652,
        ComsatReconnectAttempt = 653,
        ComsatConnectFail = 654,
        MailInvalidAttachmentSlot = 655,
        MailTooManyAttachments = 656,
        MailInvalidAttachment = 657,
        MailAttachmentExpired = 658,
        VoiceChatParentalDisableMic = 659,
        ProfaneChatName = 660,
        PlayerSilencedEcho = 661,
        PlayerUnsilencedEcho = 662,
        LootCantLootThat = 663,
        ArenaExpiredCais = 664,
        GroupActionThrottled = 665,
        AlreadyPickpocketed = 666,
        NameInvalid = 667,
        NameNoName = 668,
        NameTooShort = 669,
        NameTooLong = 670,
        NameMixedLanguages = 671,
        NameProfane = 672,
        NameReserved = 673,
        NameThreeConsecutive = 674,
        NameInvalidSpace = 675,
        NameConsecutiveSpaces = 676,
        NameRussianConsecutiveSilentCharacters = 677,
        NameRussianSilentCharacterAtBeginningOrEnd = 678,
        NameDeclensionDoesntMatchBaseName = 679,
        RecruitAFriendNotLinked = 680,
        RecruitAFriendNotNow = 681,
        RecruitAFriendSummonLevelMax = 682,
        RecruitAFriendSummonCooldown = 683,
        RecruitAFriendSummonOffline = 684,
        RecruitAFriendInsufExpanLvl = 685,
        RecruitAFriendMapIncomingTransferNotAllowed = 686,
        NotSameAccount = 687,
        BadOnUseEnchant = 688,
        TradeSelf = 689,
        TooManySockets = 690,
        ItemMaxLimitCategoryCountExceededIs = 691,
        TradeTargetMaxLimitCategoryCountExceededIs = 692,
        ItemMaxLimitCategorySocketedExceededIs = 693,
        ItemMaxLimitCategoryEquippedExceededIs = 694,
        ShapeshiftFormCannotEquip = 695,
        ItemInventoryFullSatchel = 696,
        ScalingStatItemLevelExceeded = 697,
        ScalingStatItemLevelTooLow = 698,
        PurchaseLevelTooLow = 699,
        GroupSwapFailed = 700,
        InviteInCombat = 701,
        InvalidGlyphSlot = 702,
        GenericNoValidTargets = 703,
        CalendarEventAlertS = 704,
        PetLearnSpellS = 705,
        PetLearnAbilityS = 706,
        PetSpellUnlearnedS = 707,
        InviteUnknownRealm = 708,
        InviteNoPartyServer = 709,
        InvitePartyBusy = 710,
        InvitePartyBusyPendingRequest = 711,
        InvitePartyBusyPendingSuggest = 712,
        PartyTargetAmbiguous = 713,
        PartyLfgInviteRaidLocked = 714,
        PartyLfgBootLimit = 715,
        PartyLfgBootCooldownS = 716,
        PartyLfgBootNotEligibleS = 717,
        PartyLfgBootInpatientTimerS = 718,
        PartyLfgBootInProgress = 719,
        PartyLfgBootTooFewPlayers = 720,
        PartyLfgBootVoteSucceeded = 721,
        PartyLfgBootVoteFailed = 722,
        PartyLfgBootInCombat = 723,
        PartyLfgBootDungeonComplete = 724,
        PartyLfgBootLootRolls = 725,
        PartyLfgBootVoteRegistered = 726,
        PartyPrivateGroupOnly = 727,
        PartyLfgTeleportInCombat = 728,
        RaidDisallowedByLevel = 729,
        RaidDisallowedByCrossRealm = 730,
        PartyRoleNotAvailable = 731,
        JoinLfgObjectFailed = 732,
        LfgRemovedLevelup = 733,
        LfgRemovedXpToggle = 734,
        LfgRemovedFactionChange = 735,
        BattlegroundInfoThrottled = 736,
        BattlegroundAlreadyIn = 737,
        ArenaTeamChangeFailedQueued = 738,
        ArenaTeamPermissions = 739,
        NotWhileFalling = 740,
        NotWhileMoving = 741,
        NotWhileFatigued = 742,
        MaxSockets = 743,
        MultiCastActionTotemS = 744,
        BattlegroundJoinLevelup = 745,
        RemoveFromPvpQueueXpGain = 746,
        BattlegroundJoinXpGain = 747,
        BattlegroundJoinMercenary = 748,
        BattlegroundJoinTooManyHealers = 749,
        BattlegroundJoinRatedTooManyHealers = 750,
        BattlegroundJoinTooManyTanks = 751,
        BattlegroundJoinTooManyDamage = 752,
        RaidDifficultyFailed = 753,
        RaidDifficultyChangedS = 754,
        LegacyRaidDifficultyChangedS = 755,
        RaidLockoutChangedS = 756,
        RaidConvertedToParty = 757,
        PartyConvertedToRaid = 758,
        PlayerDifficultyChangedS = 759,
        GmresponseDbError = 760,
        BattlegroundJoinRangeIndex = 761,
        ArenaJoinRangeIndex = 762,
        RemoveFromPvpQueueFactionChange = 763,
        BattlegroundJoinFailed = 764,
        BattlegroundJoinNoValidSpecForRole = 765,
        BattlegroundJoinRespec = 766,
        BattlegroundInvitationDeclined = 767,
        BattlegroundInvitationDeclinedBy = 768,
        BattlegroundJoinTimedOut = 769,
        BattlegroundDupeQueue = 770,
        BattlegroundJoinMustCompleteQuest = 771,
        InBattlegroundRespec = 772,
        MailLimitedDurationItem = 773,
        YellRestrictedTrial = 774,
        ChatRaidRestrictedTrial = 775,
        LfgRoleCheckFailed = 776,
        LfgRoleCheckFailedTimeout = 777,
        LfgRoleCheckFailedNotViable = 778,
        LfgReadyCheckFailed = 779,
        LfgReadyCheckFailedTimeout = 780,
        LfgGroupFull = 781,
        LfgNoLfgObject = 782,
        LfgNoSlotsPlayer = 783,
        LfgNoSlotsParty = 784,
        LfgNoSpec = 785,
        LfgMismatchedSlots = 786,
        LfgMismatchedSlotsLocalXrealm = 787,
        LfgPartyPlayersFromDifferentRealms = 788,
        LfgMembersNotPresent = 789,
        LfgGetInfoTimeout = 790,
        LfgInvalidSlot = 791,
        LfgDeserterPlayer = 792,
        LfgDeserterParty = 793,
        LfgDead = 794,
        LfgRandomCooldownPlayer = 795,
        LfgRandomCooldownParty = 796,
        LfgTooManyMembers = 797,
        LfgTooFewMembers = 798,
        LfgProposalFailed = 799,
        LfgProposalDeclinedSelf = 800,
        LfgProposalDeclinedParty = 801,
        LfgNoSlotsSelected = 802,
        LfgNoRolesSelected = 803,
        LfgRoleCheckInitiated = 804,
        LfgReadyCheckInitiated = 805,
        LfgPlayerDeclinedRoleCheck = 806,
        LfgPlayerDeclinedReadyCheck = 807,
        LfgJoinedQueue = 808,
        LfgJoinedFlexQueue = 809,
        LfgJoinedRfQueue = 810,
        LfgJoinedScenarioQueue = 811,
        LfgJoinedWorldPvpQueue = 812,
        LfgJoinedBattlefieldQueue = 813,
        LfgJoinedList = 814,
        LfgLeftQueue = 815,
        LfgLeftList = 816,
        LfgRoleCheckAborted = 817,
        LfgReadyCheckAborted = 818,
        LfgCantUseBattleground = 819,
        LfgCantUseDungeons = 820,
        LfgReasonTooManyLfg = 821,
        LfgFarmLimit = 822,
        LfgNoCrossFactionParties = 823,
        InvalidTeleportLocation = 824,
        TooFarToInteract = 825,
        BattlegroundPlayersFromDifferentRealms = 826,
        DifficultyChangeCooldownS = 827,
        DifficultyChangeCombatCooldownS = 828,
        DifficultyChangeWorldstate = 829,
        DifficultyChangeEncounter = 830,
        DifficultyChangeCombat = 831,
        DifficultyChangePlayerBusy = 832,
        DifficultyChangePlayerOnVehicle = 833,
        DifficultyChangeAlreadyStarted = 834,
        DifficultyChangeOtherHeroicS = 835,
        DifficultyChangeHeroicInstanceAlreadyRunning = 836,
        ArenaTeamPartySize = 837,
        SoloShuffleWargameGroupSize = 838,
        SoloShuffleWargameGroupComp = 839,
        SoloMinItemLevel = 840,
        PvpPlayerAbandoned = 841,
        BattlegroundJoinGroupQueueWithoutHealer = 842,
        QuestForceRemovedS = 843,
        AttackNoActions = 844,
        InRandomBg = 845,
        InNonRandomBg = 846,
        BnFriendSelf = 847,
        BnFriendAlready = 848,
        BnFriendBlocked = 849,
        BnFriendListFull = 850,
        BnFriendRequestSent = 851,
        BnBroadcastThrottle = 852,
        BgDeveloperOnly = 853,
        CurrencySpellSlotMismatch = 854,
        CurrencyNotTradable = 855,
        RequiresExpansionS = 856,
        QuestFailedSpell = 857,
        TalentFailedUnspentTalentPoints = 858,
        TalentFailedNotEnoughTalentsInPrimaryTree = 859,
        TalentFailedNoPrimaryTreeSelected = 860,
        TalentFailedCantRemoveTalent = 861,
        TalentFailedUnknown = 862,
        TalentFailedInCombat = 863,
        TalentFailedInPvpMatch = 864,
        TalentFailedInMythicPlus = 865,
        WargameRequestFailure = 866,
        RankRequiresAuthenticator = 867,
        GuildBankVoucherFailed = 868,
        WargameRequestSent = 869,
        RequiresAchievementI = 870,
        RefundResultExceedMaxCurrency = 871,
        CantBuyQuantity = 872,
        ItemIsBattlePayLocked = 873,
        PartyAlreadyInBattlegroundQueue = 874,
        PartyConfirmingBattlegroundQueue = 875,
        BattlefieldTeamPartySize = 876,
        InsuffTrackedCurrencyIs = 877,
        NotOnTournamentRealm = 878,
        GuildTrialAccountTrial = 879,
        GuildTrialAccountVeteran = 880,
        GuildUndeletableDueToLevel = 881,
        CantDoThatInAGroup = 882,
        GuildLeaderReplaced = 883,
        TransmogrifyCantEquip = 884,
        TransmogrifyInvalidItemType = 885,
        TransmogrifyNotSoulbound = 886,
        TransmogrifyInvalidSource = 887,
        TransmogrifyInvalidDestination = 888,
        TransmogrifyMismatch = 889,
        TransmogrifyLegendary = 890,
        TransmogrifySameItem = 891,
        TransmogrifySameAppearance = 892,
        TransmogrifyNotEquipped = 893,
        VoidDepositFull = 894,
        VoidWithdrawFull = 895,
        VoidStorageWrapped = 896,
        VoidStorageStackable = 897,
        VoidStorageUnbound = 898,
        VoidStorageRepair = 899,
        VoidStorageCharges = 900,
        VoidStorageQuest = 901,
        VoidStorageConjured = 902,
        VoidStorageMail = 903,
        VoidStorageBag = 904,
        VoidTransferStorageFull = 905,
        VoidTransferInvFull = 906,
        VoidTransferInternalError = 907,
        VoidTransferItemInvalid = 908,
        DifficultyDisabledInLfg = 909,
        VoidStorageUnique = 910,
        VoidStorageLoot = 911,
        VoidStorageHoliday = 912,
        VoidStorageDuration = 913,
        VoidStorageLoadFailed = 914,
        VoidStorageInvalidItem = 915,
        ParentalControlsChatMuted = 916,
        SorStartExperienceIncomplete = 917,
        SorInvalidEmail = 918,
        SorInvalidComment = 919,
        ChallengeModeResetCooldownS = 920,
        ChallengeModeResetKeystone = 921,
        PetJournalAlreadyInLoadout = 922,
        ReportSubmittedSuccessfully = 923,
        ReportSubmissionFailed = 924,
        SuggestionSubmittedSuccessfully = 925,
        BugSubmittedSuccessfully = 926,
        ChallengeModeEnabled = 927,
        ChallengeModeDisabled = 928,
        PetbattleCreateFailed = 929,
        PetbattleNotHere = 930,
        PetbattleNotHereOnTransport = 931,
        PetbattleNotHereUnevenGround = 932,
        PetbattleNotHereObstructed = 933,
        PetbattleNotWhileInCombat = 934,
        PetbattleNotWhileDead = 935,
        PetbattleNotWhileFlying = 936,
        PetbattleTargetInvalid = 937,
        PetbattleTargetOutOfRange = 938,
        PetbattleTargetNotCapturable = 939,
        PetbattleNotATrainer = 940,
        PetbattleDeclined = 941,
        PetbattleInBattle = 942,
        PetbattleInvalidLoadout = 943,
        PetbattleAllPetsDead = 944,
        PetbattleNoPetsInSlots = 945,
        PetbattleNoAccountLock = 946,
        PetbattleWildPetTapped = 947,
        PetbattleRestrictedAccount = 948,
        PetbattleOpponentNotAvailable = 949,
        PetbattleNotWhileInMatchedBattle = 950,
        CantHaveMorePetsOfThatType = 951,
        CantHaveMorePets = 952,
        PvpMapNotFound = 953,
        PvpMapNotSet = 954,
        PetbattleQueueQueued = 955,
        PetbattleQueueAlreadyQueued = 956,
        PetbattleQueueJoinFailed = 957,
        PetbattleQueueJournalLock = 958,
        PetbattleQueueRemoved = 959,
        PetbattleQueueProposalDeclined = 960,
        PetbattleQueueProposalTimeout = 961,
        PetbattleQueueOpponentDeclined = 962,
        PetbattleQueueRequeuedInternal = 963,
        PetbattleQueueRequeuedRemoved = 964,
        PetbattleQueueSlotLocked = 965,
        PetbattleQueueSlotEmpty = 966,
        PetbattleQueueSlotNoTracker = 967,
        PetbattleQueueSlotNoSpecies = 968,
        PetbattleQueueSlotCantBattle = 969,
        PetbattleQueueSlotRevoked = 970,
        PetbattleQueueSlotDead = 971,
        PetbattleQueueSlotNoPet = 972,
        PetbattleQueueNotWhileNeutral = 973,
        PetbattleGameTimeLimitWarning = 974,
        PetbattleGameRoundsLimitWarning = 975,
        HasRestriction = 976,
        ItemUpgradeItemTooLowLevel = 977,
        ItemUpgradeNoPath = 978,
        ItemUpgradeNoMoreUpgrades = 979,
        BonusRollEmpty = 980,
        ChallengeModeFull = 981,
        ChallengeModeInProgress = 982,
        ChallengeModeIncorrectKeystone = 983,
        BattletagFriendNotFound = 984,
        BattletagFriendNotValid = 985,
        BattletagFriendNotAllowed = 986,
        BattletagFriendThrottled = 987,
        BattletagFriendSuccess = 988,
        PetTooHighLevelToUncage = 989,
        PetbattleInternal = 990,
        CantCagePetYet = 991,
        NoLootInChallengeMode = 992,
        QuestPetBattleVictoriesPvpIi = 993,
        RoleCheckAlreadyInProgress = 994,
        RecruitAFriendAccountLimit = 995,
        RecruitAFriendFailed = 996,
        SetLootPersonal = 997,
        SetLootMethodFailedCombat = 998,
        ReagentBankFull = 999,
        ReagentBankLocked = 1000,
        GarrisonBuildingExists = 1001,
        GarrisonInvalidPlot = 1002,
        GarrisonInvalidBuildingid = 1003,
        GarrisonInvalidPlotBuilding = 1004,
        GarrisonRequiresBlueprint = 1005,
        GarrisonNotEnoughCurrency = 1006,
        GarrisonNotEnoughGold = 1007,
        GarrisonCompleteMissionWrongFollowerType = 1008,
        AlreadyUsingLfgList = 1009,
        RestrictedAccountLfgListTrial = 1010,
        ToyUseLimitReached = 1011,
        ToyAlreadyKnown = 1012,
        TransmogSetAlreadyKnown = 1013,
        NotEnoughCurrency = 1014,
        SpecIsDisabled = 1015,
        FeatureRestrictedTrial = 1016,
        CantBeObliterated = 1017,
        CantBeScrapped = 1018,
        CantBeRecrafted = 1019,
        ArtifactRelicDoesNotMatchArtifact = 1020,
        MustEquipArtifact = 1021,
        CantDoThatRightNow = 1022,
        AffectingCombat = 1023,
        EquipmentManagerCombatSwapS = 1024,
        EquipmentManagerBagsFull = 1025,
        EquipmentManagerMissingItemS = 1026,
        MovieRecordingWarningPerf = 1027,
        MovieRecordingWarningDiskFull = 1028,
        MovieRecordingWarningNoMovie = 1029,
        MovieRecordingWarningRequirements = 1030,
        MovieRecordingWarningCompressing = 1031,
        NoChallengeModeReward = 1032,
        ClaimedChallengeModeReward = 1033,
        ChallengeModePeriodResetSs = 1034,
        CantDoThatChallengeModeActive = 1035,
        TalentFailedRestArea = 1036,
        CannotAbandonLastPet = 1037,
        TestCvarSetSss = 1038,
        QuestTurnInFailReason = 1039,
        ClaimedChallengeModeRewardOld = 1040,
        TalentGrantedByAura = 1041,
        ChallengeModeAlreadyComplete = 1042,
        GlyphTargetNotAvailable = 1043,
        PvpWarmodeToggleOn = 1044,
        PvpWarmodeToggleOff = 1045,
        SpellFailedLevelRequirement = 1046,
        SpellFailedCantFlyHere = 1047,
        BattlegroundJoinRequiresLevel = 1048,
        BattlegroundJoinDisqualified = 1049,
        BattlegroundJoinDisqualifiedNoName = 1050,
        VoiceChatGenericUnableToConnect = 1051,
        VoiceChatServiceLost = 1052,
        VoiceChatChannelNameTooShort = 1053,
        VoiceChatChannelNameTooLong = 1054,
        VoiceChatChannelAlreadyExists = 1055,
        VoiceChatTargetNotFound = 1056,
        VoiceChatTooManyRequests = 1057,
        VoiceChatPlayerSilenced = 1058,
        VoiceChatParentalDisableAll = 1059,
        VoiceChatDisabled = 1060,
        NoPvpReward = 1061,
        ClaimedPvpReward = 1062,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1063,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1064,
        AzeriteEssenceSelectionFailedConditionFailed = 1065,
        AzeriteEssenceSelectionFailedRestArea = 1066,
        AzeriteEssenceSelectionFailedSlotLocked = 1067,
        AzeriteEssenceSelectionFailedNotAtForge = 1068,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1069,
        AzeriteEssenceSelectionFailedNotEquipped = 1070,
        SocketingRequiresPunchcardredGem = 1071,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1072,
        SocketingRequiresPunchcardyellowGem = 1073,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1074,
        SocketingRequiresPunchcardblueGem = 1075,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1076,
        SocketingRequiresDominationShard = 1077,
        SocketingDominationShardOnlyInDominationslot = 1078,
        SocketingRequiresCypherGem = 1079,
        SocketingCypherGemOnlyInCypherslot = 1080,
        SocketingRequiresTinkerGem = 1081,
        SocketingTinkerGemOnlyInTinkerslot = 1082,
        SocketingRequiresPrimordialGem = 1083,
        SocketingPrimordialGemOnlyInPrimordialslot = 1084,
        LevelLinkingResultLinked = 1085,
        LevelLinkingResultUnlinked = 1086,
        ClubFinderErrorPostClub = 1087,
        ClubFinderErrorApplyClub = 1088,
        ClubFinderErrorRespondApplicant = 1089,
        ClubFinderErrorCancelApplication = 1090,
        ClubFinderErrorTypeAcceptApplication = 1091,
        ClubFinderErrorTypeNoInvitePermissions = 1092,
        ClubFinderErrorTypeNoPostingPermissions = 1093,
        ClubFinderErrorTypeApplicantList = 1094,
        ClubFinderErrorTypeApplicantListNoPerm = 1095,
        ClubFinderErrorTypeFinderNotAvailable = 1096,
        ClubFinderErrorTypeGetPostingIds = 1097,
        ClubFinderErrorTypeJoinApplication = 1098,
        ClubFinderErrorTypeRealmNotEligible = 1099,
        ClubFinderErrorTypeFlaggedRename = 1100,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1101,
        ItemInteractionNotEnoughGold = 1102,
        ItemInteractionNotEnoughCurrency = 1103,
        ItemInteractionNoConversionOutput = 1104,
        PlayerChoiceErrorPendingChoice = 1105,
        SoulbindInvalidConduit = 1106,
        SoulbindInvalidConduitItem = 1107,
        SoulbindInvalidTalent = 1108,
        SoulbindDuplicateConduit = 1109,
        ActivateSoulbindS = 1110,
        ActivateSoulbindFailedRestArea = 1111,
        CantUseProfanity = 1112,
        NotInPetBattle = 1113,
        NotInNpe = 1114,
        NoSpec = 1115,
        NoDominationshardOverwrite = 1116,
        UseWeeklyRewardsDisabled = 1117,
        CrossFactionGroupJoined = 1118,
        CantTargetUnfriendlyInOverworld = 1119,
        EquipablespellsSlotsFull = 1120,
        ItemModAppearanceGroupAlreadyKnown = 1121,
        CantBulkSellItemWithRefund = 1122,
        WowLabsPartyErrorTypePartyIsFull = 1123,
        WowLabsPartyErrorTypeMaxInviteSent = 1124,
        WowLabsPartyErrorTypePlayerAlreadyInvited = 1125,
        WowLabsPartyErrorTypePartyInviteInvalid = 1126,
        WowLabsLobbyMatchmakerErrorEnterQueueFailed = 1127,
        WowLabsLobbyMatchmakerErrorLeaveQueueFailed = 1128,
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
