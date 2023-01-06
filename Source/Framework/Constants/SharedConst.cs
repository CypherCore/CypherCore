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
        public const int SmartActionParamCount = 7;
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
            | GetMaskForRace(Race.KulTiran) | GetMaskForRace(Race.DarkIronDwarf) | GetMaskForRace(Race.Vulpera) | GetMaskForRace(Race.MagharOrc) | GetMaskForRace(Race.MechaGnome) | GetMaskForRace(Race.DracthyrAlliance) | GetMaskForRace(Race.DracthyrHorde));

        public static ulong RaceMaskAlliance = (ulong)(GetMaskForRace(Race.Human) | GetMaskForRace(Race.Dwarf) | GetMaskForRace(Race.NightElf) | GetMaskForRace(Race.Gnome)
            | GetMaskForRace(Race.Draenei) | GetMaskForRace(Race.Worgen) | GetMaskForRace(Race.PandarenAlliance) | GetMaskForRace(Race.VoidElf) | GetMaskForRace(Race.LightforgedDraenei)
            | GetMaskForRace(Race.KulTiran) | GetMaskForRace(Race.DarkIronDwarf) | GetMaskForRace(Race.MechaGnome) | GetMaskForRace(Race.DracthyrAlliance));

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

        static int GetRaceBit(Race raceId)
        {
            switch (raceId)
            {
                case Race.Human:
                case Race.Orc:
                case Race.Dwarf:
                case Race.NightElf:
                case Race.Undead:
                case Race.Tauren:
                case Race.Gnome:
                case Race.Troll:
                case Race.Goblin:
                case Race.BloodElf:
                case Race.Draenei:
                case Race.Worgen:
                case Race.PandarenNeutral:
                case Race.PandarenAlliance:
                case Race.PandarenHorde:
                case Race.Nightborne:
                case Race.HighmountainTauren:
                case Race.VoidElf:
                case Race.LightforgedDraenei:
                case Race.ZandalariTroll:
                case Race.KulTiran:
                    return (int)raceId - 1;
                case Race.DarkIronDwarf:
                    return 11;
                case Race.Vulpera:
                    return 12;
                case Race.MagharOrc:
                    return 13;
                case Race.MechaGnome:
                    return 14;
                case Race.DracthyrAlliance:
                    return 16;
                case Race.DracthyrHorde:
                    return 15;
                default:
                    return -1;
            }
        }
        
        public static long GetMaskForRace(Race raceId)
        {
            int raceBit = GetRaceBit(raceId);
            return raceBit >= 0 && raceBit < sizeof(long) * 8 ? (1L << raceBit) : 0;
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
        Max = 20,

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
        GuildLeaderLeave = 146,
        GuildRanksLocked = 147,
        GuildRankInUse = 148,
        GuildRankTooHighS = 149,
        GuildRankTooLowS = 150,
        GuildNameExistsS = 151,
        GuildWithdrawLimit = 152,
        GuildNotEnoughMoney = 153,
        GuildTooMuchMoney = 154,
        GuildBankConjuredItem = 155,
        GuildBankEquippedItem = 156,
        GuildBankBoundItem = 157,
        GuildBankQuestItem = 158,
        GuildBankWrappedItem = 159,
        GuildBankFull = 160,
        GuildBankWrongTab = 161,
        NoGuildCharter = 162,
        OutOfRange = 163,
        PlayerDead = 164,
        ClientLockedOut = 165,
        ClientOnTransport = 166,
        KilledByS = 167,
        LootLocked = 168,
        LootTooFar = 169,
        LootDidntKill = 170,
        LootBadFacing = 171,
        LootNotstanding = 172,
        LootStunned = 173,
        LootNoUi = 174,
        LootWhileInvulnerable = 175,
        NoLoot = 176,
        QuestAcceptedS = 177,
        QuestCompleteS = 178,
        QuestFailedS = 179,
        QuestFailedBagFullS = 180,
        QuestFailedMaxCountS = 181,
        QuestFailedLowLevel = 182,
        QuestFailedMissingItems = 183,
        QuestFailedWrongRace = 184,
        QuestFailedNotEnoughMoney = 185,
        QuestFailedExpansion = 186,
        QuestOnlyOneTimed = 187,
        QuestNeedPrereqs = 188,
        QuestNeedPrereqsCustom = 189,
        QuestAlreadyOn = 190,
        QuestAlreadyDone = 191,
        QuestAlreadyDoneDaily = 192,
        QuestHasInProgress = 193,
        QuestRewardExpI = 194,
        QuestRewardMoneyS = 195,
        QuestMustChoose = 196,
        QuestLogFull = 197,
        CombatDamageSsi = 198,
        InspectS = 199,
        CantUseItem = 200,
        CantUseItemInArena = 201,
        CantUseItemInRatedBattleground = 202,
        MustEquipItem = 203,
        PassiveAbility = 204,
        H2skillnotfound = 205,
        NoAttackTarget = 206,
        InvalidAttackTarget = 207,
        AttackPvpTargetWhileUnflagged = 208,
        AttackStunned = 209,
        AttackPacified = 210,
        AttackMounted = 211,
        AttackFleeing = 212,
        AttackConfused = 213,
        AttackCharmed = 214,
        AttackDead = 215,
        AttackPreventedByMechanicS = 216,
        AttackChannel = 217,
        Taxisamenode = 218,
        Taxinosuchpath = 219,
        Taxiunspecifiedservererror = 220,
        Taxinotenoughmoney = 221,
        Taxitoofaraway = 222,
        Taxinovendornearby = 223,
        Taxinotvisited = 224,
        Taxiplayerbusy = 225,
        Taxiplayeralreadymounted = 226,
        Taxiplayershapeshifted = 227,
        Taxiplayermoving = 228,
        Taxinopaths = 229,
        Taxinoteligible = 230,
        Taxinotstanding = 231,
        Taxiincombat = 232,
        NoReplyTarget = 233,
        GenericNoTarget = 234,
        InitiateTradeS = 235,
        TradeRequestS = 236,
        TradeBlockedS = 237,
        TradeTargetDead = 238,
        TradeTooFar = 239,
        TradeCancelled = 240,
        TradeComplete = 241,
        TradeBagFull = 242,
        TradeTargetBagFull = 243,
        TradeMaxCountExceeded = 244,
        TradeTargetMaxCountExceeded = 245,
        InventoryTradeTooManyUniqueItem = 246,
        AlreadyTrading = 247,
        MountInvalidmountee = 248,
        MountToofaraway = 249,
        MountAlreadymounted = 250,
        MountNotmountable = 251,
        MountNotyourpet = 252,
        MountOther = 253,
        MountLooting = 254,
        MountRacecantmount = 255,
        MountShapeshifted = 256,
        MountNoFavorites = 257,
        MountNoMounts = 258,
        DismountNopet = 259,
        DismountNotmounted = 260,
        DismountNotyourpet = 261,
        SpellFailedTotems = 262,
        SpellFailedReagents = 263,
        SpellFailedReagentsGeneric = 264,
        SpellFailedOptionalReagents = 265,
        CantTradeGold = 266,
        SpellFailedEquippedItem = 267,
        SpellFailedEquippedItemClassS = 268,
        SpellFailedShapeshiftFormS = 269,
        SpellFailedAnotherInProgress = 270,
        Badattackfacing = 271,
        Badattackpos = 272,
        ChestInUse = 273,
        UseCantOpen = 274,
        UseLocked = 275,
        DoorLocked = 276,
        ButtonLocked = 277,
        UseLockedWithItemS = 278,
        UseLockedWithSpellS = 279,
        UseLockedWithSpellKnownSi = 280,
        UseTooFar = 281,
        UseBadAngle = 282,
        UseObjectMoving = 283,
        UseSpellFocus = 284,
        UseDestroyed = 285,
        SetLootFreeforall = 286,
        SetLootRoundrobin = 287,
        SetLootMaster = 288,
        SetLootGroup = 289,
        SetLootThresholdS = 290,
        NewLootMasterS = 291,
        SpecifyMasterLooter = 292,
        LootSpecChangedS = 293,
        TameFailed = 294,
        ChatWhileDead = 295,
        ChatPlayerNotFoundS = 296,
        Newtaxipath = 297,
        NoPet = 298,
        Notyourpet = 299,
        PetNotRenameable = 300,
        QuestObjectiveCompleteS = 301,
        QuestUnknownComplete = 302,
        QuestAddKillSii = 303,
        QuestAddFoundSii = 304,
        QuestAddItemSii = 305,
        QuestAddPlayerKillSii = 306,
        Cannotcreatedirectory = 307,
        Cannotcreatefile = 308,
        PlayerWrongFaction = 309,
        PlayerIsNeutral = 310,
        BankslotFailedTooMany = 311,
        BankslotInsufficientFunds = 312,
        BankslotNotbanker = 313,
        FriendDbError = 314,
        FriendListFull = 315,
        FriendAddedS = 316,
        BattletagFriendAddedS = 317,
        FriendOnlineSs = 318,
        FriendOfflineS = 319,
        FriendNotFound = 320,
        FriendWrongFaction = 321,
        FriendRemovedS = 322,
        BattletagFriendRemovedS = 323,
        FriendError = 324,
        FriendAlreadyS = 325,
        FriendSelf = 326,
        FriendDeleted = 327,
        IgnoreFull = 328,
        IgnoreSelf = 329,
        IgnoreNotFound = 330,
        IgnoreAlreadyS = 331,
        IgnoreAddedS = 332,
        IgnoreRemovedS = 333,
        IgnoreAmbiguous = 334,
        IgnoreDeleted = 335,
        OnlyOneBolt = 336,
        OnlyOneAmmo = 337,
        SpellFailedEquippedSpecificItem = 338,
        WrongBagTypeSubclass = 339,
        CantWrapStackable = 340,
        CantWrapEquipped = 341,
        CantWrapWrapped = 342,
        CantWrapBound = 343,
        CantWrapUnique = 344,
        CantWrapBags = 345,
        OutOfMana = 346,
        OutOfRage = 347,
        OutOfFocus = 348,
        OutOfEnergy = 349,
        OutOfChi = 350,
        OutOfHealth = 351,
        OutOfRunes = 352,
        OutOfRunicPower = 353,
        OutOfSoulShards = 354,
        OutOfLunarPower = 355,
        OutOfHolyPower = 356,
        OutOfMaelstrom = 357,
        OutOfComboPoints = 358,
        OutOfInsanity = 359,
        OutOfEssence = 360,
        OutOfArcaneCharges = 361,
        OutOfFury = 362,
        OutOfPain = 363,
        OutOfPowerDisplay = 364,
        LootGone = 365,
        MountForceddismount = 366,
        AutofollowTooFar = 367,
        UnitNotFound = 368,
        InvalidFollowTarget = 369,
        InvalidFollowPvpCombat = 370,
        InvalidFollowTargetPvpCombat = 371,
        InvalidInspectTarget = 372,
        GuildemblemSuccess = 373,
        GuildemblemInvalidTabardColors = 374,
        GuildemblemNoguild = 375,
        GuildemblemNotguildmaster = 376,
        GuildemblemNotenoughmoney = 377,
        GuildemblemInvalidvendor = 378,
        EmblemerrorNotabardgeoset = 379,
        SpellOutOfRange = 380,
        CommandNeedsTarget = 381,
        NoammoS = 382,
        Toobusytofollow = 383,
        DuelRequested = 384,
        DuelCancelled = 385,
        Deathbindalreadybound = 386,
        DeathbindSuccessS = 387,
        Noemotewhilerunning = 388,
        ZoneExplored = 389,
        ZoneExploredXp = 390,
        InvalidItemTarget = 391,
        InvalidQuestTarget = 392,
        IgnoringYouS = 393,
        FishNotHooked = 394,
        FishEscaped = 395,
        SpellFailedNotunsheathed = 396,
        PetitionOfferedS = 397,
        PetitionSigned = 398,
        PetitionSignedS = 399,
        PetitionDeclinedS = 400,
        PetitionAlreadySigned = 401,
        PetitionRestrictedAccountTrial = 402,
        PetitionAlreadySignedOther = 403,
        PetitionInGuild = 404,
        PetitionCreator = 405,
        PetitionNotEnoughSignatures = 406,
        PetitionNotSameServer = 407,
        PetitionFull = 408,
        PetitionAlreadySignedByS = 409,
        GuildNameInvalid = 410,
        SpellUnlearnedS = 411,
        PetSpellRooted = 412,
        PetSpellAffectingCombat = 413,
        PetSpellOutOfRange = 414,
        PetSpellNotBehind = 415,
        PetSpellTargetsDead = 416,
        PetSpellDead = 417,
        PetSpellNopath = 418,
        ItemCantBeDestroyed = 419,
        TicketAlreadyExists = 420,
        TicketCreateError = 421,
        TicketUpdateError = 422,
        TicketDbError = 423,
        TicketNoText = 424,
        TicketTextTooLong = 425,
        ObjectIsBusy = 426,
        ExhaustionWellrested = 427,
        ExhaustionRested = 428,
        ExhaustionNormal = 429,
        ExhaustionTired = 430,
        ExhaustionExhausted = 431,
        NoItemsWhileShapeshifted = 432,
        CantInteractShapeshifted = 433,
        RealmNotFound = 434,
        MailQuestItem = 435,
        MailBoundItem = 436,
        MailConjuredItem = 437,
        MailBag = 438,
        MailToSelf = 439,
        MailTargetNotFound = 440,
        MailDatabaseError = 441,
        MailDeleteItemError = 442,
        MailWrappedCod = 443,
        MailCantSendRealm = 444,
        MailTempReturnOutage = 445,
        MailRecepientCantReceiveMail = 446,
        MailSent = 447,
        MailTargetIsTrial = 448,
        NotHappyEnough = 449,
        UseCantImmune = 450,
        CantBeDisenchanted = 451,
        CantUseDisarmed = 452,
        AuctionDatabaseError = 453,
        AuctionHigherBid = 454,
        AuctionAlreadyBid = 455,
        AuctionOutbidS = 456,
        AuctionWonS = 457,
        AuctionRemovedS = 458,
        AuctionBidPlaced = 459,
        LogoutFailed = 460,
        QuestPushSuccessS = 461,
        QuestPushInvalidS = 462,
        QuestPushInvalidToRecipientS = 463,
        QuestPushAcceptedS = 464,
        QuestPushDeclinedS = 465,
        QuestPushBusyS = 466,
        QuestPushDeadS = 467,
        QuestPushDeadToRecipientS = 468,
        QuestPushLogFullS = 469,
        QuestPushLogFullToRecipientS = 470,
        QuestPushOnquestS = 471,
        QuestPushOnquestToRecipientS = 472,
        QuestPushAlreadyDoneS = 473,
        QuestPushAlreadyDoneToRecipientS = 474,
        QuestPushNotDailyS = 475,
        QuestPushTimerExpiredS = 476,
        QuestPushNotInPartyS = 477,
        QuestPushDifferentServerDailyS = 478,
        QuestPushDifferentServerDailyToRecipientS = 479,
        QuestPushNotAllowedS = 480,
        QuestPushPrerequisiteS = 481,
        QuestPushPrerequisiteToRecipientS = 482,
        QuestPushLowLevelS = 483,
        QuestPushLowLevelToRecipientS = 484,
        QuestPushHighLevelS = 485,
        QuestPushHighLevelToRecipientS = 486,
        QuestPushClassS = 487,
        QuestPushClassToRecipientS = 488,
        QuestPushRaceS = 489,
        QuestPushRaceToRecipientS = 490,
        QuestPushLowFactionS = 491,
        QuestPushLowFactionToRecipientS = 492,
        QuestPushExpansionS = 493,
        QuestPushExpansionToRecipientS = 494,
        QuestPushNotGarrisonOwnerS = 495,
        QuestPushNotGarrisonOwnerToRecipientS = 496,
        QuestPushWrongCovenantS = 497,
        QuestPushWrongCovenantToRecipientS = 498,
        QuestPushNewPlayerExperienceS = 499,
        QuestPushNewPlayerExperienceToRecipientS = 500,
        QuestPushWrongFactionS = 501,
        QuestPushWrongFactionToRecipientS = 502,
        QuestPushCrossFactionRestrictedS = 503,
        RaidGroupLowlevel = 504,
        RaidGroupOnly = 505,
        RaidGroupFull = 506,
        RaidGroupRequirementsUnmatch = 507,
        CorpseIsNotInInstance = 508,
        PvpKillHonorable = 509,
        PvpKillDishonorable = 510,
        SpellFailedAlreadyAtFullHealth = 511,
        SpellFailedAlreadyAtFullMana = 512,
        SpellFailedAlreadyAtFullPowerS = 513,
        AutolootMoneyS = 514,
        GenericStunned = 515,
        GenericThrottle = 516,
        ClubFinderSearchingTooFast = 517,
        TargetStunned = 518,
        MustRepairDurability = 519,
        RaidYouJoined = 520,
        RaidYouLeft = 521,
        InstanceGroupJoinedWithParty = 522,
        InstanceGroupJoinedWithRaid = 523,
        RaidMemberAddedS = 524,
        RaidMemberRemovedS = 525,
        InstanceGroupAddedS = 526,
        InstanceGroupRemovedS = 527,
        ClickOnItemToFeed = 528,
        TooManyChatChannels = 529,
        LootRollPending = 530,
        LootPlayerNotFound = 531,
        NotInRaid = 532,
        LoggingOut = 533,
        TargetLoggingOut = 534,
        NotWhileMounted = 535,
        NotWhileShapeshifted = 536,
        NotInCombat = 537,
        NotWhileDisarmed = 538,
        PetBroken = 539,
        TalentWipeError = 540,
        SpecWipeError = 541,
        GlyphWipeError = 542,
        PetSpecWipeError = 543,
        FeignDeathResisted = 544,
        MeetingStoneInQueueS = 545,
        MeetingStoneLeftQueueS = 546,
        MeetingStoneOtherMemberLeft = 547,
        MeetingStonePartyKickedFromQueue = 548,
        MeetingStoneMemberStillInQueue = 549,
        MeetingStoneSuccess = 550,
        MeetingStoneInProgress = 551,
        MeetingStoneMemberAddedS = 552,
        MeetingStoneGroupFull = 553,
        MeetingStoneNotLeader = 554,
        MeetingStoneInvalidLevel = 555,
        MeetingStoneTargetNotInParty = 556,
        MeetingStoneTargetInvalidLevel = 557,
        MeetingStoneMustBeLeader = 558,
        MeetingStoneNoRaidGroup = 559,
        MeetingStoneNeedParty = 560,
        MeetingStoneNotFound = 561,
        MeetingStoneTargetInVehicle = 562,
        GuildemblemSame = 563,
        EquipTradeItem = 564,
        PvpToggleOn = 565,
        PvpToggleOff = 566,
        GroupJoinBattlegroundDeserters = 567,
        GroupJoinBattlegroundDead = 568,
        GroupJoinBattlegroundS = 569,
        GroupJoinBattlegroundFail = 570,
        GroupJoinBattlegroundTooMany = 571,
        SoloJoinBattlegroundS = 572,
        JoinSingleScenarioS = 573,
        BattlegroundTooManyQueues = 574,
        BattlegroundCannotQueueForRated = 575,
        BattledgroundQueuedForRated = 576,
        BattlegroundTeamLeftQueue = 577,
        BattlegroundNotInBattleground = 578,
        AlreadyInArenaTeamS = 579,
        InvalidPromotionCode = 580,
        BgPlayerJoinedSs = 581,
        BgPlayerLeftS = 582,
        RestrictedAccount = 583,
        RestrictedAccountTrial = 584,
        PlayTimeExceeded = 585,
        ApproachingPartialPlayTime = 586,
        ApproachingPartialPlayTime2 = 587,
        ApproachingNoPlayTime = 588,
        ApproachingNoPlayTime2 = 589,
        UnhealthyTime = 590,
        ChatRestrictedTrial = 591,
        ChatThrottled = 592,
        MailReachedCap = 593,
        InvalidRaidTarget = 594,
        RaidLeaderReadyCheckStartS = 595,
        ReadyCheckInProgress = 596,
        ReadyCheckThrottled = 597,
        DungeonDifficultyFailed = 598,
        DungeonDifficultyChangedS = 599,
        TradeWrongRealm = 600,
        TradeNotOnTaplist = 601,
        ChatPlayerAmbiguousS = 602,
        LootCantLootThatNow = 603,
        LootMasterInvFull = 604,
        LootMasterUniqueItem = 605,
        LootMasterOther = 606,
        FilteringYouS = 607,
        UsePreventedByMechanicS = 608,
        ItemUniqueEquippable = 609,
        LfgLeaderIsLfmS = 610,
        LfgPending = 611,
        CantSpeakLangage = 612,
        VendorMissingTurnins = 613,
        BattlegroundNotInTeam = 614,
        NotInBattleground = 615,
        NotEnoughHonorPoints = 616,
        NotEnoughArenaPoints = 617,
        SocketingRequiresMetaGem = 618,
        SocketingMetaGemOnlyInMetaslot = 619,
        SocketingRequiresHydraulicGem = 620,
        SocketingHydraulicGemOnlyInHydraulicslot = 621,
        SocketingRequiresCogwheelGem = 622,
        SocketingCogwheelGemOnlyInCogwheelslot = 623,
        SocketingItemTooLowLevel = 624,
        ItemMaxCountSocketed = 625,
        SystemDisabled = 626,
        QuestFailedTooManyDailyQuestsI = 627,
        ItemMaxCountEquippedSocketed = 628,
        ItemUniqueEquippableSocketed = 629,
        UserSquelched = 630,
        AccountSilenced = 631,
        PartyMemberSilenced = 632,
        PartyMemberSilencedLfgDelist = 633,
        TooMuchGold = 634,
        NotBarberSitting = 635,
        QuestFailedCais = 636,
        InviteRestrictedTrial = 637,
        VoiceIgnoreFull = 638,
        VoiceIgnoreSelf = 639,
        VoiceIgnoreNotFound = 640,
        VoiceIgnoreAlreadyS = 641,
        VoiceIgnoreAddedS = 642,
        VoiceIgnoreRemovedS = 643,
        VoiceIgnoreAmbiguous = 644,
        VoiceIgnoreDeleted = 645,
        UnknownMacroOptionS = 646,
        NotDuringArenaMatch = 647,
        NotInRatedBattleground = 648,
        PlayerSilenced = 649,
        PlayerUnsilenced = 650,
        ComsatDisconnect = 651,
        ComsatReconnectAttempt = 652,
        ComsatConnectFail = 653,
        MailInvalidAttachmentSlot = 654,
        MailTooManyAttachments = 655,
        MailInvalidAttachment = 656,
        MailAttachmentExpired = 657,
        VoiceChatParentalDisableMic = 658,
        ProfaneChatName = 659,
        PlayerSilencedEcho = 660,
        PlayerUnsilencedEcho = 661,
        LootCantLootThat = 662,
        ArenaExpiredCais = 663,
        GroupActionThrottled = 664,
        AlreadyPickpocketed = 665,
        NameInvalid = 666,
        NameNoName = 667,
        NameTooShort = 668,
        NameTooLong = 669,
        NameMixedLanguages = 670,
        NameProfane = 671,
        NameReserved = 672,
        NameThreeConsecutive = 673,
        NameInvalidSpace = 674,
        NameConsecutiveSpaces = 675,
        NameRussianConsecutiveSilentCharacters = 676,
        NameRussianSilentCharacterAtBeginningOrEnd = 677,
        NameDeclensionDoesntMatchBaseName = 678,
        RecruitAFriendNotLinked = 679,
        RecruitAFriendNotNow = 680,
        RecruitAFriendSummonLevelMax = 681,
        RecruitAFriendSummonCooldown = 682,
        RecruitAFriendSummonOffline = 683,
        RecruitAFriendInsufExpanLvl = 684,
        RecruitAFriendMapIncomingTransferNotAllowed = 685,
        NotSameAccount = 686,
        BadOnUseEnchant = 687,
        TradeSelf = 688,
        TooManySockets = 689,
        ItemMaxLimitCategoryCountExceededIs = 690,
        TradeTargetMaxLimitCategoryCountExceededIs = 691,
        ItemMaxLimitCategorySocketedExceededIs = 692,
        ItemMaxLimitCategoryEquippedExceededIs = 693,
        ShapeshiftFormCannotEquip = 694,
        ItemInventoryFullSatchel = 695,
        ScalingStatItemLevelExceeded = 696,
        ScalingStatItemLevelTooLow = 697,
        PurchaseLevelTooLow = 698,
        GroupSwapFailed = 699,
        InviteInCombat = 700,
        InvalidGlyphSlot = 701,
        GenericNoValidTargets = 702,
        CalendarEventAlertS = 703,
        PetLearnSpellS = 704,
        PetLearnAbilityS = 705,
        PetSpellUnlearnedS = 706,
        InviteUnknownRealm = 707,
        InviteNoPartyServer = 708,
        InvitePartyBusy = 709,
        InvitePartyBusyPendingRequest = 710,
        InvitePartyBusyPendingSuggest = 711,
        PartyTargetAmbiguous = 712,
        PartyLfgInviteRaidLocked = 713,
        PartyLfgBootLimit = 714,
        PartyLfgBootCooldownS = 715,
        PartyLfgBootNotEligibleS = 716,
        PartyLfgBootInpatientTimerS = 717,
        PartyLfgBootInProgress = 718,
        PartyLfgBootTooFewPlayers = 719,
        PartyLfgBootVoteSucceeded = 720,
        PartyLfgBootVoteFailed = 721,
        PartyLfgBootInCombat = 722,
        PartyLfgBootDungeonComplete = 723,
        PartyLfgBootLootRolls = 724,
        PartyLfgBootVoteRegistered = 725,
        PartyPrivateGroupOnly = 726,
        PartyLfgTeleportInCombat = 727,
        RaidDisallowedByLevel = 728,
        RaidDisallowedByCrossRealm = 729,
        PartyRoleNotAvailable = 730,
        JoinLfgObjectFailed = 731,
        LfgRemovedLevelup = 732,
        LfgRemovedXpToggle = 733,
        LfgRemovedFactionChange = 734,
        BattlegroundInfoThrottled = 735,
        BattlegroundAlreadyIn = 736,
        ArenaTeamChangeFailedQueued = 737,
        ArenaTeamPermissions = 738,
        NotWhileFalling = 739,
        NotWhileMoving = 740,
        NotWhileFatigued = 741,
        MaxSockets = 742,
        MultiCastActionTotemS = 743,
        BattlegroundJoinLevelup = 744,
        RemoveFromPvpQueueXpGain = 745,
        BattlegroundJoinXpGain = 746,
        BattlegroundJoinMercenary = 747,
        BattlegroundJoinTooManyHealers = 748,
        BattlegroundJoinRatedTooManyHealers = 749,
        BattlegroundJoinTooManyTanks = 750,
        BattlegroundJoinTooManyDamage = 751,
        RaidDifficultyFailed = 752,
        RaidDifficultyChangedS = 753,
        LegacyRaidDifficultyChangedS = 754,
        RaidLockoutChangedS = 755,
        RaidConvertedToParty = 756,
        PartyConvertedToRaid = 757,
        PlayerDifficultyChangedS = 758,
        GmresponseDbError = 759,
        BattlegroundJoinRangeIndex = 760,
        ArenaJoinRangeIndex = 761,
        RemoveFromPvpQueueFactionChange = 762,
        BattlegroundJoinFailed = 763,
        BattlegroundJoinNoValidSpecForRole = 764,
        BattlegroundJoinRespec = 765,
        BattlegroundInvitationDeclined = 766,
        BattlegroundJoinTimedOut = 767,
        BattlegroundDupeQueue = 768,
        BattlegroundJoinMustCompleteQuest = 769,
        InBattlegroundRespec = 770,
        MailLimitedDurationItem = 771,
        YellRestrictedTrial = 772,
        ChatRaidRestrictedTrial = 773,
        LfgRoleCheckFailed = 774,
        LfgRoleCheckFailedTimeout = 775,
        LfgRoleCheckFailedNotViable = 776,
        LfgReadyCheckFailed = 777,
        LfgReadyCheckFailedTimeout = 778,
        LfgGroupFull = 779,
        LfgNoLfgObject = 780,
        LfgNoSlotsPlayer = 781,
        LfgNoSlotsParty = 782,
        LfgNoSpec = 783,
        LfgMismatchedSlots = 784,
        LfgMismatchedSlotsLocalXrealm = 785,
        LfgPartyPlayersFromDifferentRealms = 786,
        LfgMembersNotPresent = 787,
        LfgGetInfoTimeout = 788,
        LfgInvalidSlot = 789,
        LfgDeserterPlayer = 790,
        LfgDeserterParty = 791,
        LfgDead = 792,
        LfgRandomCooldownPlayer = 793,
        LfgRandomCooldownParty = 794,
        LfgTooManyMembers = 795,
        LfgTooFewMembers = 796,
        LfgProposalFailed = 797,
        LfgProposalDeclinedSelf = 798,
        LfgProposalDeclinedParty = 799,
        LfgNoSlotsSelected = 800,
        LfgNoRolesSelected = 801,
        LfgRoleCheckInitiated = 802,
        LfgReadyCheckInitiated = 803,
        LfgPlayerDeclinedRoleCheck = 804,
        LfgPlayerDeclinedReadyCheck = 805,
        LfgJoinedQueue = 806,
        LfgJoinedFlexQueue = 807,
        LfgJoinedRfQueue = 808,
        LfgJoinedScenarioQueue = 809,
        LfgJoinedWorldPvpQueue = 810,
        LfgJoinedBattlefieldQueue = 811,
        LfgJoinedList = 812,
        LfgLeftQueue = 813,
        LfgLeftList = 814,
        LfgRoleCheckAborted = 815,
        LfgReadyCheckAborted = 816,
        LfgCantUseBattleground = 817,
        LfgCantUseDungeons = 818,
        LfgReasonTooManyLfg = 819,
        LfgFarmLimit = 820,
        LfgNoCrossFactionParties = 821,
        InvalidTeleportLocation = 822,
        TooFarToInteract = 823,
        BattlegroundPlayersFromDifferentRealms = 824,
        DifficultyChangeCooldownS = 825,
        DifficultyChangeCombatCooldownS = 826,
        DifficultyChangeWorldstate = 827,
        DifficultyChangeEncounter = 828,
        DifficultyChangeCombat = 829,
        DifficultyChangePlayerBusy = 830,
        DifficultyChangeAlreadyStarted = 831,
        DifficultyChangeOtherHeroicS = 832,
        DifficultyChangeHeroicInstanceAlreadyRunning = 833,
        ArenaTeamPartySize = 834,
        SoloShuffleWargameGroupSize = 835,
        SoloShuffleWargameGroupComp = 836,
        SoloShuffleMinItemLevel = 837,
        PvpPlayerAbandoned = 838,
        QuestForceRemovedS = 839,
        AttackNoActions = 840,
        InRandomBg = 841,
        InNonRandomBg = 842,
        BnFriendSelf = 843,
        BnFriendAlready = 844,
        BnFriendBlocked = 845,
        BnFriendListFull = 846,
        BnFriendRequestSent = 847,
        BnBroadcastThrottle = 848,
        BgDeveloperOnly = 849,
        CurrencySpellSlotMismatch = 850,
        CurrencyNotTradable = 851,
        RequiresExpansionS = 852,
        QuestFailedSpell = 853,
        TalentFailedUnspentTalentPoints = 854,
        TalentFailedNotEnoughTalentsInPrimaryTree = 855,
        TalentFailedNoPrimaryTreeSelected = 856,
        TalentFailedCantRemoveTalent = 857,
        TalentFailedUnknown = 858,
        TalentFailedInCombat = 859,
        TalentFailedInPvpMatch = 860,
        TalentFailedInMythicPlus = 861,
        WargameRequestFailure = 862,
        RankRequiresAuthenticator = 863,
        GuildBankVoucherFailed = 864,
        WargameRequestSent = 865,
        RequiresAchievementI = 866,
        RefundResultExceedMaxCurrency = 867,
        CantBuyQuantity = 868,
        ItemIsBattlePayLocked = 869,
        PartyAlreadyInBattlegroundQueue = 870,
        PartyConfirmingBattlegroundQueue = 871,
        BattlefieldTeamPartySize = 872,
        InsuffTrackedCurrencyIs = 873,
        NotOnTournamentRealm = 874,
        GuildTrialAccountTrial = 875,
        GuildTrialAccountVeteran = 876,
        GuildUndeletableDueToLevel = 877,
        CantDoThatInAGroup = 878,
        GuildLeaderReplaced = 879,
        TransmogrifyCantEquip = 880,
        TransmogrifyInvalidItemType = 881,
        TransmogrifyNotSoulbound = 882,
        TransmogrifyInvalidSource = 883,
        TransmogrifyInvalidDestination = 884,
        TransmogrifyMismatch = 885,
        TransmogrifyLegendary = 886,
        TransmogrifySameItem = 887,
        TransmogrifySameAppearance = 888,
        TransmogrifyNotEquipped = 889,
        VoidDepositFull = 890,
        VoidWithdrawFull = 891,
        VoidStorageWrapped = 892,
        VoidStorageStackable = 893,
        VoidStorageUnbound = 894,
        VoidStorageRepair = 895,
        VoidStorageCharges = 896,
        VoidStorageQuest = 897,
        VoidStorageConjured = 898,
        VoidStorageMail = 899,
        VoidStorageBag = 900,
        VoidTransferStorageFull = 901,
        VoidTransferInvFull = 902,
        VoidTransferInternalError = 903,
        VoidTransferItemInvalid = 904,
        DifficultyDisabledInLfg = 905,
        VoidStorageUnique = 906,
        VoidStorageLoot = 907,
        VoidStorageHoliday = 908,
        VoidStorageDuration = 909,
        VoidStorageLoadFailed = 910,
        VoidStorageInvalidItem = 911,
        ParentalControlsChatMuted = 912,
        SorStartExperienceIncomplete = 913,
        SorInvalidEmail = 914,
        SorInvalidComment = 915,
        ChallengeModeResetCooldownS = 916,
        ChallengeModeResetKeystone = 917,
        PetJournalAlreadyInLoadout = 918,
        ReportSubmittedSuccessfully = 919,
        ReportSubmissionFailed = 920,
        SuggestionSubmittedSuccessfully = 921,
        BugSubmittedSuccessfully = 922,
        ChallengeModeEnabled = 923,
        ChallengeModeDisabled = 924,
        PetbattleCreateFailed = 925,
        PetbattleNotHere = 926,
        PetbattleNotHereOnTransport = 927,
        PetbattleNotHereUnevenGround = 928,
        PetbattleNotHereObstructed = 929,
        PetbattleNotWhileInCombat = 930,
        PetbattleNotWhileDead = 931,
        PetbattleNotWhileFlying = 932,
        PetbattleTargetInvalid = 933,
        PetbattleTargetOutOfRange = 934,
        PetbattleTargetNotCapturable = 935,
        PetbattleNotATrainer = 936,
        PetbattleDeclined = 937,
        PetbattleInBattle = 938,
        PetbattleInvalidLoadout = 939,
        PetbattleAllPetsDead = 940,
        PetbattleNoPetsInSlots = 941,
        PetbattleNoAccountLock = 942,
        PetbattleWildPetTapped = 943,
        PetbattleRestrictedAccount = 944,
        PetbattleOpponentNotAvailable = 945,
        PetbattleNotWhileInMatchedBattle = 946,
        CantHaveMorePetsOfThatType = 947,
        CantHaveMorePets = 948,
        PvpMapNotFound = 949,
        PvpMapNotSet = 950,
        PetbattleQueueQueued = 951,
        PetbattleQueueAlreadyQueued = 952,
        PetbattleQueueJoinFailed = 953,
        PetbattleQueueJournalLock = 954,
        PetbattleQueueRemoved = 955,
        PetbattleQueueProposalDeclined = 956,
        PetbattleQueueProposalTimeout = 957,
        PetbattleQueueOpponentDeclined = 958,
        PetbattleQueueRequeuedInternal = 959,
        PetbattleQueueRequeuedRemoved = 960,
        PetbattleQueueSlotLocked = 961,
        PetbattleQueueSlotEmpty = 962,
        PetbattleQueueSlotNoTracker = 963,
        PetbattleQueueSlotNoSpecies = 964,
        PetbattleQueueSlotCantBattle = 965,
        PetbattleQueueSlotRevoked = 966,
        PetbattleQueueSlotDead = 967,
        PetbattleQueueSlotNoPet = 968,
        PetbattleQueueNotWhileNeutral = 969,
        PetbattleGameTimeLimitWarning = 970,
        PetbattleGameRoundsLimitWarning = 971,
        HasRestriction = 972,
        ItemUpgradeItemTooLowLevel = 973,
        ItemUpgradeNoPath = 974,
        ItemUpgradeNoMoreUpgrades = 975,
        BonusRollEmpty = 976,
        ChallengeModeFull = 977,
        ChallengeModeInProgress = 978,
        ChallengeModeIncorrectKeystone = 979,
        BattletagFriendNotFound = 980,
        BattletagFriendNotValid = 981,
        BattletagFriendNotAllowed = 982,
        BattletagFriendThrottled = 983,
        BattletagFriendSuccess = 984,
        PetTooHighLevelToUncage = 985,
        PetbattleInternal = 986,
        CantCagePetYet = 987,
        NoLootInChallengeMode = 988,
        QuestPetBattleVictoriesPvpIi = 989,
        RoleCheckAlreadyInProgress = 990,
        RecruitAFriendAccountLimit = 991,
        RecruitAFriendFailed = 992,
        SetLootPersonal = 993,
        SetLootMethodFailedCombat = 994,
        ReagentBankFull = 995,
        ReagentBankLocked = 996,
        GarrisonBuildingExists = 997,
        GarrisonInvalidPlot = 998,
        GarrisonInvalidBuildingid = 999,
        GarrisonInvalidPlotBuilding = 1000,
        GarrisonRequiresBlueprint = 1001,
        GarrisonNotEnoughCurrency = 1002,
        GarrisonNotEnoughGold = 1003,
        GarrisonCompleteMissionWrongFollowerType = 1004,
        AlreadyUsingLfgList = 1005,
        RestrictedAccountLfgListTrial = 1006,
        ToyUseLimitReached = 1007,
        ToyAlreadyKnown = 1008,
        TransmogSetAlreadyKnown = 1009,
        NotEnoughCurrency = 1010,
        SpecIsDisabled = 1011,
        FeatureRestrictedTrial = 1012,
        CantBeObliterated = 1013,
        CantBeScrapped = 1014,
        CantBeRecrafted = 1015,
        ArtifactRelicDoesNotMatchArtifact = 1016,
        MustEquipArtifact = 1017,
        CantDoThatRightNow = 1018,
        AffectingCombat = 1019,
        EquipmentManagerCombatSwapS = 1020,
        EquipmentManagerBagsFull = 1021,
        EquipmentManagerMissingItemS = 1022,
        MovieRecordingWarningPerf = 1023,
        MovieRecordingWarningDiskFull = 1024,
        MovieRecordingWarningNoMovie = 1025,
        MovieRecordingWarningRequirements = 1026,
        MovieRecordingWarningCompressing = 1027,
        NoChallengeModeReward = 1028,
        ClaimedChallengeModeReward = 1029,
        ChallengeModePeriodResetSs = 1030,
        CantDoThatChallengeModeActive = 1031,
        TalentFailedRestArea = 1032,
        CannotAbandonLastPet = 1033,
        TestCvarSetSss = 1034,
        QuestTurnInFailReason = 1035,
        ClaimedChallengeModeRewardOld = 1036,
        TalentGrantedByAura = 1037,
        ChallengeModeAlreadyComplete = 1038,
        GlyphTargetNotAvailable = 1039,
        PvpWarmodeToggleOn = 1040,
        PvpWarmodeToggleOff = 1041,
        SpellFailedLevelRequirement = 1042,
        SpellFailedCantFlyHere = 1043,
        BattlegroundJoinRequiresLevel = 1044,
        BattlegroundJoinDisqualified = 1045,
        BattlegroundJoinDisqualifiedNoName = 1046,
        VoiceChatGenericUnableToConnect = 1047,
        VoiceChatServiceLost = 1048,
        VoiceChatChannelNameTooShort = 1049,
        VoiceChatChannelNameTooLong = 1050,
        VoiceChatChannelAlreadyExists = 1051,
        VoiceChatTargetNotFound = 1052,
        VoiceChatTooManyRequests = 1053,
        VoiceChatPlayerSilenced = 1054,
        VoiceChatParentalDisableAll = 1055,
        VoiceChatDisabled = 1056,
        NoPvpReward = 1057,
        ClaimedPvpReward = 1058,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1059,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1060,
        AzeriteEssenceSelectionFailedConditionFailed = 1061,
        AzeriteEssenceSelectionFailedRestArea = 1062,
        AzeriteEssenceSelectionFailedSlotLocked = 1063,
        AzeriteEssenceSelectionFailedNotAtForge = 1064,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1065,
        AzeriteEssenceSelectionFailedNotEquipped = 1066,
        SocketingRequiresPunchcardredGem = 1067,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1068,
        SocketingRequiresPunchcardyellowGem = 1069,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1070,
        SocketingRequiresPunchcardblueGem = 1071,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1072,
        SocketingRequiresDominationShard = 1073,
        SocketingDominationShardOnlyInDominationslot = 1074,
        SocketingRequiresCypherGem = 1075,
        SocketingCypherGemOnlyInCypherslot = 1076,
        SocketingRequiresTinkerGem = 1077,
        SocketingTinkerGemOnlyInTinkerslot = 1078,
        LevelLinkingResultLinked = 1079,
        LevelLinkingResultUnlinked = 1080,
        ClubFinderErrorPostClub = 1081,
        ClubFinderErrorApplyClub = 1082,
        ClubFinderErrorRespondApplicant = 1083,
        ClubFinderErrorCancelApplication = 1084,
        ClubFinderErrorTypeAcceptApplication = 1085,
        ClubFinderErrorTypeNoInvitePermissions = 1086,
        ClubFinderErrorTypeNoPostingPermissions = 1087,
        ClubFinderErrorTypeApplicantList = 1088,
        ClubFinderErrorTypeApplicantListNoPerm = 1089,
        ClubFinderErrorTypeFinderNotAvailable = 1090,
        ClubFinderErrorTypeGetPostingIds = 1091,
        ClubFinderErrorTypeJoinApplication = 1092,
        ClubFinderErrorTypeRealmNotEligible = 1093,
        ClubFinderErrorTypeFlaggedRename = 1094,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1095,
        ItemInteractionNotEnoughGold = 1096,
        ItemInteractionNotEnoughCurrency = 1097,
        PlayerChoiceErrorPendingChoice = 1098,
        SoulbindInvalidConduit = 1099,
        SoulbindInvalidConduitItem = 1100,
        SoulbindInvalidTalent = 1101,
        SoulbindDuplicateConduit = 1102,
        ActivateSoulbindS = 1103,
        ActivateSoulbindFailedRestArea = 1104,
        CantUseProfanity = 1105,
        NotInPetBattle = 1106,
        NotInNpe = 1107,
        NoSpec = 1108,
        NoDominationshardOverwrite = 1109,
        UseWeeklyRewardsDisabled = 1110,
        CrossFactionGroupJoined = 1111,
        CantTargetUnfriendlyInOverworld = 1112,
        EquipablespellsSlotsFull = 1113
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
