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
        GuildBankWarbandsBankSource = 165,
        GuildBankRealmMismatch = 166,
        GuildNewLeaderWrongRealm = 167,
        NoGuildCharter = 168,
        OutOfRange = 169,
        PlayerDead = 170,
        ClientLockedOut = 171,
        ClientOnTransport = 172,
        KilledByS = 173,
        LootLocked = 174,
        LootTooFar = 175,
        LootDidntKill = 176,
        LootBadFacing = 177,
        LootNotstanding = 178,
        LootStunned = 179,
        LootNoUi = 180,
        LootWhileInvulnerable = 181,
        NoLoot = 182,
        QuestAcceptedS = 183,
        QuestCompleteS = 184,
        QuestFailedS = 185,
        QuestFailedBagFullS = 186,
        QuestFailedMaxCountS = 187,
        QuestFailedLowLevel = 188,
        QuestFailedMissingItems = 189,
        QuestFailedWrongRace = 190,
        QuestFailedNotEnoughMoney = 191,
        QuestFailedExpansion = 192,
        QuestOnlyOneTimed = 193,
        QuestNeedPrereqs = 194,
        QuestNeedPrereqsCustom = 195,
        QuestAlreadyOn = 196,
        QuestAlreadyDone = 197,
        QuestAlreadyDoneDaily = 198,
        QuestHasInProgress = 199,
        QuestRewardExpI = 200,
        QuestRewardMoneyS = 201,
        QuestMustChoose = 202,
        QuestLogFull = 203,
        CombatDamageSsi = 204,
        InspectS = 205,
        CantUseItem = 206,
        CantUseItemInArena = 207,
        CantUseItemInRatedBattleground = 208,
        MustEquipItem = 209,
        PassiveAbility = 210,
        H2SkillNotFound = 211,
        NoAttackTarget = 212,
        InvalidAttackTarget = 213,
        AttackPvpTargetWhileUnflagged = 214,
        AttackStunned = 215,
        AttackPacified = 216,
        AttackMounted = 217,
        AttackFleeing = 218,
        AttackConfused = 219,
        AttackCharmed = 220,
        AttackDead = 221,
        AttackPreventedByMechanicS = 222,
        AttackChannel = 223,
        Taxisamenode = 224,
        Taxinosuchpath = 225,
        Taxiunspecifiedservererror = 226,
        Taxinotenoughmoney = 227,
        Taxitoofaraway = 228,
        Taxinovendornearby = 229,
        Taxinotvisited = 230,
        Taxiplayerbusy = 231,
        Taxiplayeralreadymounted = 232,
        Taxiplayershapeshifted = 233,
        Taxiplayermoving = 234,
        Taxinopaths = 235,
        Taxinoteligible = 236,
        Taxinotstanding = 237,
        Taxiincombat = 238,
        NoReplyTarget = 239,
        GenericNoTarget = 240,
        InitiateTradeS = 241,
        TradeRequestS = 242,
        TradeBlockedS = 243,
        TradeTargetDead = 244,
        TradeTooFar = 245,
        TradeCancelled = 246,
        TradeComplete = 247,
        TradeBagFull = 248,
        TradeTargetBagFull = 249,
        TradeMaxCountExceeded = 250,
        TradeTargetMaxCountExceeded = 251,
        InventoryTradeTooManyUniqueItem = 252,
        AlreadyTrading = 253,
        MountInvalidmountee = 254,
        MountToofaraway = 255,
        MountAlreadymounted = 256,
        MountNotmountable = 257,
        MountNotyourpet = 258,
        MountOther = 259,
        MountLooting = 260,
        MountRacecantmount = 261,
        MountShapeshifted = 262,
        MountNoFavorites = 263,
        MountNoMounts = 264,
        DismountNopet = 265,
        DismountNotmounted = 266,
        DismountNotyourpet = 267,
        SpellFailedTotems = 268,
        SpellFailedReagents = 269,
        SpellFailedReagentsGeneric = 270,
        SpellFailedOptionalReagents = 271,
        CantTradeGold = 272,
        SpellFailedEquippedItem = 273,
        SpellFailedEquippedItemClassS = 274,
        SpellFailedShapeshiftFormS = 275,
        SpellFailedAnotherInProgress = 276,
        Badattackfacing = 277,
        Badattackpos = 278,
        ChestInUse = 279,
        UseCantOpen = 280,
        UseLocked = 281,
        DoorLocked = 282,
        ButtonLocked = 283,
        UseLockedWithItemS = 284,
        UseLockedWithSpellS = 285,
        UseLockedWithSpellKnownSi = 286,
        UseTooFar = 287,
        UseBadAngle = 288,
        UseObjectMoving = 289,
        UseSpellFocus = 290,
        UseDestroyed = 291,
        SetLootFreeforall = 292,
        SetLootRoundrobin = 293,
        SetLootMaster = 294,
        SetLootGroup = 295,
        SetLootThresholdS = 296,
        NewLootMasterS = 297,
        SpecifyMasterLooter = 298,
        LootSpecChangedS = 299,
        TameFailed = 300,
        ChatWhileDead = 301,
        ChatPlayerNotFoundS = 302,
        Newtaxipath = 303,
        NoPet = 304,
        Notyourpet = 305,
        PetNotRenameable = 306,
        QuestObjectiveCompleteS = 307,
        QuestUnknownComplete = 308,
        QuestAddKillSii = 309,
        QuestAddFoundSii = 310,
        QuestAddItemSii = 311,
        QuestAddPlayerKillSii = 312,
        Cannotcreatedirectory = 313,
        Cannotcreatefile = 314,
        PlayerWrongFaction = 315,
        PlayerIsNeutral = 316,
        BankslotFailedTooMany = 317,
        BankslotInsufficientFunds = 318,
        BankslotNotbanker = 319,
        FriendDbError = 320,
        FriendListFull = 321,
        FriendAddedS = 322,
        BattletagFriendAddedS = 323,
        FriendOnlineSs = 324,
        FriendOfflineS = 325,
        FriendNotFound = 326,
        FriendWrongFaction = 327,
        FriendRemovedS = 328,
        BattletagFriendRemovedS = 329,
        FriendError = 330,
        FriendAlreadyS = 331,
        FriendSelf = 332,
        FriendDeleted = 333,
        IgnoreFull = 334,
        IgnoreSelf = 335,
        IgnoreNotFound = 336,
        IgnoreAlreadyS = 337,
        IgnoreAddedS = 338,
        IgnoreRemovedS = 339,
        IgnoreAmbiguous = 340,
        IgnoreDeleted = 341,
        OnlyOneBolt = 342,
        OnlyOneAmmo = 343,
        SpellFailedEquippedSpecificItem = 344,
        WrongBagTypeSubclass = 345,
        CantWrapStackable = 346,
        CantWrapEquipped = 347,
        CantWrapWrapped = 348,
        CantWrapBound = 349,
        CantWrapUnique = 350,
        CantWrapBags = 351,
        OutOfMana = 352,
        OutOfRage = 353,
        OutOfFocus = 354,
        OutOfEnergy = 355,
        OutOfChi = 356,
        OutOfHealth = 357,
        OutOfRunes = 358,
        OutOfRunicPower = 359,
        OutOfSoulShards = 360,
        OutOfLunarPower = 361,
        OutOfHolyPower = 362,
        OutOfMaelstrom = 363,
        OutOfComboPoints = 364,
        OutOfInsanity = 365,
        OutOfEssence = 366,
        OutOfArcaneCharges = 367,
        OutOfFury = 368,
        OutOfPain = 369,
        OutOfPowerDisplay = 370,
        LootGone = 371,
        MountForceddismount = 372,
        AutofollowTooFar = 373,
        UnitNotFound = 374,
        InvalidFollowTarget = 375,
        InvalidFollowPvpCombat = 376,
        InvalidFollowTargetPvpCombat = 377,
        InvalidInspectTarget = 378,
        GuildemblemSuccess = 379,
        GuildemblemInvalidTabardColors = 380,
        GuildemblemNoguild = 381,
        GuildemblemNotguildmaster = 382,
        GuildemblemNotenoughmoney = 383,
        GuildemblemInvalidvendor = 384,
        EmblemerrorNotabardgeoset = 385,
        SpellOutOfRange = 386,
        CommandNeedsTarget = 387,
        NoammoS = 388,
        Toobusytofollow = 389,
        DuelRequested = 390,
        DuelCancelled = 391,
        Deathbindalreadybound = 392,
        DeathbindSuccessS = 393,
        Noemotewhilerunning = 394,
        ZoneExplored = 395,
        ZoneExploredXp = 396,
        InvalidItemTarget = 397,
        InvalidQuestTarget = 398,
        IgnoringYouS = 399,
        FishNotHooked = 400,
        FishEscaped = 401,
        SpellFailedNotunsheathed = 402,
        PetitionOfferedS = 403,
        PetitionSigned = 404,
        PetitionSignedS = 405,
        PetitionDeclinedS = 406,
        PetitionAlreadySigned = 407,
        PetitionRestrictedAccountTrial = 408,
        PetitionAlreadySignedOther = 409,
        PetitionInGuild = 410,
        PetitionCreator = 411,
        PetitionNotEnoughSignatures = 412,
        PetitionNotSameServer = 413,
        PetitionFull = 414,
        PetitionAlreadySignedByS = 415,
        GuildNameInvalid = 416,
        SpellUnlearnedS = 417,
        PetSpellRooted = 418,
        PetSpellAffectingCombat = 419,
        PetSpellOutOfRange = 420,
        PetSpellNotBehind = 421,
        PetSpellTargetsDead = 422,
        PetSpellDead = 423,
        PetSpellNopath = 424,
        ItemCantBeDestroyed = 425,
        TicketAlreadyExists = 426,
        TicketCreateError = 427,
        TicketUpdateError = 428,
        TicketDbError = 429,
        TicketNoText = 430,
        TicketTextTooLong = 431,
        ObjectIsBusy = 432,
        ExhaustionWellrested = 433,
        ExhaustionRested = 434,
        ExhaustionNormal = 435,
        ExhaustionTired = 436,
        ExhaustionExhausted = 437,
        NoItemsWhileShapeshifted = 438,
        CantInteractShapeshifted = 439,
        RealmNotFound = 440,
        MailQuestItem = 441,
        MailBoundItem = 442,
        MailConjuredItem = 443,
        MailBag = 444,
        MailToSelf = 445,
        MailTargetNotFound = 446,
        MailDatabaseError = 447,
        MailDeleteItemError = 448,
        MailWrappedCod = 449,
        MailCantSendRealm = 450,
        MailTempReturnOutage = 451,
        MailRecepientCantReceiveMail = 452,
        MailSent = 453,
        MailTargetIsTrial = 454,
        NotHappyEnough = 455,
        UseCantImmune = 456,
        CantBeDisenchanted = 457,
        CantUseDisarmed = 458,
        AuctionDatabaseError = 459,
        AuctionHigherBid = 460,
        AuctionAlreadyBid = 461,
        AuctionOutbidS = 462,
        AuctionWonS = 463,
        AuctionRemovedS = 464,
        AuctionBidPlaced = 465,
        LogoutFailed = 466,
        QuestPushSuccessS = 467,
        QuestPushInvalidS = 468,
        QuestPushInvalidToRecipientS = 469,
        QuestPushAcceptedS = 470,
        QuestPushDeclinedS = 471,
        QuestPushBusyS = 472,
        QuestPushDeadS = 473,
        QuestPushDeadToRecipientS = 474,
        QuestPushLogFullS = 475,
        QuestPushLogFullToRecipientS = 476,
        QuestPushOnquestS = 477,
        QuestPushOnquestToRecipientS = 478,
        QuestPushAlreadyDoneS = 479,
        QuestPushAlreadyDoneToRecipientS = 480,
        QuestPushNotDailyS = 481,
        QuestPushTimerExpiredS = 482,
        QuestPushNotInPartyS = 483,
        QuestPushDifferentServerDailyS = 484,
        QuestPushDifferentServerDailyToRecipientS = 485,
        QuestPushNotAllowedS = 486,
        QuestPushPrerequisiteS = 487,
        QuestPushPrerequisiteToRecipientS = 488,
        QuestPushLowLevelS = 489,
        QuestPushLowLevelToRecipientS = 490,
        QuestPushHighLevelS = 491,
        QuestPushHighLevelToRecipientS = 492,
        QuestPushClassS = 493,
        QuestPushClassToRecipientS = 494,
        QuestPushRaceS = 495,
        QuestPushRaceToRecipientS = 496,
        QuestPushLowFactionS = 497,
        QuestPushLowFactionToRecipientS = 498,
        QuestPushHighFactionS = 499,
        QuestPushHighFactionToRecipientS = 500,
        QuestPushExpansionS = 501,
        QuestPushExpansionToRecipientS = 502,
        QuestPushNotGarrisonOwnerS = 503,
        QuestPushNotGarrisonOwnerToRecipientS = 504,
        QuestPushWrongCovenantS = 505,
        QuestPushWrongCovenantToRecipientS = 506,
        QuestPushNewPlayerExperienceS = 507,
        QuestPushNewPlayerExperienceToRecipientS = 508,
        QuestPushWrongFactionS = 509,
        QuestPushWrongFactionToRecipientS = 510,
        QuestPushCrossFactionRestrictedS = 511,
        RaidGroupLowlevel = 512,
        RaidGroupOnly = 513,
        RaidGroupFull = 514,
        RaidGroupRequirementsUnmatch = 515,
        CorpseIsNotInInstance = 516,
        PvpKillHonorable = 517,
        PvpKillDishonorable = 518,
        SpellFailedAlreadyAtFullHealth = 519,
        SpellFailedAlreadyAtFullMana = 520,
        SpellFailedAlreadyAtFullPowerS = 521,
        AutolootMoneyS = 522,
        GenericStunned = 523,
        GenericThrottle = 524,
        ClubFinderSearchingTooFast = 525,
        TargetStunned = 526,
        MustRepairDurability = 527,
        RaidYouJoined = 528,
        RaidYouLeft = 529,
        InstanceGroupJoinedWithParty = 530,
        InstanceGroupJoinedWithRaid = 531,
        RaidMemberAddedS = 532,
        RaidMemberRemovedS = 533,
        InstanceGroupAddedS = 534,
        InstanceGroupRemovedS = 535,
        ClickOnItemToFeed = 536,
        TooManyChatChannels = 537,
        LootRollPending = 538,
        LootPlayerNotFound = 539,
        NotInRaid = 540,
        LoggingOut = 541,
        TargetLoggingOut = 542,
        NotWhileMounted = 543,
        NotWhileShapeshifted = 544,
        NotInCombat = 545,
        NotWhileDisarmed = 546,
        PetBroken = 547,
        TalentWipeError = 548,
        SpecWipeError = 549,
        GlyphWipeError = 550,
        PetSpecWipeError = 551,
        FeignDeathResisted = 552,
        MeetingStoneInQueueS = 553,
        MeetingStoneLeftQueueS = 554,
        MeetingStoneOtherMemberLeft = 555,
        MeetingStonePartyKickedFromQueue = 556,
        MeetingStoneMemberStillInQueue = 557,
        MeetingStoneSuccess = 558,
        MeetingStoneInProgress = 559,
        MeetingStoneMemberAddedS = 560,
        MeetingStoneGroupFull = 561,
        MeetingStoneNotLeader = 562,
        MeetingStoneInvalidLevel = 563,
        MeetingStoneTargetNotInParty = 564,
        MeetingStoneTargetInvalidLevel = 565,
        MeetingStoneMustBeLeader = 566,
        MeetingStoneNoRaidGroup = 567,
        MeetingStoneNeedParty = 568,
        MeetingStoneNotFound = 569,
        MeetingStoneTargetInVehicle = 570,
        GuildemblemSame = 571,
        EquipTradeItem = 572,
        PvpToggleOn = 573,
        PvpToggleOff = 574,
        GroupJoinBattlegroundDeserters = 575,
        GroupJoinBattlegroundDead = 576,
        GroupJoinBattlegroundS = 577,
        GroupJoinBattlegroundFail = 578,
        GroupJoinBattlegroundTooMany = 579,
        SoloJoinBattlegroundS = 580,
        JoinSingleScenarioS = 581,
        BattlegroundTooManyQueues = 582,
        BattlegroundCannotQueueForRated = 583,
        BattledgroundQueuedForRated = 584,
        BattlegroundTeamLeftQueue = 585,
        BattlegroundNotInBattleground = 586,
        AlreadyInArenaTeamS = 587,
        InvalidPromotionCode = 588,
        BgPlayerJoinedSs = 589,
        BgPlayerLeftS = 590,
        RestrictedAccount = 591,
        RestrictedAccountTrial = 592,
        NotEnoughPurchasedGameTime = 593,
        PlayTimeExceeded = 594,
        ApproachingPartialPlayTime = 595,
        ApproachingPartialPlayTime2 = 596,
        ApproachingNoPlayTime = 597,
        ApproachingNoPlayTime2 = 598,
        UnhealthyTime = 599,
        ChatRestrictedTrial = 600,
        ChatThrottled = 601,
        MailReachedCap = 602,
        InvalidRaidTarget = 603,
        RaidLeaderReadyCheckStartS = 604,
        ReadyCheckInProgress = 605,
        ReadyCheckThrottled = 606,
        DungeonDifficultyFailed = 607,
        DungeonDifficultyChangedS = 608,
        TradeWrongRealm = 609,
        TradeNotOnTaplist = 610,
        ChatPlayerAmbiguousS = 611,
        LootCantLootThatNow = 612,
        LootMasterInvFull = 613,
        LootMasterUniqueItem = 614,
        LootMasterOther = 615,
        FilteringYouS = 616,
        UsePreventedByMechanicS = 617,
        ItemUniqueEquippable = 618,
        LfgLeaderIsLfmS = 619,
        LfgPending = 620,
        CantSpeakLangage = 621,
        VendorMissingTurnins = 622,
        BattlegroundNotInTeam = 623,
        NotInBattleground = 624,
        NotEnoughHonorPoints = 625,
        NotEnoughArenaPoints = 626,
        SocketingRequiresMetaGem = 627,
        SocketingMetaGemOnlyInMetaslot = 628,
        SocketingRequiresHydraulicGem = 629,
        SocketingHydraulicGemOnlyInHydraulicslot = 630,
        SocketingRequiresCogwheelGem = 631,
        SocketingCogwheelGemOnlyInCogwheelslot = 632,
        SocketingItemTooLowLevel = 633,
        ItemMaxCountSocketed = 634,
        SystemDisabled = 635,
        QuestFailedTooManyDailyQuestsI = 636,
        ItemMaxCountEquippedSocketed = 637,
        ItemUniqueEquippableSocketed = 638,
        UserSquelched = 639,
        AccountSilenced = 640,
        PartyMemberSilenced = 641,
        PartyMemberSilencedLfgDelist = 642,
        TooMuchGold = 643,
        NotBarberSitting = 644,
        QuestFailedCais = 645,
        InviteRestrictedTrial = 646,
        VoiceIgnoreFull = 647,
        VoiceIgnoreSelf = 648,
        VoiceIgnoreNotFound = 649,
        VoiceIgnoreAlreadyS = 650,
        VoiceIgnoreAddedS = 651,
        VoiceIgnoreRemovedS = 652,
        VoiceIgnoreAmbiguous = 653,
        VoiceIgnoreDeleted = 654,
        UnknownMacroOptionS = 655,
        NotDuringArenaMatch = 656,
        NotInRatedBattleground = 657,
        PlayerSilenced = 658,
        PlayerUnsilenced = 659,
        ComsatDisconnect = 660,
        ComsatReconnectAttempt = 661,
        ComsatConnectFail = 662,
        MailInvalidAttachmentSlot = 663,
        MailTooManyAttachments = 664,
        MailInvalidAttachment = 665,
        MailAttachmentExpired = 666,
        VoiceChatParentalDisableMic = 667,
        ProfaneChatName = 668,
        PlayerSilencedEcho = 669,
        PlayerUnsilencedEcho = 670,
        LootCantLootThat = 671,
        ArenaExpiredCais = 672,
        GroupActionThrottled = 673,
        AlreadyPickpocketed = 674,
        NameInvalid = 675,
        NameNoName = 676,
        NameTooShort = 677,
        NameTooLong = 678,
        NameMixedLanguages = 679,
        NameProfane = 680,
        NameReserved = 681,
        NameThreeConsecutive = 682,
        NameInvalidSpace = 683,
        NameConsecutiveSpaces = 684,
        NameRussianConsecutiveSilentCharacters = 685,
        NameRussianSilentCharacterAtBeginningOrEnd = 686,
        NameDeclensionDoesntMatchBaseName = 687,
        RecruitAFriendNotLinked = 688,
        RecruitAFriendNotNow = 689,
        RecruitAFriendSummonLevelMax = 690,
        RecruitAFriendSummonCooldown = 691,
        RecruitAFriendSummonOffline = 692,
        RecruitAFriendInsufExpanLvl = 693,
        RecruitAFriendMapIncomingTransferNotAllowed = 694,
        NotSameAccount = 695,
        BadOnUseEnchant = 696,
        TradeSelf = 697,
        TooManySockets = 698,
        ItemMaxLimitCategoryCountExceededIs = 699,
        TradeTargetMaxLimitCategoryCountExceededIs = 700,
        ItemMaxLimitCategorySocketedExceededIs = 701,
        ItemMaxLimitCategoryEquippedExceededIs = 702,
        ShapeshiftFormCannotEquip = 703,
        ItemInventoryFullSatchel = 704,
        ScalingStatItemLevelExceeded = 705,
        ScalingStatItemLevelTooLow = 706,
        PurchaseLevelTooLow = 707,
        GroupSwapFailed = 708,
        InviteInCombat = 709,
        InvalidGlyphSlot = 710,
        GenericNoValidTargets = 711,
        CalendarEventAlertS = 712,
        PetLearnSpellS = 713,
        PetLearnAbilityS = 714,
        PetSpellUnlearnedS = 715,
        InviteUnknownRealm = 716,
        InviteNoPartyServer = 717,
        InvitePartyBusy = 718,
        InvitePartyBusyPendingRequest = 719,
        InvitePartyBusyPendingSuggest = 720,
        PartyTargetAmbiguous = 721,
        PartyLfgInviteRaidLocked = 722,
        PartyLfgBootLimit = 723,
        PartyLfgBootCooldownS = 724,
        PartyLfgBootNotEligibleS = 725,
        PartyLfgBootInpatientTimerS = 726,
        PartyLfgBootInProgress = 727,
        PartyLfgBootTooFewPlayers = 728,
        PartyLfgBootVoteSucceeded = 729,
        PartyLfgBootVoteFailed = 730,
        PartyLfgBootDisallowedByMap = 731,
        PartyLfgBootDungeonComplete = 732,
        PartyLfgBootLootRolls = 733,
        PartyLfgBootVoteRegistered = 734,
        PartyPrivateGroupOnly = 735,
        PartyLfgTeleportInCombat = 736,
        PartyTimeRunningSeasonIdMustMatch = 737,
        RaidDisallowedByLevel = 738,
        RaidDisallowedByCrossRealm = 739,
        PartyRoleNotAvailable = 740,
        JoinLfgObjectFailed = 741,
        LfgRemovedLevelup = 742,
        LfgRemovedXpToggle = 743,
        LfgRemovedFactionChange = 744,
        BattlegroundInfoThrottled = 745,
        BattlegroundAlreadyIn = 746,
        ArenaTeamChangeFailedQueued = 747,
        ArenaTeamPermissions = 748,
        NotWhileFalling = 749,
        NotWhileMoving = 750,
        NotWhileFatigued = 751,
        MaxSockets = 752,
        MultiCastActionTotemS = 753,
        BattlegroundJoinLevelup = 754,
        RemoveFromPvpQueueXpGain = 755,
        BattlegroundJoinXpGain = 756,
        BattlegroundJoinMercenary = 757,
        BattlegroundJoinTooManyHealers = 758,
        BattlegroundJoinRatedTooManyHealers = 759,
        BattlegroundJoinTooManyTanks = 760,
        BattlegroundJoinTooManyDamage = 761,
        RaidDifficultyFailed = 762,
        RaidDifficultyChangedS = 763,
        LegacyRaidDifficultyChangedS = 764,
        RaidLockoutChangedS = 765,
        RaidConvertedToParty = 766,
        PartyConvertedToRaid = 767,
        PlayerDifficultyChangedS = 768,
        GmresponseDbError = 769,
        BattlegroundJoinRangeIndex = 770,
        ArenaJoinRangeIndex = 771,
        RemoveFromPvpQueueFactionChange = 772,
        BattlegroundJoinFailed = 773,
        BattlegroundJoinNoValidSpecForRole = 774,
        BattlegroundJoinRespec = 775,
        BattlegroundInvitationDeclined = 776,
        BattlegroundInvitationDeclinedBy = 777,
        BattlegroundJoinTimedOut = 778,
        BattlegroundDupeQueue = 779,
        BattlegroundJoinMustCompleteQuest = 780,
        InBattlegroundRespec = 781,
        MailLimitedDurationItem = 782,
        YellRestrictedTrial = 783,
        ChatRaidRestrictedTrial = 784,
        LfgRoleCheckFailed = 785,
        LfgRoleCheckFailedTimeout = 786,
        LfgRoleCheckFailedNotViable = 787,
        LfgReadyCheckFailed = 788,
        LfgReadyCheckFailedTimeout = 789,
        LfgGroupFull = 790,
        LfgNoLfgObject = 791,
        LfgNoSlotsPlayer = 792,
        LfgNoSlotsParty = 793,
        LfgNoSpec = 794,
        LfgMismatchedSlots = 795,
        LfgMismatchedSlotsLocalXrealm = 796,
        LfgPartyPlayersFromDifferentRealms = 797,
        LfgMembersNotPresent = 798,
        LfgGetInfoTimeout = 799,
        LfgInvalidSlot = 800,
        LfgDeserterPlayer = 801,
        LfgDeserterParty = 802,
        LfgDead = 803,
        LfgRandomCooldownPlayer = 804,
        LfgRandomCooldownParty = 805,
        LfgTooManyMembers = 806,
        LfgTooFewMembers = 807,
        LfgProposalFailed = 808,
        LfgProposalDeclinedSelf = 809,
        LfgProposalDeclinedParty = 810,
        LfgNoSlotsSelected = 811,
        LfgNoRolesSelected = 812,
        LfgRoleCheckInitiated = 813,
        LfgReadyCheckInitiated = 814,
        LfgPlayerDeclinedRoleCheck = 815,
        LfgPlayerDeclinedReadyCheck = 816,
        LfgJoinedQueue = 817,
        LfgJoinedFlexQueue = 818,
        LfgJoinedRfQueue = 819,
        LfgJoinedScenarioQueue = 820,
        LfgJoinedWorldPvpQueue = 821,
        LfgJoinedBattlefieldQueue = 822,
        LfgJoinedList = 823,
        QueuedPlunderstorm = 824,
        LfgLeftQueue = 825,
        LfgLeftList = 826,
        LfgRoleCheckAborted = 827,
        LfgReadyCheckAborted = 828,
        LfgCantUseBattleground = 829,
        LfgCantUseDungeons = 830,
        LfgReasonTooManyLfg = 831,
        LfgFarmLimit = 832,
        LfgNoCrossFactionParties = 833,
        InvalidTeleportLocation = 834,
        TooFarToInteract = 835,
        BattlegroundPlayersFromDifferentRealms = 836,
        DifficultyChangeCooldownS = 837,
        DifficultyChangeCombatCooldownS = 838,
        DifficultyChangeWorldstate = 839,
        DifficultyChangeEncounter = 840,
        DifficultyChangeCombat = 841,
        DifficultyChangePlayerBusy = 842,
        DifficultyChangePlayerOnVehicle = 843,
        DifficultyChangeAlreadyStarted = 844,
        DifficultyChangeOtherHeroicS = 845,
        DifficultyChangeHeroicInstanceAlreadyRunning = 846,
        ArenaTeamPartySize = 847,
        SoloShuffleWargameGroupSize = 848,
        SoloShuffleWargameGroupComp = 849,
        SoloRbgWargameGroupSize = 850,
        SoloRbgWargameGroupComp = 851,
        SoloMinItemLevel = 852,
        PvpPlayerAbandoned = 853,
        BattlegroundJoinGroupQueueWithoutHealer = 854,
        QuestForceRemovedS = 855,
        AttackNoActions = 856,
        InRandomBg = 857,
        InNonRandomBg = 858,
        BnFriendSelf = 859,
        BnFriendAlready = 860,
        BnFriendBlocked = 861,
        BnFriendListFull = 862,
        BnFriendRequestSent = 863,
        BnBroadcastThrottle = 864,
        BgDeveloperOnly = 865,
        CurrencySpellSlotMismatch = 866,
        CurrencyNotTradable = 867,
        RequiresExpansionS = 868,
        QuestFailedSpell = 869,
        TalentFailedUnspentTalentPoints = 870,
        TalentFailedNotEnoughTalentsInPrimaryTree = 871,
        TalentFailedNoPrimaryTreeSelected = 872,
        TalentFailedCantRemoveTalent = 873,
        TalentFailedUnknown = 874,
        TalentFailedInCombat = 875,
        TalentFailedInPvpMatch = 876,
        TalentFailedInMythicPlus = 877,
        WargameRequestFailure = 878,
        RankRequiresAuthenticator = 879,
        GuildBankVoucherFailed = 880,
        WargameRequestSent = 881,
        RequiresAchievementI = 882,
        RefundResultExceedMaxCurrency = 883,
        CantBuyQuantity = 884,
        ItemIsBattlePayLocked = 885,
        PartyAlreadyInBattlegroundQueue = 886,
        PartyConfirmingBattlegroundQueue = 887,
        BattlefieldTeamPartySize = 888,
        InsuffTrackedCurrencyIs = 889,
        NotOnTournamentRealm = 890,
        GuildTrialAccountTrial = 891,
        GuildTrialAccountVeteran = 892,
        GuildUndeletableDueToLevel = 893,
        CantDoThatInAGroup = 894,
        GuildLeaderReplaced = 895,
        TransmogrifyCantEquip = 896,
        TransmogrifyInvalidItemType = 897,
        TransmogrifyNotSoulbound = 898,
        TransmogrifyInvalidSource = 899,
        TransmogrifyInvalidDestination = 900,
        TransmogrifyMismatch = 901,
        TransmogrifyLegendary = 902,
        TransmogrifySameItem = 903,
        TransmogrifySameAppearance = 904,
        TransmogrifyNotEquipped = 905,
        VoidDepositFull = 906,
        VoidWithdrawFull = 907,
        VoidStorageWrapped = 908,
        VoidStorageStackable = 909,
        VoidStorageUnbound = 910,
        VoidStorageRepair = 911,
        VoidStorageCharges = 912,
        VoidStorageQuest = 913,
        VoidStorageConjured = 914,
        VoidStorageMail = 915,
        VoidStorageBag = 916,
        VoidTransferStorageFull = 917,
        VoidTransferInvFull = 918,
        VoidTransferInternalError = 919,
        VoidTransferItemInvalid = 920,
        DifficultyDisabledInLfg = 921,
        VoidStorageUnique = 922,
        VoidStorageLoot = 923,
        VoidStorageHoliday = 924,
        VoidStorageDuration = 925,
        VoidStorageLoadFailed = 926,
        VoidStorageInvalidItem = 927,
        VoidStorageAccountItem = 928,
        ParentalControlsChatMuted = 929,
        SorStartExperienceIncomplete = 930,
        SorInvalidEmail = 931,
        SorInvalidComment = 932,
        ChallengeModeResetCooldownS = 933,
        ChallengeModeResetKeystone = 934,
        PetJournalAlreadyInLoadout = 935,
        ReportSubmittedSuccessfully = 936,
        ReportSubmissionFailed = 937,
        SuggestionSubmittedSuccessfully = 938,
        BugSubmittedSuccessfully = 939,
        ChallengeModeEnabled = 940,
        ChallengeModeDisabled = 941,
        PetbattleCreateFailed = 942,
        PetbattleNotHere = 943,
        PetbattleNotHereOnTransport = 944,
        PetbattleNotHereUnevenGround = 945,
        PetbattleNotHereObstructed = 946,
        PetbattleNotWhileInCombat = 947,
        PetbattleNotWhileDead = 948,
        PetbattleNotWhileFlying = 949,
        PetbattleTargetInvalid = 950,
        PetbattleTargetOutOfRange = 951,
        PetbattleTargetNotCapturable = 952,
        PetbattleNotATrainer = 953,
        PetbattleDeclined = 954,
        PetbattleInBattle = 955,
        PetbattleInvalidLoadout = 956,
        PetbattleAllPetsDead = 957,
        PetbattleNoPetsInSlots = 958,
        PetbattleNoAccountLock = 959,
        PetbattleWildPetTapped = 960,
        PetbattleRestrictedAccount = 961,
        PetbattleOpponentNotAvailable = 962,
        PetbattleNotWhileInMatchedBattle = 963,
        CantHaveMorePetsOfThatType = 964,
        CantHaveMorePets = 965,
        PvpMapNotFound = 966,
        PvpMapNotSet = 967,
        PetbattleQueueQueued = 968,
        PetbattleQueueAlreadyQueued = 969,
        PetbattleQueueJoinFailed = 970,
        PetbattleQueueJournalLock = 971,
        PetbattleQueueRemoved = 972,
        PetbattleQueueProposalDeclined = 973,
        PetbattleQueueProposalTimeout = 974,
        PetbattleQueueOpponentDeclined = 975,
        PetbattleQueueRequeuedInternal = 976,
        PetbattleQueueRequeuedRemoved = 977,
        PetbattleQueueSlotLocked = 978,
        PetbattleQueueSlotEmpty = 979,
        PetbattleQueueSlotNoTracker = 980,
        PetbattleQueueSlotNoSpecies = 981,
        PetbattleQueueSlotCantBattle = 982,
        PetbattleQueueSlotRevoked = 983,
        PetbattleQueueSlotDead = 984,
        PetbattleQueueSlotNoPet = 985,
        PetbattleQueueNotWhileNeutral = 986,
        PetbattleGameTimeLimitWarning = 987,
        PetbattleGameRoundsLimitWarning = 988,
        HasRestriction = 989,
        ItemUpgradeItemTooLowLevel = 990,
        ItemUpgradeNoPath = 991,
        ItemUpgradeNoMoreUpgrades = 992,
        BonusRollEmpty = 993,
        ChallengeModeFull = 994,
        ChallengeModeInProgress = 995,
        ChallengeModeIncorrectKeystone = 996,
        BattletagFriendNotFound = 997,
        BattletagFriendNotValid = 998,
        BattletagFriendNotAllowed = 999,
        BattletagFriendThrottled = 1000,
        BattletagFriendSuccess = 1001,
        PetTooHighLevelToUncage = 1002,
        PetbattleInternal = 1003,
        CantCagePetYet = 1004,
        NoLootInChallengeMode = 1005,
        QuestPetBattleVictoriesPvpIi = 1006,
        RoleCheckAlreadyInProgress = 1007,
        RecruitAFriendAccountLimit = 1008,
        RecruitAFriendFailed = 1009,
        SetLootPersonal = 1010,
        SetLootMethodFailedCombat = 1011,
        ReagentBankFull = 1012,
        ReagentBankLocked = 1013,
        GarrisonBuildingExists = 1014,
        GarrisonInvalidPlot = 1015,
        GarrisonInvalidBuildingid = 1016,
        GarrisonInvalidPlotBuilding = 1017,
        GarrisonRequiresBlueprint = 1018,
        GarrisonNotEnoughCurrency = 1019,
        GarrisonNotEnoughGold = 1020,
        GarrisonCompleteMissionWrongFollowerType = 1021,
        AlreadyUsingLfgList = 1022,
        RestrictedAccountLfgListTrial = 1023,
        ToyUseLimitReached = 1024,
        ToyAlreadyKnown = 1025,
        TransmogSetAlreadyKnown = 1026,
        NotEnoughCurrency = 1027,
        SpecIsDisabled = 1028,
        FeatureRestrictedTrial = 1029,
        CantBeObliterated = 1030,
        CantBeScrapped = 1031,
        CantBeRecrafted = 1032,
        ArtifactRelicDoesNotMatchArtifact = 1033,
        MustEquipArtifact = 1034,
        CantDoThatRightNow = 1035,
        AffectingCombat = 1036,
        EquipmentManagerCombatSwapS = 1037,
        EquipmentManagerBagsFull = 1038,
        EquipmentManagerMissingItemS = 1039,
        MovieRecordingWarningPerf = 1040,
        MovieRecordingWarningDiskFull = 1041,
        MovieRecordingWarningNoMovie = 1042,
        MovieRecordingWarningRequirements = 1043,
        MovieRecordingWarningCompressing = 1044,
        NoChallengeModeReward = 1045,
        ClaimedChallengeModeReward = 1046,
        ChallengeModePeriodResetSs = 1047,
        CantDoThatChallengeModeActive = 1048,
        TalentFailedRestArea = 1049,
        CannotAbandonLastPet = 1050,
        TestCvarSetSss = 1051,
        QuestTurnInFailReason = 1052,
        ClaimedChallengeModeRewardOld = 1053,
        TalentGrantedByAura = 1054,
        ChallengeModeAlreadyComplete = 1055,
        GlyphTargetNotAvailable = 1056,
        PvpWarmodeToggleOn = 1057,
        PvpWarmodeToggleOff = 1058,
        SpellFailedLevelRequirement = 1059,
        SpellFailedCantFlyHere = 1060,
        BattlegroundJoinRequiresLevel = 1061,
        BattlegroundJoinDisqualified = 1062,
        BattlegroundJoinDisqualifiedNoName = 1063,
        VoiceChatGenericUnableToConnect = 1064,
        VoiceChatServiceLost = 1065,
        VoiceChatChannelNameTooShort = 1066,
        VoiceChatChannelNameTooLong = 1067,
        VoiceChatChannelAlreadyExists = 1068,
        VoiceChatTargetNotFound = 1069,
        VoiceChatTooManyRequests = 1070,
        VoiceChatPlayerSilenced = 1071,
        VoiceChatParentalDisableAll = 1072,
        VoiceChatDisabled = 1073,
        NoPvpReward = 1074,
        ClaimedPvpReward = 1075,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1076,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1077,
        AzeriteEssenceSelectionFailedConditionFailed = 1078,
        AzeriteEssenceSelectionFailedRestArea = 1079,
        AzeriteEssenceSelectionFailedSlotLocked = 1080,
        AzeriteEssenceSelectionFailedNotAtForge = 1081,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1082,
        AzeriteEssenceSelectionFailedNotEquipped = 1083,
        SocketingRequiresPunchcardredGem = 1084,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1085,
        SocketingRequiresPunchcardyellowGem = 1086,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1087,
        SocketingRequiresPunchcardblueGem = 1088,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1089,
        SocketingRequiresDominationShard = 1090,
        SocketingDominationShardOnlyInDominationslot = 1091,
        SocketingRequiresCypherGem = 1092,
        SocketingCypherGemOnlyInCypherslot = 1093,
        SocketingRequiresTinkerGem = 1094,
        SocketingTinkerGemOnlyInTinkerslot = 1095,
        SocketingRequiresPrimordialGem = 1096,
        SocketingPrimordialGemOnlyInPrimordialslot = 1097,
        SocketingRequiresFragranceGem = 1098,
        SocketingFragranceGemOnlyInFragranceslot = 1099,
        SocketingRequiresSingingThunderGem = 1100,
        SocketingSingingthunderGemOnlyInSingingthunderslot = 1101,
        SocketingRequiresSingingSeaGem = 1102,
        SocketingSingingseaGemOnlyInSingingseaslot = 1103,
        SocketingRequiresSingingWindGem = 1104,
        SocketingSingingwindGemOnlyInSingingwindslot = 1105,
        LevelLinkingResultLinked = 1106,
        LevelLinkingResultUnlinked = 1107,
        ClubFinderErrorPostClub = 1108,
        ClubFinderErrorApplyClub = 1109,
        ClubFinderErrorRespondApplicant = 1110,
        ClubFinderErrorCancelApplication = 1111,
        ClubFinderErrorTypeAcceptApplication = 1112,
        ClubFinderErrorTypeNoInvitePermissions = 1113,
        ClubFinderErrorTypeNoPostingPermissions = 1114,
        ClubFinderErrorTypeApplicantList = 1115,
        ClubFinderErrorTypeApplicantListNoPerm = 1116,
        ClubFinderErrorTypeFinderNotAvailable = 1117,
        ClubFinderErrorTypeGetPostingIds = 1118,
        ClubFinderErrorTypeJoinApplication = 1119,
        ClubFinderErrorTypeRealmNotEligible = 1120,
        ClubFinderErrorTypeFlaggedRename = 1121,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1122,
        ItemInteractionNotEnoughGold = 1123,
        ItemInteractionNotEnoughCurrency = 1124,
        ItemInteractionNoConversionOutput = 1125,
        PlayerChoiceErrorPendingChoice = 1126,
        SoulbindInvalidConduit = 1127,
        SoulbindInvalidConduitItem = 1128,
        SoulbindInvalidTalent = 1129,
        SoulbindDuplicateConduit = 1130,
        ActivateSoulbindS = 1131,
        ActivateSoulbindFailedRestArea = 1132,
        CantUseProfanity = 1133,
        NotInPetBattle = 1134,
        NotInNpe = 1135,
        NoSpec = 1136,
        NoDominationshardOverwrite = 1137,
        UseWeeklyRewardsDisabled = 1138,
        CrossFactionGroupJoined = 1139,
        CantTargetUnfriendlyInOverworld = 1140,
        EquipablespellsSlotsFull = 1141,
        ItemModAppearanceGroupAlreadyKnown = 1142,
        CantBulkSellItemWithRefund = 1143,
        NoSoulboundItemInAccountBank = 1144,
        NoRefundableItemInAccountBank = 1145,
        CantDeleteInAccountBank = 1146,
        NoImmediateContainerInAccountBank = 1147,
        NoOpenImmediateContainerInAccountBank = 1148,
        CantTradeAccountItem = 1149,
        NoAccountInventoryLock = 1150,
        BankNotAccessible = 1151,
        TooManyAccountBankTabs = 1152,
        AccountBankTabNotUnlocked = 1153,
        AccountMoneyLocked = 1154,
        BankTabInvalidName = 1155,
        BankTabInvalidText = 1156,
        WowLabsPartyErrorTypePartyIsFull = 1157,
        WowLabsPartyErrorTypeMaxInviteSent = 1158,
        WowLabsPartyErrorTypePlayerAlreadyInvited = 1159,
        WowLabsPartyErrorTypePartyInviteInvalid = 1160,
        WowLabsLobbyMatchmakerErrorEnterQueueFailed = 1161,
        WowLabsLobbyMatchmakerErrorLeaveQueueFailed = 1162,
        WowLabsSetWowLabsAreaIdFailed = 1163,
        PlunderstormCannotQueue = 1164,
        TargetIsSelfFoundCannotTrade = 1165,
        PlayerIsSelfFoundCannotTrade = 1166,
        MailRecepientIsSelfFoundCannotReceiveMail = 1167,
        PlayerIsSelfFoundCannotSendMail = 1168,
        PlayerIsSelfFoundCannotUseAuctionHouse = 1169,
        MailTargetCannotReceiveMail = 1170,
        RemixInvalidTransferRequest = 1171,
        CurrencyTransferInvalidCharacter = 1172,
        CurrencyTransferInvalidCurrency = 1173,
        CurrencyTransferInsufficientCurrency = 1174,
        CurrencyTransferMaxQuantity = 1175,
        CurrencyTransferNoValidSource = 1176,
        CurrencyTransferCharacterLoggedIn = 1177,
        CurrencyTransferServerError = 1178,
        CurrencyTransferUnmetRequirements = 1179,
        CurrencyTransferTransactionInProgress = 1180,
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
