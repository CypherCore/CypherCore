/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
        public const int ReputationCap = 42000;
        public const int ReputationBottom = -42000;
        public const int MaxClientMailItems = 12; // max number of items a player is allowed to attach
        public const int MaxMailItems = 16;
        public const int MaxDeclinedNameCases = 5;
        public const int MaxHolidayDurations = 10;
        public const int MaxHolidayDates = 26;
        public const int MaxHolidayFlags = 10;
        public const int DefaultMaxLevel = 60;
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
        public const int SmartActionParamCount = 6;
        public const uint SmartSummonCounter = 0xFFFFFF;
        public const uint SmartEscortTargets = 0xFFFFFF;

        /// <summary>
        /// BGs / Arena Const
        /// </summary>
        public const int PvpTeamsCount = 2;
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

        public static ulong RaceMaskAllPlayable = (ulong)(GetMaskForRace(Race.Human) | GetMaskForRace(Race.Orc) | GetMaskForRace(Race.Dwarf) | GetMaskForRace(Race.NightElf) | GetMaskForRace(Race.Undead)
            | GetMaskForRace(Race.Tauren) | GetMaskForRace(Race.Gnome) | GetMaskForRace(Race.Troll) | GetMaskForRace(Race.BloodElf) | GetMaskForRace(Race.Draenei)
            | GetMaskForRace(Race.Goblin) | GetMaskForRace(Race.Worgen) | GetMaskForRace(Race.PandarenNeutral) | GetMaskForRace(Race.PandarenAlliance) | GetMaskForRace(Race.PandarenHorde)
            | GetMaskForRace(Race.Nightborne) | GetMaskForRace(Race.HighmountainTauren) | GetMaskForRace(Race.VoidElf) | GetMaskForRace(Race.LightforgedDraenei) | GetMaskForRace(Race.ZandalariTroll)
            | GetMaskForRace(Race.KulTiran) | GetMaskForRace(Race.DarkIronDwarf) | GetMaskForRace(Race.Vulpera) | GetMaskForRace(Race.MagharOrc) | GetMaskForRace(Race.MechaGnome));

        public static ulong RaceMaskAlliance = (ulong)(GetMaskForRace(Race.Human) | GetMaskForRace(Race.Dwarf) | GetMaskForRace(Race.NightElf) | GetMaskForRace(Race.Gnome)
            | GetMaskForRace(Race.Draenei) | GetMaskForRace(Race.Worgen) | GetMaskForRace(Race.PandarenAlliance) | GetMaskForRace(Race.VoidElf) | GetMaskForRace(Race.LightforgedDraenei)
            | GetMaskForRace(Race.KulTiran) | GetMaskForRace(Race.DarkIronDwarf) | GetMaskForRace(Race.MechaGnome));

        public static ulong RaceMaskHorde = RaceMaskAllPlayable & ~RaceMaskAlliance;

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
                case LockType.Archaeology:
                    return SkillType.Archaeology;
                case LockType.LumberMill:
                    return SkillType.Logging;
                case LockType.ClassicHerbalism:
                    return SkillType.Herbalism2;
                case LockType.OutlandHerbalism:
                    return SkillType.OutlandHerbalism;
                case LockType.NorthrendHerbalism:
                    return SkillType.NorthrendHerbalism;
                case LockType.CataclysmHerbalism:
                    return SkillType.CataclysmHerbalism;
                case LockType.PandariaHerbalism:
                    return SkillType.PandariaHerbalism;
                case LockType.DraenorHerbalism:
                    return SkillType.DraenorHerbalism;
                case LockType.LegionHerbalism:
                    return SkillType.LegionHerbalism;
                case LockType.KulTiranHerbalism:
                    return SkillType.KulTiranHerbalism;
                case LockType.ClassicMining:
                    return SkillType.Mining2;
                case LockType.OutlandMining:
                    return SkillType.OutlandMining;
                case LockType.NorthrendMining:
                    return SkillType.NorthrendMining;
                case LockType.CataclysmMining:
                    return SkillType.CataclysmMining;
                case LockType.PandariaMining:
                    return SkillType.PandariaMining;
                case LockType.DraenorMining:
                    return SkillType.DraenorMining;
                case LockType.LegionMining:
                    return SkillType.LegionMining;
                case LockType.KulTiranMining:
                    return SkillType.KulTiranMining;
            }
            return SkillType.None;
        }

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

        public static long GetMaskForRace(Race raceId)
        {
            return raceId < Race.Max && raceBits[(int)raceId] >= 0 && raceBits[(int)raceId] < 64 ? (1 << raceBits[(int)raceId]) : 0;
        }

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
        Max
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
        Max = 19,
        All = 127,          // default for class?
        Health = -2,    // (-2 as signed value)
        MaxPerClass = 7
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
        Heroic = 0x01,
        Default = 0x02,
        CanSelect = 0x04, // Player can select this difficulty in dropdown menu
        ChallengeMode = 0x08,

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
        ItemMaxCount = 22,
        NotEquippable = 23,
        CantStack = 24,
        CantSwap = 25,
        SlotEmpty = 26,
        ItemNotFound = 27,
        TooFewToSplit = 28,
        SplitFailed = 29,
        NotABag = 30,
        NotOwner = 31,
        OnlyOneQuiver = 32,
        NoBankSlot = 33,
        NoBankHere = 34,
        ItemLocked = 35,
        Equipped2handed = 36,
        VendorNotInterested = 37,
        VendorRefuseScrappableAzerite = 38,
        VendorHatesYou = 39,
        VendorSoldOut = 40,
        VendorTooFar = 41,
        VendorDoesntBuy = 42,
        NotEnoughMoney = 43,
        ReceiveItemS = 44,
        DropBoundItem = 45,
        TradeBoundItem = 46,
        TradeQuestItem = 47,
        TradeTempEnchantBound = 48,
        TradeGroundItem = 49,
        TradeBag = 50,
        TradeFactionSpecific = 51,
        SpellFailedS = 52,
        ItemCooldown = 53,
        PotionCooldown = 54,
        FoodCooldown = 55,
        SpellCooldown = 56,
        AbilityCooldown = 57,
        SpellAlreadyKnownS = 58,
        PetSpellAlreadyKnownS = 59,
        ProficiencyGainedS = 60,
        SkillGainedS = 61,
        SkillUpSi = 62,
        LearnSpellS = 63,
        LearnAbilityS = 64,
        LearnPassiveS = 65,
        LearnRecipeS = 66,
        LearnCompanionS = 67,
        LearnMountS = 68,
        LearnToyS = 69,
        LearnHeirloomS = 70,
        LearnTransmogS = 71,
        CompletedTransmogSetS = 72,
        AppearanceAlreadyLearned = 73,
        RevokeTransmogS = 74,
        InvitePlayerS = 75,
        InviteSelf = 76,
        InvitedToGroupSs = 77,
        InvitedAlreadyInGroupSs = 78,
        AlreadyInGroupS = 79,
        CrossRealmRaidInvite = 80,
        PlayerBusyS = 81,
        NewLeaderS = 82,
        NewLeaderYou = 83,
        NewGuideS = 84,
        NewGuideYou = 85,
        LeftGroupS = 86,
        LeftGroupYou = 87,
        GroupDisbanded = 88,
        DeclineGroupS = 89,
        JoinedGroupS = 90,
        UninviteYou = 91,
        BadPlayerNameS = 92,
        NotInGroup = 93,
        TargetNotInGroupS = 94,
        TargetNotInInstanceS = 95,
        NotInInstanceGroup = 96,
        GroupFull = 97,
        NotLeader = 98,
        PlayerDiedS = 99,
        GuildCreateS = 100,
        GuildInviteS = 101,
        InvitedToGuildSss = 102,
        AlreadyInGuildS = 103,
        AlreadyInvitedToGuildS = 104,
        InvitedToGuild = 105,
        AlreadyInGuild = 106,
        GuildAccept = 107,
        GuildDeclineS = 108,
        GuildDeclineAutoS = 109,
        GuildPermissions = 110,
        GuildJoinS = 111,
        GuildFounderS = 112,
        GuildPromoteSss = 113,
        GuildDemoteSs = 114,
        GuildDemoteSss = 115,
        GuildInviteSelf = 116,
        GuildQuitS = 117,
        GuildLeaveS = 118,
        GuildRemoveSs = 119,
        GuildRemoveSelf = 120,
        GuildDisbandS = 121,
        GuildDisbandSelf = 122,
        GuildLeaderS = 123,
        GuildLeaderSelf = 124,
        GuildPlayerNotFoundS = 125,
        GuildPlayerNotInGuildS = 126,
        GuildPlayerNotInGuild = 127,
        GuildCantPromoteS = 128,
        GuildCantDemoteS = 129,
        GuildNotInAGuild = 130,
        GuildInternal = 131,
        GuildLeaderIsS = 132,
        GuildLeaderChangedSs = 133,
        GuildDisbanded = 134,
        GuildNotAllied = 135,
        GuildLeaderLeave = 136,
        GuildRanksLocked = 137,
        GuildRankInUse = 138,
        GuildRankTooHighS = 139,
        GuildRankTooLowS = 140,
        GuildNameExistsS = 141,
        GuildWithdrawLimit = 142,
        GuildNotEnoughMoney = 143,
        GuildTooMuchMoney = 144,
        GuildBankConjuredItem = 145,
        GuildBankEquippedItem = 146,
        GuildBankBoundItem = 147,
        GuildBankQuestItem = 148,
        GuildBankWrappedItem = 149,
        GuildBankFull = 150,
        GuildBankWrongTab = 151,
        NoGuildCharter = 152,
        OutOfRange = 153,
        PlayerDead = 154,
        ClientLockedOut = 155,
        ClientOnTransport = 156,
        KilledByS = 157,
        LootLocked = 158,
        LootTooFar = 159,
        LootDidntKill = 160,
        LootBadFacing = 161,
        LootNotstanding = 162,
        LootStunned = 163,
        LootNoUi = 164,
        LootWhileInvulnerable = 165,
        NoLoot = 166,
        QuestAcceptedS = 167,
        QuestCompleteS = 168,
        QuestFailedS = 169,
        QuestFailedBagFullS = 170,
        QuestFailedMaxCountS = 171,
        QuestFailedLowLevel = 172,
        QuestFailedMissingItems = 173,
        QuestFailedWrongRace = 174,
        QuestFailedNotEnoughMoney = 175,
        QuestFailedExpansion = 176,
        QuestOnlyOneTimed = 177,
        QuestNeedPrereqs = 178,
        QuestNeedPrereqsCustom = 179,
        QuestAlreadyOn = 180,
        QuestAlreadyDone = 181,
        QuestAlreadyDoneDaily = 182,
        QuestHasInProgress = 183,
        QuestRewardExpI = 184,
        QuestRewardMoneyS = 185,
        QuestMustChoose = 186,
        QuestLogFull = 187,
        CombatDamageSsi = 188,
        InspectS = 189,
        CantUseItem = 190,
        CantUseItemInArena = 191,
        CantUseItemInRatedBattleground = 192,
        MustEquipItem = 193,
        PassiveAbility = 194,
        Skill2HNotFound = 195,
        NoAttackTarget = 196,
        InvalidAttackTarget = 197,
        AttackPvpTargetWhileUnflagged = 198,
        AttackStunned = 199,
        AttackPacified = 200,
        AttackMounted = 201,
        AttackFleeing = 202,
        AttackConfused = 203,
        AttackCharmed = 204,
        AttackDead = 205,
        AttackPreventedByMechanicS = 206,
        AttackChannel = 207,
        Taxisamenode = 208,
        Taxinosuchpath = 209,
        Taxiunspecifiedservererror = 210,
        Taxinotenoughmoney = 211,
        Taxitoofaraway = 212,
        Taxinovendornearby = 213,
        Taxinotvisited = 214,
        Taxiplayerbusy = 215,
        Taxiplayeralreadymounted = 216,
        Taxiplayershapeshifted = 217,
        Taxiplayermoving = 218,
        Taxinopaths = 219,
        Taxinoteligible = 220,
        Taxinotstanding = 221,
        NoReplyTarget = 222,
        GenericNoTarget = 223,
        InitiateTradeS = 224,
        TradeRequestS = 225,
        TradeBlockedS = 226,
        TradeTargetDead = 227,
        TradeTooFar = 228,
        TradeCancelled = 229,
        TradeComplete = 230,
        TradeBagFull = 231,
        TradeTargetBagFull = 232,
        TradeMaxCountExceeded = 233,
        TradeTargetMaxCountExceeded = 234,
        InventoryTradeTooManyUniqueItem = 235,
        AlreadyTrading = 236,
        MountInvalidmountee = 237,
        MountToofaraway = 238,
        MountAlreadymounted = 239,
        MountNotmountable = 240,
        MountNotyourpet = 241,
        MountOther = 242,
        MountLooting = 243,
        MountRacecantmount = 244,
        MountShapeshifted = 245,
        MountNoFavorites = 246,
        MountNoMounts = 247,
        DismountNopet = 248,
        DismountNotmounted = 249,
        DismountNotyourpet = 250,
        SpellFailedTotems = 251,
        SpellFailedReagents = 252,
        SpellFailedReagentsGeneric = 253,
        SpellFailedOptionalReagents = 254,
        CantTradeGold = 255,
        SpellFailedEquippedItem = 256,
        SpellFailedEquippedItemClassS = 257,
        SpellFailedShapeshiftFormS = 258,
        SpellFailedAnotherInProgress = 259,
        Badattackfacing = 260,
        Badattackpos = 261,
        ChestInUse = 262,
        UseCantOpen = 263,
        UseLocked = 264,
        DoorLocked = 265,
        ButtonLocked = 266,
        UseLockedWithItemS = 267,
        UseLockedWithSpellS = 268,
        UseLockedWithSpellKnownSi = 269,
        UseTooFar = 270,
        UseBadAngle = 271,
        UseObjectMoving = 272,
        UseSpellFocus = 273,
        UseDestroyed = 274,
        SetLootFreeforall = 275,
        SetLootRoundrobin = 276,
        SetLootMaster = 277,
        SetLootGroup = 278,
        SetLootThresholdS = 279,
        NewLootMasterS = 280,
        SpecifyMasterLooter = 281,
        LootSpecChangedS = 282,
        TameFailed = 283,
        ChatWhileDead = 284,
        ChatPlayerNotFoundS = 285,
        Newtaxipath = 286,
        NoPet = 287,
        Notyourpet = 288,
        PetNotRenameable = 289,
        QuestObjectiveCompleteS = 290,
        QuestUnknownComplete = 291,
        QuestAddKillSii = 292,
        QuestAddFoundSii = 293,
        QuestAddItemSii = 294,
        QuestAddPlayerKillSii = 295,
        Cannotcreatedirectory = 296,
        Cannotcreatefile = 297,
        PlayerWrongFaction = 298,
        PlayerIsNeutral = 299,
        BankslotFailedTooMany = 300,
        BankslotInsufficientFunds = 301,
        BankslotNotbanker = 302,
        FriendDbError = 303,
        FriendListFull = 304,
        FriendAddedS = 305,
        BattletagFriendAddedS = 306,
        FriendOnlineSs = 307,
        FriendOfflineS = 308,
        FriendNotFound = 309,
        FriendWrongFaction = 310,
        FriendRemovedS = 311,
        BattletagFriendRemovedS = 312,
        FriendError = 313,
        FriendAlreadyS = 314,
        FriendSelf = 315,
        FriendDeleted = 316,
        IgnoreFull = 317,
        IgnoreSelf = 318,
        IgnoreNotFound = 319,
        IgnoreAlreadyS = 320,
        IgnoreAddedS = 321,
        IgnoreRemovedS = 322,
        IgnoreAmbiguous = 323,
        IgnoreDeleted = 324,
        OnlyOneBolt = 325,
        OnlyOneAmmo = 326,
        SpellFailedEquippedSpecificItem = 327,
        WrongBagTypeSubclass = 328,
        CantWrapStackable = 329,
        CantWrapEquipped = 330,
        CantWrapWrapped = 331,
        CantWrapBound = 332,
        CantWrapUnique = 333,
        CantWrapBags = 334,
        OutOfMana = 335,
        OutOfRage = 336,
        OutOfFocus = 337,
        OutOfEnergy = 338,
        OutOfChi = 339,
        OutOfHealth = 340,
        OutOfRunes = 341,
        OutOfRunicPower = 342,
        OutOfSoulShards = 343,
        OutOfLunarPower = 344,
        OutOfHolyPower = 345,
        OutOfMaelstrom = 346,
        OutOfComboPoints = 347,
        OutOfInsanity = 348,
        OutOfArcaneCharges = 349,
        OutOfFury = 350,
        OutOfPain = 351,
        OutOfPowerDisplay = 352,
        LootGone = 353,
        MountForceddismount = 354,
        AutofollowTooFar = 355,
        UnitNotFound = 356,
        InvalidFollowTarget = 357,
        InvalidFollowPvpCombat = 358,
        InvalidFollowTargetPvpCombat = 359,
        InvalidInspectTarget = 360,
        GuildemblemSuccess = 361,
        GuildemblemInvalidTabardColors = 362,
        GuildemblemNoguild = 363,
        GuildemblemNotguildmaster = 364,
        GuildemblemNotenoughmoney = 365,
        GuildemblemInvalidvendor = 366,
        EmblemerrorNotabardgeoset = 367,
        SpellOutOfRange = 368,
        CommandNeedsTarget = 369,
        NoammoS = 370,
        Toobusytofollow = 371,
        DuelRequested = 372,
        DuelCancelled = 373,
        Deathbindalreadybound = 374,
        DeathbindSuccessS = 375,
        Noemotewhilerunning = 376,
        ZoneExplored = 377,
        ZoneExploredXp = 378,
        InvalidItemTarget = 379,
        InvalidQuestTarget = 380,
        IgnoringYouS = 381,
        FishNotHooked = 382,
        FishEscaped = 383,
        SpellFailedNotunsheathed = 384,
        PetitionOfferedS = 385,
        PetitionSigned = 386,
        PetitionSignedS = 387,
        PetitionDeclinedS = 388,
        PetitionAlreadySigned = 389,
        PetitionRestrictedAccountTrial = 390,
        PetitionAlreadySignedOther = 391,
        PetitionInGuild = 392,
        PetitionCreator = 393,
        PetitionNotEnoughSignatures = 394,
        PetitionNotSameServer = 395,
        PetitionFull = 396,
        PetitionAlreadySignedByS = 397,
        GuildNameInvalid = 398,
        SpellUnlearnedS = 399,
        PetSpellRooted = 400,
        PetSpellAffectingCombat = 401,
        PetSpellOutOfRange = 402,
        PetSpellNotBehind = 403,
        PetSpellTargetsDead = 404,
        PetSpellDead = 405,
        PetSpellNopath = 406,
        ItemCantBeDestroyed = 407,
        TicketAlreadyExists = 408,
        TicketCreateError = 409,
        TicketUpdateError = 410,
        TicketDbError = 411,
        TicketNoText = 412,
        TicketTextTooLong = 413,
        ObjectIsBusy = 414,
        ExhaustionWellrested = 415,
        ExhaustionRested = 416,
        ExhaustionNormal = 417,
        ExhaustionTired = 418,
        ExhaustionExhausted = 419,
        NoItemsWhileShapeshifted = 420,
        CantInteractShapeshifted = 421,
        RealmNotFound = 422,
        MailQuestItem = 423,
        MailBoundItem = 424,
        MailConjuredItem = 425,
        MailBag = 426,
        MailToSelf = 427,
        MailTargetNotFound = 428,
        MailDatabaseError = 429,
        MailDeleteItemError = 430,
        MailWrappedCod = 431,
        MailCantSendRealm = 432,
        MailTempReturnOutage = 433,
        MailRecepientCantReceiveMail = 434,
        MailSent = 435,
        MailTargetIsTrial = 436,
        NotHappyEnough = 437,
        UseCantImmune = 438,
        CantBeDisenchanted = 439,
        CantUseDisarmed = 440,
        AuctionDatabaseError = 441,
        AuctionHigherBid = 442,
        AuctionAlreadyBid = 443,
        AuctionOutbidS = 444,
        AuctionWonS = 445,
        AuctionRemovedS = 446,
        AuctionBidPlaced = 447,
        LogoutFailed = 448,
        QuestPushSuccessS = 449,
        QuestPushInvalidS = 450,
        QuestPushInvalidToRecipientS = 451,
        QuestPushAcceptedS = 452,
        QuestPushDeclinedS = 453,
        QuestPushBusyS = 454,
        QuestPushDeadS = 455,
        QuestPushDeadToRecipientS = 456,
        QuestPushLogFullS = 457,
        QuestPushLogFullToRecipientS = 458,
        QuestPushOnquestS = 459,
        QuestPushOnquestToRecipientS = 460,
        QuestPushAlreadyDoneS = 461,
        QuestPushAlreadyDoneToRecipientS = 462,
        QuestPushNotDailyS = 463,
        QuestPushTimerExpiredS = 464,
        QuestPushNotInPartyS = 465,
        QuestPushDifferentServerDailyS = 466,
        QuestPushDifferentServerDailyToRecipientS = 467,
        QuestPushNotAllowedS = 468,
        QuestPushPrerequisiteS = 469,
        QuestPushPrerequisiteToRecipientS = 470,
        QuestPushLowLevelS = 471,
        QuestPushLowLevelToRecipientS = 472,
        QuestPushHighLevelS = 473,
        QuestPushHighLevelToRecipientS = 474,
        QuestPushClassS = 475,
        QuestPushClassToRecipientS = 476,
        QuestPushRaceS = 477,
        QuestPushRaceToRecipientS = 478,
        QuestPushLowFactionS = 479,
        QuestPushLowFactionToRecipientS = 480,
        QuestPushExpansionS = 481,
        QuestPushExpansionToRecipientS = 482,
        QuestPushNotGarrisonOwnerS = 483,
        QuestPushNotGarrisonOwnerToRecipientS = 484,
        QuestPushWrongCovenantS = 485,
        QuestPushWrongCovenantToRecipientS = 486,
        QuestPushNewPlayerExperienceS = 487,
        QuestPushNewPlayerExperienceToRecipientS = 488,
        QuestPushWrongFactionS = 489,
        QuestPushWrongFactionToRecipientS = 490,
        QuestPushCrossFactionRestrictedS = 491,
        RaidGroupLowlevel = 492,
        RaidGroupOnly = 493,
        RaidGroupFull = 494,
        RaidGroupRequirementsUnmatch = 495,
        CorpseIsNotInInstance = 496,
        PvpKillHonorable = 497,
        PvpKillDishonorable = 498,
        SpellFailedAlreadyAtFullHealth = 499,
        SpellFailedAlreadyAtFullMana = 500,
        SpellFailedAlreadyAtFullPowerS = 501,
        AutolootMoneyS = 502,
        GenericStunned = 503,
        GenericThrottle = 504,
        ClubFinderSearchingTooFast = 505,
        TargetStunned = 506,
        MustRepairDurability = 507,
        RaidYouJoined = 508,
        RaidYouLeft = 509,
        InstanceGroupJoinedWithParty = 510,
        InstanceGroupJoinedWithRaid = 511,
        RaidMemberAddedS = 512,
        RaidMemberRemovedS = 513,
        InstanceGroupAddedS = 514,
        InstanceGroupRemovedS = 515,
        ClickOnItemToFeed = 516,
        TooManyChatChannels = 517,
        LootRollPending = 518,
        LootPlayerNotFound = 519,
        NotInRaid = 520,
        LoggingOut = 521,
        TargetLoggingOut = 522,
        NotWhileMounted = 523,
        NotWhileShapeshifted = 524,
        NotInCombat = 525,
        NotWhileDisarmed = 526,
        PetBroken = 527,
        TalentWipeError = 528,
        SpecWipeError = 529,
        GlyphWipeError = 530,
        PetSpecWipeError = 531,
        FeignDeathResisted = 532,
        MeetingStoneInQueueS = 533,
        MeetingStoneLeftQueueS = 534,
        MeetingStoneOtherMemberLeft = 535,
        MeetingStonePartyKickedFromQueue = 536,
        MeetingStoneMemberStillInQueue = 537,
        MeetingStoneSuccess = 538,
        MeetingStoneInProgress = 539,
        MeetingStoneMemberAddedS = 540,
        MeetingStoneGroupFull = 541,
        MeetingStoneNotLeader = 542,
        MeetingStoneInvalidLevel = 543,
        MeetingStoneTargetNotInParty = 544,
        MeetingStoneTargetInvalidLevel = 545,
        MeetingStoneMustBeLeader = 546,
        MeetingStoneNoRaidGroup = 547,
        MeetingStoneNeedParty = 548,
        MeetingStoneNotFound = 549,
        MeetingStoneTargetInVehicle = 550,
        GuildemblemSame = 551,
        EquipTradeItem = 552,
        PvpToggleOn = 553,
        PvpToggleOff = 554,
        GroupJoinBattlegroundDeserters = 555,
        GroupJoinBattlegroundDead = 556,
        GroupJoinBattlegroundS = 557,
        GroupJoinBattlegroundFail = 558,
        GroupJoinBattlegroundTooMany = 559,
        SoloJoinBattlegroundS = 560,
        JoinSingleScenarioS = 561,
        BattlegroundTooManyQueues = 562,
        BattlegroundCannotQueueForRated = 563,
        BattledgroundQueuedForRated = 564,
        BattlegroundTeamLeftQueue = 565,
        BattlegroundNotInBattleground = 566,
        AlreadyInArenaTeamS = 567,
        InvalidPromotionCode = 568,
        BgPlayerJoinedSs = 569,
        BgPlayerLeftS = 570,
        RestrictedAccount = 571,
        RestrictedAccountTrial = 572,
        PlayTimeExceeded = 573,
        ApproachingPartialPlayTime = 574,
        ApproachingPartialPlayTime2 = 575,
        ApproachingNoPlayTime = 576,
        ApproachingNoPlayTime2 = 577,
        UnhealthyTime = 578,
        ChatRestrictedTrial = 579,
        ChatThrottled = 580,
        MailReachedCap = 581,
        InvalidRaidTarget = 582,
        RaidLeaderReadyCheckStartS = 583,
        ReadyCheckInProgress = 584,
        ReadyCheckThrottled = 585,
        DungeonDifficultyFailed = 586,
        DungeonDifficultyChangedS = 587,
        TradeWrongRealm = 588,
        TradeNotOnTaplist = 589,
        ChatPlayerAmbiguousS = 590,
        LootCantLootThatNow = 591,
        LootMasterInvFull = 592,
        LootMasterUniqueItem = 593,
        LootMasterOther = 594,
        FilteringYouS = 595,
        UsePreventedByMechanicS = 596,
        ItemUniqueEquippable = 597,
        LfgLeaderIsLfmS = 598,
        LfgPending = 599,
        CantSpeakLangage = 600,
        VendorMissingTurnins = 601,
        BattlegroundNotInTeam = 602,
        NotInBattleground = 603,
        NotEnoughHonorPoints = 604,
        NotEnoughArenaPoints = 605,
        SocketingRequiresMetaGem = 606,
        SocketingMetaGemOnlyInMetaslot = 607,
        SocketingRequiresHydraulicGem = 608,
        SocketingHydraulicGemOnlyInHydraulicslot = 609,
        SocketingRequiresCogwheelGem = 610,
        SocketingCogwheelGemOnlyInCogwheelslot = 611,
        SocketingItemTooLowLevel = 612,
        ItemMaxCountSocketed = 613,
        SystemDisabled = 614,
        QuestFailedTooManyDailyQuestsI = 615,
        ItemMaxCountEquippedSocketed = 616,
        ItemUniqueEquippableSocketed = 617,
        UserSquelched = 618,
        AccountSilenced = 619,
        PartyMemberSilenced = 620,
        PartyMemberSilencedLfgDelist = 621,
        TooMuchGold = 622,
        NotBarberSitting = 623,
        QuestFailedCais = 624,
        InviteRestrictedTrial = 625,
        VoiceIgnoreFull = 626,
        VoiceIgnoreSelf = 627,
        VoiceIgnoreNotFound = 628,
        VoiceIgnoreAlreadyS = 629,
        VoiceIgnoreAddedS = 630,
        VoiceIgnoreRemovedS = 631,
        VoiceIgnoreAmbiguous = 632,
        VoiceIgnoreDeleted = 633,
        UnknownMacroOptionS = 634,
        NotDuringArenaMatch = 635,
        NotInRatedBattleground = 636,
        PlayerSilenced = 637,
        PlayerUnsilenced = 638,
        ComsatDisconnect = 639,
        ComsatReconnectAttempt = 640,
        ComsatConnectFail = 641,
        MailInvalidAttachmentSlot = 642,
        MailTooManyAttachments = 643,
        MailInvalidAttachment = 644,
        MailAttachmentExpired = 645,
        VoiceChatParentalDisableMic = 646,
        ProfaneChatName = 647,
        PlayerSilencedEcho = 648,
        PlayerUnsilencedEcho = 649,
        LootCantLootThat = 650,
        ArenaExpiredCais = 651,
        GroupActionThrottled = 652,
        AlreadyPickpocketed = 653,
        NameInvalid = 654,
        NameNoName = 655,
        NameTooShort = 656,
        NameTooLong = 657,
        NameMixedLanguages = 658,
        NameProfane = 659,
        NameReserved = 660,
        NameThreeConsecutive = 661,
        NameInvalidSpace = 662,
        NameConsecutiveSpaces = 663,
        NameRussianConsecutiveSilentCharacters = 664,
        NameRussianSilentCharacterAtBeginningOrEnd = 665,
        NameDeclensionDoesntMatchBaseName = 666,
        RecruitAFriendNotLinked = 667,
        RecruitAFriendNotNow = 668,
        RecruitAFriendSummonLevelMax = 669,
        RecruitAFriendSummonCooldown = 670,
        RecruitAFriendSummonOffline = 671,
        RecruitAFriendInsufExpanLvl = 672,
        RecruitAFriendMapIncomingTransferNotAllowed = 673,
        NotSameAccount = 674,
        BadOnUseEnchant = 675,
        TradeSelf = 676,
        TooManySockets = 677,
        ItemMaxLimitCategoryCountExceededIs = 678,
        TradeTargetMaxLimitCategoryCountExceededIs = 679,
        ItemMaxLimitCategorySocketedExceededIs = 680,
        ItemMaxLimitCategoryEquippedExceededIs = 681,
        ShapeshiftFormCannotEquip = 682,
        ItemInventoryFullSatchel = 683,
        ScalingStatItemLevelExceeded = 684,
        ScalingStatItemLevelTooLow = 685,
        PurchaseLevelTooLow = 686,
        GroupSwapFailed = 687,
        InviteInCombat = 688,
        InvalidGlyphSlot = 689,
        GenericNoValidTargets = 690,
        CalendarEventAlertS = 691,
        PetLearnSpellS = 692,
        PetLearnAbilityS = 693,
        PetSpellUnlearnedS = 694,
        InviteUnknownRealm = 695,
        InviteNoPartyServer = 696,
        InvitePartyBusy = 697,
        PartyTargetAmbiguous = 698,
        PartyLfgInviteRaidLocked = 699,
        PartyLfgBootLimit = 700,
        PartyLfgBootCooldownS = 701,
        PartyLfgBootNotEligibleS = 702,
        PartyLfgBootInpatientTimerS = 703,
        PartyLfgBootInProgress = 704,
        PartyLfgBootTooFewPlayers = 705,
        PartyLfgBootVoteSucceeded = 706,
        PartyLfgBootVoteFailed = 707,
        PartyLfgBootInCombat = 708,
        PartyLfgBootDungeonComplete = 709,
        PartyLfgBootLootRolls = 710,
        PartyLfgBootVoteRegistered = 711,
        PartyPrivateGroupOnly = 712,
        PartyLfgTeleportInCombat = 713,
        RaidDisallowedByLevel = 714,
        RaidDisallowedByCrossRealm = 715,
        PartyRoleNotAvailable = 716,
        JoinLfgObjectFailed = 717,
        LfgRemovedLevelup = 718,
        LfgRemovedXpToggle = 719,
        LfgRemovedFactionChange = 720,
        BattlegroundInfoThrottled = 721,
        BattlegroundAlreadyIn = 722,
        ArenaTeamChangeFailedQueued = 723,
        ArenaTeamPermissions = 724,
        NotWhileFalling = 725,
        NotWhileMoving = 726,
        NotWhileFatigued = 727,
        MaxSockets = 728,
        MultiCastActionTotemS = 729,
        BattlegroundJoinLevelup = 730,
        RemoveFromPvpQueueXpGain = 731,
        BattlegroundJoinXpGain = 732,
        BattlegroundJoinMercenary = 733,
        BattlegroundJoinTooManyHealers = 734,
        BattlegroundJoinRatedTooManyHealers = 735,
        BattlegroundJoinTooManyTanks = 736,
        BattlegroundJoinTooManyDamage = 737,
        RaidDifficultyFailed = 738,
        RaidDifficultyChangedS = 739,
        LegacyRaidDifficultyChangedS = 740,
        RaidLockoutChangedS = 741,
        RaidConvertedToParty = 742,
        PartyConvertedToRaid = 743,
        PlayerDifficultyChangedS = 744,
        GmresponseDbError = 745,
        BattlegroundJoinRangeIndex = 746,
        ArenaJoinRangeIndex = 747,
        RemoveFromPvpQueueFactionChange = 748,
        BattlegroundJoinFailed = 749,
        BattlegroundJoinNoValidSpecForRole = 750,
        BattlegroundJoinRespec = 751,
        BattlegroundInvitationDeclined = 752,
        BattlegroundJoinTimedOut = 753,
        BattlegroundDupeQueue = 754,
        BattlegroundJoinMustCompleteQuest = 755,
        InBattlegroundRespec = 756,
        MailLimitedDurationItem = 757,
        YellRestrictedTrial = 758,
        ChatRaidRestrictedTrial = 759,
        LfgRoleCheckFailed = 760,
        LfgRoleCheckFailedTimeout = 761,
        LfgRoleCheckFailedNotViable = 762,
        LfgReadyCheckFailed = 763,
        LfgReadyCheckFailedTimeout = 764,
        LfgGroupFull = 765,
        LfgNoLfgObject = 766,
        LfgNoSlotsPlayer = 767,
        LfgNoSlotsParty = 768,
        LfgNoSpec = 769,
        LfgMismatchedSlots = 770,
        LfgMismatchedSlotsLocalXrealm = 771,
        LfgPartyPlayersFromDifferentRealms = 772,
        LfgMembersNotPresent = 773,
        LfgGetInfoTimeout = 774,
        LfgInvalidSlot = 775,
        LfgDeserterPlayer = 776,
        LfgDeserterParty = 777,
        LfgDead = 778,
        LfgRandomCooldownPlayer = 779,
        LfgRandomCooldownParty = 780,
        LfgTooManyMembers = 781,
        LfgTooFewMembers = 782,
        LfgProposalFailed = 783,
        LfgProposalDeclinedSelf = 784,
        LfgProposalDeclinedParty = 785,
        LfgNoSlotsSelected = 786,
        LfgNoRolesSelected = 787,
        LfgRoleCheckInitiated = 788,
        LfgReadyCheckInitiated = 789,
        LfgPlayerDeclinedRoleCheck = 790,
        LfgPlayerDeclinedReadyCheck = 791,
        LfgJoinedQueue = 792,
        LfgJoinedFlexQueue = 793,
        LfgJoinedRfQueue = 794,
        LfgJoinedScenarioQueue = 795,
        LfgJoinedWorldPvpQueue = 796,
        LfgJoinedBattlefieldQueue = 797,
        LfgJoinedList = 798,
        LfgLeftQueue = 799,
        LfgLeftList = 800,
        LfgRoleCheckAborted = 801,
        LfgReadyCheckAborted = 802,
        LfgCantUseBattleground = 803,
        LfgCantUseDungeons = 804,
        LfgReasonTooManyLfg = 805,
        LfgFarmLimit = 806,
        LfgNoCrossFactionParties = 807,
        InvalidTeleportLocation = 808,
        TooFarToInteract = 809,
        BattlegroundPlayersFromDifferentRealms = 810,
        DifficultyChangeCooldownS = 811,
        DifficultyChangeCombatCooldownS = 812,
        DifficultyChangeWorldstate = 813,
        DifficultyChangeEncounter = 814,
        DifficultyChangeCombat = 815,
        DifficultyChangePlayerBusy = 816,
        DifficultyChangeAlreadyStarted = 817,
        DifficultyChangeOtherHeroicS = 818,
        DifficultyChangeHeroicInstanceAlreadyRunning = 819,
        ArenaTeamPartySize = 820,
        SoloShuffleWargameGroupSize = 821,
        SoloShuffleWargameGroupComp = 822,
        PvpPlayerAbandoned = 823,
        QuestForceRemovedS = 824,
        AttackNoActions = 825,
        InRandomBg = 826,
        InNonRandomBg = 827,
        BnFriendSelf = 828,
        BnFriendAlready = 829,
        BnFriendBlocked = 830,
        BnFriendListFull = 831,
        BnFriendRequestSent = 832,
        BnBroadcastThrottle = 833,
        BgDeveloperOnly = 834,
        CurrencySpellSlotMismatch = 835,
        CurrencyNotTradable = 836,
        RequiresExpansionS = 837,
        QuestFailedSpell = 838,
        TalentFailedNotEnoughTalentsInPrimaryTree = 839,
        TalentFailedNoPrimaryTreeSelected = 840,
        TalentFailedCantRemoveTalent = 841,
        TalentFailedUnknown = 842,
        WargameRequestFailure = 843,
        RankRequiresAuthenticator = 844,
        GuildBankVoucherFailed = 845,
        WargameRequestSent = 846,
        RequiresAchievementI = 847,
        RefundResultExceedMaxCurrency = 848,
        CantBuyQuantity = 849,
        ItemIsBattlePayLocked = 850,
        PartyAlreadyInBattlegroundQueue = 851,
        PartyConfirmingBattlegroundQueue = 852,
        BattlefieldTeamPartySize = 853,
        InsuffTrackedCurrencyIs = 854,
        NotOnTournamentRealm = 855,
        GuildTrialAccountTrial = 856,
        GuildTrialAccountVeteran = 857,
        GuildUndeletableDueToLevel = 858,
        CantDoThatInAGroup = 859,
        GuildLeaderReplaced = 860,
        TransmogrifyCantEquip = 861,
        TransmogrifyInvalidItemType = 862,
        TransmogrifyNotSoulbound = 863,
        TransmogrifyInvalidSource = 864,
        TransmogrifyInvalidDestination = 865,
        TransmogrifyMismatch = 866,
        TransmogrifyLegendary = 867,
        TransmogrifySameItem = 868,
        TransmogrifySameAppearance = 869,
        TransmogrifyNotEquipped = 870,
        VoidDepositFull = 871,
        VoidWithdrawFull = 872,
        VoidStorageWrapped = 873,
        VoidStorageStackable = 874,
        VoidStorageUnbound = 875,
        VoidStorageRepair = 876,
        VoidStorageCharges = 877,
        VoidStorageQuest = 878,
        VoidStorageConjured = 879,
        VoidStorageMail = 880,
        VoidStorageBag = 881,
        VoidTransferStorageFull = 882,
        VoidTransferInvFull = 883,
        VoidTransferInternalError = 884,
        VoidTransferItemInvalid = 885,
        DifficultyDisabledInLfg = 886,
        VoidStorageUnique = 887,
        VoidStorageLoot = 888,
        VoidStorageHoliday = 889,
        VoidStorageDuration = 890,
        VoidStorageLoadFailed = 891,
        VoidStorageInvalidItem = 892,
        ParentalControlsChatMuted = 893,
        SorStartExperienceIncomplete = 894,
        SorInvalidEmail = 895,
        SorInvalidComment = 896,
        ChallengeModeResetCooldownS = 897,
        ChallengeModeResetKeystone = 898,
        PetJournalAlreadyInLoadout = 899,
        ReportSubmittedSuccessfully = 900,
        ReportSubmissionFailed = 901,
        SuggestionSubmittedSuccessfully = 902,
        BugSubmittedSuccessfully = 903,
        ChallengeModeEnabled = 904,
        ChallengeModeDisabled = 905,
        PetbattleCreateFailed = 906,
        PetbattleNotHere = 907,
        PetbattleNotHereOnTransport = 908,
        PetbattleNotHereUnevenGround = 909,
        PetbattleNotHereObstructed = 910,
        PetbattleNotWhileInCombat = 911,
        PetbattleNotWhileDead = 912,
        PetbattleNotWhileFlying = 913,
        PetbattleTargetInvalid = 914,
        PetbattleTargetOutOfRange = 915,
        PetbattleTargetNotCapturable = 916,
        PetbattleNotATrainer = 917,
        PetbattleDeclined = 918,
        PetbattleInBattle = 919,
        PetbattleInvalidLoadout = 920,
        PetbattleAllPetsDead = 921,
        PetbattleNoPetsInSlots = 922,
        PetbattleNoAccountLock = 923,
        PetbattleWildPetTapped = 924,
        PetbattleRestrictedAccount = 925,
        PetbattleOpponentNotAvailable = 926,
        PetbattleNotWhileInMatchedBattle = 927,
        CantHaveMorePetsOfThatType = 928,
        CantHaveMorePets = 929,
        PvpMapNotFound = 930,
        PvpMapNotSet = 931,
        PetbattleQueueQueued = 932,
        PetbattleQueueAlreadyQueued = 933,
        PetbattleQueueJoinFailed = 934,
        PetbattleQueueJournalLock = 935,
        PetbattleQueueRemoved = 936,
        PetbattleQueueProposalDeclined = 937,
        PetbattleQueueProposalTimeout = 938,
        PetbattleQueueOpponentDeclined = 939,
        PetbattleQueueRequeuedInternal = 940,
        PetbattleQueueRequeuedRemoved = 941,
        PetbattleQueueSlotLocked = 942,
        PetbattleQueueSlotEmpty = 943,
        PetbattleQueueSlotNoTracker = 944,
        PetbattleQueueSlotNoSpecies = 945,
        PetbattleQueueSlotCantBattle = 946,
        PetbattleQueueSlotRevoked = 947,
        PetbattleQueueSlotDead = 948,
        PetbattleQueueSlotNoPet = 949,
        PetbattleQueueNotWhileNeutral = 950,
        PetbattleGameTimeLimitWarning = 951,
        PetbattleGameRoundsLimitWarning = 952,
        HasRestriction = 953,
        ItemUpgradeItemTooLowLevel = 954,
        ItemUpgradeNoPath = 955,
        ItemUpgradeNoMoreUpgrades = 956,
        BonusRollEmpty = 957,
        ChallengeModeFull = 958,
        ChallengeModeInProgress = 959,
        ChallengeModeIncorrectKeystone = 960,
        BattletagFriendNotFound = 961,
        BattletagFriendNotValid = 962,
        BattletagFriendNotAllowed = 963,
        BattletagFriendThrottled = 964,
        BattletagFriendSuccess = 965,
        PetTooHighLevelToUncage = 966,
        PetbattleInternal = 967,
        CantCagePetYet = 968,
        NoLootInChallengeMode = 969,
        QuestPetBattleVictoriesPvpIi = 970,
        RoleCheckAlreadyInProgress = 971,
        RecruitAFriendAccountLimit = 972,
        RecruitAFriendFailed = 973,
        SetLootPersonal = 974,
        SetLootMethodFailedCombat = 975,
        ReagentBankFull = 976,
        ReagentBankLocked = 977,
        GarrisonBuildingExists = 978,
        GarrisonInvalidPlot = 979,
        GarrisonInvalidBuildingid = 980,
        GarrisonInvalidPlotBuilding = 981,
        GarrisonRequiresBlueprint = 982,
        GarrisonNotEnoughCurrency = 983,
        GarrisonNotEnoughGold = 984,
        GarrisonCompleteMissionWrongFollowerType = 985,
        AlreadyUsingLfgList = 986,
        RestrictedAccountLfgListTrial = 987,
        ToyUseLimitReached = 988,
        ToyAlreadyKnown = 989,
        TransmogSetAlreadyKnown = 990,
        NotEnoughCurrency = 991,
        SpecIsDisabled = 992,
        FeatureRestrictedTrial = 993,
        CantBeObliterated = 994,
        CantBeScrapped = 995,
        ArtifactRelicDoesNotMatchArtifact = 996,
        MustEquipArtifact = 997,
        CantDoThatRightNow = 998,
        AffectingCombat = 999,
        EquipmentManagerCombatSwapS = 1000,
        EquipmentManagerBagsFull = 1001,
        EquipmentManagerMissingItemS = 1002,
        MovieRecordingWarningPerf = 1003,
        MovieRecordingWarningDiskFull = 1004,
        MovieRecordingWarningNoMovie = 1005,
        MovieRecordingWarningRequirements = 1006,
        MovieRecordingWarningCompressing = 1007,
        NoChallengeModeReward = 1008,
        ClaimedChallengeModeReward = 1009,
        ChallengeModePeriodResetSs = 1010,
        CantDoThatChallengeModeActive = 1011,
        TalentFailedRestArea = 1012,
        CannotAbandonLastPet = 1013,
        TestCvarSetSss = 1014,
        QuestTurnInFailReason = 1015,
        ClaimedChallengeModeRewardOld = 1016,
        TalentGrantedByAura = 1017,
        ChallengeModeAlreadyComplete = 1018,
        GlyphTargetNotAvailable = 1019,
        PvpWarmodeToggleOn = 1020,
        PvpWarmodeToggleOff = 1021,
        SpellFailedLevelRequirement = 1022,
        BattlegroundJoinRequiresLevel = 1023,
        BattlegroundJoinDisqualified = 1024,
        BattlegroundJoinDisqualifiedNoName = 1025,
        VoiceChatGenericUnableToConnect = 1026,
        VoiceChatServiceLost = 1027,
        VoiceChatChannelNameTooShort = 1028,
        VoiceChatChannelNameTooLong = 1029,
        VoiceChatChannelAlreadyExists = 1030,
        VoiceChatTargetNotFound = 1031,
        VoiceChatTooManyRequests = 1032,
        VoiceChatPlayerSilenced = 1033,
        VoiceChatParentalDisableAll = 1034,
        VoiceChatDisabled = 1035,
        NoPvpReward = 1036,
        ClaimedPvpReward = 1037,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1038,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1039,
        AzeriteEssenceSelectionFailedConditionFailed = 1040,
        AzeriteEssenceSelectionFailedRestArea = 1041,
        AzeriteEssenceSelectionFailedSlotLocked = 1042,
        AzeriteEssenceSelectionFailedNotAtForge = 1043,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1044,
        AzeriteEssenceSelectionFailedNotEquipped = 1045,
        SocketingRequiresPunchcardredGem = 1046,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1047,
        SocketingRequiresPunchcardyellowGem = 1048,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1049,
        SocketingRequiresPunchcardblueGem = 1050,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1051,
        SocketingRequiresDominationShard = 1052,
        SocketingDominationShardOnlyInDominationslot = 1053,
        SocketingRequiresCypherGem = 1054,
        SocketingCypherGemOnlyInCypherslot = 1055,
        LevelLinkingResultLinked = 1056,
        LevelLinkingResultUnlinked = 1057,
        ClubFinderErrorPostClub = 1058,
        ClubFinderErrorApplyClub = 1059,
        ClubFinderErrorRespondApplicant = 1060,
        ClubFinderErrorCancelApplication = 1061,
        ClubFinderErrorTypeAcceptApplication = 1062,
        ClubFinderErrorTypeNoInvitePermissions = 1063,
        ClubFinderErrorTypeNoPostingPermissions = 1064,
        ClubFinderErrorTypeApplicantList = 1065,
        ClubFinderErrorTypeApplicantListNoPerm = 1066,
        ClubFinderErrorTypeFinderNotAvailable = 1067,
        ClubFinderErrorTypeGetPostingIds = 1068,
        ClubFinderErrorTypeJoinApplication = 1069,
        ClubFinderErrorTypeRealmNotEligible = 1070,
        ClubFinderErrorTypeFlaggedRename = 1071,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1072,
        ItemInteractionNotEnoughGold = 1073,
        ItemInteractionNotEnoughCurrency = 1074,
        PlayerChoiceErrorPendingChoice = 1075,
        SoulbindInvalidConduit = 1076,
        SoulbindInvalidConduitItem = 1077,
        SoulbindInvalidTalent = 1078,
        SoulbindDuplicateConduit = 1079,
        ActivateSoulbindS = 1080,
        ActivateSoulbindFailedRestArea = 1081,
        CantUseProfanity = 1082,
        NotInPetBattle = 1083,
        NotInNpe = 1084,
        NoSpec = 1085,
        NoDominationshardOverwrite = 1086,
        UseWeeklyRewardsDisabled = 1087,
        CrossFactionGroupJoined = 1088,
        CantTargetUnfriendlyInOverworld = 1089
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
