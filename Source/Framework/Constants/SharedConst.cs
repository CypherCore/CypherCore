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
        public const double Gravity = 19.291103363037109375;
        public const float terminalVelocity = 60.148003f;
        public const float terminalSafefallVelocity = 7.0f;
        public const float terminal_length = (float)((terminalVelocity * terminalVelocity) / (2.0f * Gravity));
        public const float terminal_safeFall_length = (float)((terminalSafefallVelocity * terminalSafefallVelocity) / (2.0f * Gravity));
        public const float terminal_fallTime = (float)(terminalVelocity / Gravity); // the time that needed to reach terminalVelocity
        public const float terminal_safeFall_fallTime = (float)(terminalSafefallVelocity / Gravity); // the time that needed to reach terminalVelocity with safefall

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
            LockType.MidnightMining or LockType.MidnightMining25 => SkillType.MidnightMining,
            LockType.MidnightHerbalism or LockType.MidnightHerbalism25 => SkillType.MidnightHerbalism,
            LockType.MidnightSkinning or LockType.MidnightSkinning25 => SkillType.MidnightSkinning,
            LockType.MidnightAlchemy or LockType.MidnightAlchemy25 => SkillType.MidnightAlchemy,
            LockType.MidnightBlacksmithing or LockType.MidnightBlacksmithing25 => SkillType.MidnightBlacksmithing,
            LockType.MidnightEngineering or LockType.MidnightEngineering25 => SkillType.MidnightEngineering,
            LockType.MidnightLeatherworking or LockType.MidnightLeatherworking25 => SkillType.MidnightLeatherworking,
            LockType.MidnightTailoring or LockType.MidnightTailoring25 => SkillType.MidnightTailoring,
            LockType.MidnightEnchanting25 => SkillType.MidnightEnchanting,
            LockType.MidnightInscription25 => SkillType.MidnightInscription,
            LockType.MidnightJewelcrafting25 => SkillType.MidnightJewelcrafting,
            LockType.MidnightCooking25 => SkillType.MidnightCooking,
            LockType.MidnightFishing25 => SkillType.MidnightFishing,
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
        Traveler = 15,
        Max = 16,

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
        EarthenDwarfHorde = 84, // Earthen (Horde) (Racemask Bit 17)
        EarthenDwarfAlliance = 85, // Earthen (Alliance) (Racemask Bit 18)
        HaranirAlliance = 86, // Haranir (Alliance) (RaceMask bit 20)
        //RACE_ROSTRUM_AIRSHIP_SEASON_1   = 87,
        //RACE_ROSTRUM_AIRSHIP_SEASON_2   = 90,
        HaranirHorde = 91, // Haranir (Horde) (RaceMask bit 19)
        //RACE_ROSTRUM_AIRSHIP_SEASON_3   = 92,
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
        Midnight = 11,
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

        Health = -2,    // (-2 as signed value)
        MaxPerClass = 10
    }

    public enum Stats
    {
        Strength = 0,
        Agility = 1,
        Stamina = 2,
        Intellect = 3,
        Spirit = 4,

        Max
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
        Ping = 0x43,
        EncounterEvent = 0x44,

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
        Step = 7,
    }

    public enum Difficulty : short
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

    [Flags]
    public enum MapFlags3
    {
        IsDelve = 0x00000100,
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

        Event = First,
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
        TrialRestricted = 9,
        BalanceNearCap = 9,
        NotEnoughPurchasedGameTime = 10,
        ThrottleTokens = 11
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
        WhoListUpdateInterval,
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
        StartDeathKnightPlayerMoney,
        StartDemonHunterPlayerMoney,
        StartEvokerPlayerMoney,
        StartAlliedRaceMoney,
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
        VoteToAbandonEncounter = 620,
        DungeonDifficultyFailed = 621,
        DungeonDifficultyChangedS = 622,
        TradeWrongRealm = 623,
        TradeNotOnTaplist = 624,
        ChatPlayerAmbiguousS = 625,
        LootCantLootThatNow = 626,
        LootMasterInvFull = 627,
        LootMasterUniqueItem = 628,
        LootMasterOther = 629,
        FilteringYouS = 630,
        UsePreventedByMechanicS = 631,
        ItemUniqueEquippable = 632,
        LfgLeaderIsLfmS = 633,
        LfgPending = 634,
        CantSpeakLangage = 635,
        VendorMissingTurnins = 636,
        BattlegroundNotInTeam = 637,
        NotInBattleground = 638,
        NotEnoughHonorPoints = 639,
        NotEnoughArenaPoints = 640,
        SocketingRequiresMetaGem = 641,
        SocketingMetaGemOnlyInMetaslot = 642,
        SocketingRequiresHydraulicGem = 643,
        SocketingHydraulicGemOnlyInHydraulicslot = 644,
        SocketingRequiresCogwheelGem = 645,
        SocketingCogwheelGemOnlyInCogwheelslot = 646,
        SocketingItemTooLowLevel = 647,
        SocketingItemTooHighLevel = 648,
        ItemMaxCountSocketed = 649,
        SystemDisabled = 650,
        QuestFailedTooManyDailyQuestsI = 651,
        ItemMaxCountEquippedSocketed = 652,
        ItemUniqueEquippableSocketed = 653,
        UserSquelched = 654,
        AccountSilenced = 655,
        PartyMemberSilenced = 656,
        PartyMemberSilencedLfgDelist = 657,
        TooMuchGold = 658,
        NotBarberSitting = 659,
        QuestFailedCais = 660,
        InviteRestrictedTrial = 661,
        VoiceIgnoreFull = 662,
        VoiceIgnoreSelf = 663,
        VoiceIgnoreNotFound = 664,
        VoiceIgnoreAlreadyS = 665,
        VoiceIgnoreAddedS = 666,
        VoiceIgnoreRemovedS = 667,
        VoiceIgnoreAmbiguous = 668,
        VoiceIgnoreDeleted = 669,
        UnknownMacroOptionS = 670,
        NotDuringArenaMatch = 671,
        NotInRatedBattleground = 672,
        PlayerSilenced = 673,
        PlayerUnsilenced = 674,
        ComsatDisconnect = 675,
        ComsatReconnectAttempt = 676,
        ComsatConnectFail = 677,
        MailInvalidAttachmentSlot = 678,
        MailTooManyAttachments = 679,
        MailInvalidAttachment = 680,
        MailAttachmentExpired = 681,
        VoiceChatParentalDisableMic = 682,
        ProfaneChatName = 683,
        PlayerSilencedEcho = 684,
        PlayerUnsilencedEcho = 685,
        LootCantLootThat = 686,
        ArenaExpiredCais = 687,
        GroupActionThrottled = 688,
        AlreadyPickpocketed = 689,
        NameInvalid = 690,
        NameNoName = 691,
        NameTooShort = 692,
        NameTooLong = 693,
        NameMixedLanguages = 694,
        NameProfane = 695,
        NameReserved = 696,
        NameThreeConsecutive = 697,
        NameInvalidSpace = 698,
        NameConsecutiveSpaces = 699,
        NameRussianConsecutiveSilentCharacters = 700,
        NameRussianSilentCharacterAtBeginningOrEnd = 701,
        NameDeclensionDoesntMatchBaseName = 702,
        RecruitAFriendNotLinked = 703,
        RecruitAFriendNotNow = 704,
        RecruitAFriendSummonLevelMax = 705,
        RecruitAFriendSummonCooldown = 706,
        RecruitAFriendSummonOffline = 707,
        RecruitAFriendInsufExpanLvl = 708,
        RecruitAFriendMapIncomingTransferNotAllowed = 709,
        NotSameAccount = 710,
        BadOnUseEnchant = 711,
        TradeSelf = 712,
        TooManySockets = 713,
        ItemMaxLimitCategoryCountExceededIs = 714,
        TradeTargetMaxLimitCategoryCountExceededIs = 715,
        ItemMaxLimitCategorySocketedExceededIs = 716,
        ItemMaxLimitCategoryEquippedExceededIs = 717,
        ShapeshiftFormCannotEquip = 718,
        ItemInventoryFullSatchel = 719,
        ScalingStatItemLevelExceeded = 720,
        ScalingStatItemLevelTooLow = 721,
        PurchaseLevelTooLow = 722,
        GroupSwapFailed = 723,
        InviteInCombat = 724,
        InvalidGlyphSlot = 725,
        GenericNoValidTargets = 726,
        CalendarEventAlertS = 727,
        PetLearnSpellS = 728,
        PetLearnAbilityS = 729,
        PetSpellUnlearnedS = 730,
        InviteUnknownRealm = 731,
        InviteNoPartyServer = 732,
        InvitePartyBusy = 733,
        InvitePartyBusyPendingRequest = 734,
        InvitePartyBusyPendingSuggest = 735,
        PartyTargetAmbiguous = 736,
        PartyLfgInviteRaidLocked = 737,
        PartyLfgBootLimit = 738,
        PartyLfgBootCooldownS = 739,
        PartyLfgBootNotEligibleS = 740,
        PartyLfgBootInpatientTimerS = 741,
        PartyLfgBootInProgress = 742,
        PartyLfgBootTooFewPlayers = 743,
        PartyLfgBootVoteSucceeded = 744,
        PartyLfgBootVoteFailed = 745,
        PartyLfgBootDisallowedByMap = 746,
        PartyLfgBootDungeonComplete = 747,
        PartyLfgBootLootRolls = 748,
        PartyLfgBootVoteRegistered = 749,
        PartyPrivateGroupOnly = 750,
        PartyLfgTeleportInCombat = 751,
        PartyTimeRunningSeasonIdMustMatch = 752,
        RaidDisallowedByLevel = 753,
        RaidDisallowedByCrossRealm = 754,
        PartyRoleNotAvailable = 755,
        JoinLfgObjectFailed = 756,
        LfgRemovedLevelup = 757,
        LfgRemovedXpToggle = 758,
        LfgRemovedFactionChange = 759,
        BattlegroundInfoThrottled = 760,
        BattlegroundAlreadyIn = 761,
        ArenaTeamChangeFailedQueued = 762,
        ArenaTeamPermissions = 763,
        NotWhileFalling = 764,
        NotWhileMoving = 765,
        NotWhileFatigued = 766,
        MaxSockets = 767,
        MultiCastActionTotemS = 768,
        BattlegroundJoinLevelup = 769,
        RemoveFromPvpQueueXpGain = 770,
        BattlegroundJoinXpGain = 771,
        BattlegroundJoinMercenary = 772,
        BattlegroundJoinTooManyHealers = 773,
        BattlegroundJoinRatedTooManyHealers = 774,
        BattlegroundJoinTooManyTanks = 775,
        BattlegroundJoinTooManyDamage = 776,
        RaidDifficultyFailed = 777,
        RaidDifficultyChangedS = 778,
        LegacyRaidDifficultyChangedS = 779,
        RaidLockoutChangedS = 780,
        RaidConvertedToParty = 781,
        PartyConvertedToRaid = 782,
        PlayerDifficultyChangedS = 783,
        GmresponseDbError = 784,
        BattlegroundJoinRangeIndex = 785,
        ArenaJoinRangeIndex = 786,
        RemoveFromPvpQueueFactionChange = 787,
        BattlegroundJoinFailed = 788,
        BattlegroundJoinNoValidSpecForRole = 789,
        BattlegroundJoinRespec = 790,
        BattlegroundInvitationDeclined = 791,
        BattlegroundInvitationDeclinedBy = 792,
        BattlegroundJoinTimedOut = 793,
        BattlegroundDupeQueue = 794,
        BattlegroundJoinMustCompleteQuest = 795,
        InBattlegroundRespec = 796,
        MailLimitedDurationItem = 797,
        YellRestrictedTrial = 798,
        ChatRaidRestrictedTrial = 799,
        LfgRoleCheckFailed = 800,
        LfgRoleCheckFailedTimeout = 801,
        LfgRoleCheckFailedNotViable = 802,
        LfgReadyCheckFailed = 803,
        LfgReadyCheckFailedTimeout = 804,
        LfgGroupFull = 805,
        LfgNoLfgObject = 806,
        LfgNoSlotsPlayer = 807,
        LfgNoSlotsParty = 808,
        LfgNoSpec = 809,
        LfgMismatchedSlots = 810,
        LfgMismatchedSlotsLocalXrealm = 811,
        LfgPartyPlayersFromDifferentRealms = 812,
        LfgMembersNotPresent = 813,
        LfgGetInfoTimeout = 814,
        LfgInvalidSlot = 815,
        LfgDeserterPlayer = 816,
        LfgDeserterParty = 817,
        LfgDead = 818,
        LfgRandomCooldownPlayer = 819,
        LfgRandomCooldownParty = 820,
        LfgTooManyMembers = 821,
        LfgTooFewMembers = 822,
        LfgProposalFailed = 823,
        LfgProposalDeclinedSelf = 824,
        LfgProposalDeclinedParty = 825,
        LfgNoSlotsSelected = 826,
        LfgNoRolesSelected = 827,
        LfgRoleCheckInitiated = 828,
        LfgReadyCheckInitiated = 829,
        LfgPlayerDeclinedRoleCheck = 830,
        LfgPlayerDeclinedReadyCheck = 831,
        LfgLorewalking = 832,
        LfgJoinedQueue = 833,
        LfgJoinedFlexQueue = 834,
        LfgJoinedRfQueue = 835,
        LfgJoinedScenarioQueue = 836,
        LfgJoinedWorldPvpQueue = 837,
        LfgJoinedBattlefieldQueue = 838,
        LfgJoinedList = 839,
        QueuedPlunderstorm = 840,
        LfgLeftQueue = 841,
        LfgLeftList = 842,
        LfgRoleCheckAborted = 843,
        LfgReadyCheckAborted = 844,
        LfgCantUseBattleground = 845,
        LfgCantUseDungeons = 846,
        LfgReasonTooManyLfg = 847,
        LfgFarmLimit = 848,
        LfgNoCrossFactionParties = 849,
        InvalidTeleportLocation = 850,
        TooFarToInteract = 851,
        BattlegroundPlayersFromDifferentRealms = 852,
        DifficultyChangeCooldownS = 853,
        DifficultyChangeCombatCooldownS = 854,
        DifficultyChangeWorldstate = 855,
        DifficultyChangeEncounter = 856,
        DifficultyChangeCombat = 857,
        DifficultyChangePlayerBusy = 858,
        DifficultyChangePlayerOnVehicle = 859,
        DifficultyChangeAlreadyStarted = 860,
        DifficultyChangeOtherHeroicS = 861,
        DifficultyChangeHeroicInstanceAlreadyRunning = 862,
        ArenaTeamPartySize = 863,
        SoloShuffleWargameGroupSize = 864,
        SoloShuffleWargameGroupComp = 865,
        SoloRbgWargameGroupSize = 866,
        SoloRbgWargameGroupComp = 867,
        SoloMinItemLevel = 868,
        PvpPlayerAbandoned = 869,
        BattlegroundJoinGroupQueueWithoutHealer = 870,
        QuestForceRemovedS = 871,
        AttackNoActions = 872,
        InRandomBg = 873,
        InNonRandomBg = 874,
        BnFriendSelf = 875,
        BnFriendAlready = 876,
        BnFriendBlocked = 877,
        BnFriendListFull = 878,
        BnFriendRequestSent = 879,
        BnBroadcastThrottle = 880,
        BgDeveloperOnly = 881,
        CurrencySpellSlotMismatch = 882,
        CurrencyNotTradable = 883,
        RequiresExpansionS = 884,
        QuestFailedSpell = 885,
        TalentFailedUnspentTalentPoints = 886,
        TalentFailedNotEnoughTalentsInPrimaryTree = 887,
        TalentFailedNoPrimaryTreeSelected = 888,
        TalentFailedCantRemoveTalent = 889,
        TalentFailedUnknown = 890,
        TalentFailedInCombat = 891,
        TalentFailedInPvpMatch = 892,
        TalentFailedInMythicPlus = 893,
        WargameRequestFailure = 894,
        RankRequiresAuthenticator = 895,
        GuildBankVoucherFailed = 896,
        WargameRequestSent = 897,
        RequiresAchievementI = 898,
        RefundResultExceedMaxCurrency = 899,
        CantBuyQuantity = 900,
        ItemIsBattlePayLocked = 901,
        PartyAlreadyInBattlegroundQueue = 902,
        PartyConfirmingBattlegroundQueue = 903,
        BattlefieldTeamPartySize = 904,
        InsuffTrackedCurrencyIs = 905,
        NotOnTournamentRealm = 906,
        GuildTrialAccountTrial = 907,
        GuildTrialAccountVeteran = 908,
        GuildUndeletableDueToLevel = 909,
        CantDoThatInAGroup = 910,
        GuildLeaderReplaced = 911,
        TransmogrifyCantEquip = 912,
        TransmogrifyInvalidItemType = 913,
        TransmogrifyNotSoulbound = 914,
        TransmogrifyInvalidSource = 915,
        TransmogrifyInvalidDestination = 916,
        TransmogrifyMismatch = 917,
        TransmogrifyLegendary = 918,
        TransmogrifySameItem = 919,
        TransmogrifySameAppearance = 920,
        TransmogrifyNotEquipped = 921,
        TransmogInvalidActionTrialOfStyle = 922,
        VoidDepositFull = 923,
        VoidWithdrawFull = 924,
        VoidStorageWrapped = 925,
        VoidStorageStackable = 926,
        VoidStorageUnbound = 927,
        VoidStorageRepair = 928,
        VoidStorageCharges = 929,
        VoidStorageQuest = 930,
        VoidStorageConjured = 931,
        VoidStorageMail = 932,
        VoidStorageBag = 933,
        VoidTransferStorageFull = 934,
        VoidTransferInvFull = 935,
        VoidTransferInternalError = 936,
        VoidTransferItemInvalid = 937,
        DifficultyDisabledInLfg = 938,
        VoidStorageUnique = 939,
        VoidStorageLoot = 940,
        VoidStorageHoliday = 941,
        VoidStorageDuration = 942,
        VoidStorageLoadFailed = 943,
        VoidStorageInvalidItem = 944,
        VoidStorageAccountItem = 945,
        ParentalControlsChatMuted = 946,
        SorStartExperienceIncomplete = 947,
        SorInvalidEmail = 948,
        SorInvalidComment = 949,
        ChallengeModeResetCooldownS = 950,
        ChallengeModeResetKeystone = 951,
        PetJournalAlreadyInLoadout = 952,
        ReportSubmittedSuccessfully = 953,
        ReportSubmissionFailed = 954,
        SuggestionSubmittedSuccessfully = 955,
        BugSubmittedSuccessfully = 956,
        ChallengeModeEnabled = 957,
        ChallengeModeDisabled = 958,
        PetbattleCreateFailed = 959,
        PetbattleNotHere = 960,
        PetbattleNotHereOnTransport = 961,
        PetbattleNotHereUnevenGround = 962,
        PetbattleNotHereObstructed = 963,
        PetbattleNotWhileInCombat = 964,
        PetbattleNotWhileDead = 965,
        PetbattleNotWhileFlying = 966,
        PetbattleTargetInvalid = 967,
        PetbattleTargetOutOfRange = 968,
        PetbattleTargetNotCapturable = 969,
        PetbattleNotATrainer = 970,
        PetbattleDeclined = 971,
        PetbattleInBattle = 972,
        PetbattleInvalidLoadout = 973,
        PetbattleAllPetsDead = 974,
        PetbattleNoPetsInSlots = 975,
        PetbattleNoAccountLock = 976,
        PetbattleWildPetTapped = 977,
        PetbattleRestrictedAccount = 978,
        PetbattleOpponentNotAvailable = 979,
        PetbattleNotWhileInMatchedBattle = 980,
        CantHaveMorePetsOfThatType = 981,
        CantHaveMorePets = 982,
        PvpMapNotFound = 983,
        PvpMapNotSet = 984,
        PetbattleQueueQueued = 985,
        PetbattleQueueAlreadyQueued = 986,
        PetbattleQueueJoinFailed = 987,
        PetbattleQueueJournalLock = 988,
        PetbattleQueueRemoved = 989,
        PetbattleQueueProposalDeclined = 990,
        PetbattleQueueProposalTimeout = 991,
        PetbattleQueueOpponentDeclined = 992,
        PetbattleQueueRequeuedInternal = 993,
        PetbattleQueueRequeuedRemoved = 994,
        PetbattleQueueSlotLocked = 995,
        PetbattleQueueSlotEmpty = 996,
        PetbattleQueueSlotNoTracker = 997,
        PetbattleQueueSlotNoSpecies = 998,
        PetbattleQueueSlotCantBattle = 999,
        PetbattleQueueSlotRevoked = 1000,
        PetbattleQueueSlotDead = 1001,
        PetbattleQueueSlotNoPet = 1002,
        PetbattleQueueNotWhileNeutral = 1003,
        PetbattleGameTimeLimitWarning = 1004,
        PetbattleGameRoundsLimitWarning = 1005,
        HasRestriction = 1006,
        ItemUpgradeItemTooLowLevel = 1007,
        ItemUpgradeNoPath = 1008,
        ItemUpgradeNoMoreUpgrades = 1009,
        BonusRollEmpty = 1010,
        ChallengeModeFull = 1011,
        ChallengeModeInProgress = 1012,
        ChallengeModeIncorrectKeystone = 1013,
        StartRestrictedChallengeMode = 1014,
        BattletagFriendNotFound = 1015,
        BattletagFriendNotValid = 1016,
        BattletagFriendNotAllowed = 1017,
        BattletagFriendThrottled = 1018,
        BattletagFriendSuccess = 1019,
        PetTooHighLevelToUncage = 1020,
        PetbattleInternal = 1021,
        CantCagePetYet = 1022,
        NoLootInChallengeMode = 1023,
        QuestPetBattleVictoriesPvpIi = 1024,
        RoleCheckAlreadyInProgress = 1025,
        RecruitAFriendAccountLimit = 1026,
        RecruitAFriendFailed = 1027,
        SetLootPersonal = 1028,
        SetLootMethodFailedCombat = 1029,
        ReagentBankFull = 1030,
        ReagentBankLocked = 1031,
        GarrisonBuildingExists = 1032,
        GarrisonInvalidPlot = 1033,
        GarrisonInvalidBuildingid = 1034,
        GarrisonInvalidPlotBuilding = 1035,
        GarrisonRequiresBlueprint = 1036,
        GarrisonNotEnoughCurrency = 1037,
        GarrisonNotEnoughGold = 1038,
        GarrisonCompleteMissionWrongFollowerType = 1039,
        AlreadyUsingLfgList = 1040,
        RestrictedAccountLfgListTrial = 1041,
        ToyUseLimitReached = 1042,
        ToyAlreadyKnown = 1043,
        TransmogSetAlreadyKnown = 1044,
        NotEnoughCurrency = 1045,
        SpecIsDisabled = 1046,
        FeatureRestrictedTrial = 1047,
        CantBeObliterated = 1048,
        CantBeScrapped = 1049,
        CantBeRecrafted = 1050,
        ArtifactRelicDoesNotMatchArtifact = 1051,
        MustEquipArtifact = 1052,
        CantDoThatRightNow = 1053,
        AffectingCombat = 1054,
        EquipmentManagerCombatSwapS = 1055,
        EquipmentManagerBagsFull = 1056,
        EquipmentManagerMissingItemS = 1057,
        MovieRecordingWarningPerf = 1058,
        MovieRecordingWarningDiskFull = 1059,
        MovieRecordingWarningNoMovie = 1060,
        MovieRecordingWarningRequirements = 1061,
        MovieRecordingWarningCompressing = 1062,
        NoChallengeModeReward = 1063,
        ClaimedChallengeModeReward = 1064,
        ChallengeModePeriodResetSs = 1065,
        CantDoThatChallengeModeActive = 1066,
        TalentFailedRestArea = 1067,
        CannotAbandonLastPet = 1068,
        TestCvarSetSss = 1069,
        QuestTurnInFailReason = 1070,
        ClaimedChallengeModeRewardOld = 1071,
        TalentGrantedByAura = 1072,
        ChallengeModeAlreadyComplete = 1073,
        GlyphTargetNotAvailable = 1074,
        PvpWarmodeToggleOn = 1075,
        PvpWarmodeToggleOff = 1076,
        SpellFailedLevelRequirement = 1077,
        SpellFailedCantFlyHere = 1078,
        BattlegroundJoinRequiresLevel = 1079,
        BattlegroundJoinDisqualified = 1080,
        BattlegroundJoinDisqualifiedNoName = 1081,
        VoiceChatGenericUnableToConnect = 1082,
        VoiceChatServiceLost = 1083,
        VoiceChatChannelNameTooShort = 1084,
        VoiceChatChannelNameTooLong = 1085,
        VoiceChatChannelAlreadyExists = 1086,
        VoiceChatTargetNotFound = 1087,
        VoiceChatTooManyRequests = 1088,
        VoiceChatPlayerSilenced = 1089,
        VoiceChatParentalDisableAll = 1090,
        VoiceChatDisabled = 1091,
        NoPvpReward = 1092,
        ClaimedPvpReward = 1093,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1094,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1095,
        AzeriteEssenceSelectionFailedConditionFailed = 1096,
        AzeriteEssenceSelectionFailedRestArea = 1097,
        AzeriteEssenceSelectionFailedSlotLocked = 1098,
        AzeriteEssenceSelectionFailedNotAtForge = 1099,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1100,
        AzeriteEssenceSelectionFailedNotEquipped = 1101,
        SocketingGenericFailure = 1102,
        SocketingRequiresPunchcardredGem = 1103,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1104,
        SocketingRequiresPunchcardyellowGem = 1105,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1106,
        SocketingRequiresPunchcardblueGem = 1107,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1108,
        SocketingRequiresDominationShard = 1109,
        SocketingDominationShardOnlyInDominationslot = 1110,
        SocketingRequiresCypherGem = 1111,
        SocketingCypherGemOnlyInCypherslot = 1112,
        SocketingRequiresTinkerGem = 1113,
        SocketingTinkerGemOnlyInTinkerslot = 1114,
        SocketingRequiresPrimordialGem = 1115,
        SocketingPrimordialGemOnlyInPrimordialslot = 1116,
        SocketingRequiresFragranceGem = 1117,
        SocketingFragranceGemOnlyInFragranceslot = 1118,
        SocketingRequiresSingingThunderGem = 1119,
        SocketingSingingthunderGemOnlyInSingingthunderslot = 1120,
        SocketingRequiresSingingSeaGem = 1121,
        SocketingSingingseaGemOnlyInSingingseaslot = 1122,
        SocketingRequiresSingingWindGem = 1123,
        SocketingSingingwindGemOnlyInSingingwindslot = 1124,
        SocketingRequiresFiberGem = 1125,
        SocketingFiberGemOnlyInFiberslot = 1126,
        LevelLinkingResultLinked = 1127,
        LevelLinkingResultUnlinked = 1128,
        ClubFinderErrorPostClub = 1129,
        ClubFinderErrorApplyClub = 1130,
        ClubFinderErrorRespondApplicant = 1131,
        ClubFinderErrorCancelApplication = 1132,
        ClubFinderErrorTypeAcceptApplication = 1133,
        ClubFinderErrorTypeNoInvitePermissions = 1134,
        ClubFinderErrorTypeNoPostingPermissions = 1135,
        ClubFinderErrorTypeApplicantList = 1136,
        ClubFinderErrorTypeApplicantListNoPerm = 1137,
        ClubFinderErrorTypeFinderNotAvailable = 1138,
        ClubFinderErrorTypeGetPostingIds = 1139,
        ClubFinderErrorTypeJoinApplication = 1140,
        ClubFinderErrorTypeRealmNotEligible = 1141,
        ClubFinderErrorTypeFlaggedRename = 1142,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1143,
        ItemInteractionNotEnoughGold = 1144,
        ItemInteractionNotEnoughCurrency = 1145,
        ItemInteractionNoConversionOutput = 1146,
        PlayerChoiceErrorPendingChoice = 1147,
        SoulbindInvalidConduit = 1148,
        SoulbindInvalidConduitItem = 1149,
        SoulbindInvalidTalent = 1150,
        SoulbindDuplicateConduit = 1151,
        ActivateSoulbindS = 1152,
        ActivateSoulbindFailedRestArea = 1153,
        CantUseProfanity = 1154,
        NotInPetBattle = 1155,
        NotInNpe = 1156,
        NoSpec = 1157,
        NoDominationshardOverwrite = 1158,
        UseWeeklyRewardsDisabled = 1159,
        CrossFactionGroupJoined = 1160,
        CantTargetUnfriendlyInOverworld = 1161,
        EquipablespellsSlotsFull = 1162,
        ItemModAppearanceGroupAlreadyKnown = 1163,
        CantBulkSellItemWithRefund = 1164,
        NoSoulboundItemInAccountBank = 1165,
        NoRefundableItemInAccountBank = 1166,
        CantDeleteInAccountBank = 1167,
        NoImmediateContainerInAccountBank = 1168,
        NoOpenImmediateContainerInAccountBank = 1169,
        CantTradeAccountItem = 1170,
        NoAccountInventoryLock = 1171,
        BankNotAccessible = 1172,
        TooManyAccountBankTabs = 1173,
        BankTabNotUnlocked = 1174,
        AccountMoneyLocked = 1175,
        BankTabInvalidName = 1176,
        BankTabInvalidText = 1177,
        CharacterBankNotConverted = 1178,
        NoSalvagedItemsInAccountBank = 1179,
        WowLabsPartyErrorTypePartyIsFull = 1180,
        WowLabsPartyErrorTypeMaxInviteSent = 1181,
        WowLabsPartyErrorTypePlayerAlreadyInvited = 1182,
        WowLabsPartyErrorTypePartyInviteInvalid = 1183,
        WowLabsLobbyMatchmakerErrorEnterQueueFailed = 1184,
        WowLabsLobbyMatchmakerErrorLeaveQueueFailed = 1185,
        WowLabsSetWowLabsAreaIdFailed = 1186,
        PlunderstormCannotQueue = 1187,
        TargetIsSelfFoundCannotTrade = 1188,
        PlayerIsSelfFoundCannotTrade = 1189,
        MailRecepientIsSelfFoundCannotReceiveMail = 1190,
        PlayerIsSelfFoundCannotSendMail = 1191,
        PlayerIsSelfFoundCannotUseAuctionHouse = 1192,
        MailTargetCannotReceiveMail = 1193,
        RemixInvalidTransferRequest = 1194,
        FollowerDungeonInvalidTransferRequest = 1195,
        CurrencyTransferInvalidCharacter = 1196,
        CurrencyTransferInvalidCurrency = 1197,
        CurrencyTransferInsufficientCurrency = 1198,
        CurrencyTransferMaxQuantity = 1199,
        CurrencyTransferNoValidSource = 1200,
        CurrencyTransferCharacterLoggedIn = 1201,
        CurrencyTransferServerError = 1202,
        CurrencyTransferUnmetRequirements = 1203,
        CurrencyTransferTransactionInProgress = 1204,
        CurrencyTransferDisabled = 1205,
        NoOwnedHouseInThisNeighborhoodMap = 1206,
        HousingResultNeighborhoodNotFound = 1207,
        InvitedToNeighborhood = 1208,
        NeighborhoodOwnerTransferredS = 1209,
        NotWhileHouseEdit = 1210,
        NotWhileHouseInspectMode = 1211,
        NewPartyNeighborhoodReservation = 1212,
        HouseMoved = 1213,
        CharterSignatureRequestSent = 1214,
        CharterSignatureReceived = 1215,
        CharterSignatureRequestFailedMissingExpansion = 1216,
        CharterSignatureRequestFailedDuplicateSignature = 1217,
        CharterSignatureRequestFailedGeneric = 1218,
        CharterSystemRequestFailedGeneric = 1219,
        HousingActionUnavailable = 1220,
        HousingExteriorFailsafeReset = 1221,
        HousingResultMissingExpansionAccess = 1222,
        HousingResultPermissionDenied = 1223,
        HousingActionNoentry = 1224,
        GuildNeighborhoodBuiltHouseS = 1225,
        GuildNeighborhoodSoldHouseS = 1226,
        GuildNeighborhoodNewSubdivision = 1227,
        GuildNeighborhoodRenameS = 1228,
        CharterNeighborhoodRename = 1229,
        CharterSignatureRemoved = 1230,
        EndeavorRewardAvailable = 1231,
        HousingResultCosmeticOwnerNotInGuild = 1232,
        HousingResultPlotNotVacant = 1233,
        HousingResultPlotReserved = 1234,
        HousingResultMissingPrivateNeighborhoodInvite = 1235,
        CharterNeighborhoodOwnershipTransferSuccess = 1236,
        CharterNeighborhoodRenameNotificationSs = 1237,
        RecentAllyPinServerError = 1238,
        PvpTrainingGroundsDisabled = 1239,
        SoloJoinTrainingGround = 1240,
        LfgJoinedTrainingGroundsQueue = 1241,
        RestrictedAccountLfgListClassTrial = 1242,
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

    [Flags]
    public enum WMOAreaTableFlags
    {
        RenderMinimap = 0x01,
        ForceIndoors = 0x02,
        ForceOutdoors = 0x04,
        GenerateSingleExteriorMap = 0x08,
        Stormwind = 0x10,
        ChunkUsesTerrainForMinimap = 0x20,
        IgnoreForMinimapAndEffects = 0x40,
        IgnoreFatigue = 0x80
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
