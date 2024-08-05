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
        QuestPushHighFactionS = 494,
        QuestPushHighFactionToRecipientS = 495,
        QuestPushExpansionS = 496,
        QuestPushExpansionToRecipientS = 497,
        QuestPushNotGarrisonOwnerS = 498,
        QuestPushNotGarrisonOwnerToRecipientS = 499,
        QuestPushWrongCovenantS = 500,
        QuestPushWrongCovenantToRecipientS = 501,
        QuestPushNewPlayerExperienceS = 502,
        QuestPushNewPlayerExperienceToRecipientS = 503,
        QuestPushWrongFactionS = 504,
        QuestPushWrongFactionToRecipientS = 505,
        QuestPushCrossFactionRestrictedS = 506,
        RaidGroupLowlevel = 507,
        RaidGroupOnly = 508,
        RaidGroupFull = 509,
        RaidGroupRequirementsUnmatch = 510,
        CorpseIsNotInInstance = 511,
        PvpKillHonorable = 512,
        PvpKillDishonorable = 513,
        SpellFailedAlreadyAtFullHealth = 514,
        SpellFailedAlreadyAtFullMana = 515,
        SpellFailedAlreadyAtFullPowerS = 516,
        AutolootMoneyS = 517,
        GenericStunned = 518,
        GenericThrottle = 519,
        ClubFinderSearchingTooFast = 520,
        TargetStunned = 521,
        MustRepairDurability = 522,
        RaidYouJoined = 523,
        RaidYouLeft = 524,
        InstanceGroupJoinedWithParty = 525,
        InstanceGroupJoinedWithRaid = 526,
        RaidMemberAddedS = 527,
        RaidMemberRemovedS = 528,
        InstanceGroupAddedS = 529,
        InstanceGroupRemovedS = 530,
        ClickOnItemToFeed = 531,
        TooManyChatChannels = 532,
        LootRollPending = 533,
        LootPlayerNotFound = 534,
        NotInRaid = 535,
        LoggingOut = 536,
        TargetLoggingOut = 537,
        NotWhileMounted = 538,
        NotWhileShapeshifted = 539,
        NotInCombat = 540,
        NotWhileDisarmed = 541,
        PetBroken = 542,
        TalentWipeError = 543,
        SpecWipeError = 544,
        GlyphWipeError = 545,
        PetSpecWipeError = 546,
        FeignDeathResisted = 547,
        MeetingStoneInQueueS = 548,
        MeetingStoneLeftQueueS = 549,
        MeetingStoneOtherMemberLeft = 550,
        MeetingStonePartyKickedFromQueue = 551,
        MeetingStoneMemberStillInQueue = 552,
        MeetingStoneSuccess = 553,
        MeetingStoneInProgress = 554,
        MeetingStoneMemberAddedS = 555,
        MeetingStoneGroupFull = 556,
        MeetingStoneNotLeader = 557,
        MeetingStoneInvalidLevel = 558,
        MeetingStoneTargetNotInParty = 559,
        MeetingStoneTargetInvalidLevel = 560,
        MeetingStoneMustBeLeader = 561,
        MeetingStoneNoRaidGroup = 562,
        MeetingStoneNeedParty = 563,
        MeetingStoneNotFound = 564,
        MeetingStoneTargetInVehicle = 565,
        GuildemblemSame = 566,
        EquipTradeItem = 567,
        PvpToggleOn = 568,
        PvpToggleOff = 569,
        GroupJoinBattlegroundDeserters = 570,
        GroupJoinBattlegroundDead = 571,
        GroupJoinBattlegroundS = 572,
        GroupJoinBattlegroundFail = 573,
        GroupJoinBattlegroundTooMany = 574,
        SoloJoinBattlegroundS = 575,
        JoinSingleScenarioS = 576,
        BattlegroundTooManyQueues = 577,
        BattlegroundCannotQueueForRated = 578,
        BattledgroundQueuedForRated = 579,
        BattlegroundTeamLeftQueue = 580,
        BattlegroundNotInBattleground = 581,
        AlreadyInArenaTeamS = 582,
        InvalidPromotionCode = 583,
        BgPlayerJoinedSs = 584,
        BgPlayerLeftS = 585,
        RestrictedAccount = 586,
        RestrictedAccountTrial = 587,
        PlayTimeExceeded = 588,
        ApproachingPartialPlayTime = 589,
        ApproachingPartialPlayTime2 = 590,
        ApproachingNoPlayTime = 591,
        ApproachingNoPlayTime2 = 592,
        UnhealthyTime = 593,
        ChatRestrictedTrial = 594,
        ChatThrottled = 595,
        MailReachedCap = 596,
        InvalidRaidTarget = 597,
        RaidLeaderReadyCheckStartS = 598,
        ReadyCheckInProgress = 599,
        ReadyCheckThrottled = 600,
        DungeonDifficultyFailed = 601,
        DungeonDifficultyChangedS = 602,
        TradeWrongRealm = 603,
        TradeNotOnTaplist = 604,
        ChatPlayerAmbiguousS = 605,
        LootCantLootThatNow = 606,
        LootMasterInvFull = 607,
        LootMasterUniqueItem = 608,
        LootMasterOther = 609,
        FilteringYouS = 610,
        UsePreventedByMechanicS = 611,
        ItemUniqueEquippable = 612,
        LfgLeaderIsLfmS = 613,
        LfgPending = 614,
        CantSpeakLangage = 615,
        VendorMissingTurnins = 616,
        BattlegroundNotInTeam = 617,
        NotInBattleground = 618,
        NotEnoughHonorPoints = 619,
        NotEnoughArenaPoints = 620,
        SocketingRequiresMetaGem = 621,
        SocketingMetaGemOnlyInMetaslot = 622,
        SocketingRequiresHydraulicGem = 623,
        SocketingHydraulicGemOnlyInHydraulicslot = 624,
        SocketingRequiresCogwheelGem = 625,
        SocketingCogwheelGemOnlyInCogwheelslot = 626,
        SocketingItemTooLowLevel = 627,
        ItemMaxCountSocketed = 628,
        SystemDisabled = 629,
        QuestFailedTooManyDailyQuestsI = 630,
        ItemMaxCountEquippedSocketed = 631,
        ItemUniqueEquippableSocketed = 632,
        UserSquelched = 633,
        AccountSilenced = 634,
        PartyMemberSilenced = 635,
        PartyMemberSilencedLfgDelist = 636,
        TooMuchGold = 637,
        NotBarberSitting = 638,
        QuestFailedCais = 639,
        InviteRestrictedTrial = 640,
        VoiceIgnoreFull = 641,
        VoiceIgnoreSelf = 642,
        VoiceIgnoreNotFound = 643,
        VoiceIgnoreAlreadyS = 644,
        VoiceIgnoreAddedS = 645,
        VoiceIgnoreRemovedS = 646,
        VoiceIgnoreAmbiguous = 647,
        VoiceIgnoreDeleted = 648,
        UnknownMacroOptionS = 649,
        NotDuringArenaMatch = 650,
        NotInRatedBattleground = 651,
        PlayerSilenced = 652,
        PlayerUnsilenced = 653,
        ComsatDisconnect = 654,
        ComsatReconnectAttempt = 655,
        ComsatConnectFail = 656,
        MailInvalidAttachmentSlot = 657,
        MailTooManyAttachments = 658,
        MailInvalidAttachment = 659,
        MailAttachmentExpired = 660,
        VoiceChatParentalDisableMic = 661,
        ProfaneChatName = 662,
        PlayerSilencedEcho = 663,
        PlayerUnsilencedEcho = 664,
        LootCantLootThat = 665,
        ArenaExpiredCais = 666,
        GroupActionThrottled = 667,
        AlreadyPickpocketed = 668,
        NameInvalid = 669,
        NameNoName = 670,
        NameTooShort = 671,
        NameTooLong = 672,
        NameMixedLanguages = 673,
        NameProfane = 674,
        NameReserved = 675,
        NameThreeConsecutive = 676,
        NameInvalidSpace = 677,
        NameConsecutiveSpaces = 678,
        NameRussianConsecutiveSilentCharacters = 679,
        NameRussianSilentCharacterAtBeginningOrEnd = 680,
        NameDeclensionDoesntMatchBaseName = 681,
        RecruitAFriendNotLinked = 682,
        RecruitAFriendNotNow = 683,
        RecruitAFriendSummonLevelMax = 684,
        RecruitAFriendSummonCooldown = 685,
        RecruitAFriendSummonOffline = 686,
        RecruitAFriendInsufExpanLvl = 687,
        RecruitAFriendMapIncomingTransferNotAllowed = 688,
        NotSameAccount = 689,
        BadOnUseEnchant = 690,
        TradeSelf = 691,
        TooManySockets = 692,
        ItemMaxLimitCategoryCountExceededIs = 693,
        TradeTargetMaxLimitCategoryCountExceededIs = 694,
        ItemMaxLimitCategorySocketedExceededIs = 695,
        ItemMaxLimitCategoryEquippedExceededIs = 696,
        ShapeshiftFormCannotEquip = 697,
        ItemInventoryFullSatchel = 698,
        ScalingStatItemLevelExceeded = 699,
        ScalingStatItemLevelTooLow = 700,
        PurchaseLevelTooLow = 701,
        GroupSwapFailed = 702,
        InviteInCombat = 703,
        InvalidGlyphSlot = 704,
        GenericNoValidTargets = 705,
        CalendarEventAlertS = 706,
        PetLearnSpellS = 707,
        PetLearnAbilityS = 708,
        PetSpellUnlearnedS = 709,
        InviteUnknownRealm = 710,
        InviteNoPartyServer = 711,
        InvitePartyBusy = 712,
        InvitePartyBusyPendingRequest = 713,
        InvitePartyBusyPendingSuggest = 714,
        PartyTargetAmbiguous = 715,
        PartyLfgInviteRaidLocked = 716,
        PartyLfgBootLimit = 717,
        PartyLfgBootCooldownS = 718,
        PartyLfgBootNotEligibleS = 719,
        PartyLfgBootInpatientTimerS = 720,
        PartyLfgBootInProgress = 721,
        PartyLfgBootTooFewPlayers = 722,
        PartyLfgBootVoteSucceeded = 723,
        PartyLfgBootVoteFailed = 724,
        PartyLfgBootInCombat = 725,
        PartyLfgBootDungeonComplete = 726,
        PartyLfgBootLootRolls = 727,
        PartyLfgBootVoteRegistered = 728,
        PartyPrivateGroupOnly = 729,
        PartyLfgTeleportInCombat = 730,
        RaidDisallowedByLevel = 731,
        RaidDisallowedByCrossRealm = 732,
        PartyRoleNotAvailable = 733,
        JoinLfgObjectFailed = 734,
        LfgRemovedLevelup = 735,
        LfgRemovedXpToggle = 736,
        LfgRemovedFactionChange = 737,
        BattlegroundInfoThrottled = 738,
        BattlegroundAlreadyIn = 739,
        ArenaTeamChangeFailedQueued = 740,
        ArenaTeamPermissions = 741,
        NotWhileFalling = 742,
        NotWhileMoving = 743,
        NotWhileFatigued = 744,
        MaxSockets = 745,
        MultiCastActionTotemS = 746,
        BattlegroundJoinLevelup = 747,
        RemoveFromPvpQueueXpGain = 748,
        BattlegroundJoinXpGain = 749,
        BattlegroundJoinMercenary = 750,
        BattlegroundJoinTooManyHealers = 751,
        BattlegroundJoinRatedTooManyHealers = 752,
        BattlegroundJoinTooManyTanks = 753,
        BattlegroundJoinTooManyDamage = 754,
        RaidDifficultyFailed = 755,
        RaidDifficultyChangedS = 756,
        LegacyRaidDifficultyChangedS = 757,
        RaidLockoutChangedS = 758,
        RaidConvertedToParty = 759,
        PartyConvertedToRaid = 760,
        PlayerDifficultyChangedS = 761,
        GmresponseDbError = 762,
        BattlegroundJoinRangeIndex = 763,
        ArenaJoinRangeIndex = 764,
        RemoveFromPvpQueueFactionChange = 765,
        BattlegroundJoinFailed = 766,
        BattlegroundJoinNoValidSpecForRole = 767,
        BattlegroundJoinRespec = 768,
        BattlegroundInvitationDeclined = 769,
        BattlegroundInvitationDeclinedBy = 770,
        BattlegroundJoinTimedOut = 771,
        BattlegroundDupeQueue = 772,
        BattlegroundJoinMustCompleteQuest = 773,
        InBattlegroundRespec = 774,
        MailLimitedDurationItem = 775,
        YellRestrictedTrial = 776,
        ChatRaidRestrictedTrial = 777,
        LfgRoleCheckFailed = 778,
        LfgRoleCheckFailedTimeout = 779,
        LfgRoleCheckFailedNotViable = 780,
        LfgReadyCheckFailed = 781,
        LfgReadyCheckFailedTimeout = 782,
        LfgGroupFull = 783,
        LfgNoLfgObject = 784,
        LfgNoSlotsPlayer = 785,
        LfgNoSlotsParty = 786,
        LfgNoSpec = 787,
        LfgMismatchedSlots = 788,
        LfgMismatchedSlotsLocalXrealm = 789,
        LfgPartyPlayersFromDifferentRealms = 790,
        LfgMembersNotPresent = 791,
        LfgGetInfoTimeout = 792,
        LfgInvalidSlot = 793,
        LfgDeserterPlayer = 794,
        LfgDeserterParty = 795,
        LfgDead = 796,
        LfgRandomCooldownPlayer = 797,
        LfgRandomCooldownParty = 798,
        LfgTooManyMembers = 799,
        LfgTooFewMembers = 800,
        LfgProposalFailed = 801,
        LfgProposalDeclinedSelf = 802,
        LfgProposalDeclinedParty = 803,
        LfgNoSlotsSelected = 804,
        LfgNoRolesSelected = 805,
        LfgRoleCheckInitiated = 806,
        LfgReadyCheckInitiated = 807,
        LfgPlayerDeclinedRoleCheck = 808,
        LfgPlayerDeclinedReadyCheck = 809,
        LfgJoinedQueue = 810,
        LfgJoinedFlexQueue = 811,
        LfgJoinedRfQueue = 812,
        LfgJoinedScenarioQueue = 813,
        LfgJoinedWorldPvpQueue = 814,
        LfgJoinedBattlefieldQueue = 815,
        LfgJoinedList = 816,
        LfgLeftQueue = 817,
        LfgLeftList = 818,
        LfgRoleCheckAborted = 819,
        LfgReadyCheckAborted = 820,
        LfgCantUseBattleground = 821,
        LfgCantUseDungeons = 822,
        LfgReasonTooManyLfg = 823,
        LfgFarmLimit = 824,
        LfgNoCrossFactionParties = 825,
        InvalidTeleportLocation = 826,
        TooFarToInteract = 827,
        BattlegroundPlayersFromDifferentRealms = 828,
        DifficultyChangeCooldownS = 829,
        DifficultyChangeCombatCooldownS = 830,
        DifficultyChangeWorldstate = 831,
        DifficultyChangeEncounter = 832,
        DifficultyChangeCombat = 833,
        DifficultyChangePlayerBusy = 834,
        DifficultyChangePlayerOnVehicle = 835,
        DifficultyChangeAlreadyStarted = 836,
        DifficultyChangeOtherHeroicS = 837,
        DifficultyChangeHeroicInstanceAlreadyRunning = 838,
        ArenaTeamPartySize = 839,
        SoloShuffleWargameGroupSize = 840,
        SoloShuffleWargameGroupComp = 841,
        SoloMinItemLevel = 842,
        PvpPlayerAbandoned = 843,
        BattlegroundJoinGroupQueueWithoutHealer = 844,
        QuestForceRemovedS = 845,
        AttackNoActions = 846,
        InRandomBg = 847,
        InNonRandomBg = 848,
        BnFriendSelf = 849,
        BnFriendAlready = 850,
        BnFriendBlocked = 851,
        BnFriendListFull = 852,
        BnFriendRequestSent = 853,
        BnBroadcastThrottle = 854,
        BgDeveloperOnly = 855,
        CurrencySpellSlotMismatch = 856,
        CurrencyNotTradable = 857,
        RequiresExpansionS = 858,
        QuestFailedSpell = 859,
        TalentFailedUnspentTalentPoints = 860,
        TalentFailedNotEnoughTalentsInPrimaryTree = 861,
        TalentFailedNoPrimaryTreeSelected = 862,
        TalentFailedCantRemoveTalent = 863,
        TalentFailedUnknown = 864,
        TalentFailedInCombat = 865,
        TalentFailedInPvpMatch = 866,
        TalentFailedInMythicPlus = 867,
        WargameRequestFailure = 868,
        RankRequiresAuthenticator = 869,
        GuildBankVoucherFailed = 870,
        WargameRequestSent = 871,
        RequiresAchievementI = 872,
        RefundResultExceedMaxCurrency = 873,
        CantBuyQuantity = 874,
        ItemIsBattlePayLocked = 875,
        PartyAlreadyInBattlegroundQueue = 876,
        PartyConfirmingBattlegroundQueue = 877,
        BattlefieldTeamPartySize = 878,
        InsuffTrackedCurrencyIs = 879,
        NotOnTournamentRealm = 880,
        GuildTrialAccountTrial = 881,
        GuildTrialAccountVeteran = 882,
        GuildUndeletableDueToLevel = 883,
        CantDoThatInAGroup = 884,
        GuildLeaderReplaced = 885,
        TransmogrifyCantEquip = 886,
        TransmogrifyInvalidItemType = 887,
        TransmogrifyNotSoulbound = 888,
        TransmogrifyInvalidSource = 889,
        TransmogrifyInvalidDestination = 890,
        TransmogrifyMismatch = 891,
        TransmogrifyLegendary = 892,
        TransmogrifySameItem = 893,
        TransmogrifySameAppearance = 894,
        TransmogrifyNotEquipped = 895,
        VoidDepositFull = 896,
        VoidWithdrawFull = 897,
        VoidStorageWrapped = 898,
        VoidStorageStackable = 899,
        VoidStorageUnbound = 900,
        VoidStorageRepair = 901,
        VoidStorageCharges = 902,
        VoidStorageQuest = 903,
        VoidStorageConjured = 904,
        VoidStorageMail = 905,
        VoidStorageBag = 906,
        VoidTransferStorageFull = 907,
        VoidTransferInvFull = 908,
        VoidTransferInternalError = 909,
        VoidTransferItemInvalid = 910,
        DifficultyDisabledInLfg = 911,
        VoidStorageUnique = 912,
        VoidStorageLoot = 913,
        VoidStorageHoliday = 914,
        VoidStorageDuration = 915,
        VoidStorageLoadFailed = 916,
        VoidStorageInvalidItem = 917,
        ParentalControlsChatMuted = 918,
        SorStartExperienceIncomplete = 919,
        SorInvalidEmail = 920,
        SorInvalidComment = 921,
        ChallengeModeResetCooldownS = 922,
        ChallengeModeResetKeystone = 923,
        PetJournalAlreadyInLoadout = 924,
        ReportSubmittedSuccessfully = 925,
        ReportSubmissionFailed = 926,
        SuggestionSubmittedSuccessfully = 927,
        BugSubmittedSuccessfully = 928,
        ChallengeModeEnabled = 929,
        ChallengeModeDisabled = 930,
        PetbattleCreateFailed = 931,
        PetbattleNotHere = 932,
        PetbattleNotHereOnTransport = 933,
        PetbattleNotHereUnevenGround = 934,
        PetbattleNotHereObstructed = 935,
        PetbattleNotWhileInCombat = 936,
        PetbattleNotWhileDead = 937,
        PetbattleNotWhileFlying = 938,
        PetbattleTargetInvalid = 939,
        PetbattleTargetOutOfRange = 940,
        PetbattleTargetNotCapturable = 941,
        PetbattleNotATrainer = 942,
        PetbattleDeclined = 943,
        PetbattleInBattle = 944,
        PetbattleInvalidLoadout = 945,
        PetbattleAllPetsDead = 946,
        PetbattleNoPetsInSlots = 947,
        PetbattleNoAccountLock = 948,
        PetbattleWildPetTapped = 949,
        PetbattleRestrictedAccount = 950,
        PetbattleOpponentNotAvailable = 951,
        PetbattleNotWhileInMatchedBattle = 952,
        CantHaveMorePetsOfThatType = 953,
        CantHaveMorePets = 954,
        PvpMapNotFound = 955,
        PvpMapNotSet = 956,
        PetbattleQueueQueued = 957,
        PetbattleQueueAlreadyQueued = 958,
        PetbattleQueueJoinFailed = 959,
        PetbattleQueueJournalLock = 960,
        PetbattleQueueRemoved = 961,
        PetbattleQueueProposalDeclined = 962,
        PetbattleQueueProposalTimeout = 963,
        PetbattleQueueOpponentDeclined = 964,
        PetbattleQueueRequeuedInternal = 965,
        PetbattleQueueRequeuedRemoved = 966,
        PetbattleQueueSlotLocked = 967,
        PetbattleQueueSlotEmpty = 968,
        PetbattleQueueSlotNoTracker = 969,
        PetbattleQueueSlotNoSpecies = 970,
        PetbattleQueueSlotCantBattle = 971,
        PetbattleQueueSlotRevoked = 972,
        PetbattleQueueSlotDead = 973,
        PetbattleQueueSlotNoPet = 974,
        PetbattleQueueNotWhileNeutral = 975,
        PetbattleGameTimeLimitWarning = 976,
        PetbattleGameRoundsLimitWarning = 977,
        HasRestriction = 978,
        ItemUpgradeItemTooLowLevel = 979,
        ItemUpgradeNoPath = 980,
        ItemUpgradeNoMoreUpgrades = 981,
        BonusRollEmpty = 982,
        ChallengeModeFull = 983,
        ChallengeModeInProgress = 984,
        ChallengeModeIncorrectKeystone = 985,
        BattletagFriendNotFound = 986,
        BattletagFriendNotValid = 987,
        BattletagFriendNotAllowed = 988,
        BattletagFriendThrottled = 989,
        BattletagFriendSuccess = 990,
        PetTooHighLevelToUncage = 991,
        PetbattleInternal = 992,
        CantCagePetYet = 993,
        NoLootInChallengeMode = 994,
        QuestPetBattleVictoriesPvpIi = 995,
        RoleCheckAlreadyInProgress = 996,
        RecruitAFriendAccountLimit = 997,
        RecruitAFriendFailed = 998,
        SetLootPersonal = 999,
        SetLootMethodFailedCombat = 1000,
        ReagentBankFull = 1001,
        ReagentBankLocked = 1002,
        GarrisonBuildingExists = 1003,
        GarrisonInvalidPlot = 1004,
        GarrisonInvalidBuildingid = 1005,
        GarrisonInvalidPlotBuilding = 1006,
        GarrisonRequiresBlueprint = 1007,
        GarrisonNotEnoughCurrency = 1008,
        GarrisonNotEnoughGold = 1009,
        GarrisonCompleteMissionWrongFollowerType = 1010,
        AlreadyUsingLfgList = 1011,
        RestrictedAccountLfgListTrial = 1012,
        ToyUseLimitReached = 1013,
        ToyAlreadyKnown = 1014,
        TransmogSetAlreadyKnown = 1015,
        NotEnoughCurrency = 1016,
        SpecIsDisabled = 1017,
        FeatureRestrictedTrial = 1018,
        CantBeObliterated = 1019,
        CantBeScrapped = 1020,
        CantBeRecrafted = 1021,
        ArtifactRelicDoesNotMatchArtifact = 1022,
        MustEquipArtifact = 1023,
        CantDoThatRightNow = 1024,
        AffectingCombat = 1025,
        EquipmentManagerCombatSwapS = 1026,
        EquipmentManagerBagsFull = 1027,
        EquipmentManagerMissingItemS = 1028,
        MovieRecordingWarningPerf = 1029,
        MovieRecordingWarningDiskFull = 1030,
        MovieRecordingWarningNoMovie = 1031,
        MovieRecordingWarningRequirements = 1032,
        MovieRecordingWarningCompressing = 1033,
        NoChallengeModeReward = 1034,
        ClaimedChallengeModeReward = 1035,
        ChallengeModePeriodResetSs = 1036,
        CantDoThatChallengeModeActive = 1037,
        TalentFailedRestArea = 1038,
        CannotAbandonLastPet = 1039,
        TestCvarSetSss = 1040,
        QuestTurnInFailReason = 1041,
        ClaimedChallengeModeRewardOld = 1042,
        TalentGrantedByAura = 1043,
        ChallengeModeAlreadyComplete = 1044,
        GlyphTargetNotAvailable = 1045,
        PvpWarmodeToggleOn = 1046,
        PvpWarmodeToggleOff = 1047,
        SpellFailedLevelRequirement = 1048,
        SpellFailedCantFlyHere = 1049,
        BattlegroundJoinRequiresLevel = 1050,
        BattlegroundJoinDisqualified = 1051,
        BattlegroundJoinDisqualifiedNoName = 1052,
        VoiceChatGenericUnableToConnect = 1053,
        VoiceChatServiceLost = 1054,
        VoiceChatChannelNameTooShort = 1055,
        VoiceChatChannelNameTooLong = 1056,
        VoiceChatChannelAlreadyExists = 1057,
        VoiceChatTargetNotFound = 1058,
        VoiceChatTooManyRequests = 1059,
        VoiceChatPlayerSilenced = 1060,
        VoiceChatParentalDisableAll = 1061,
        VoiceChatDisabled = 1062,
        NoPvpReward = 1063,
        ClaimedPvpReward = 1064,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1065,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1066,
        AzeriteEssenceSelectionFailedConditionFailed = 1067,
        AzeriteEssenceSelectionFailedRestArea = 1068,
        AzeriteEssenceSelectionFailedSlotLocked = 1069,
        AzeriteEssenceSelectionFailedNotAtForge = 1070,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1071,
        AzeriteEssenceSelectionFailedNotEquipped = 1072,
        SocketingRequiresPunchcardredGem = 1073,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1074,
        SocketingRequiresPunchcardyellowGem = 1075,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1076,
        SocketingRequiresPunchcardblueGem = 1077,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1078,
        SocketingRequiresDominationShard = 1079,
        SocketingDominationShardOnlyInDominationslot = 1080,
        SocketingRequiresCypherGem = 1081,
        SocketingCypherGemOnlyInCypherslot = 1082,
        SocketingRequiresTinkerGem = 1083,
        SocketingTinkerGemOnlyInTinkerslot = 1084,
        SocketingRequiresPrimordialGem = 1085,
        SocketingPrimordialGemOnlyInPrimordialslot = 1086,
        LevelLinkingResultLinked = 1087,
        LevelLinkingResultUnlinked = 1088,
        ClubFinderErrorPostClub = 1089,
        ClubFinderErrorApplyClub = 1090,
        ClubFinderErrorRespondApplicant = 1091,
        ClubFinderErrorCancelApplication = 1092,
        ClubFinderErrorTypeAcceptApplication = 1093,
        ClubFinderErrorTypeNoInvitePermissions = 1094,
        ClubFinderErrorTypeNoPostingPermissions = 1095,
        ClubFinderErrorTypeApplicantList = 1096,
        ClubFinderErrorTypeApplicantListNoPerm = 1097,
        ClubFinderErrorTypeFinderNotAvailable = 1098,
        ClubFinderErrorTypeGetPostingIds = 1099,
        ClubFinderErrorTypeJoinApplication = 1100,
        ClubFinderErrorTypeRealmNotEligible = 1101,
        ClubFinderErrorTypeFlaggedRename = 1102,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1103,
        ItemInteractionNotEnoughGold = 1104,
        ItemInteractionNotEnoughCurrency = 1105,
        ItemInteractionNoConversionOutput = 1106,
        PlayerChoiceErrorPendingChoice = 1107,
        SoulbindInvalidConduit = 1108,
        SoulbindInvalidConduitItem = 1109,
        SoulbindInvalidTalent = 1110,
        SoulbindDuplicateConduit = 1111,
        ActivateSoulbindS = 1112,
        ActivateSoulbindFailedRestArea = 1113,
        CantUseProfanity = 1114,
        NotInPetBattle = 1115,
        NotInNpe = 1116,
        NoSpec = 1117,
        NoDominationshardOverwrite = 1118,
        UseWeeklyRewardsDisabled = 1119,
        CrossFactionGroupJoined = 1120,
        CantTargetUnfriendlyInOverworld = 1121,
        EquipablespellsSlotsFull = 1122,
        ItemModAppearanceGroupAlreadyKnown = 1123,
        CantBulkSellItemWithRefund = 1124,
        WowLabsPartyErrorTypePartyIsFull = 1125,
        WowLabsPartyErrorTypeMaxInviteSent = 1126,
        WowLabsPartyErrorTypePlayerAlreadyInvited = 1127,
        WowLabsPartyErrorTypePartyInviteInvalid = 1128,
        WowLabsLobbyMatchmakerErrorEnterQueueFailed = 1129,
        WowLabsLobbyMatchmakerErrorLeaveQueueFailed = 1130,
        TargetIsSelfFoundCannotTrade = 1131,
        PlayerIsSelfFoundCannotTrade = 1132,
        MailRecepientIsSelfFoundCannotReceiveMail = 1133,
        PlayerIsSelfFoundCannotSendMail = 1134,
        PlayerIsSelfFoundCannotUseAuctionHouse = 1135,
        MailTargetCannotReceiveMail = 1136,
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
