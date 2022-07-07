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
        public static TimeSpan ReviveBattlePetsCooldown = TimeSpan.FromSeconds(180);

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
        public const int MaxNRQuestItems = 32;
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
        None = 0x0000,
        Visible = 0x0001,                   // makes visible in client (set or can be set at interaction with target of this faction)
        AtWar = 0x0002,                   // enable AtWar-button in client. player controlled (except opposition team always war state), Flag only set on initial creation
        Hidden = 0x0004,                   // hidden faction from reputation pane in client (player can gain reputation, but this update not sent to client)
        Header = 0x0008,                   // Display as header in UI
        Peaceful = 0x0010,
        Inactive = 0x0020,                   // player controlled (CMSG_SET_FACTION_INACTIVE)
        ShowPropagated = 0x0040,
        HeaderShowsBar = 0x0080,                   // Header has its own reputation bar
        CapitalCityForRaceChange = 0x0100,
        Guild = 0x0200,
        GarrisonInvasion = 0x0400
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

    public enum MapFlags
    {
        CanToggleDifficulty = 0x0100,
        FlexLocking = 0x8000, // All difficulties share completed encounters lock, not bound to a single instance id
                              // heroic difficulty flag overrides it and uses instance id bind
        Garrison = 0x4000000
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
        WardenClientBanDuration,
        WardenClientCheckHoldoff,
        WardenClientFailAction,
        WardenClientResponseDelay,
        WardenEnabled,
        WardenNumMemChecks,
        WardenNumOtherChecks,
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
        XpBoostDaymask
    }

    public enum TimerType
    {
        Pvp = 0,
        ChallengerMode = 1
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
        SpellFailedS = 51,
        ItemCooldown = 52,
        PotionCooldown = 53,
        FoodCooldown = 54,
        SpellCooldown = 55,
        AbilityCooldown = 56,
        SpellAlreadyKnownS = 57,
        PetSpellAlreadyKnownS = 58,
        ProficiencyGainedS = 59,
        SkillGainedS = 60,
        SkillUpSi = 61,
        LearnSpellS = 62,
        LearnAbilityS = 63,
        LearnPassiveS = 64,
        LearnRecipeS = 65,
        LearnCompanionS = 66,
        LearnMountS = 67,
        LearnToyS = 68,
        LearnHeirloomS = 69,
        LearnTransmogS = 70,
        AppearanceAlreadyLearned = 72,
        RevokeTransmogS = 73,
        InvitePlayerS = 74,
        InviteSelf = 75,
        InvitedToGroupSs = 76,
        InvitedAlreadyInGroupSs = 77,
        AlreadyInGroupS = 78,
        CrossRealmRaidInvite = 79,
        PlayerBusyS = 80,
        NewLeaderS = 81,
        NewLeaderYou = 82,
        NewGuideS = 83,
        NewGuideYou = 84,
        LeftGroupS = 85,
        LeftGroupYou = 86,
        GroupDisbanded = 87,
        DeclineGroupS = 88,
        JoinedGroupS = 89,
        UninviteYou = 90,
        BadPlayerNameS = 91,
        NotInGroup = 92,
        TargetNotInGroupS = 93,
        TargetNotInInstanceS = 94,
        NotInInstanceGroup = 95,
        GroupFull = 96,
        NotLeader = 97,
        PlayerDiedS = 98,
        GuildCreateS = 99,
        GuildInviteS = 100,
        InvitedToGuildSss = 101,
        AlreadyInGuildS = 102,
        AlreadyInvitedToGuildS = 103,
        InvitedToGuild = 104,
        AlreadyInGuild = 105,
        GuildAccept = 106,
        GuildDeclineS = 107,
        GuildDeclineAutoS = 108,
        GuildPermissions = 109,
        GuildJoinS = 110,
        GuildFounderS = 111,
        GuildPromoteSss = 112,
        GuildDemoteSs = 113,
        GuildDemoteSss = 114,
        GuildInviteSelf = 115,
        GuildQuitS = 116,
        GuildLeaveS = 117,
        GuildRemoveSs = 118,
        GuildRemoveSelf = 119,
        GuildDisbandS = 120,
        GuildDisbandSelf = 121,
        GuildLeaderS = 122,
        GuildLeaderSelf = 123,
        GuildPlayerNotFoundS = 124,
        GuildPlayerNotInGuildS = 125,
        GuildPlayerNotInGuild = 126,
        GuildCantPromoteS = 127,
        GuildCantDemoteS = 128,
        GuildNotInAGuild = 129,
        GuildInternal = 130,
        GuildLeaderIsS = 131,
        GuildLeaderChangedSs = 132,
        GuildDisbanded = 133,
        GuildNotAllied = 134,
        GuildLeaderLeave = 135,
        GuildRanksLocked = 136,
        GuildRankInUse = 137,
        GuildRankTooHighS = 138,
        GuildRankTooLowS = 139,
        GuildNameExistsS = 140,
        GuildWithdrawLimit = 141,
        GuildNotEnoughMoney = 142,
        GuildTooMuchMoney = 143,
        GuildBankConjuredItem = 144,
        GuildBankEquippedItem = 145,
        GuildBankBoundItem = 146,
        GuildBankQuestItem = 147,
        GuildBankWrappedItem = 148,
        GuildBankFull = 149,
        GuildBankWrongTab = 150,
        NoGuildCharter = 151,
        OutOfRange = 152,
        PlayerDead = 153,
        ClientLockedOut = 154,
        ClientOnTransport = 155,
        KilledByS = 156,
        LootLocked = 157,
        LootTooFar = 158,
        LootDidntKill = 159,
        LootBadFacing = 160,
        LootNotstanding = 161,
        LootStunned = 162,
        LootNoUi = 163,
        LootWhileInvulnerable = 164,
        NoLoot = 165,
        QuestAcceptedS = 166,
        QuestCompleteS = 167,
        QuestFailedS = 168,
        QuestFailedBagFullS = 169,
        QuestFailedMaxCountS = 170,
        QuestFailedLowLevel = 171,
        QuestFailedMissingItems = 172,
        QuestFailedWrongRace = 173,
        QuestFailedNotEnoughMoney = 174,
        QuestFailedExpansion = 175,
        QuestOnlyOneTimed = 176,
        QuestNeedPrereqs = 177,
        QuestNeedPrereqsCustom = 178,
        QuestAlreadyOn = 179,
        QuestAlreadyDone = 180,
        QuestAlreadyDoneDaily = 181,
        QuestHasInProgress = 182,
        QuestRewardExpI = 183,
        QuestRewardMoneyS = 184,
        QuestMustChoose = 185,
        QuestLogFull = 186,
        CombatDamageSsi = 187,
        InspectS = 188,
        CantUseItem = 189,
        CantUseItemInArena = 190,
        CantUseItemInRatedBattleground = 191,
        MustEquipItem = 192,
        PassiveAbility = 193,
        SkillNotFound2H = 194,
        NoAttackTarget = 195,
        InvalidAttackTarget = 196,
        AttackPvpTargetWhileUnflagged = 197,
        AttackStunned = 198,
        AttackPacified = 199,
        AttackMounted = 200,
        AttackFleeing = 201,
        AttackConfused = 202,
        AttackCharmed = 203,
        AttackDead = 204,
        AttackPreventedByMechanicS = 205,
        AttackChannel = 206,
        Taxisamenode = 207,
        Taxinosuchpath = 208,
        Taxiunspecifiedservererror = 209,
        Taxinotenoughmoney = 210,
        Taxitoofaraway = 211,
        Taxinovendornearby = 212,
        Taxinotvisited = 213,
        Taxiplayerbusy = 214,
        Taxiplayeralreadymounted = 215,
        Taxiplayershapeshifted = 216,
        Taxiplayermoving = 217,
        Taxinopaths = 218,
        Taxinoteligible = 219,
        Taxinotstanding = 220,
        NoReplyTarget = 221,
        GenericNoTarget = 222,
        InitiateTradeS = 223,
        TradeRequestS = 224,
        TradeBlockedS = 225,
        TradeTargetDead = 226,
        TradeTooFar = 227,
        TradeCancelled = 228,
        TradeComplete = 229,
        TradeBagFull = 230,
        TradeTargetBagFull = 231,
        TradeMaxCountExceeded = 232,
        TradeTargetMaxCountExceeded = 233,
        InventoryTradeTooManyUniqueItem = 234,
        AlreadyTrading = 235,
        MountInvalidmountee = 236,
        MountToofaraway = 237,
        MountAlreadymounted = 238,
        MountNotmountable = 239,
        MountNotyourpet = 240,
        MountOther = 241,
        MountLooting = 242,
        MountRacecantmount = 243,
        MountShapeshifted = 244,
        MountNoFavorites = 245,
        MountNoMounts = 246,
        DismountNopet = 247,
        DismountNotmounted = 248,
        DismountNotyourpet = 249,
        SpellFailedTotems = 250,
        SpellFailedReagents = 251,
        SpellFailedReagentsGeneric = 252,
        SpellFailedOptionalReagents = 253,
        CantTradeGold = 254,
        SpellFailedEquippedItem = 255,
        SpellFailedEquippedItemClassS = 256,
        SpellFailedShapeshiftFormS = 257,
        SpellFailedAnotherInProgress = 258,
        Badattackfacing = 259,
        Badattackpos = 260,
        ChestInUse = 261,
        UseCantOpen = 262,
        UseLocked = 263,
        DoorLocked = 264,
        ButtonLocked = 265,
        UseLockedWithItemS = 266,
        UseLockedWithSpellS = 267,
        UseLockedWithSpellKnownSi = 268,
        UseTooFar = 269,
        UseBadAngle = 270,
        UseObjectMoving = 271,
        UseSpellFocus = 272,
        UseDestroyed = 273,
        SetLootFreeforall = 274,
        SetLootRoundrobin = 275,
        SetLootMaster = 276,
        SetLootGroup = 277,
        SetLootThresholdS = 278,
        NewLootMasterS = 279,
        SpecifyMasterLooter = 280,
        LootSpecChangedS = 281,
        TameFailed = 282,
        ChatWhileDead = 283,
        ChatPlayerNotFoundS = 284,
        Newtaxipath = 285,
        NoPet = 286,
        Notyourpet = 287,
        PetNotRenameable = 288,
        QuestObjectiveCompleteS = 289,
        QuestUnknownComplete = 290,
        QuestAddKillSii = 291,
        QuestAddFoundSii = 292,
        QuestAddItemSii = 293,
        QuestAddPlayerKillSii = 294,
        Cannotcreatedirectory = 295,
        Cannotcreatefile = 296,
        PlayerWrongFaction = 297,
        PlayerIsNeutral = 298,
        BankslotFailedTooMany = 299,
        BankslotInsufficientFunds = 300,
        BankslotNotbanker = 301,
        FriendDbError = 302,
        FriendListFull = 303,
        FriendAddedS = 304,
        BattletagFriendAddedS = 305,
        FriendOnlineSs = 306,
        FriendOfflineS = 307,
        FriendNotFound = 308,
        FriendWrongFaction = 309,
        FriendRemovedS = 310,
        BattletagFriendRemovedS = 311,
        FriendError = 312,
        FriendAlreadyS = 313,
        FriendSelf = 314,
        FriendDeleted = 315,
        IgnoreFull = 316,
        IgnoreSelf = 317,
        IgnoreNotFound = 318,
        IgnoreAlreadyS = 319,
        IgnoreAddedS = 320,
        IgnoreRemovedS = 321,
        IgnoreAmbiguous = 322,
        IgnoreDeleted = 323,
        OnlyOneBolt = 324,
        OnlyOneAmmo = 325,
        SpellFailedEquippedSpecificItem = 326,
        WrongBagTypeSubclass = 327,
        CantWrapStackable = 328,
        CantWrapEquipped = 329,
        CantWrapWrapped = 330,
        CantWrapBound = 331,
        CantWrapUnique = 332,
        CantWrapBags = 333,
        OutOfMana = 334,
        OutOfRage = 335,
        OutOfFocus = 336,
        OutOfEnergy = 337,
        OutOfChi = 338,
        OutOfHealth = 339,
        OutOfRunes = 340,
        OutOfRunicPower = 341,
        OutOfSoulShards = 342,
        OutOfLunarPower = 343,
        OutOfHolyPower = 344,
        OutOfMaelstrom = 345,
        OutOfComboPoints = 346,
        OutOfInsanity = 347,
        OutOfArcaneCharges = 348,
        OutOfFury = 349,
        OutOfPain = 350,
        OutOfPowerDisplay = 351,
        LootGone = 352,
        MountForceddismount = 353,
        AutofollowTooFar = 354,
        UnitNotFound = 355,
        InvalidFollowTarget = 356,
        InvalidFollowPvpCombat = 357,
        InvalidFollowTargetPvpCombat = 358,
        InvalidInspectTarget = 359,
        GuildemblemSuccess = 360,
        GuildemblemInvalidTabardColors = 361,
        GuildemblemNoguild = 362,
        GuildemblemNotguildmaster = 363,
        GuildemblemNotenoughmoney = 364,
        GuildemblemInvalidvendor = 365,
        EmblemerrorNotabardgeoset = 366,
        SpellOutOfRange = 367,
        CommandNeedsTarget = 368,
        NoammoS = 369,
        Toobusytofollow = 370,
        DuelRequested = 371,
        DuelCancelled = 372,
        Deathbindalreadybound = 373,
        DeathbindSuccessS = 374,
        Noemotewhilerunning = 375,
        ZoneExplored = 376,
        ZoneExploredXp = 377,
        InvalidItemTarget = 378,
        InvalidQuestTarget = 379,
        IgnoringYouS = 380,
        FishNotHooked = 381,
        FishEscaped = 382,
        SpellFailedNotunsheathed = 383,
        PetitionOfferedS = 384,
        PetitionSigned = 385,
        PetitionSignedS = 386,
        PetitionDeclinedS = 387,
        PetitionAlreadySigned = 388,
        PetitionRestrictedAccountTrial = 389,
        PetitionAlreadySignedOther = 390,
        PetitionInGuild = 391,
        PetitionCreator = 392,
        PetitionNotEnoughSignatures = 393,
        PetitionNotSameServer = 394,
        PetitionFull = 395,
        PetitionAlreadySignedByS = 396,
        GuildNameInvalid = 397,
        SpellUnlearnedS = 398,
        PetSpellRooted = 399,
        PetSpellAffectingCombat = 400,
        PetSpellOutOfRange = 401,
        PetSpellNotBehind = 402,
        PetSpellTargetsDead = 403,
        PetSpellDead = 404,
        PetSpellNopath = 405,
        ItemCantBeDestroyed = 406,
        TicketAlreadyExists = 407,
        TicketCreateError = 408,
        TicketUpdateError = 409,
        TicketDbError = 410,
        TicketNoText = 411,
        TicketTextTooLong = 412,
        ObjectIsBusy = 413,
        ExhaustionWellrested = 414,
        ExhaustionRested = 415,
        ExhaustionNormal = 416,
        ExhaustionTired = 417,
        ExhaustionExhausted = 418,
        NoItemsWhileShapeshifted = 419,
        CantInteractShapeshifted = 420,
        RealmNotFound = 421,
        MailQuestItem = 422,
        MailBoundItem = 423,
        MailConjuredItem = 424,
        MailBag = 425,
        MailToSelf = 426,
        MailTargetNotFound = 427,
        MailDatabaseError = 428,
        MailDeleteItemError = 429,
        MailWrappedCod = 430,
        MailCantSendRealm = 431,
        MailTempReturnOutage = 432,
        MailSent = 433,
        NotHappyEnough = 434,
        UseCantImmune = 435,
        CantBeDisenchanted = 436,
        CantUseDisarmed = 437,
        AuctionDatabaseError = 438,
        AuctionHigherBid = 439,
        AuctionAlreadyBid = 440,
        AuctionOutbidS = 441,
        AuctionWonS = 442,
        AuctionRemovedS = 443,
        AuctionBidPlaced = 444,
        LogoutFailed = 445,
        QuestPushSuccessS = 446,
        QuestPushInvalidS = 447,
        QuestPushInvalidToRecipientS = 448,
        QuestPushAcceptedS = 449,
        QuestPushDeclinedS = 450,
        QuestPushBusyS = 451,
        QuestPushDeadS = 452,
        QuestPushDeadToRecipientS = 453,
        QuestPushLogFullS = 454,
        QuestPushLogFullToRecipientS = 455,
        QuestPushOnquestS = 456,
        QuestPushOnquestToRecipientS = 457,
        QuestPushAlreadyDoneS = 458,
        QuestPushAlreadyDoneToRecipientS = 459,
        QuestPushNotDailyS = 460,
        QuestPushTimerExpiredS = 461,
        QuestPushNotInPartyS = 462,
        QuestPushDifferentServerDailyS = 463,
        QuestPushDifferentServerDailyToRecipientS = 464,
        QuestPushNotAllowedS = 465,
        QuestPushPrerequisiteS = 466,
        QuestPushPrerequisiteToRecipientS = 467,
        QuestPushLowLevelS = 468,
        QuestPushLowLevelToRecipientS = 469,
        QuestPushHighLevelS = 470,
        QuestPushHighLevelToRecipientS = 471,
        QuestPushClassS = 472,
        QuestPushClassToRecipientS = 473,
        QuestPushRaceS = 474,
        QuestPushRaceToRecipientS = 475,
        QuestPushLowFactionS = 476,
        QuestPushLowFactionToRecipientS = 477,
        QuestPushExpansionS = 478,
        QuestPushExpansionToRecipientS = 479,
        QuestPushNotGarrisonOwnerS = 480,
        QuestPushNotGarrisonOwnerToRecipientS = 481,
        QuestPushWrongCovenantS = 482,
        QuestPushWrongCovenantToRecipientS = 483,
        QuestPushNewPlayerExperienceS = 484,
        QuestPushNewPlayerExperienceToRecipientS = 485,
        QuestPushWrongFactionS = 486,
        QuestPushWrongFactionToRecipientS = 487,
        RaidGroupLowlevel = 488,
        RaidGroupOnly = 489,
        RaidGroupFull = 490,
        RaidGroupRequirementsUnmatch = 491,
        CorpseIsNotInInstance = 492,
        PvpKillHonorable = 493,
        PvpKillDishonorable = 494,
        SpellFailedAlreadyAtFullHealth = 495,
        SpellFailedAlreadyAtFullMana = 496,
        SpellFailedAlreadyAtFullPowerS = 497,
        AutolootMoneyS = 498,
        GenericStunned = 499,
        GenericThrottle = 500,
        ClubFinderSearchingTooFast = 501,
        TargetStunned = 502,
        MustRepairDurability = 503,
        RaidYouJoined = 504,
        RaidYouLeft = 505,
        InstanceGroupJoinedWithParty = 506,
        InstanceGroupJoinedWithRaid = 507,
        RaidMemberAddedS = 508,
        RaidMemberRemovedS = 509,
        InstanceGroupAddedS = 510,
        InstanceGroupRemovedS = 511,
        ClickOnItemToFeed = 512,
        TooManyChatChannels = 513,
        LootRollPending = 514,
        LootPlayerNotFound = 515,
        NotInRaid = 516,
        LoggingOut = 517,
        TargetLoggingOut = 518,
        NotWhileMounted = 519,
        NotWhileShapeshifted = 520,
        NotInCombat = 521,
        NotWhileDisarmed = 522,
        PetBroken = 523,
        TalentWipeError = 524,
        SpecWipeError = 525,
        GlyphWipeError = 526,
        PetSpecWipeError = 527,
        FeignDeathResisted = 528,
        MeetingStoneInQueueS = 529,
        MeetingStoneLeftQueueS = 530,
        MeetingStoneOtherMemberLeft = 531,
        MeetingStonePartyKickedFromQueue = 532,
        MeetingStoneMemberStillInQueue = 533,
        MeetingStoneSuccess = 534,
        MeetingStoneInProgress = 535,
        MeetingStoneMemberAddedS = 536,
        MeetingStoneGroupFull = 537,
        MeetingStoneNotLeader = 538,
        MeetingStoneInvalidLevel = 539,
        MeetingStoneTargetNotInParty = 540,
        MeetingStoneTargetInvalidLevel = 541,
        MeetingStoneMustBeLeader = 542,
        MeetingStoneNoRaidGroup = 543,
        MeetingStoneNeedParty = 544,
        MeetingStoneNotFound = 545,
        MeetingStoneTargetInVehicle = 546,
        GuildemblemSame = 547,
        EquipTradeItem = 548,
        PvpToggleOn = 549,
        PvpToggleOff = 550,
        GroupJoinBattlegroundDeserters = 551,
        GroupJoinBattlegroundDead = 552,
        GroupJoinBattlegroundS = 553,
        GroupJoinBattlegroundFail = 554,
        GroupJoinBattlegroundTooMany = 555,
        SoloJoinBattlegroundS = 556,
        JoinSingleScenarioS = 557,
        BattlegroundTooManyQueues = 558,
        BattlegroundCannotQueueForRated = 559,
        BattledgroundQueuedForRated = 560,
        BattlegroundTeamLeftQueue = 561,
        BattlegroundNotInBattleground = 562,
        AlreadyInArenaTeamS = 563,
        InvalidPromotionCode = 564,
        BgPlayerJoinedSs = 565,
        BgPlayerLeftS = 566,
        RestrictedAccount = 567,
        RestrictedAccountTrial = 568,
        PlayTimeExceeded = 569,
        ApproachingPartialPlayTime = 570,
        ApproachingPartialPlayTime2 = 571,
        ApproachingNoPlayTime = 572,
        ApproachingNoPlayTime2 = 573,
        UnhealthyTime = 574,
        ChatRestrictedTrial = 575,
        ChatThrottled = 576,
        MailReachedCap = 577,
        InvalidRaidTarget = 578,
        RaidLeaderReadyCheckStartS = 579,
        ReadyCheckInProgress = 580,
        ReadyCheckThrottled = 581,
        DungeonDifficultyFailed = 582,
        DungeonDifficultyChangedS = 583,
        TradeWrongRealm = 584,
        TradeNotOnTaplist = 585,
        ChatPlayerAmbiguousS = 586,
        LootCantLootThatNow = 587,
        LootMasterInvFull = 588,
        LootMasterUniqueItem = 589,
        LootMasterOther = 590,
        FilteringYouS = 591,
        UsePreventedByMechanicS = 592,
        ItemUniqueEquippable = 593,
        LfgLeaderIsLfmS = 594,
        LfgPending = 595,
        CantSpeakLangage = 596,
        VendorMissingTurnins = 597,
        BattlegroundNotInTeam = 598,
        NotInBattleground = 599,
        NotEnoughHonorPoints = 600,
        NotEnoughArenaPoints = 601,
        SocketingRequiresMetaGem = 602,
        SocketingMetaGemOnlyInMetaslot = 603,
        SocketingRequiresHydraulicGem = 604,
        SocketingHydraulicGemOnlyInHydraulicslot = 605,
        SocketingRequiresCogwheelGem = 606,
        SocketingCogwheelGemOnlyInCogwheelslot = 607,
        SocketingItemTooLowLevel = 608,
        ItemMaxCountSocketed = 609,
        SystemDisabled = 610,
        QuestFailedTooManyDailyQuestsI = 611,
        ItemMaxCountEquippedSocketed = 612,
        ItemUniqueEquippableSocketed = 613,
        UserSquelched = 614,
        AccountSilenced = 615,
        PartyMemberSilenced = 616,
        PartyMemberSilencedLfgDelist = 617,
        TooMuchGold = 618,
        NotBarberSitting = 619,
        QuestFailedCais = 620,
        InviteRestrictedTrial = 621,
        VoiceIgnoreFull = 622,
        VoiceIgnoreSelf = 623,
        VoiceIgnoreNotFound = 624,
        VoiceIgnoreAlreadyS = 625,
        VoiceIgnoreAddedS = 626,
        VoiceIgnoreRemovedS = 627,
        VoiceIgnoreAmbiguous = 628,
        VoiceIgnoreDeleted = 629,
        UnknownMacroOptionS = 630,
        NotDuringArenaMatch = 631,
        NotInRatedBattleground = 632,
        PlayerSilenced = 633,
        PlayerUnsilenced = 634,
        ComsatDisconnect = 635,
        ComsatReconnectAttempt = 636,
        ComsatConnectFail = 637,
        MailInvalidAttachmentSlot = 638,
        MailTooManyAttachments = 639,
        MailInvalidAttachment = 640,
        MailAttachmentExpired = 641,
        VoiceChatParentalDisableMic = 642,
        ProfaneChatName = 643,
        PlayerSilencedEcho = 644,
        PlayerUnsilencedEcho = 645,
        LootCantLootThat = 646,
        ArenaExpiredCais = 647,
        GroupActionThrottled = 648,
        AlreadyPickpocketed = 649,
        NameInvalid = 650,
        NameNoName = 651,
        NameTooShort = 652,
        NameTooLong = 653,
        NameMixedLanguages = 654,
        NameProfane = 655,
        NameReserved = 656,
        NameThreeConsecutive = 657,
        NameInvalidSpace = 658,
        NameConsecutiveSpaces = 659,
        NameRussianConsecutiveSilentCharacters = 660,
        NameRussianSilentCharacterAtBeginningOrEnd = 661,
        NameDeclensionDoesntMatchBaseName = 662,
        RecruitAFriendNotLinked = 663,
        RecruitAFriendNotNow = 664,
        RecruitAFriendSummonLevelMax = 665,
        RecruitAFriendSummonCooldown = 666,
        RecruitAFriendSummonOffline = 667,
        RecruitAFriendInsufExpanLvl = 668,
        RecruitAFriendMapIncomingTransferNotAllowed = 669,
        NotSameAccount = 670,
        BadOnUseEnchant = 671,
        TradeSelf = 672,
        TooManySockets = 673,
        ItemMaxLimitCategoryCountExceededIs = 674,
        TradeTargetMaxLimitCategoryCountExceededIs = 675,
        ItemMaxLimitCategorySocketedExceededIs = 676,
        ItemMaxLimitCategoryEquippedExceededIs = 677,
        ShapeshiftFormCannotEquip = 678,
        ItemInventoryFullSatchel = 679,
        ScalingStatItemLevelExceeded = 680,
        ScalingStatItemLevelTooLow = 681,
        PurchaseLevelTooLow = 682,
        GroupSwapFailed = 683,
        InviteInCombat = 684,
        InvalidGlyphSlot = 685,
        GenericNoValidTargets = 686,
        CalendarEventAlertS = 687,
        PetLearnSpellS = 688,
        PetLearnAbilityS = 689,
        PetSpellUnlearnedS = 690,
        InviteUnknownRealm = 691,
        InviteNoPartyServer = 692,
        InvitePartyBusy = 693,
        PartyTargetAmbiguous = 694,
        PartyLfgInviteRaidLocked = 695,
        PartyLfgBootLimit = 696,
        PartyLfgBootCooldownS = 697,
        PartyLfgBootNotEligibleS = 698,
        PartyLfgBootInpatientTimerS = 699,
        PartyLfgBootInProgress = 700,
        PartyLfgBootTooFewPlayers = 701,
        PartyLfgBootVoteSucceeded = 702,
        PartyLfgBootVoteFailed = 703,
        PartyLfgBootInCombat = 704,
        PartyLfgBootDungeonComplete = 705,
        PartyLfgBootLootRolls = 706,
        PartyLfgBootVoteRegistered = 707,
        PartyPrivateGroupOnly = 708,
        PartyLfgTeleportInCombat = 709,
        RaidDisallowedByLevel = 710,
        RaidDisallowedByCrossRealm = 711,
        PartyRoleNotAvailable = 712,
        JoinLfgObjectFailed = 713,
        LfgRemovedLevelup = 714,
        LfgRemovedXpToggle = 715,
        LfgRemovedFactionChange = 716,
        BattlegroundInfoThrottled = 717,
        BattlegroundAlreadyIn = 718,
        ArenaTeamChangeFailedQueued = 719,
        ArenaTeamPermissions = 720,
        NotWhileFalling = 721,
        NotWhileMoving = 722,
        NotWhileFatigued = 723,
        MaxSockets = 724,
        MultiCastActionTotemS = 725,
        BattlegroundJoinLevelup = 726,
        RemoveFromPvpQueueXpGain = 727,
        BattlegroundJoinXpGain = 728,
        BattlegroundJoinMercenary = 729,
        BattlegroundJoinTooManyHealers = 730,
        BattlegroundJoinRatedTooManyHealers = 731,
        BattlegroundJoinTooManyTanks = 732,
        BattlegroundJoinTooManyDamage = 733,
        RaidDifficultyFailed = 734,
        RaidDifficultyChangedS = 735,
        LegacyRaidDifficultyChangedS = 736,
        RaidLockoutChangedS = 737,
        RaidConvertedToParty = 738,
        PartyConvertedToRaid = 739,
        PlayerDifficultyChangedS = 740,
        GmresponseDbError = 741,
        BattlegroundJoinRangeIndex = 742,
        ArenaJoinRangeIndex = 743,
        RemoveFromPvpQueueFactionChange = 744,
        BattlegroundJoinFailed = 745,
        BattlegroundJoinNoValidSpecForRole = 746,
        BattlegroundJoinRespec = 747,
        BattlegroundInvitationDeclined = 748,
        BattlegroundJoinTimedOut = 749,
        BattlegroundDupeQueue = 750,
        BattlegroundJoinMustCompleteQuest = 751,
        InBattlegroundRespec = 752,
        MailLimitedDurationItem = 753,
        YellRestrictedTrial = 754,
        ChatRaidRestrictedTrial = 755,
        LfgRoleCheckFailed = 756,
        LfgRoleCheckFailedTimeout = 757,
        LfgRoleCheckFailedNotViable = 758,
        LfgReadyCheckFailed = 759,
        LfgReadyCheckFailedTimeout = 760,
        LfgGroupFull = 761,
        LfgNoLfgObject = 762,
        LfgNoSlotsPlayer = 763,
        LfgNoSlotsParty = 764,
        LfgNoSpec = 765,
        LfgMismatchedSlots = 766,
        LfgMismatchedSlotsLocalXrealm = 767,
        LfgPartyPlayersFromDifferentRealms = 768,
        LfgMembersNotPresent = 769,
        LfgGetInfoTimeout = 770,
        LfgInvalidSlot = 771,
        LfgDeserterPlayer = 772,
        LfgDeserterParty = 773,
        LfgDead = 774,
        LfgRandomCooldownPlayer = 775,
        LfgRandomCooldownParty = 776,
        LfgTooManyMembers = 777,
        LfgTooFewMembers = 778,
        LfgProposalFailed = 779,
        LfgProposalDeclinedSelf = 780,
        LfgProposalDeclinedParty = 781,
        LfgNoSlotsSelected = 782,
        LfgNoRolesSelected = 783,
        LfgRoleCheckInitiated = 784,
        LfgReadyCheckInitiated = 785,
        LfgPlayerDeclinedRoleCheck = 786,
        LfgPlayerDeclinedReadyCheck = 787,
        LfgJoinedQueue = 788,
        LfgJoinedFlexQueue = 789,
        LfgJoinedRfQueue = 790,
        LfgJoinedScenarioQueue = 791,
        LfgJoinedWorldPvpQueue = 792,
        LfgJoinedBattlefieldQueue = 793,
        LfgJoinedList = 794,
        LfgLeftQueue = 795,
        LfgLeftList = 796,
        LfgRoleCheckAborted = 797,
        LfgReadyCheckAborted = 798,
        LfgCantUseBattleground = 799,
        LfgCantUseDungeons = 800,
        LfgReasonTooManyLfg = 801,
        InvalidTeleportLocation = 802,
        TooFarToInteract = 803,
        BattlegroundPlayersFromDifferentRealms = 804,
        DifficultyChangeCooldownS = 805,
        DifficultyChangeCombatCooldownS = 806,
        DifficultyChangeWorldstate = 807,
        DifficultyChangeEncounter = 808,
        DifficultyChangeCombat = 809,
        DifficultyChangePlayerBusy = 810,
        DifficultyChangeAlreadyStarted = 811,
        DifficultyChangeOtherHeroicS = 812,
        DifficultyChangeHeroicInstanceAlreadyRunning = 813,
        ArenaTeamPartySize = 814,
        SoloShuffleWargameGroupSize = 815,
        SoloShuffleWargameGroupComp = 816,
        PvpPlayerAbandoned = 817,
        QuestForceRemovedS = 818,
        AttackNoActions = 819,
        InRandomBg = 820,
        InNonRandomBg = 821,
        BnFriendSelf = 822,
        BnFriendAlready = 823,
        BnFriendBlocked = 824,
        BnFriendListFull = 825,
        BnFriendRequestSent = 826,
        BnBroadcastThrottle = 827,
        BgDeveloperOnly = 828,
        CurrencySpellSlotMismatch = 829,
        CurrencyNotTradable = 830,
        RequiresExpansionS = 831,
        QuestFailedSpell = 832,
        TalentFailedNotEnoughTalentsInPrimaryTree = 833,
        TalentFailedNoPrimaryTreeSelected = 834,
        TalentFailedCantRemoveTalent = 835,
        TalentFailedUnknown = 836,
        WargameRequestFailure = 837,
        RankRequiresAuthenticator = 838,
        GuildBankVoucherFailed = 839,
        WargameRequestSent = 840,
        RequiresAchievementI = 841,
        RefundResultExceedMaxCurrency = 842,
        CantBuyQuantity = 843,
        ItemIsBattlePayLocked = 844,
        PartyAlreadyInBattlegroundQueue = 845,
        PartyConfirmingBattlegroundQueue = 846,
        BattlefieldTeamPartySize = 847,
        InsuffTrackedCurrencyIs = 848,
        NotOnTournamentRealm = 849,
        GuildTrialAccountTrial = 850,
        GuildTrialAccountVeteran = 851,
        GuildUndeletableDueToLevel = 852,
        CantDoThatInAGroup = 853,
        GuildLeaderReplaced = 854,
        TransmogrifyCantEquip = 855,
        TransmogrifyInvalidItemType = 856,
        TransmogrifyNotSoulbound = 857,
        TransmogrifyInvalidSource = 858,
        TransmogrifyInvalidDestination = 859,
        TransmogrifyMismatch = 860,
        TransmogrifyLegendary = 861,
        TransmogrifySameItem = 862,
        TransmogrifySameAppearance = 863,
        TransmogrifyNotEquipped = 864,
        VoidDepositFull = 865,
        VoidWithdrawFull = 866,
        VoidStorageWrapped = 867,
        VoidStorageStackable = 868,
        VoidStorageUnbound = 869,
        VoidStorageRepair = 870,
        VoidStorageCharges = 871,
        VoidStorageQuest = 872,
        VoidStorageConjured = 873,
        VoidStorageMail = 874,
        VoidStorageBag = 875,
        VoidTransferStorageFull = 876,
        VoidTransferInvFull = 877,
        VoidTransferInternalError = 878,
        VoidTransferItemInvalid = 879,
        DifficultyDisabledInLfg = 880,
        VoidStorageUnique = 881,
        VoidStorageLoot = 882,
        VoidStorageHoliday = 883,
        VoidStorageDuration = 884,
        VoidStorageLoadFailed = 885,
        VoidStorageInvalidItem = 886,
        ParentalControlsChatMuted = 887,
        SorStartExperienceIncomplete = 888,
        SorInvalidEmail = 889,
        SorInvalidComment = 890,
        ChallengeModeResetCooldownS = 891,
        ChallengeModeResetKeystone = 892,
        PetJournalAlreadyInLoadout = 893,
        ReportSubmittedSuccessfully = 894,
        ReportSubmissionFailed = 895,
        SuggestionSubmittedSuccessfully = 896,
        BugSubmittedSuccessfully = 897,
        ChallengeModeEnabled = 898,
        ChallengeModeDisabled = 899,
        PetbattleCreateFailed = 900,
        PetbattleNotHere = 901,
        PetbattleNotHereOnTransport = 902,
        PetbattleNotHereUnevenGround = 903,
        PetbattleNotHereObstructed = 904,
        PetbattleNotWhileInCombat = 905,
        PetbattleNotWhileDead = 906,
        PetbattleNotWhileFlying = 907,
        PetbattleTargetInvalid = 908,
        PetbattleTargetOutOfRange = 909,
        PetbattleTargetNotCapturable = 910,
        PetbattleNotATrainer = 911,
        PetbattleDeclined = 912,
        PetbattleInBattle = 913,
        PetbattleInvalidLoadout = 914,
        PetbattleAllPetsDead = 915,
        PetbattleNoPetsInSlots = 916,
        PetbattleNoAccountLock = 917,
        PetbattleWildPetTapped = 918,
        PetbattleRestrictedAccount = 919,
        PetbattleOpponentNotAvailable = 920,
        PetbattleNotWhileInMatchedBattle = 921,
        CantHaveMorePetsOfThatType = 922,
        CantHaveMorePets = 923,
        PvpMapNotFound = 924,
        PvpMapNotSet = 925,
        PetbattleQueueQueued = 926,
        PetbattleQueueAlreadyQueued = 927,
        PetbattleQueueJoinFailed = 928,
        PetbattleQueueJournalLock = 929,
        PetbattleQueueRemoved = 930,
        PetbattleQueueProposalDeclined = 931,
        PetbattleQueueProposalTimeout = 932,
        PetbattleQueueOpponentDeclined = 933,
        PetbattleQueueRequeuedInternal = 934,
        PetbattleQueueRequeuedRemoved = 935,
        PetbattleQueueSlotLocked = 936,
        PetbattleQueueSlotEmpty = 937,
        PetbattleQueueSlotNoTracker = 938,
        PetbattleQueueSlotNoSpecies = 939,
        PetbattleQueueSlotCantBattle = 940,
        PetbattleQueueSlotRevoked = 941,
        PetbattleQueueSlotDead = 942,
        PetbattleQueueSlotNoPet = 943,
        PetbattleQueueNotWhileNeutral = 944,
        PetbattleGameTimeLimitWarning = 945,
        PetbattleGameRoundsLimitWarning = 946,
        HasRestriction = 947,
        ItemUpgradeItemTooLowLevel = 948,
        ItemUpgradeNoPath = 949,
        ItemUpgradeNoMoreUpgrades = 950,
        BonusRollEmpty = 951,
        ChallengeModeFull = 952,
        ChallengeModeInProgress = 953,
        ChallengeModeIncorrectKeystone = 954,
        BattletagFriendNotFound = 955,
        BattletagFriendNotValid = 956,
        BattletagFriendNotAllowed = 957,
        BattletagFriendThrottled = 958,
        BattletagFriendSuccess = 959,
        PetTooHighLevelToUncage = 960,
        PetbattleInternal = 961,
        CantCagePetYet = 962,
        NoLootInChallengeMode = 963,
        QuestPetBattleVictoriesPvpIi = 964,
        RoleCheckAlreadyInProgress = 965,
        RecruitAFriendAccountLimit = 966,
        RecruitAFriendFailed = 967,
        SetLootPersonal = 968,
        SetLootMethodFailedCombat = 969,
        ReagentBankFull = 970,
        ReagentBankLocked = 971,
        GarrisonBuildingExists = 972,
        GarrisonInvalidPlot = 973,
        GarrisonInvalidBuildingid = 974,
        GarrisonInvalidPlotBuilding = 975,
        GarrisonRequiresBlueprint = 976,
        GarrisonNotEnoughCurrency = 977,
        GarrisonNotEnoughGold = 978,
        GarrisonCompleteMissionWrongFollowerType = 979,
        AlreadyUsingLfgList = 980,
        RestrictedAccountLfgListTrial = 981,
        ToyUseLimitReached = 982,
        ToyAlreadyKnown = 983,
        TransmogSetAlreadyKnown = 984,
        NotEnoughCurrency = 985,
        SpecIsDisabled = 986,
        FeatureRestrictedTrial = 987,
        CantBeObliterated = 988,
        CantBeScrapped = 989,
        ArtifactRelicDoesNotMatchArtifact = 990,
        MustEquipArtifact = 991,
        CantDoThatRightNow = 992,
        AffectingCombat = 993,
        EquipmentManagerCombatSwapS = 994,
        EquipmentManagerBagsFull = 995,
        EquipmentManagerMissingItemS = 996,
        MovieRecordingWarningPerf = 997,
        MovieRecordingWarningDiskFull = 998,
        MovieRecordingWarningNoMovie = 999,
        MovieRecordingWarningRequirements = 1000,
        MovieRecordingWarningCompressing = 1001,
        NoChallengeModeReward = 1002,
        ClaimedChallengeModeReward = 1003,
        ChallengeModePeriodResetSs = 1004,
        CantDoThatChallengeModeActive = 1005,
        TalentFailedRestArea = 1006,
        CannotAbandonLastPet = 1007,
        TestCvarSetSss = 1008,
        QuestTurnInFailReason = 1009,
        ClaimedChallengeModeRewardOld = 1010,
        TalentGrantedByAura = 1011,
        ChallengeModeAlreadyComplete = 1012,
        GlyphTargetNotAvailable = 1013,
        PvpWarmodeToggleOn = 1014,
        PvpWarmodeToggleOff = 1015,
        SpellFailedLevelRequirement = 1016,
        BattlegroundJoinRequiresLevel = 1017,
        BattlegroundJoinDisqualified = 1018,
        BattlegroundJoinDisqualifiedNoName = 1019,
        VoiceChatGenericUnableToConnect = 1020,
        VoiceChatServiceLost = 1021,
        VoiceChatChannelNameTooShort = 1022,
        VoiceChatChannelNameTooLong = 1023,
        VoiceChatChannelAlreadyExists = 1024,
        VoiceChatTargetNotFound = 1025,
        VoiceChatTooManyRequests = 1026,
        VoiceChatPlayerSilenced = 1027,
        VoiceChatParentalDisableAll = 1028,
        VoiceChatDisabled = 1029,
        NoPvpReward = 1030,
        ClaimedPvpReward = 1031,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1032,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1033,
        AzeriteEssenceSelectionFailedConditionFailed = 1034,
        AzeriteEssenceSelectionFailedRestArea = 1035,
        AzeriteEssenceSelectionFailedSlotLocked = 1036,
        AzeriteEssenceSelectionFailedNotAtForge = 1037,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1038,
        AzeriteEssenceSelectionFailedNotEquipped = 1039,
        SocketingRequiresPunchcardredGem = 1040,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1041,
        SocketingRequiresPunchcardyellowGem = 1042,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1043,
        SocketingRequiresPunchcardblueGem = 1044,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1045,
        SocketingRequiresDominationShard = 1046,
        SocketingDominationShardOnlyInDominationslot = 1047,
        SocketingRequiresCypherGem = 1048,
        SocketingCypherGemOnlyInCypherslot = 1049,
        LevelLinkingResultLinked = 1050,
        LevelLinkingResultUnlinked = 1051,
        ClubFinderErrorPostClub = 1052,
        ClubFinderErrorApplyClub = 1053,
        ClubFinderErrorRespondApplicant = 1054,
        ClubFinderErrorCancelApplication = 1055,
        ClubFinderErrorTypeAcceptApplication = 1056,
        ClubFinderErrorTypeNoInvitePermissions = 1057,
        ClubFinderErrorTypeNoPostingPermissions = 1058,
        ClubFinderErrorTypeApplicantList = 1059,
        ClubFinderErrorTypeApplicantListNoPerm = 1060,
        ClubFinderErrorTypeFinderNotAvailable = 1061,
        ClubFinderErrorTypeGetPostingIds = 1062,
        ClubFinderErrorTypeJoinApplication = 1063,
        ClubFinderErrorTypeRealmNotEligible = 1064,
        ClubFinderErrorTypeFlaggedRename = 1065,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1066,
        ItemInteractionNotEnoughGold = 1067,
        ItemInteractionNotEnoughCurrency = 1068,
        PlayerChoiceErrorPendingChoice = 1069,
        SoulbindInvalidConduit = 1070,
        SoulbindInvalidConduitItem = 1071,
        SoulbindInvalidTalent = 1072,
        SoulbindDuplicateConduit = 1073,
        ActivateSoulbindS = 1074,
        ActivateSoulbindFailedRestArea = 1075,
        CantUseProfanity = 1076,
        NotInPetBattle = 1077,
        NotInNpe = 1078,
        NoSpec = 1079,
        NoDominationshardOverwrite = 1080,
        UseWeeklyRewardsDisabled = 1081,
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
        Lootable = 0x0001
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

        CurrencyResetTime = 20001,                     // Next Arena Distribution Time
        WeeklyQuestResetTime = 20002,                     // Next Weekly Quest Reset Time
        BgDailyResetTime = 20003,                     // Next Daily Bg Reset Time
        CleaningFlags = 20004,                     // Cleaning Flags
        GuildDailyResetTime = 20006,                     // Next Guild Cap Reset Time
        MonthlyQuestResetTime = 20007,                     // Next Monthly Quest Reset Time
        DailyQuestResetTime = 20008,                     // Next Daily Quest Reset Time
        DailyCalendarDeletionOldEventsTime = 20009,      // Next Daily Calendar Deletions Of Old Events Time
        GuildWeeklyResetTime = 20050,                     // Next Guild Week Reset Time
    }
}
