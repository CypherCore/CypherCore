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
        GuildBankWarbandsBankSource = 165,
        GuildNewLeaderWrongRealm = 166,
        NoGuildCharter = 167,
        OutOfRange = 168,
        PlayerDead = 169,
        ClientLockedOut = 170,
        ClientOnTransport = 171,
        KilledByS = 172,
        LootLocked = 173,
        LootTooFar = 174,
        LootDidntKill = 175,
        LootBadFacing = 176,
        LootNotstanding = 177,
        LootStunned = 178,
        LootNoUi = 179,
        LootWhileInvulnerable = 180,
        NoLoot = 181,
        QuestAcceptedS = 182,
        QuestCompleteS = 183,
        QuestFailedS = 184,
        QuestFailedBagFullS = 185,
        QuestFailedMaxCountS = 186,
        QuestFailedLowLevel = 187,
        QuestFailedMissingItems = 188,
        QuestFailedWrongRace = 189,
        QuestFailedNotEnoughMoney = 190,
        QuestFailedExpansion = 191,
        QuestOnlyOneTimed = 192,
        QuestNeedPrereqs = 193,
        QuestNeedPrereqsCustom = 194,
        QuestAlreadyOn = 195,
        QuestAlreadyDone = 196,
        QuestAlreadyDoneDaily = 197,
        QuestHasInProgress = 198,
        QuestRewardExpI = 199,
        QuestRewardMoneyS = 200,
        QuestMustChoose = 201,
        QuestLogFull = 202,
        CombatDamageSsi = 203,
        InspectS = 204,
        CantUseItem = 205,
        CantUseItemInArena = 206,
        CantUseItemInRatedBattleground = 207,
        MustEquipItem = 208,
        PassiveAbility = 209,
        H2SkillNotFound = 210,
        NoAttackTarget = 211,
        InvalidAttackTarget = 212,
        AttackPvpTargetWhileUnflagged = 213,
        AttackStunned = 214,
        AttackPacified = 215,
        AttackMounted = 216,
        AttackFleeing = 217,
        AttackConfused = 218,
        AttackCharmed = 219,
        AttackDead = 220,
        AttackPreventedByMechanicS = 221,
        AttackChannel = 222,
        Taxisamenode = 223,
        Taxinosuchpath = 224,
        Taxiunspecifiedservererror = 225,
        Taxinotenoughmoney = 226,
        Taxitoofaraway = 227,
        Taxinovendornearby = 228,
        Taxinotvisited = 229,
        Taxiplayerbusy = 230,
        Taxiplayeralreadymounted = 231,
        Taxiplayershapeshifted = 232,
        Taxiplayermoving = 233,
        Taxinopaths = 234,
        Taxinoteligible = 235,
        Taxinotstanding = 236,
        Taxiincombat = 237,
        NoReplyTarget = 238,
        GenericNoTarget = 239,
        InitiateTradeS = 240,
        TradeRequestS = 241,
        TradeBlockedS = 242,
        TradeTargetDead = 243,
        TradeTooFar = 244,
        TradeCancelled = 245,
        TradeComplete = 246,
        TradeBagFull = 247,
        TradeTargetBagFull = 248,
        TradeMaxCountExceeded = 249,
        TradeTargetMaxCountExceeded = 250,
        InventoryTradeTooManyUniqueItem = 251,
        AlreadyTrading = 252,
        MountInvalidmountee = 253,
        MountToofaraway = 254,
        MountAlreadymounted = 255,
        MountNotmountable = 256,
        MountNotyourpet = 257,
        MountOther = 258,
        MountLooting = 259,
        MountRacecantmount = 260,
        MountShapeshifted = 261,
        MountNoFavorites = 262,
        MountNoMounts = 263,
        DismountNopet = 264,
        DismountNotmounted = 265,
        DismountNotyourpet = 266,
        SpellFailedTotems = 267,
        SpellFailedReagents = 268,
        SpellFailedReagentsGeneric = 269,
        SpellFailedOptionalReagents = 270,
        CantTradeGold = 271,
        SpellFailedEquippedItem = 272,
        SpellFailedEquippedItemClassS = 273,
        SpellFailedShapeshiftFormS = 274,
        SpellFailedAnotherInProgress = 275,
        Badattackfacing = 276,
        Badattackpos = 277,
        ChestInUse = 278,
        UseCantOpen = 279,
        UseLocked = 280,
        DoorLocked = 281,
        ButtonLocked = 282,
        UseLockedWithItemS = 283,
        UseLockedWithSpellS = 284,
        UseLockedWithSpellKnownSi = 285,
        UseTooFar = 286,
        UseBadAngle = 287,
        UseObjectMoving = 288,
        UseSpellFocus = 289,
        UseDestroyed = 290,
        SetLootFreeforall = 291,
        SetLootRoundrobin = 292,
        SetLootMaster = 293,
        SetLootGroup = 294,
        SetLootThresholdS = 295,
        NewLootMasterS = 296,
        SpecifyMasterLooter = 297,
        LootSpecChangedS = 298,
        TameFailed = 299,
        ChatWhileDead = 300,
        ChatPlayerNotFoundS = 301,
        Newtaxipath = 302,
        NoPet = 303,
        Notyourpet = 304,
        PetNotRenameable = 305,
        QuestObjectiveCompleteS = 306,
        QuestUnknownComplete = 307,
        QuestAddKillSii = 308,
        QuestAddFoundSii = 309,
        QuestAddItemSii = 310,
        QuestAddPlayerKillSii = 311,
        Cannotcreatedirectory = 312,
        Cannotcreatefile = 313,
        PlayerWrongFaction = 314,
        PlayerIsNeutral = 315,
        BankslotFailedTooMany = 316,
        BankslotInsufficientFunds = 317,
        BankslotNotbanker = 318,
        FriendDbError = 319,
        FriendListFull = 320,
        FriendAddedS = 321,
        BattletagFriendAddedS = 322,
        FriendOnlineSs = 323,
        FriendOfflineS = 324,
        FriendNotFound = 325,
        FriendWrongFaction = 326,
        FriendRemovedS = 327,
        BattletagFriendRemovedS = 328,
        FriendError = 329,
        FriendAlreadyS = 330,
        FriendSelf = 331,
        FriendDeleted = 332,
        IgnoreFull = 333,
        IgnoreSelf = 334,
        IgnoreNotFound = 335,
        IgnoreAlreadyS = 336,
        IgnoreAddedS = 337,
        IgnoreRemovedS = 338,
        IgnoreAmbiguous = 339,
        IgnoreDeleted = 340,
        OnlyOneBolt = 341,
        OnlyOneAmmo = 342,
        SpellFailedEquippedSpecificItem = 343,
        WrongBagTypeSubclass = 344,
        CantWrapStackable = 345,
        CantWrapEquipped = 346,
        CantWrapWrapped = 347,
        CantWrapBound = 348,
        CantWrapUnique = 349,
        CantWrapBags = 350,
        OutOfMana = 351,
        OutOfRage = 352,
        OutOfFocus = 353,
        OutOfEnergy = 354,
        OutOfChi = 355,
        OutOfHealth = 356,
        OutOfRunes = 357,
        OutOfRunicPower = 358,
        OutOfSoulShards = 359,
        OutOfLunarPower = 360,
        OutOfHolyPower = 361,
        OutOfMaelstrom = 362,
        OutOfComboPoints = 363,
        OutOfInsanity = 364,
        OutOfEssence = 365,
        OutOfArcaneCharges = 366,
        OutOfFury = 367,
        OutOfPain = 368,
        OutOfPowerDisplay = 369,
        LootGone = 370,
        MountForceddismount = 371,
        AutofollowTooFar = 372,
        UnitNotFound = 373,
        InvalidFollowTarget = 374,
        InvalidFollowPvpCombat = 375,
        InvalidFollowTargetPvpCombat = 376,
        InvalidInspectTarget = 377,
        GuildemblemSuccess = 378,
        GuildemblemInvalidTabardColors = 379,
        GuildemblemNoguild = 380,
        GuildemblemNotguildmaster = 381,
        GuildemblemNotenoughmoney = 382,
        GuildemblemInvalidvendor = 383,
        EmblemerrorNotabardgeoset = 384,
        SpellOutOfRange = 385,
        CommandNeedsTarget = 386,
        NoammoS = 387,
        Toobusytofollow = 388,
        DuelRequested = 389,
        DuelCancelled = 390,
        Deathbindalreadybound = 391,
        DeathbindSuccessS = 392,
        Noemotewhilerunning = 393,
        ZoneExplored = 394,
        ZoneExploredXp = 395,
        InvalidItemTarget = 396,
        InvalidQuestTarget = 397,
        IgnoringYouS = 398,
        FishNotHooked = 399,
        FishEscaped = 400,
        SpellFailedNotunsheathed = 401,
        PetitionOfferedS = 402,
        PetitionSigned = 403,
        PetitionSignedS = 404,
        PetitionDeclinedS = 405,
        PetitionAlreadySigned = 406,
        PetitionRestrictedAccountTrial = 407,
        PetitionAlreadySignedOther = 408,
        PetitionInGuild = 409,
        PetitionCreator = 410,
        PetitionNotEnoughSignatures = 411,
        PetitionNotSameServer = 412,
        PetitionFull = 413,
        PetitionAlreadySignedByS = 414,
        GuildNameInvalid = 415,
        SpellUnlearnedS = 416,
        PetSpellRooted = 417,
        PetSpellAffectingCombat = 418,
        PetSpellOutOfRange = 419,
        PetSpellNotBehind = 420,
        PetSpellTargetsDead = 421,
        PetSpellDead = 422,
        PetSpellNopath = 423,
        ItemCantBeDestroyed = 424,
        TicketAlreadyExists = 425,
        TicketCreateError = 426,
        TicketUpdateError = 427,
        TicketDbError = 428,
        TicketNoText = 429,
        TicketTextTooLong = 430,
        ObjectIsBusy = 431,
        ExhaustionWellrested = 432,
        ExhaustionRested = 433,
        ExhaustionNormal = 434,
        ExhaustionTired = 435,
        ExhaustionExhausted = 436,
        NoItemsWhileShapeshifted = 437,
        CantInteractShapeshifted = 438,
        RealmNotFound = 439,
        MailQuestItem = 440,
        MailBoundItem = 441,
        MailConjuredItem = 442,
        MailBag = 443,
        MailToSelf = 444,
        MailTargetNotFound = 445,
        MailDatabaseError = 446,
        MailDeleteItemError = 447,
        MailWrappedCod = 448,
        MailCantSendRealm = 449,
        MailTempReturnOutage = 450,
        MailRecepientCantReceiveMail = 451,
        MailSent = 452,
        MailTargetIsTrial = 453,
        NotHappyEnough = 454,
        UseCantImmune = 455,
        CantBeDisenchanted = 456,
        CantUseDisarmed = 457,
        AuctionDatabaseError = 458,
        AuctionHigherBid = 459,
        AuctionAlreadyBid = 460,
        AuctionOutbidS = 461,
        AuctionWonS = 462,
        AuctionRemovedS = 463,
        AuctionBidPlaced = 464,
        LogoutFailed = 465,
        QuestPushSuccessS = 466,
        QuestPushInvalidS = 467,
        QuestPushInvalidToRecipientS = 468,
        QuestPushAcceptedS = 469,
        QuestPushDeclinedS = 470,
        QuestPushBusyS = 471,
        QuestPushDeadS = 472,
        QuestPushDeadToRecipientS = 473,
        QuestPushLogFullS = 474,
        QuestPushLogFullToRecipientS = 475,
        QuestPushOnquestS = 476,
        QuestPushOnquestToRecipientS = 477,
        QuestPushAlreadyDoneS = 478,
        QuestPushAlreadyDoneToRecipientS = 479,
        QuestPushNotDailyS = 480,
        QuestPushTimerExpiredS = 481,
        QuestPushNotInPartyS = 482,
        QuestPushDifferentServerDailyS = 483,
        QuestPushDifferentServerDailyToRecipientS = 484,
        QuestPushNotAllowedS = 485,
        QuestPushPrerequisiteS = 486,
        QuestPushPrerequisiteToRecipientS = 487,
        QuestPushLowLevelS = 488,
        QuestPushLowLevelToRecipientS = 489,
        QuestPushHighLevelS = 490,
        QuestPushHighLevelToRecipientS = 491,
        QuestPushClassS = 492,
        QuestPushClassToRecipientS = 493,
        QuestPushRaceS = 494,
        QuestPushRaceToRecipientS = 495,
        QuestPushLowFactionS = 496,
        QuestPushLowFactionToRecipientS = 497,
        QuestPushHighFactionS = 498,
        QuestPushHighFactionToRecipientS = 499,
        QuestPushExpansionS = 500,
        QuestPushExpansionToRecipientS = 501,
        QuestPushNotGarrisonOwnerS = 502,
        QuestPushNotGarrisonOwnerToRecipientS = 503,
        QuestPushWrongCovenantS = 504,
        QuestPushWrongCovenantToRecipientS = 505,
        QuestPushNewPlayerExperienceS = 506,
        QuestPushNewPlayerExperienceToRecipientS = 507,
        QuestPushWrongFactionS = 508,
        QuestPushWrongFactionToRecipientS = 509,
        QuestPushCrossFactionRestrictedS = 510,
        RaidGroupLowlevel = 511,
        RaidGroupOnly = 512,
        RaidGroupFull = 513,
        RaidGroupRequirementsUnmatch = 514,
        CorpseIsNotInInstance = 515,
        PvpKillHonorable = 516,
        PvpKillDishonorable = 517,
        SpellFailedAlreadyAtFullHealth = 518,
        SpellFailedAlreadyAtFullMana = 519,
        SpellFailedAlreadyAtFullPowerS = 520,
        AutolootMoneyS = 521,
        GenericStunned = 522,
        GenericThrottle = 523,
        ClubFinderSearchingTooFast = 524,
        TargetStunned = 525,
        MustRepairDurability = 526,
        RaidYouJoined = 527,
        RaidYouLeft = 528,
        InstanceGroupJoinedWithParty = 529,
        InstanceGroupJoinedWithRaid = 530,
        RaidMemberAddedS = 531,
        RaidMemberRemovedS = 532,
        InstanceGroupAddedS = 533,
        InstanceGroupRemovedS = 534,
        ClickOnItemToFeed = 535,
        TooManyChatChannels = 536,
        LootRollPending = 537,
        LootPlayerNotFound = 538,
        NotInRaid = 539,
        LoggingOut = 540,
        TargetLoggingOut = 541,
        NotWhileMounted = 542,
        NotWhileShapeshifted = 543,
        NotInCombat = 544,
        NotWhileDisarmed = 545,
        PetBroken = 546,
        TalentWipeError = 547,
        SpecWipeError = 548,
        GlyphWipeError = 549,
        PetSpecWipeError = 550,
        FeignDeathResisted = 551,
        MeetingStoneInQueueS = 552,
        MeetingStoneLeftQueueS = 553,
        MeetingStoneOtherMemberLeft = 554,
        MeetingStonePartyKickedFromQueue = 555,
        MeetingStoneMemberStillInQueue = 556,
        MeetingStoneSuccess = 557,
        MeetingStoneInProgress = 558,
        MeetingStoneMemberAddedS = 559,
        MeetingStoneGroupFull = 560,
        MeetingStoneNotLeader = 561,
        MeetingStoneInvalidLevel = 562,
        MeetingStoneTargetNotInParty = 563,
        MeetingStoneTargetInvalidLevel = 564,
        MeetingStoneMustBeLeader = 565,
        MeetingStoneNoRaidGroup = 566,
        MeetingStoneNeedParty = 567,
        MeetingStoneNotFound = 568,
        MeetingStoneTargetInVehicle = 569,
        GuildemblemSame = 570,
        EquipTradeItem = 571,
        PvpToggleOn = 572,
        PvpToggleOff = 573,
        GroupJoinBattlegroundDeserters = 574,
        GroupJoinBattlegroundDead = 575,
        GroupJoinBattlegroundS = 576,
        GroupJoinBattlegroundFail = 577,
        GroupJoinBattlegroundTooMany = 578,
        SoloJoinBattlegroundS = 579,
        JoinSingleScenarioS = 580,
        BattlegroundTooManyQueues = 581,
        BattlegroundCannotQueueForRated = 582,
        BattledgroundQueuedForRated = 583,
        BattlegroundTeamLeftQueue = 584,
        BattlegroundNotInBattleground = 585,
        AlreadyInArenaTeamS = 586,
        InvalidPromotionCode = 587,
        BgPlayerJoinedSs = 588,
        BgPlayerLeftS = 589,
        RestrictedAccount = 590,
        RestrictedAccountTrial = 591,
        NotEnoughPurchasedGameTime = 592,
        PlayTimeExceeded = 593,
        ApproachingPartialPlayTime = 594,
        ApproachingPartialPlayTime2 = 595,
        ApproachingNoPlayTime = 596,
        ApproachingNoPlayTime2 = 597,
        UnhealthyTime = 598,
        ChatRestrictedTrial = 599,
        ChatThrottled = 600,
        MailReachedCap = 601,
        InvalidRaidTarget = 602,
        RaidLeaderReadyCheckStartS = 603,
        ReadyCheckInProgress = 604,
        ReadyCheckThrottled = 605,
        DungeonDifficultyFailed = 606,
        DungeonDifficultyChangedS = 607,
        TradeWrongRealm = 608,
        TradeNotOnTaplist = 609,
        ChatPlayerAmbiguousS = 610,
        LootCantLootThatNow = 611,
        LootMasterInvFull = 612,
        LootMasterUniqueItem = 613,
        LootMasterOther = 614,
        FilteringYouS = 615,
        UsePreventedByMechanicS = 616,
        ItemUniqueEquippable = 617,
        LfgLeaderIsLfmS = 618,
        LfgPending = 619,
        CantSpeakLangage = 620,
        VendorMissingTurnins = 621,
        BattlegroundNotInTeam = 622,
        NotInBattleground = 623,
        NotEnoughHonorPoints = 624,
        NotEnoughArenaPoints = 625,
        SocketingRequiresMetaGem = 626,
        SocketingMetaGemOnlyInMetaslot = 627,
        SocketingRequiresHydraulicGem = 628,
        SocketingHydraulicGemOnlyInHydraulicslot = 629,
        SocketingRequiresCogwheelGem = 630,
        SocketingCogwheelGemOnlyInCogwheelslot = 631,
        SocketingItemTooLowLevel = 632,
        ItemMaxCountSocketed = 633,
        SystemDisabled = 634,
        QuestFailedTooManyDailyQuestsI = 635,
        ItemMaxCountEquippedSocketed = 636,
        ItemUniqueEquippableSocketed = 637,
        UserSquelched = 638,
        AccountSilenced = 639,
        PartyMemberSilenced = 640,
        PartyMemberSilencedLfgDelist = 641,
        TooMuchGold = 642,
        NotBarberSitting = 643,
        QuestFailedCais = 644,
        InviteRestrictedTrial = 645,
        VoiceIgnoreFull = 646,
        VoiceIgnoreSelf = 647,
        VoiceIgnoreNotFound = 648,
        VoiceIgnoreAlreadyS = 649,
        VoiceIgnoreAddedS = 650,
        VoiceIgnoreRemovedS = 651,
        VoiceIgnoreAmbiguous = 652,
        VoiceIgnoreDeleted = 653,
        UnknownMacroOptionS = 654,
        NotDuringArenaMatch = 655,
        NotInRatedBattleground = 656,
        PlayerSilenced = 657,
        PlayerUnsilenced = 658,
        ComsatDisconnect = 659,
        ComsatReconnectAttempt = 660,
        ComsatConnectFail = 661,
        MailInvalidAttachmentSlot = 662,
        MailTooManyAttachments = 663,
        MailInvalidAttachment = 664,
        MailAttachmentExpired = 665,
        VoiceChatParentalDisableMic = 666,
        ProfaneChatName = 667,
        PlayerSilencedEcho = 668,
        PlayerUnsilencedEcho = 669,
        LootCantLootThat = 670,
        ArenaExpiredCais = 671,
        GroupActionThrottled = 672,
        AlreadyPickpocketed = 673,
        NameInvalid = 674,
        NameNoName = 675,
        NameTooShort = 676,
        NameTooLong = 677,
        NameMixedLanguages = 678,
        NameProfane = 679,
        NameReserved = 680,
        NameThreeConsecutive = 681,
        NameInvalidSpace = 682,
        NameConsecutiveSpaces = 683,
        NameRussianConsecutiveSilentCharacters = 684,
        NameRussianSilentCharacterAtBeginningOrEnd = 685,
        NameDeclensionDoesntMatchBaseName = 686,
        RecruitAFriendNotLinked = 687,
        RecruitAFriendNotNow = 688,
        RecruitAFriendSummonLevelMax = 689,
        RecruitAFriendSummonCooldown = 690,
        RecruitAFriendSummonOffline = 691,
        RecruitAFriendInsufExpanLvl = 692,
        RecruitAFriendMapIncomingTransferNotAllowed = 693,
        NotSameAccount = 694,
        BadOnUseEnchant = 695,
        TradeSelf = 696,
        TooManySockets = 697,
        ItemMaxLimitCategoryCountExceededIs = 698,
        TradeTargetMaxLimitCategoryCountExceededIs = 699,
        ItemMaxLimitCategorySocketedExceededIs = 700,
        ItemMaxLimitCategoryEquippedExceededIs = 701,
        ShapeshiftFormCannotEquip = 702,
        ItemInventoryFullSatchel = 703,
        ScalingStatItemLevelExceeded = 704,
        ScalingStatItemLevelTooLow = 705,
        PurchaseLevelTooLow = 706,
        GroupSwapFailed = 707,
        InviteInCombat = 708,
        InvalidGlyphSlot = 709,
        GenericNoValidTargets = 710,
        CalendarEventAlertS = 711,
        PetLearnSpellS = 712,
        PetLearnAbilityS = 713,
        PetSpellUnlearnedS = 714,
        InviteUnknownRealm = 715,
        InviteNoPartyServer = 716,
        InvitePartyBusy = 717,
        InvitePartyBusyPendingRequest = 718,
        InvitePartyBusyPendingSuggest = 719,
        PartyTargetAmbiguous = 720,
        PartyLfgInviteRaidLocked = 721,
        PartyLfgBootLimit = 722,
        PartyLfgBootCooldownS = 723,
        PartyLfgBootNotEligibleS = 724,
        PartyLfgBootInpatientTimerS = 725,
        PartyLfgBootInProgress = 726,
        PartyLfgBootTooFewPlayers = 727,
        PartyLfgBootVoteSucceeded = 728,
        PartyLfgBootVoteFailed = 729,
        PartyLfgBootDisallowedByMap = 730,
        PartyLfgBootDungeonComplete = 731,
        PartyLfgBootLootRolls = 732,
        PartyLfgBootVoteRegistered = 733,
        PartyPrivateGroupOnly = 734,
        PartyLfgTeleportInCombat = 735,
        PartyTimeRunningSeasonIdMustMatch = 736,
        RaidDisallowedByLevel = 737,
        RaidDisallowedByCrossRealm = 738,
        PartyRoleNotAvailable = 739,
        JoinLfgObjectFailed = 740,
        LfgRemovedLevelup = 741,
        LfgRemovedXpToggle = 742,
        LfgRemovedFactionChange = 743,
        BattlegroundInfoThrottled = 744,
        BattlegroundAlreadyIn = 745,
        ArenaTeamChangeFailedQueued = 746,
        ArenaTeamPermissions = 747,
        NotWhileFalling = 748,
        NotWhileMoving = 749,
        NotWhileFatigued = 750,
        MaxSockets = 751,
        MultiCastActionTotemS = 752,
        BattlegroundJoinLevelup = 753,
        RemoveFromPvpQueueXpGain = 754,
        BattlegroundJoinXpGain = 755,
        BattlegroundJoinMercenary = 756,
        BattlegroundJoinTooManyHealers = 757,
        BattlegroundJoinRatedTooManyHealers = 758,
        BattlegroundJoinTooManyTanks = 759,
        BattlegroundJoinTooManyDamage = 760,
        RaidDifficultyFailed = 761,
        RaidDifficultyChangedS = 762,
        LegacyRaidDifficultyChangedS = 763,
        RaidLockoutChangedS = 764,
        RaidConvertedToParty = 765,
        PartyConvertedToRaid = 766,
        PlayerDifficultyChangedS = 767,
        GmresponseDbError = 768,
        BattlegroundJoinRangeIndex = 769,
        ArenaJoinRangeIndex = 770,
        RemoveFromPvpQueueFactionChange = 771,
        BattlegroundJoinFailed = 772,
        BattlegroundJoinNoValidSpecForRole = 773,
        BattlegroundJoinRespec = 774,
        BattlegroundInvitationDeclined = 775,
        BattlegroundInvitationDeclinedBy = 776,
        BattlegroundJoinTimedOut = 777,
        BattlegroundDupeQueue = 778,
        BattlegroundJoinMustCompleteQuest = 779,
        InBattlegroundRespec = 780,
        MailLimitedDurationItem = 781,
        YellRestrictedTrial = 782,
        ChatRaidRestrictedTrial = 783,
        LfgRoleCheckFailed = 784,
        LfgRoleCheckFailedTimeout = 785,
        LfgRoleCheckFailedNotViable = 786,
        LfgReadyCheckFailed = 787,
        LfgReadyCheckFailedTimeout = 788,
        LfgGroupFull = 789,
        LfgNoLfgObject = 790,
        LfgNoSlotsPlayer = 791,
        LfgNoSlotsParty = 792,
        LfgNoSpec = 793,
        LfgMismatchedSlots = 794,
        LfgMismatchedSlotsLocalXrealm = 795,
        LfgPartyPlayersFromDifferentRealms = 796,
        LfgMembersNotPresent = 797,
        LfgGetInfoTimeout = 798,
        LfgInvalidSlot = 799,
        LfgDeserterPlayer = 800,
        LfgDeserterParty = 801,
        LfgDead = 802,
        LfgRandomCooldownPlayer = 803,
        LfgRandomCooldownParty = 804,
        LfgTooManyMembers = 805,
        LfgTooFewMembers = 806,
        LfgProposalFailed = 807,
        LfgProposalDeclinedSelf = 808,
        LfgProposalDeclinedParty = 809,
        LfgNoSlotsSelected = 810,
        LfgNoRolesSelected = 811,
        LfgRoleCheckInitiated = 812,
        LfgReadyCheckInitiated = 813,
        LfgPlayerDeclinedRoleCheck = 814,
        LfgPlayerDeclinedReadyCheck = 815,
        LfgJoinedQueue = 816,
        LfgJoinedFlexQueue = 817,
        LfgJoinedRfQueue = 818,
        LfgJoinedScenarioQueue = 819,
        LfgJoinedWorldPvpQueue = 820,
        LfgJoinedBattlefieldQueue = 821,
        LfgJoinedList = 822,
        LfgLeftQueue = 823,
        LfgLeftList = 824,
        LfgRoleCheckAborted = 825,
        LfgReadyCheckAborted = 826,
        LfgCantUseBattleground = 827,
        LfgCantUseDungeons = 828,
        LfgReasonTooManyLfg = 829,
        LfgFarmLimit = 830,
        LfgNoCrossFactionParties = 831,
        InvalidTeleportLocation = 832,
        TooFarToInteract = 833,
        BattlegroundPlayersFromDifferentRealms = 834,
        DifficultyChangeCooldownS = 835,
        DifficultyChangeCombatCooldownS = 836,
        DifficultyChangeWorldstate = 837,
        DifficultyChangeEncounter = 838,
        DifficultyChangeCombat = 839,
        DifficultyChangePlayerBusy = 840,
        DifficultyChangePlayerOnVehicle = 841,
        DifficultyChangeAlreadyStarted = 842,
        DifficultyChangeOtherHeroicS = 843,
        DifficultyChangeHeroicInstanceAlreadyRunning = 844,
        ArenaTeamPartySize = 845,
        SoloShuffleWargameGroupSize = 846,
        SoloShuffleWargameGroupComp = 847,
        SoloRbgWargameGroupSize = 848,
        SoloRbgWargameGroupComp = 849,
        SoloMinItemLevel = 850,
        PvpPlayerAbandoned = 851,
        BattlegroundJoinGroupQueueWithoutHealer = 852,
        QuestForceRemovedS = 853,
        AttackNoActions = 854,
        InRandomBg = 855,
        InNonRandomBg = 856,
        BnFriendSelf = 857,
        BnFriendAlready = 858,
        BnFriendBlocked = 859,
        BnFriendListFull = 860,
        BnFriendRequestSent = 861,
        BnBroadcastThrottle = 862,
        BgDeveloperOnly = 863,
        CurrencySpellSlotMismatch = 864,
        CurrencyNotTradable = 865,
        RequiresExpansionS = 866,
        QuestFailedSpell = 867,
        TalentFailedUnspentTalentPoints = 868,
        TalentFailedNotEnoughTalentsInPrimaryTree = 869,
        TalentFailedNoPrimaryTreeSelected = 870,
        TalentFailedCantRemoveTalent = 871,
        TalentFailedUnknown = 872,
        TalentFailedInCombat = 873,
        TalentFailedInPvpMatch = 874,
        TalentFailedInMythicPlus = 875,
        WargameRequestFailure = 876,
        RankRequiresAuthenticator = 877,
        GuildBankVoucherFailed = 878,
        WargameRequestSent = 879,
        RequiresAchievementI = 880,
        RefundResultExceedMaxCurrency = 881,
        CantBuyQuantity = 882,
        ItemIsBattlePayLocked = 883,
        PartyAlreadyInBattlegroundQueue = 884,
        PartyConfirmingBattlegroundQueue = 885,
        BattlefieldTeamPartySize = 886,
        InsuffTrackedCurrencyIs = 887,
        NotOnTournamentRealm = 888,
        GuildTrialAccountTrial = 889,
        GuildTrialAccountVeteran = 890,
        GuildUndeletableDueToLevel = 891,
        CantDoThatInAGroup = 892,
        GuildLeaderReplaced = 893,
        TransmogrifyCantEquip = 894,
        TransmogrifyInvalidItemType = 895,
        TransmogrifyNotSoulbound = 896,
        TransmogrifyInvalidSource = 897,
        TransmogrifyInvalidDestination = 898,
        TransmogrifyMismatch = 899,
        TransmogrifyLegendary = 900,
        TransmogrifySameItem = 901,
        TransmogrifySameAppearance = 902,
        TransmogrifyNotEquipped = 903,
        VoidDepositFull = 904,
        VoidWithdrawFull = 905,
        VoidStorageWrapped = 906,
        VoidStorageStackable = 907,
        VoidStorageUnbound = 908,
        VoidStorageRepair = 909,
        VoidStorageCharges = 910,
        VoidStorageQuest = 911,
        VoidStorageConjured = 912,
        VoidStorageMail = 913,
        VoidStorageBag = 914,
        VoidTransferStorageFull = 915,
        VoidTransferInvFull = 916,
        VoidTransferInternalError = 917,
        VoidTransferItemInvalid = 918,
        DifficultyDisabledInLfg = 919,
        VoidStorageUnique = 920,
        VoidStorageLoot = 921,
        VoidStorageHoliday = 922,
        VoidStorageDuration = 923,
        VoidStorageLoadFailed = 924,
        VoidStorageInvalidItem = 925,
        VoidStorageAccountItem = 926,
        ParentalControlsChatMuted = 927,
        SorStartExperienceIncomplete = 928,
        SorInvalidEmail = 929,
        SorInvalidComment = 930,
        ChallengeModeResetCooldownS = 931,
        ChallengeModeResetKeystone = 932,
        PetJournalAlreadyInLoadout = 933,
        ReportSubmittedSuccessfully = 934,
        ReportSubmissionFailed = 935,
        SuggestionSubmittedSuccessfully = 936,
        BugSubmittedSuccessfully = 937,
        ChallengeModeEnabled = 938,
        ChallengeModeDisabled = 939,
        PetbattleCreateFailed = 940,
        PetbattleNotHere = 941,
        PetbattleNotHereOnTransport = 942,
        PetbattleNotHereUnevenGround = 943,
        PetbattleNotHereObstructed = 944,
        PetbattleNotWhileInCombat = 945,
        PetbattleNotWhileDead = 946,
        PetbattleNotWhileFlying = 947,
        PetbattleTargetInvalid = 948,
        PetbattleTargetOutOfRange = 949,
        PetbattleTargetNotCapturable = 950,
        PetbattleNotATrainer = 951,
        PetbattleDeclined = 952,
        PetbattleInBattle = 953,
        PetbattleInvalidLoadout = 954,
        PetbattleAllPetsDead = 955,
        PetbattleNoPetsInSlots = 956,
        PetbattleNoAccountLock = 957,
        PetbattleWildPetTapped = 958,
        PetbattleRestrictedAccount = 959,
        PetbattleOpponentNotAvailable = 960,
        PetbattleNotWhileInMatchedBattle = 961,
        CantHaveMorePetsOfThatType = 962,
        CantHaveMorePets = 963,
        PvpMapNotFound = 964,
        PvpMapNotSet = 965,
        PetbattleQueueQueued = 966,
        PetbattleQueueAlreadyQueued = 967,
        PetbattleQueueJoinFailed = 968,
        PetbattleQueueJournalLock = 969,
        PetbattleQueueRemoved = 970,
        PetbattleQueueProposalDeclined = 971,
        PetbattleQueueProposalTimeout = 972,
        PetbattleQueueOpponentDeclined = 973,
        PetbattleQueueRequeuedInternal = 974,
        PetbattleQueueRequeuedRemoved = 975,
        PetbattleQueueSlotLocked = 976,
        PetbattleQueueSlotEmpty = 977,
        PetbattleQueueSlotNoTracker = 978,
        PetbattleQueueSlotNoSpecies = 979,
        PetbattleQueueSlotCantBattle = 980,
        PetbattleQueueSlotRevoked = 981,
        PetbattleQueueSlotDead = 982,
        PetbattleQueueSlotNoPet = 983,
        PetbattleQueueNotWhileNeutral = 984,
        PetbattleGameTimeLimitWarning = 985,
        PetbattleGameRoundsLimitWarning = 986,
        HasRestriction = 987,
        ItemUpgradeItemTooLowLevel = 988,
        ItemUpgradeNoPath = 989,
        ItemUpgradeNoMoreUpgrades = 990,
        BonusRollEmpty = 991,
        ChallengeModeFull = 992,
        ChallengeModeInProgress = 993,
        ChallengeModeIncorrectKeystone = 994,
        BattletagFriendNotFound = 995,
        BattletagFriendNotValid = 996,
        BattletagFriendNotAllowed = 997,
        BattletagFriendThrottled = 998,
        BattletagFriendSuccess = 999,
        PetTooHighLevelToUncage = 1000,
        PetbattleInternal = 1001,
        CantCagePetYet = 1002,
        NoLootInChallengeMode = 1003,
        QuestPetBattleVictoriesPvpIi = 1004,
        RoleCheckAlreadyInProgress = 1005,
        RecruitAFriendAccountLimit = 1006,
        RecruitAFriendFailed = 1007,
        SetLootPersonal = 1008,
        SetLootMethodFailedCombat = 1009,
        ReagentBankFull = 1010,
        ReagentBankLocked = 1011,
        GarrisonBuildingExists = 1012,
        GarrisonInvalidPlot = 1013,
        GarrisonInvalidBuildingid = 1014,
        GarrisonInvalidPlotBuilding = 1015,
        GarrisonRequiresBlueprint = 1016,
        GarrisonNotEnoughCurrency = 1017,
        GarrisonNotEnoughGold = 1018,
        GarrisonCompleteMissionWrongFollowerType = 1019,
        AlreadyUsingLfgList = 1020,
        RestrictedAccountLfgListTrial = 1021,
        ToyUseLimitReached = 1022,
        ToyAlreadyKnown = 1023,
        TransmogSetAlreadyKnown = 1024,
        NotEnoughCurrency = 1025,
        SpecIsDisabled = 1026,
        FeatureRestrictedTrial = 1027,
        CantBeObliterated = 1028,
        CantBeScrapped = 1029,
        CantBeRecrafted = 1030,
        ArtifactRelicDoesNotMatchArtifact = 1031,
        MustEquipArtifact = 1032,
        CantDoThatRightNow = 1033,
        AffectingCombat = 1034,
        EquipmentManagerCombatSwapS = 1035,
        EquipmentManagerBagsFull = 1036,
        EquipmentManagerMissingItemS = 1037,
        MovieRecordingWarningPerf = 1038,
        MovieRecordingWarningDiskFull = 1039,
        MovieRecordingWarningNoMovie = 1040,
        MovieRecordingWarningRequirements = 1041,
        MovieRecordingWarningCompressing = 1042,
        NoChallengeModeReward = 1043,
        ClaimedChallengeModeReward = 1044,
        ChallengeModePeriodResetSs = 1045,
        CantDoThatChallengeModeActive = 1046,
        TalentFailedRestArea = 1047,
        CannotAbandonLastPet = 1048,
        TestCvarSetSss = 1049,
        QuestTurnInFailReason = 1050,
        ClaimedChallengeModeRewardOld = 1051,
        TalentGrantedByAura = 1052,
        ChallengeModeAlreadyComplete = 1053,
        GlyphTargetNotAvailable = 1054,
        PvpWarmodeToggleOn = 1055,
        PvpWarmodeToggleOff = 1056,
        SpellFailedLevelRequirement = 1057,
        SpellFailedCantFlyHere = 1058,
        BattlegroundJoinRequiresLevel = 1059,
        BattlegroundJoinDisqualified = 1060,
        BattlegroundJoinDisqualifiedNoName = 1061,
        VoiceChatGenericUnableToConnect = 1062,
        VoiceChatServiceLost = 1063,
        VoiceChatChannelNameTooShort = 1064,
        VoiceChatChannelNameTooLong = 1065,
        VoiceChatChannelAlreadyExists = 1066,
        VoiceChatTargetNotFound = 1067,
        VoiceChatTooManyRequests = 1068,
        VoiceChatPlayerSilenced = 1069,
        VoiceChatParentalDisableAll = 1070,
        VoiceChatDisabled = 1071,
        NoPvpReward = 1072,
        ClaimedPvpReward = 1073,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1074,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1075,
        AzeriteEssenceSelectionFailedConditionFailed = 1076,
        AzeriteEssenceSelectionFailedRestArea = 1077,
        AzeriteEssenceSelectionFailedSlotLocked = 1078,
        AzeriteEssenceSelectionFailedNotAtForge = 1079,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1080,
        AzeriteEssenceSelectionFailedNotEquipped = 1081,
        SocketingRequiresPunchcardredGem = 1082,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1083,
        SocketingRequiresPunchcardyellowGem = 1084,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1085,
        SocketingRequiresPunchcardblueGem = 1086,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1087,
        SocketingRequiresDominationShard = 1088,
        SocketingDominationShardOnlyInDominationslot = 1089,
        SocketingRequiresCypherGem = 1090,
        SocketingCypherGemOnlyInCypherslot = 1091,
        SocketingRequiresTinkerGem = 1092,
        SocketingTinkerGemOnlyInTinkerslot = 1093,
        SocketingRequiresPrimordialGem = 1094,
        SocketingPrimordialGemOnlyInPrimordialslot = 1095,
        SocketingRequiresFragranceGem = 1096,
        SocketingFragranceGemOnlyInFragranceslot = 1097,
        LevelLinkingResultLinked = 1098,
        LevelLinkingResultUnlinked = 1099,
        ClubFinderErrorPostClub = 1100,
        ClubFinderErrorApplyClub = 1101,
        ClubFinderErrorRespondApplicant = 1102,
        ClubFinderErrorCancelApplication = 1103,
        ClubFinderErrorTypeAcceptApplication = 1104,
        ClubFinderErrorTypeNoInvitePermissions = 1105,
        ClubFinderErrorTypeNoPostingPermissions = 1106,
        ClubFinderErrorTypeApplicantList = 1107,
        ClubFinderErrorTypeApplicantListNoPerm = 1108,
        ClubFinderErrorTypeFinderNotAvailable = 1109,
        ClubFinderErrorTypeGetPostingIds = 1110,
        ClubFinderErrorTypeJoinApplication = 1111,
        ClubFinderErrorTypeRealmNotEligible = 1112,
        ClubFinderErrorTypeFlaggedRename = 1113,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1114,
        ItemInteractionNotEnoughGold = 1115,
        ItemInteractionNotEnoughCurrency = 1116,
        ItemInteractionNoConversionOutput = 1117,
        PlayerChoiceErrorPendingChoice = 1118,
        SoulbindInvalidConduit = 1119,
        SoulbindInvalidConduitItem = 1120,
        SoulbindInvalidTalent = 1121,
        SoulbindDuplicateConduit = 1122,
        ActivateSoulbindS = 1123,
        ActivateSoulbindFailedRestArea = 1124,
        CantUseProfanity = 1125,
        NotInPetBattle = 1126,
        NotInNpe = 1127,
        NoSpec = 1128,
        NoDominationshardOverwrite = 1129,
        UseWeeklyRewardsDisabled = 1130,
        CrossFactionGroupJoined = 1131,
        CantTargetUnfriendlyInOverworld = 1132,
        EquipablespellsSlotsFull = 1133,
        ItemModAppearanceGroupAlreadyKnown = 1134,
        CantBulkSellItemWithRefund = 1135,
        NoSoulboundItemInAccountBank = 1136,
        NoRefundableItemInAccountBank = 1137,
        CantDeleteInAccountBank = 1138,
        NoImmediateContainerInAccountBank = 1139,
        NoOpenImmediateContainerInAccountBank = 1140,
        CantTradeAccountItem = 1141,
        NoAccountInventoryLock = 1142,
        BankNotAccessible = 1143,
        TooManyAccountBankTabs = 1144,
        AccountBankTabNotUnlocked = 1145,
        BankTabInvalidName = 1146,
        BankTabInvalidText = 1147,
        WowLabsPartyErrorTypePartyIsFull = 1148,
        WowLabsPartyErrorTypeMaxInviteSent = 1149,
        WowLabsPartyErrorTypePlayerAlreadyInvited = 1150,
        WowLabsPartyErrorTypePartyInviteInvalid = 1151,
        WowLabsLobbyMatchmakerErrorEnterQueueFailed = 1152,
        WowLabsLobbyMatchmakerErrorLeaveQueueFailed = 1153,
        TargetIsSelfFoundCannotTrade = 1154,
        PlayerIsSelfFoundCannotTrade = 1155,
        MailRecepientIsSelfFoundCannotReceiveMail = 1156,
        PlayerIsSelfFoundCannotSendMail = 1157,
        PlayerIsSelfFoundCannotUseAuctionHouse = 1158,
        MailTargetCannotReceiveMail = 1159,
        RemixInvalidTransferRequest = 1160,
        CurrencyTransferInvalidCharacter = 1161,
        CurrencyTransferInvalidCurrency = 1162,
        CurrencyTransferInsufficientCurrency = 1163,
        CurrencyTransferMaxQuantity = 1164,
        CurrencyTransferNoValidSource = 1165,
        CurrencyTransferCharacterLoggedIn = 1166,
        CurrencyTransferServerError = 1167,
        CurrencyTransferUnmetRequirements = 1168,
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
