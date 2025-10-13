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
        public const int DefaultMaxLevel = 80;
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
        public const int MaxCharactersPerRealm = 200;
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
            LockType.KhazAlgarMining or LockType.KhazAlgarMining25 => SkillType.KhazAlgarMining,
            LockType.KhazAlgarHerbalism or LockType.KhazAlgarHerbalism25 => SkillType.KhazAlgarHerbalism,
            LockType.KhazAlgarAlchemy25 => SkillType.KhazAlgarAlchemy,
            LockType.KhazAlgarBlacksmithing25 => SkillType.KhazAlgarBlacksmithing,
            LockType.KhazAlgarEnchanting25 => SkillType.KhazAlgarEnchanting,
            LockType.KhazAlgarEngineering25 => SkillType.KhazAlgarEngineering,
            LockType.KhazAlgarInscription25 => SkillType.KhazAlgarInscription,
            LockType.KhazAlgarJewelcrafting25 => SkillType.KhazAlgarJewelcrafting,
            LockType.KhazAlgarLeatherworking25 => SkillType.KhazAlgarLeatherworking,
            LockType.KhazAlgarSkinning25 => SkillType.KhazAlgarSkinning,
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
        DeathKnight = 6,
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
            (1 << (Rogue - 1)) | (1 << (Priest - 1)) | (1 << (DeathKnight - 1)) | (1 << (Shaman - 1)) |
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
        //CompanionNetherwingDrake = 80,
        //RostrumStormGryphon      = 82,
        //RostrumFaerieDragon      = 83,
        EarthenDwarfHorde = 84, // Title Earthen Description Earthen (Horde) (Racemask Bit 17)
        EarthenDwarfAlliance = 85, // Title Earthen Description Earthen (Alliance) (Racemask Bit 18)
        //Harronir                   = 86,
        //RostrumAirship            = 87,
        Max = 88
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
        TheWarWithin = 10,
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
        ChatFloodAddonMessageCount,
        ChatFloodAddonMessageDelay,
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
        DbPingInterval,
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
        MaxVisibilityDistanceContinent,
        MaxVisibilityDistanceInstance,
        MaxVisibilityDistanceBattleground,
        MaxVisibilityDistanceArena,
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
        VisibilityNotifyPeriodContinent,
        VisibilityNotifyPeriodInstance,
        VisibilityNotifyPeriodBattleground,
        VisibilityNotifyPeriodArena,
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
        LearnWarbandSceneS = 77,
        CompletedTransmogSetS = 78,
        AppearanceAlreadyLearned = 79,
        RevokeTransmogS = 80,
        InvitePlayerS = 81,
        SuggestInvitePlayerS = 82,
        InformSuggestInviteS = 83,
        InformSuggestInviteSs = 84,
        RequestJoinPlayerS = 85,
        InviteSelf = 86,
        InvitedToGroupSs = 87,
        InvitedAlreadyInGroupSs = 88,
        AlreadyInGroupS = 89,
        RequestedInviteToGroupSs = 90,
        CrossRealmRaidInvite = 91,
        PlayerBusyS = 92,
        NewLeaderS = 93,
        NewLeaderYou = 94,
        NewGuideS = 95,
        NewGuideYou = 96,
        LeftGroupS = 97,
        LeftGroupYou = 98,
        GroupDisbanded = 99,
        DeclineGroupS = 100,
        DeclineGroupRequestS = 101,
        JoinedGroupS = 102,
        UninviteYou = 103,
        BadPlayerNameS = 104,
        NotInGroup = 105,
        TargetNotInGroupS = 106,
        TargetNotInInstanceS = 107,
        NotInInstanceGroup = 108,
        GroupFull = 109,
        NotLeader = 110,
        PlayerDiedS = 111,
        GuildCreateS = 112,
        GuildInviteS = 113,
        InvitedToGuildSss = 114,
        AlreadyInGuildS = 115,
        AlreadyInvitedToGuildS = 116,
        InvitedToGuild = 117,
        AlreadyInGuild = 118,
        GuildAccept = 119,
        GuildDeclineS = 120,
        GuildDeclineAutoS = 121,
        GuildPermissions = 122,
        GuildJoinS = 123,
        GuildFounderS = 124,
        GuildPromoteSss = 125,
        GuildDemoteSs = 126,
        GuildDemoteSss = 127,
        GuildInviteSelf = 128,
        GuildQuitS = 129,
        GuildLeaveS = 130,
        GuildRemoveSs = 131,
        GuildRemoveSelf = 132,
        GuildDisbandS = 133,
        GuildDisbandSelf = 134,
        GuildLeaderS = 135,
        GuildLeaderSelf = 136,
        GuildPlayerNotFoundS = 137,
        GuildPlayerNotInGuildS = 138,
        GuildPlayerNotInGuild = 139,
        GuildBankNotAvailable = 140,
        GuildCantPromoteS = 141,
        GuildCantDemoteS = 142,
        GuildNotInAGuild = 143,
        GuildInternal = 144,
        GuildLeaderIsS = 145,
        GuildLeaderChangedSs = 146,
        GuildDisbanded = 147,
        GuildNotAllied = 148,
        GuildNewLeaderNotAllied = 149,
        GuildLeaderLeave = 150,
        GuildRanksLocked = 151,
        GuildRankInUse = 152,
        GuildRankTooHighS = 153,
        GuildRankTooLowS = 154,
        GuildNameExistsS = 155,
        GuildWithdrawLimit = 156,
        GuildNotEnoughMoney = 157,
        GuildTooMuchMoney = 158,
        GuildBankConjuredItem = 159,
        GuildBankEquippedItem = 160,
        GuildBankBoundItem = 161,
        GuildBankQuestItem = 162,
        GuildBankWrappedItem = 163,
        GuildBankFull = 164,
        GuildBankWrongTab = 165,
        GuildBankWarbandsBankSource = 166,
        GuildBankRealmMismatch = 167,
        GuildNewLeaderWrongRealm = 168,
        NoGuildCharter = 169,
        OutOfRange = 170,
        PlayerDead = 171,
        ClientLockedOut = 172,
        ClientOnTransport = 173,
        KilledByS = 174,
        LootLocked = 175,
        LootTooFar = 176,
        LootDidntKill = 177,
        LootBadFacing = 178,
        LootNotstanding = 179,
        LootStunned = 180,
        LootNoUi = 181,
        LootWhileInvulnerable = 182,
        NoLoot = 183,
        QuestAcceptedS = 184,
        QuestCompleteS = 185,
        QuestFailedS = 186,
        QuestFailedBagFullS = 187,
        QuestFailedMaxCountS = 188,
        QuestFailedLowLevel = 189,
        QuestFailedMissingItems = 190,
        QuestFailedWrongRace = 191,
        QuestFailedNotEnoughMoney = 192,
        QuestFailedExpansion = 193,
        QuestOnlyOneTimed = 194,
        QuestNeedPrereqs = 195,
        QuestNeedPrereqsCustom = 196,
        QuestAlreadyOn = 197,
        QuestAlreadyDone = 198,
        QuestAlreadyDoneDaily = 199,
        QuestHasInProgress = 200,
        QuestRewardExpI = 201,
        QuestRewardMoneyS = 202,
        QuestMustChoose = 203,
        QuestLogFull = 204,
        CombatDamageSsi = 205,
        InspectS = 206,
        CantUseItem = 207,
        CantUseItemInArena = 208,
        CantUseItemInRatedBattleground = 209,
        MustEquipItem = 210,
        PassiveAbility = 211,
        Skill2hNotFound = 212,
        NoAttackTarget = 213,
        InvalidAttackTarget = 214,
        AttackPvpTargetWhileUnflagged = 215,
        AttackStunned = 216,
        AttackPacified = 217,
        AttackMounted = 218,
        AttackFleeing = 219,
        AttackConfused = 220,
        AttackCharmed = 221,
        AttackDead = 222,
        AttackPreventedByMechanicS = 223,
        AttackChannel = 224,
        Taxisamenode = 225,
        Taxinosuchpath = 226,
        Taxiunspecifiedservererror = 227,
        Taxinotenoughmoney = 228,
        Taxitoofaraway = 229,
        Taxinovendornearby = 230,
        Taxinotvisited = 231,
        Taxiplayerbusy = 232,
        Taxiplayeralreadymounted = 233,
        Taxiplayershapeshifted = 234,
        Taxiplayermoving = 235,
        Taxinopaths = 236,
        Taxinoteligible = 237,
        Taxinotstanding = 238,
        Taxiincombat = 239,
        NoReplyTarget = 240,
        GenericNoTarget = 241,
        InitiateTradeS = 242,
        TradeRequestS = 243,
        TradeBlockedS = 244,
        TradeTargetDead = 245,
        TradeTooFar = 246,
        TradeCancelled = 247,
        TradeComplete = 248,
        TradeBagFull = 249,
        TradeTargetBagFull = 250,
        TradeMaxCountExceeded = 251,
        TradeTargetMaxCountExceeded = 252,
        InventoryTradeTooManyUniqueItem = 253,
        AlreadyTrading = 254,
        MountInvalidmountee = 255,
        MountToofaraway = 256,
        MountAlreadymounted = 257,
        MountNotmountable = 258,
        MountNotyourpet = 259,
        MountOther = 260,
        MountLooting = 261,
        MountRacecantmount = 262,
        MountShapeshifted = 263,
        MountNoFavorites = 264,
        MountNoMounts = 265,
        DismountNopet = 266,
        DismountNotmounted = 267,
        DismountNotyourpet = 268,
        SpellFailedTotems = 269,
        SpellFailedReagents = 270,
        SpellFailedReagentsGeneric = 271,
        SpellFailedOptionalReagents = 272,
        CantTradeGold = 273,
        SpellFailedEquippedItem = 274,
        SpellFailedEquippedItemClassS = 275,
        SpellFailedShapeshiftFormS = 276,
        SpellFailedAnotherInProgress = 277,
        Badattackfacing = 278,
        Badattackpos = 279,
        ChestInUse = 280,
        UseCantOpen = 281,
        UseLocked = 282,
        DoorLocked = 283,
        ButtonLocked = 284,
        UseLockedWithItemS = 285,
        UseLockedWithSpellS = 286,
        UseLockedWithSpellKnownSi = 287,
        UseTooFar = 288,
        UseBadAngle = 289,
        UseObjectMoving = 290,
        UseSpellFocus = 291,
        UseDestroyed = 292,
        SetLootFreeforall = 293,
        SetLootRoundrobin = 294,
        SetLootMaster = 295,
        SetLootGroup = 296,
        SetLootNbg = 297,
        SetLootThresholdS = 298,
        NewLootMasterS = 299,
        SpecifyMasterLooter = 300,
        LootSpecChangedS = 301,
        TameFailed = 302,
        ChatWhileDead = 303,
        ChatPlayerNotFoundS = 304,
        Newtaxipath = 305,
        NoPet = 306,
        Notyourpet = 307,
        PetNotRenameable = 308,
        QuestObjectiveCompleteS = 309,
        QuestUnknownComplete = 310,
        QuestAddKillSii = 311,
        QuestAddFoundSii = 312,
        QuestAddItemSii = 313,
        QuestAddPlayerKillSii = 314,
        Cannotcreatedirectory = 315,
        Cannotcreatefile = 316,
        PlayerWrongFaction = 317,
        PlayerIsNeutral = 318,
        BankslotFailedTooMany = 319,
        BankslotInsufficientFunds = 320,
        BankslotNotbanker = 321,
        FriendDbError = 322,
        FriendListFull = 323,
        FriendAddedS = 324,
        BattletagFriendAddedS = 325,
        FriendOnlineSs = 326,
        FriendOfflineS = 327,
        FriendNotFound = 328,
        FriendWrongFaction = 329,
        FriendRemovedS = 330,
        BattletagFriendRemovedS = 331,
        FriendError = 332,
        FriendAlreadyS = 333,
        FriendSelf = 334,
        FriendDeleted = 335,
        IgnoreFull = 336,
        IgnoreSelf = 337,
        IgnoreNotFound = 338,
        IgnoreAlreadyS = 339,
        IgnoreAddedS = 340,
        IgnoreRemovedS = 341,
        IgnoreAmbiguous = 342,
        IgnoreDeleted = 343,
        OnlyOneBolt = 344,
        OnlyOneAmmo = 345,
        SpellFailedEquippedSpecificItem = 346,
        WrongBagTypeSubclass = 347,
        CantWrapStackable = 348,
        CantWrapEquipped = 349,
        CantWrapWrapped = 350,
        CantWrapBound = 351,
        CantWrapUnique = 352,
        CantWrapBags = 353,
        OutOfMana = 354,
        OutOfRage = 355,
        OutOfFocus = 356,
        OutOfEnergy = 357,
        OutOfChi = 358,
        OutOfHealth = 359,
        OutOfRunes = 360,
        OutOfRunicPower = 361,
        OutOfSoulShards = 362,
        OutOfLunarPower = 363,
        OutOfHolyPower = 364,
        OutOfMaelstrom = 365,
        OutOfComboPoints = 366,
        OutOfInsanity = 367,
        OutOfEssence = 368,
        OutOfArcaneCharges = 369,
        OutOfFury = 370,
        OutOfPain = 371,
        OutOfPowerDisplay = 372,
        OutOfRuneBlood = 373,
        OutOfRuneFrost = 374,
        OutOfRuneUnholy = 375,
        OutOfAlternateQuest = 376,
        OutOfAlternateEncounter = 377,
        OutOfAlternateMount = 378,
        OutOfBalance = 379,
        OutOfHappiness = 380,
        OutOfShadowOrbs = 381,
        OutOfRuneChromatic = 382,
        LootGone = 383,
        MountForceddismount = 384,
        AutofollowTooFar = 385,
        UnitNotFound = 386,
        InvalidFollowTarget = 387,
        InvalidFollowPvpCombat = 388,
        InvalidFollowTargetPvpCombat = 389,
        InvalidInspectTarget = 390,
        GuildemblemSuccess = 391,
        GuildemblemInvalidTabardColors = 392,
        GuildemblemNoguild = 393,
        GuildemblemNotguildmaster = 394,
        GuildemblemNotenoughmoney = 395,
        GuildemblemInvalidvendor = 396,
        EmblemerrorNotabardgeoset = 397,
        SpellOutOfRange = 398,
        CommandNeedsTarget = 399,
        NoammoS = 400,
        Toobusytofollow = 401,
        DuelRequested = 402,
        DuelCancelled = 403,
        Deathbindalreadybound = 404,
        DeathbindSuccessS = 405,
        Noemotewhilerunning = 406,
        ZoneExplored = 407,
        ZoneExploredXp = 408,
        InvalidItemTarget = 409,
        InvalidQuestTarget = 410,
        IgnoringYouS = 411,
        FishNotHooked = 412,
        FishEscaped = 413,
        SpellFailedNotunsheathed = 414,
        PetitionOfferedS = 415,
        PetitionSigned = 416,
        PetitionSignedS = 417,
        PetitionDeclinedS = 418,
        PetitionAlreadySigned = 419,
        PetitionRestrictedAccountTrial = 420,
        PetitionAlreadySignedOther = 421,
        PetitionInGuild = 422,
        PetitionCreator = 423,
        PetitionNotEnoughSignatures = 424,
        PetitionNotSameServer = 425,
        PetitionFull = 426,
        PetitionAlreadySignedByS = 427,
        GuildNameInvalid = 428,
        SpellUnlearnedS = 429,
        PetSpellRooted = 430,
        PetSpellAffectingCombat = 431,
        PetSpellOutOfRange = 432,
        PetSpellNotBehind = 433,
        PetSpellTargetsDead = 434,
        PetSpellDead = 435,
        PetSpellNopath = 436,
        ItemCantBeDestroyed = 437,
        TicketAlreadyExists = 438,
        TicketCreateError = 439,
        TicketUpdateError = 440,
        TicketDbError = 441,
        TicketNoText = 442,
        TicketTextTooLong = 443,
        ObjectIsBusy = 444,
        ExhaustionWellrested = 445,
        ExhaustionRested = 446,
        ExhaustionNormal = 447,
        ExhaustionTired = 448,
        ExhaustionExhausted = 449,
        NoItemsWhileShapeshifted = 450,
        CantInteractShapeshifted = 451,
        RealmNotFound = 452,
        MailQuestItem = 453,
        MailBoundItem = 454,
        MailConjuredItem = 455,
        MailBag = 456,
        MailToSelf = 457,
        MailTargetNotFound = 458,
        MailDatabaseError = 459,
        MailDeleteItemError = 460,
        MailWrappedCod = 461,
        MailCantSendRealm = 462,
        MailTempReturnOutage = 463,
        MailRecepientCantReceiveMail = 464,
        MailSent = 465,
        MailTargetIsTrial = 466,
        NotHappyEnough = 467,
        UseCantImmune = 468,
        CantBeDisenchanted = 469,
        CantUseDisarmed = 470,
        AuctionDatabaseError = 471,
        AuctionHigherBid = 472,
        AuctionAlreadyBid = 473,
        AuctionOutbidS = 474,
        AuctionWonS = 475,
        AuctionRemovedS = 476,
        AuctionBidPlaced = 477,
        LogoutFailed = 478,
        QuestPushSuccessS = 479,
        QuestPushInvalidS = 480,
        QuestPushInvalidToRecipientS = 481,
        QuestPushAcceptedS = 482,
        QuestPushDeclinedS = 483,
        QuestPushBusyS = 484,
        QuestPushDeadS = 485,
        QuestPushDeadToRecipientS = 486,
        QuestPushLogFullS = 487,
        QuestPushLogFullToRecipientS = 488,
        QuestPushOnquestS = 489,
        QuestPushOnquestToRecipientS = 490,
        QuestPushAlreadyDoneS = 491,
        QuestPushAlreadyDoneToRecipientS = 492,
        QuestPushNotDailyS = 493,
        QuestPushTimerExpiredS = 494,
        QuestPushNotInPartyS = 495,
        QuestPushDifferentServerDailyS = 496,
        QuestPushDifferentServerDailyToRecipientS = 497,
        QuestPushNotAllowedS = 498,
        QuestPushPrerequisiteS = 499,
        QuestPushPrerequisiteToRecipientS = 500,
        QuestPushLowLevelS = 501,
        QuestPushLowLevelToRecipientS = 502,
        QuestPushHighLevelS = 503,
        QuestPushHighLevelToRecipientS = 504,
        QuestPushClassS = 505,
        QuestPushClassToRecipientS = 506,
        QuestPushRaceS = 507,
        QuestPushRaceToRecipientS = 508,
        QuestPushLowFactionS = 509,
        QuestPushLowFactionToRecipientS = 510,
        QuestPushHighFactionS = 511,
        QuestPushHighFactionToRecipientS = 512,
        QuestPushExpansionS = 513,
        QuestPushExpansionToRecipientS = 514,
        QuestPushNotGarrisonOwnerS = 515,
        QuestPushNotGarrisonOwnerToRecipientS = 516,
        QuestPushWrongCovenantS = 517,
        QuestPushWrongCovenantToRecipientS = 518,
        QuestPushNewPlayerExperienceS = 519,
        QuestPushNewPlayerExperienceToRecipientS = 520,
        QuestPushWrongFactionS = 521,
        QuestPushWrongFactionToRecipientS = 522,
        QuestPushCrossFactionRestrictedS = 523,
        RaidGroupLowlevel = 524,
        RaidGroupOnly = 525,
        RaidGroupFull = 526,
        RaidGroupRequirementsUnmatch = 527,
        CorpseIsNotInInstance = 528,
        PvpKillHonorable = 529,
        PvpKillDishonorable = 530,
        SpellFailedAlreadyAtFullHealth = 531,
        SpellFailedAlreadyAtFullMana = 532,
        SpellFailedAlreadyAtFullPowerS = 533,
        AutolootMoneyS = 534,
        GenericStunned = 535,
        GenericThrottle = 536,
        ClubFinderSearchingTooFast = 537,
        TargetStunned = 538,
        MustRepairDurability = 539,
        RaidYouJoined = 540,
        RaidYouLeft = 541,
        InstanceGroupJoinedWithParty = 542,
        InstanceGroupJoinedWithRaid = 543,
        RaidMemberAddedS = 544,
        RaidMemberRemovedS = 545,
        InstanceGroupAddedS = 546,
        InstanceGroupRemovedS = 547,
        ClickOnItemToFeed = 548,
        TooManyChatChannels = 549,
        LootRollPending = 550,
        LootPlayerNotFound = 551,
        NotInRaid = 552,
        LoggingOut = 553,
        TargetLoggingOut = 554,
        NotWhileMounted = 555,
        NotWhileShapeshifted = 556,
        NotInCombat = 557,
        NotWhileDisarmed = 558,
        PetBroken = 559,
        TalentWipeError = 560,
        SpecWipeError = 561,
        GlyphWipeError = 562,
        PetSpecWipeError = 563,
        FeignDeathResisted = 564,
        MeetingStoneInQueueS = 565,
        MeetingStoneLeftQueueS = 566,
        MeetingStoneOtherMemberLeft = 567,
        MeetingStonePartyKickedFromQueue = 568,
        MeetingStoneMemberStillInQueue = 569,
        MeetingStoneSuccess = 570,
        MeetingStoneInProgress = 571,
        MeetingStoneMemberAddedS = 572,
        MeetingStoneGroupFull = 573,
        MeetingStoneNotLeader = 574,
        MeetingStoneInvalidLevel = 575,
        MeetingStoneTargetNotInParty = 576,
        MeetingStoneTargetInvalidLevel = 577,
        MeetingStoneMustBeLeader = 578,
        MeetingStoneNoRaidGroup = 579,
        MeetingStoneNeedParty = 580,
        MeetingStoneNotFound = 581,
        MeetingStoneTargetInVehicle = 582,
        GuildemblemSame = 583,
        EquipTradeItem = 584,
        PvpToggleOn = 585,
        PvpToggleOff = 586,
        GroupJoinBattlegroundDeserters = 587,
        GroupJoinBattlegroundDead = 588,
        GroupJoinBattlegroundS = 589,
        GroupJoinBattlegroundFail = 590,
        GroupJoinBattlegroundTooMany = 591,
        SoloJoinBattlegroundS = 592,
        JoinSingleScenarioS = 593,
        BattlegroundTooManyQueues = 594,
        BattlegroundCannotQueueForRated = 595,
        BattledgroundQueuedForRated = 596,
        BattlegroundTeamLeftQueue = 597,
        BattlegroundNotInBattleground = 598,
        AlreadyInArenaTeamS = 599,
        InvalidPromotionCode = 600,
        BgPlayerJoinedSs = 601,
        BgPlayerLeftS = 602,
        RestrictedAccount = 603,
        RestrictedAccountTrial = 604,
        NotEnoughPurchasedGameTime = 605,
        PlayTimeExceeded = 606,
        ApproachingPartialPlayTime = 607,
        ApproachingPartialPlayTime2 = 608,
        ApproachingNoPlayTime = 609,
        ApproachingNoPlayTime2 = 610,
        UnhealthyTime = 611,
        ChatRestrictedTrial = 612,
        ChatThrottled = 613,
        MailReachedCap = 614,
        InvalidRaidTarget = 615,
        RaidLeaderReadyCheckStartS = 616,
        ReadyCheckInProgress = 617,
        ReadyCheckThrottled = 618,
        VoteToAbandonNotYet = 619,
        DungeonDifficultyFailed = 620,
        DungeonDifficultyChangedS = 621,
        TradeWrongRealm = 622,
        TradeNotOnTaplist = 623,
        ChatPlayerAmbiguousS = 624,
        LootCantLootThatNow = 625,
        LootMasterInvFull = 626,
        LootMasterUniqueItem = 627,
        LootMasterOther = 628,
        FilteringYouS = 629,
        UsePreventedByMechanicS = 630,
        ItemUniqueEquippable = 631,
        LfgLeaderIsLfmS = 632,
        LfgPending = 633,
        CantSpeakLangage = 634,
        VendorMissingTurnins = 635,
        BattlegroundNotInTeam = 636,
        NotInBattleground = 637,
        NotEnoughHonorPoints = 638,
        NotEnoughArenaPoints = 639,
        SocketingRequiresMetaGem = 640,
        SocketingMetaGemOnlyInMetaslot = 641,
        SocketingRequiresHydraulicGem = 642,
        SocketingHydraulicGemOnlyInHydraulicslot = 643,
        SocketingRequiresCogwheelGem = 644,
        SocketingCogwheelGemOnlyInCogwheelslot = 645,
        SocketingItemTooLowLevel = 646,
        ItemMaxCountSocketed = 647,
        SystemDisabled = 648,
        QuestFailedTooManyDailyQuestsI = 649,
        ItemMaxCountEquippedSocketed = 650,
        ItemUniqueEquippableSocketed = 651,
        UserSquelched = 652,
        AccountSilenced = 653,
        PartyMemberSilenced = 654,
        PartyMemberSilencedLfgDelist = 655,
        TooMuchGold = 656,
        NotBarberSitting = 657,
        QuestFailedCais = 658,
        InviteRestrictedTrial = 659,
        VoiceIgnoreFull = 660,
        VoiceIgnoreSelf = 661,
        VoiceIgnoreNotFound = 662,
        VoiceIgnoreAlreadyS = 663,
        VoiceIgnoreAddedS = 664,
        VoiceIgnoreRemovedS = 665,
        VoiceIgnoreAmbiguous = 666,
        VoiceIgnoreDeleted = 667,
        UnknownMacroOptionS = 668,
        NotDuringArenaMatch = 669,
        NotInRatedBattleground = 670,
        PlayerSilenced = 671,
        PlayerUnsilenced = 672,
        ComsatDisconnect = 673,
        ComsatReconnectAttempt = 674,
        ComsatConnectFail = 675,
        MailInvalidAttachmentSlot = 676,
        MailTooManyAttachments = 677,
        MailInvalidAttachment = 678,
        MailAttachmentExpired = 679,
        VoiceChatParentalDisableMic = 680,
        ProfaneChatName = 681,
        PlayerSilencedEcho = 682,
        PlayerUnsilencedEcho = 683,
        LootCantLootThat = 684,
        ArenaExpiredCais = 685,
        GroupActionThrottled = 686,
        AlreadyPickpocketed = 687,
        NameInvalid = 688,
        NameNoName = 689,
        NameTooShort = 690,
        NameTooLong = 691,
        NameMixedLanguages = 692,
        NameProfane = 693,
        NameReserved = 694,
        NameThreeConsecutive = 695,
        NameInvalidSpace = 696,
        NameConsecutiveSpaces = 697,
        NameRussianConsecutiveSilentCharacters = 698,
        NameRussianSilentCharacterAtBeginningOrEnd = 699,
        NameDeclensionDoesntMatchBaseName = 700,
        RecruitAFriendNotLinked = 701,
        RecruitAFriendNotNow = 702,
        RecruitAFriendSummonLevelMax = 703,
        RecruitAFriendSummonCooldown = 704,
        RecruitAFriendSummonOffline = 705,
        RecruitAFriendInsufExpanLvl = 706,
        RecruitAFriendMapIncomingTransferNotAllowed = 707,
        NotSameAccount = 708,
        BadOnUseEnchant = 709,
        TradeSelf = 710,
        TooManySockets = 711,
        ItemMaxLimitCategoryCountExceededIs = 712,
        TradeTargetMaxLimitCategoryCountExceededIs = 713,
        ItemMaxLimitCategorySocketedExceededIs = 714,
        ItemMaxLimitCategoryEquippedExceededIs = 715,
        ShapeshiftFormCannotEquip = 716,
        ItemInventoryFullSatchel = 717,
        ScalingStatItemLevelExceeded = 718,
        ScalingStatItemLevelTooLow = 719,
        PurchaseLevelTooLow = 720,
        GroupSwapFailed = 721,
        InviteInCombat = 722,
        InvalidGlyphSlot = 723,
        GenericNoValidTargets = 724,
        CalendarEventAlertS = 725,
        PetLearnSpellS = 726,
        PetLearnAbilityS = 727,
        PetSpellUnlearnedS = 728,
        InviteUnknownRealm = 729,
        InviteNoPartyServer = 730,
        InvitePartyBusy = 731,
        InvitePartyBusyPendingRequest = 732,
        InvitePartyBusyPendingSuggest = 733,
        PartyTargetAmbiguous = 734,
        PartyLfgInviteRaidLocked = 735,
        PartyLfgBootLimit = 736,
        PartyLfgBootCooldownS = 737,
        PartyLfgBootNotEligibleS = 738,
        PartyLfgBootInpatientTimerS = 739,
        PartyLfgBootInProgress = 740,
        PartyLfgBootTooFewPlayers = 741,
        PartyLfgBootVoteSucceeded = 742,
        PartyLfgBootVoteFailed = 743,
        PartyLfgBootDisallowedByMap = 744,
        PartyLfgBootDungeonComplete = 745,
        PartyLfgBootLootRolls = 746,
        PartyLfgBootVoteRegistered = 747,
        PartyPrivateGroupOnly = 748,
        PartyLfgTeleportInCombat = 749,
        PartyTimeRunningSeasonIdMustMatch = 750,
        RaidDisallowedByLevel = 751,
        RaidDisallowedByCrossRealm = 752,
        PartyRoleNotAvailable = 753,
        JoinLfgObjectFailed = 754,
        LfgRemovedLevelup = 755,
        LfgRemovedXpToggle = 756,
        LfgRemovedFactionChange = 757,
        BattlegroundInfoThrottled = 758,
        BattlegroundAlreadyIn = 759,
        ArenaTeamChangeFailedQueued = 760,
        ArenaTeamPermissions = 761,
        NotWhileFalling = 762,
        NotWhileMoving = 763,
        NotWhileFatigued = 764,
        MaxSockets = 765,
        MultiCastActionTotemS = 766,
        BattlegroundJoinLevelup = 767,
        RemoveFromPvpQueueXpGain = 768,
        BattlegroundJoinXpGain = 769,
        BattlegroundJoinMercenary = 770,
        BattlegroundJoinTooManyHealers = 771,
        BattlegroundJoinRatedTooManyHealers = 772,
        BattlegroundJoinTooManyTanks = 773,
        BattlegroundJoinTooManyDamage = 774,
        RaidDifficultyFailed = 775,
        RaidDifficultyChangedS = 776,
        LegacyRaidDifficultyChangedS = 777,
        RaidLockoutChangedS = 778,
        RaidConvertedToParty = 779,
        PartyConvertedToRaid = 780,
        PlayerDifficultyChangedS = 781,
        GmresponseDbError = 782,
        BattlegroundJoinRangeIndex = 783,
        ArenaJoinRangeIndex = 784,
        RemoveFromPvpQueueFactionChange = 785,
        BattlegroundJoinFailed = 786,
        BattlegroundJoinNoValidSpecForRole = 787,
        BattlegroundJoinRespec = 788,
        BattlegroundInvitationDeclined = 789,
        BattlegroundInvitationDeclinedBy = 790,
        BattlegroundJoinTimedOut = 791,
        BattlegroundDupeQueue = 792,
        BattlegroundJoinMustCompleteQuest = 793,
        InBattlegroundRespec = 794,
        MailLimitedDurationItem = 795,
        YellRestrictedTrial = 796,
        ChatRaidRestrictedTrial = 797,
        LfgRoleCheckFailed = 798,
        LfgRoleCheckFailedTimeout = 799,
        LfgRoleCheckFailedNotViable = 800,
        LfgReadyCheckFailed = 801,
        LfgReadyCheckFailedTimeout = 802,
        LfgGroupFull = 803,
        LfgNoLfgObject = 804,
        LfgNoSlotsPlayer = 805,
        LfgNoSlotsParty = 806,
        LfgNoSpec = 807,
        LfgMismatchedSlots = 808,
        LfgMismatchedSlotsLocalXrealm = 809,
        LfgPartyPlayersFromDifferentRealms = 810,
        LfgMembersNotPresent = 811,
        LfgGetInfoTimeout = 812,
        LfgInvalidSlot = 813,
        LfgDeserterPlayer = 814,
        LfgDeserterParty = 815,
        LfgDead = 816,
        LfgRandomCooldownPlayer = 817,
        LfgRandomCooldownParty = 818,
        LfgTooManyMembers = 819,
        LfgTooFewMembers = 820,
        LfgProposalFailed = 821,
        LfgProposalDeclinedSelf = 822,
        LfgProposalDeclinedParty = 823,
        LfgNoSlotsSelected = 824,
        LfgNoRolesSelected = 825,
        LfgRoleCheckInitiated = 826,
        LfgReadyCheckInitiated = 827,
        LfgPlayerDeclinedRoleCheck = 828,
        LfgPlayerDeclinedReadyCheck = 829,
        LfgLorewalking = 830,
        LfgJoinedQueue = 831,
        LfgJoinedFlexQueue = 832,
        LfgJoinedRfQueue = 833,
        LfgJoinedScenarioQueue = 834,
        LfgJoinedWorldPvpQueue = 835,
        LfgJoinedBattlefieldQueue = 836,
        LfgJoinedList = 837,
        QueuedPlunderstorm = 838,
        LfgLeftQueue = 839,
        LfgLeftList = 840,
        LfgRoleCheckAborted = 841,
        LfgReadyCheckAborted = 842,
        LfgCantUseBattleground = 843,
        LfgCantUseDungeons = 844,
        LfgReasonTooManyLfg = 845,
        LfgFarmLimit = 846,
        LfgNoCrossFactionParties = 847,
        InvalidTeleportLocation = 848,
        TooFarToInteract = 849,
        BattlegroundPlayersFromDifferentRealms = 850,
        DifficultyChangeCooldownS = 851,
        DifficultyChangeCombatCooldownS = 852,
        DifficultyChangeWorldstate = 853,
        DifficultyChangeEncounter = 854,
        DifficultyChangeCombat = 855,
        DifficultyChangePlayerBusy = 856,
        DifficultyChangePlayerOnVehicle = 857,
        DifficultyChangeAlreadyStarted = 858,
        DifficultyChangeOtherHeroicS = 859,
        DifficultyChangeHeroicInstanceAlreadyRunning = 860,
        ArenaTeamPartySize = 861,
        SoloShuffleWargameGroupSize = 862,
        SoloShuffleWargameGroupComp = 863,
        SoloRbgWargameGroupSize = 864,
        SoloRbgWargameGroupComp = 865,
        SoloMinItemLevel = 866,
        PvpPlayerAbandoned = 867,
        BattlegroundJoinGroupQueueWithoutHealer = 868,
        QuestForceRemovedS = 869,
        AttackNoActions = 870,
        InRandomBg = 871,
        InNonRandomBg = 872,
        BnFriendSelf = 873,
        BnFriendAlready = 874,
        BnFriendBlocked = 875,
        BnFriendListFull = 876,
        BnFriendRequestSent = 877,
        BnBroadcastThrottle = 878,
        BgDeveloperOnly = 879,
        CurrencySpellSlotMismatch = 880,
        CurrencyNotTradable = 881,
        RequiresExpansionS = 882,
        QuestFailedSpell = 883,
        TalentFailedUnspentTalentPoints = 884,
        TalentFailedNotEnoughTalentsInPrimaryTree = 885,
        TalentFailedNoPrimaryTreeSelected = 886,
        TalentFailedCantRemoveTalent = 887,
        TalentFailedUnknown = 888,
        TalentFailedInCombat = 889,
        TalentFailedInPvpMatch = 890,
        TalentFailedInMythicPlus = 891,
        WargameRequestFailure = 892,
        RankRequiresAuthenticator = 893,
        GuildBankVoucherFailed = 894,
        WargameRequestSent = 895,
        RequiresAchievementI = 896,
        RefundResultExceedMaxCurrency = 897,
        CantBuyQuantity = 898,
        ItemIsBattlePayLocked = 899,
        PartyAlreadyInBattlegroundQueue = 900,
        PartyConfirmingBattlegroundQueue = 901,
        BattlefieldTeamPartySize = 902,
        InsuffTrackedCurrencyIs = 903,
        NotOnTournamentRealm = 904,
        GuildTrialAccountTrial = 905,
        GuildTrialAccountVeteran = 906,
        GuildUndeletableDueToLevel = 907,
        CantDoThatInAGroup = 908,
        GuildLeaderReplaced = 909,
        TransmogrifyCantEquip = 910,
        TransmogrifyInvalidItemType = 911,
        TransmogrifyNotSoulbound = 912,
        TransmogrifyInvalidSource = 913,
        TransmogrifyInvalidDestination = 914,
        TransmogrifyMismatch = 915,
        TransmogrifyLegendary = 916,
        TransmogrifySameItem = 917,
        TransmogrifySameAppearance = 918,
        TransmogrifyNotEquipped = 919,
        VoidDepositFull = 920,
        VoidWithdrawFull = 921,
        VoidStorageWrapped = 922,
        VoidStorageStackable = 923,
        VoidStorageUnbound = 924,
        VoidStorageRepair = 925,
        VoidStorageCharges = 926,
        VoidStorageQuest = 927,
        VoidStorageConjured = 928,
        VoidStorageMail = 929,
        VoidStorageBag = 930,
        VoidTransferStorageFull = 931,
        VoidTransferInvFull = 932,
        VoidTransferInternalError = 933,
        VoidTransferItemInvalid = 934,
        DifficultyDisabledInLfg = 935,
        VoidStorageUnique = 936,
        VoidStorageLoot = 937,
        VoidStorageHoliday = 938,
        VoidStorageDuration = 939,
        VoidStorageLoadFailed = 940,
        VoidStorageInvalidItem = 941,
        VoidStorageAccountItem = 942,
        ParentalControlsChatMuted = 943,
        SorStartExperienceIncomplete = 944,
        SorInvalidEmail = 945,
        SorInvalidComment = 946,
        ChallengeModeResetCooldownS = 947,
        ChallengeModeResetKeystone = 948,
        PetJournalAlreadyInLoadout = 949,
        ReportSubmittedSuccessfully = 950,
        ReportSubmissionFailed = 951,
        SuggestionSubmittedSuccessfully = 952,
        BugSubmittedSuccessfully = 953,
        ChallengeModeEnabled = 954,
        ChallengeModeDisabled = 955,
        PetbattleCreateFailed = 956,
        PetbattleNotHere = 957,
        PetbattleNotHereOnTransport = 958,
        PetbattleNotHereUnevenGround = 959,
        PetbattleNotHereObstructed = 960,
        PetbattleNotWhileInCombat = 961,
        PetbattleNotWhileDead = 962,
        PetbattleNotWhileFlying = 963,
        PetbattleTargetInvalid = 964,
        PetbattleTargetOutOfRange = 965,
        PetbattleTargetNotCapturable = 966,
        PetbattleNotATrainer = 967,
        PetbattleDeclined = 968,
        PetbattleInBattle = 969,
        PetbattleInvalidLoadout = 970,
        PetbattleAllPetsDead = 971,
        PetbattleNoPetsInSlots = 972,
        PetbattleNoAccountLock = 973,
        PetbattleWildPetTapped = 974,
        PetbattleRestrictedAccount = 975,
        PetbattleOpponentNotAvailable = 976,
        PetbattleNotWhileInMatchedBattle = 977,
        CantHaveMorePetsOfThatType = 978,
        CantHaveMorePets = 979,
        PvpMapNotFound = 980,
        PvpMapNotSet = 981,
        PetbattleQueueQueued = 982,
        PetbattleQueueAlreadyQueued = 983,
        PetbattleQueueJoinFailed = 984,
        PetbattleQueueJournalLock = 985,
        PetbattleQueueRemoved = 986,
        PetbattleQueueProposalDeclined = 987,
        PetbattleQueueProposalTimeout = 988,
        PetbattleQueueOpponentDeclined = 989,
        PetbattleQueueRequeuedInternal = 990,
        PetbattleQueueRequeuedRemoved = 991,
        PetbattleQueueSlotLocked = 992,
        PetbattleQueueSlotEmpty = 993,
        PetbattleQueueSlotNoTracker = 994,
        PetbattleQueueSlotNoSpecies = 995,
        PetbattleQueueSlotCantBattle = 996,
        PetbattleQueueSlotRevoked = 997,
        PetbattleQueueSlotDead = 998,
        PetbattleQueueSlotNoPet = 999,
        PetbattleQueueNotWhileNeutral = 1000,
        PetbattleGameTimeLimitWarning = 1001,
        PetbattleGameRoundsLimitWarning = 1002,
        HasRestriction = 1003,
        ItemUpgradeItemTooLowLevel = 1004,
        ItemUpgradeNoPath = 1005,
        ItemUpgradeNoMoreUpgrades = 1006,
        BonusRollEmpty = 1007,
        ChallengeModeFull = 1008,
        ChallengeModeInProgress = 1009,
        ChallengeModeIncorrectKeystone = 1010,
        StartRestrictedChallengeMode = 1011,
        BattletagFriendNotFound = 1012,
        BattletagFriendNotValid = 1013,
        BattletagFriendNotAllowed = 1014,
        BattletagFriendThrottled = 1015,
        BattletagFriendSuccess = 1016,
        PetTooHighLevelToUncage = 1017,
        PetbattleInternal = 1018,
        CantCagePetYet = 1019,
        NoLootInChallengeMode = 1020,
        QuestPetBattleVictoriesPvpIi = 1021,
        RoleCheckAlreadyInProgress = 1022,
        RecruitAFriendAccountLimit = 1023,
        RecruitAFriendFailed = 1024,
        SetLootPersonal = 1025,
        SetLootMethodFailedCombat = 1026,
        ReagentBankFull = 1027,
        ReagentBankLocked = 1028,
        GarrisonBuildingExists = 1029,
        GarrisonInvalidPlot = 1030,
        GarrisonInvalidBuildingid = 1031,
        GarrisonInvalidPlotBuilding = 1032,
        GarrisonRequiresBlueprint = 1033,
        GarrisonNotEnoughCurrency = 1034,
        GarrisonNotEnoughGold = 1035,
        GarrisonCompleteMissionWrongFollowerType = 1036,
        AlreadyUsingLfgList = 1037,
        RestrictedAccountLfgListTrial = 1038,
        ToyUseLimitReached = 1039,
        ToyAlreadyKnown = 1040,
        TransmogSetAlreadyKnown = 1041,
        NotEnoughCurrency = 1042,
        SpecIsDisabled = 1043,
        FeatureRestrictedTrial = 1044,
        CantBeObliterated = 1045,
        CantBeScrapped = 1046,
        CantBeRecrafted = 1047,
        ArtifactRelicDoesNotMatchArtifact = 1048,
        MustEquipArtifact = 1049,
        CantDoThatRightNow = 1050,
        AffectingCombat = 1051,
        EquipmentManagerCombatSwapS = 1052,
        EquipmentManagerBagsFull = 1053,
        EquipmentManagerMissingItemS = 1054,
        MovieRecordingWarningPerf = 1055,
        MovieRecordingWarningDiskFull = 1056,
        MovieRecordingWarningNoMovie = 1057,
        MovieRecordingWarningRequirements = 1058,
        MovieRecordingWarningCompressing = 1059,
        NoChallengeModeReward = 1060,
        ClaimedChallengeModeReward = 1061,
        ChallengeModePeriodResetSs = 1062,
        CantDoThatChallengeModeActive = 1063,
        TalentFailedRestArea = 1064,
        CannotAbandonLastPet = 1065,
        TestCvarSetSss = 1066,
        QuestTurnInFailReason = 1067,
        ClaimedChallengeModeRewardOld = 1068,
        TalentGrantedByAura = 1069,
        ChallengeModeAlreadyComplete = 1070,
        GlyphTargetNotAvailable = 1071,
        PvpWarmodeToggleOn = 1072,
        PvpWarmodeToggleOff = 1073,
        SpellFailedLevelRequirement = 1074,
        SpellFailedCantFlyHere = 1075,
        BattlegroundJoinRequiresLevel = 1076,
        BattlegroundJoinDisqualified = 1077,
        BattlegroundJoinDisqualifiedNoName = 1078,
        VoiceChatGenericUnableToConnect = 1079,
        VoiceChatServiceLost = 1080,
        VoiceChatChannelNameTooShort = 1081,
        VoiceChatChannelNameTooLong = 1082,
        VoiceChatChannelAlreadyExists = 1083,
        VoiceChatTargetNotFound = 1084,
        VoiceChatTooManyRequests = 1085,
        VoiceChatPlayerSilenced = 1086,
        VoiceChatParentalDisableAll = 1087,
        VoiceChatDisabled = 1088,
        NoPvpReward = 1089,
        ClaimedPvpReward = 1090,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1091,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1092,
        AzeriteEssenceSelectionFailedConditionFailed = 1093,
        AzeriteEssenceSelectionFailedRestArea = 1094,
        AzeriteEssenceSelectionFailedSlotLocked = 1095,
        AzeriteEssenceSelectionFailedNotAtForge = 1096,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1097,
        AzeriteEssenceSelectionFailedNotEquipped = 1098,
        SocketingGenericFailure = 1099,
        SocketingRequiresPunchcardredGem = 1100,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1101,
        SocketingRequiresPunchcardyellowGem = 1102,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1103,
        SocketingRequiresPunchcardblueGem = 1104,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1105,
        SocketingRequiresDominationShard = 1106,
        SocketingDominationShardOnlyInDominationslot = 1107,
        SocketingRequiresCypherGem = 1108,
        SocketingCypherGemOnlyInCypherslot = 1109,
        SocketingRequiresTinkerGem = 1110,
        SocketingTinkerGemOnlyInTinkerslot = 1111,
        SocketingRequiresPrimordialGem = 1112,
        SocketingPrimordialGemOnlyInPrimordialslot = 1113,
        SocketingRequiresFragranceGem = 1114,
        SocketingFragranceGemOnlyInFragranceslot = 1115,
        SocketingRequiresSingingThunderGem = 1116,
        SocketingSingingthunderGemOnlyInSingingthunderslot = 1117,
        SocketingRequiresSingingSeaGem = 1118,
        SocketingSingingseaGemOnlyInSingingseaslot = 1119,
        SocketingRequiresSingingWindGem = 1120,
        SocketingSingingwindGemOnlyInSingingwindslot = 1121,
        SocketingRequiresFiberGem = 1122,
        SocketingFiberGemOnlyInFiberslot = 1123,
        LevelLinkingResultLinked = 1124,
        LevelLinkingResultUnlinked = 1125,
        ClubFinderErrorPostClub = 1126,
        ClubFinderErrorApplyClub = 1127,
        ClubFinderErrorRespondApplicant = 1128,
        ClubFinderErrorCancelApplication = 1129,
        ClubFinderErrorTypeAcceptApplication = 1130,
        ClubFinderErrorTypeNoInvitePermissions = 1131,
        ClubFinderErrorTypeNoPostingPermissions = 1132,
        ClubFinderErrorTypeApplicantList = 1133,
        ClubFinderErrorTypeApplicantListNoPerm = 1134,
        ClubFinderErrorTypeFinderNotAvailable = 1135,
        ClubFinderErrorTypeGetPostingIds = 1136,
        ClubFinderErrorTypeJoinApplication = 1137,
        ClubFinderErrorTypeRealmNotEligible = 1138,
        ClubFinderErrorTypeFlaggedRename = 1139,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1140,
        ItemInteractionNotEnoughGold = 1141,
        ItemInteractionNotEnoughCurrency = 1142,
        ItemInteractionNoConversionOutput = 1143,
        PlayerChoiceErrorPendingChoice = 1144,
        SoulbindInvalidConduit = 1145,
        SoulbindInvalidConduitItem = 1146,
        SoulbindInvalidTalent = 1147,
        SoulbindDuplicateConduit = 1148,
        ActivateSoulbindS = 1149,
        ActivateSoulbindFailedRestArea = 1150,
        CantUseProfanity = 1151,
        NotInPetBattle = 1152,
        NotInNpe = 1153,
        NoSpec = 1154,
        NoDominationshardOverwrite = 1155,
        UseWeeklyRewardsDisabled = 1156,
        CrossFactionGroupJoined = 1157,
        CantTargetUnfriendlyInOverworld = 1158,
        EquipablespellsSlotsFull = 1159,
        ItemModAppearanceGroupAlreadyKnown = 1160,
        CantBulkSellItemWithRefund = 1161,
        NoSoulboundItemInAccountBank = 1162,
        NoRefundableItemInAccountBank = 1163,
        CantDeleteInAccountBank = 1164,
        NoImmediateContainerInAccountBank = 1165,
        NoOpenImmediateContainerInAccountBank = 1166,
        CantTradeAccountItem = 1167,
        NoAccountInventoryLock = 1168,
        BankNotAccessible = 1169,
        TooManyAccountBankTabs = 1170,
        BankTabNotUnlocked = 1171,
        AccountMoneyLocked = 1172,
        BankTabInvalidName = 1173,
        BankTabInvalidText = 1174,
        CharacterBankNotConverted = 1175,
        WowLabsPartyErrorTypePartyIsFull = 1176,
        WowLabsPartyErrorTypeMaxInviteSent = 1177,
        WowLabsPartyErrorTypePlayerAlreadyInvited = 1178,
        WowLabsPartyErrorTypePartyInviteInvalid = 1179,
        WowLabsLobbyMatchmakerErrorEnterQueueFailed = 1180,
        WowLabsLobbyMatchmakerErrorLeaveQueueFailed = 1181,
        WowLabsSetWowLabsAreaIdFailed = 1182,
        PlunderstormCannotQueue = 1183,
        TargetIsSelfFoundCannotTrade = 1184,
        PlayerIsSelfFoundCannotTrade = 1185,
        MailRecepientIsSelfFoundCannotReceiveMail = 1186,
        PlayerIsSelfFoundCannotSendMail = 1187,
        PlayerIsSelfFoundCannotUseAuctionHouse = 1188,
        MailTargetCannotReceiveMail = 1189,
        RemixInvalidTransferRequest = 1190,
        CurrencyTransferInvalidCharacter = 1191,
        CurrencyTransferInvalidCurrency = 1192,
        CurrencyTransferInsufficientCurrency = 1193,
        CurrencyTransferMaxQuantity = 1194,
        CurrencyTransferNoValidSource = 1195,
        CurrencyTransferCharacterLoggedIn = 1196,
        CurrencyTransferServerError = 1197,
        CurrencyTransferUnmetRequirements = 1198,
        CurrencyTransferTransactionInProgress = 1199,
        CurrencyTransferDisabled = 1200,
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

    public enum WarbandSceneFlags
    {
        DoNotInclude = 0x01,
        HiddenUntilCollected = 0x02,
        CannotBeSaved = 0x04,
        AwardedAutomatically = 0x08,
        IsDefault = 0x10
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
