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
        public const int MaxBattlePetSpeciesId = 3159;
        public const int MaxPetBattleSlots = 3;
        public const int DefaultMaxBattlePetsPerSpecies = 3;
        public const int BattlePetCageItemId = 82800;
        public const int DefaultSummonBattlePetSpell = 118301;
        public const int SpellVisualUncagePet = 222;

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
        public const int MaxNRLootItems = 16;
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
        public const int BGTeamsCount = 2;
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
        /// Misc Const
        /// </summary>
        public const uint CalendarMaxInvites = 100;
        public const uint CalendarDefaultResponseTime = 946684800; // 01/01/2000 00:00:00
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
        public const int MaxGOData = 34;
        public const uint MaxTransportStopFrames = 9;

        /// <summary>
        /// AreaTrigger Const
        /// </summary>
        public const int MaxAreatriggerEntityData = 6;
        public const int MaxAreatriggerScale = 7;

        /// <summary>
        /// Pet Const
        /// </summary>
        public const int MaxPetStables = 4;
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
        BnConversation = 0x35,
        BnConversationNotice = 0x36,
        BnConversationList = 0x37,
        BnInlineToastAlert = 0x38,
        BnInlineToastBroadcast = 0x39,
        BnInlineToastBroadcastInform = 0x3a,
        BnInlineToastConversation = 0x3b,
        BnWhisperPlayerOffline = 0x3c,
        CombatGuildXpGain = 0x3d,
        Currency = 0x3e,
        QuestBossEmote = 0x3f,
        PetBattleCombatLog = 0x40,
        PetBattleInfo = 0x41,
        InstanceChat = 0x42,
        InstanceChatLeader = 0x43,
        Max = 0x44,
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

    public enum SpawnMask
    {
        Continent = (1 << Difficulty.None), // Any Maps Without Spawn Modes

        DungeonNormal = (1 << Difficulty.Normal),
        DungeonHeroic = (1 << Difficulty.Heroic),
        DungeonAll = (DungeonNormal | DungeonHeroic),

        Raid10Normal = (1 << Difficulty.Raid10N),
        Raid25Normal = (1 << Difficulty.Raid25N),
        RaidNormalAll = (Raid10Normal | Raid25Normal),

        Raid10Heroic = (1 << Difficulty.Raid10HC),
        Raid25Heroic = (1 << Difficulty.Raid25HC),
        RaidHeroicAll = (Raid10Heroic | Raid25Heroic),

        RaidAll = (RaidNormalAll | RaidHeroicAll)
    }

    public enum MapFlags
    {
        CanToggleDifficulty = 0x0100,
        FlexLocking = 0x8000, // All difficulties share completed encounters lock, not bound to a single instance id
                              // heroic difficulty flag overrides it and uses instance id bind
        Garrison = 0x4000000
    }

    public enum WorldStates
    {
        CurrencyResetTime = 20001,          // Next currency reset time
        WeeklyQuestResetTime = 20002,       // Next weekly reset time
        BGDailyResetTime = 20003,           // Next daily BG reset time
        CleaningFlags = 20004,              // Cleaning Flags
        GuildDailyResetTime = 20006,        // Next guild cap reset time
        MonthlyQuestResetTime = 20007,      // Next monthly reset time
        // Cata specific custom worldstates
        GuildWeeklyResetTime = 20050,       // Next guild week reset time
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
        Movement = 35,               // Source = Creature, datalong = MovementType, datalong2 = MovementDistance (spawndist f.ex.), dataint = pathid
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
        AllowTrackBothResources,
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
        RateXpQuest,
        RealmZone,
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
        SaveRespawnTimeImmediately,
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
        WintergraspBattletime,
        WintergraspEnable,
        WintergraspNobattletime,
        WintergraspPlrMax,
        WintergraspPlrMin,
        WintergraspPlrMinLvl,
        WintergraspRestartAfterCrash,
        WorldBossLevelDiff
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
        CompletedTransmogSetS = 71,
        RevokeTransmogS = 72,
        InvitePlayerS = 73,
        InviteSelf = 74,
        InvitedToGroupSs = 75,
        InvitedAlreadyInGroupSs = 76,
        AlreadyInGroupS = 77,
        CrossRealmRaidInvite = 78,
        PlayerBusyS = 79,
        NewLeaderS = 80,
        NewLeaderYou = 81,
        NewGuideS = 82,
        NewGuideYou = 83,
        LeftGroupS = 84,
        LeftGroupYou = 85,
        GroupDisbanded = 86,
        DeclineGroupS = 87,
        JoinedGroupS = 88,
        UninviteYou = 89,
        BadPlayerNameS = 90,
        NotInGroup = 91,
        TargetNotInGroupS = 92,
        TargetNotInInstanceS = 93,
        NotInInstanceGroup = 94,
        GroupFull = 95,
        NotLeader = 96,
        PlayerDiedS = 97,
        GuildCreateS = 98,
        GuildInviteS = 99,
        InvitedToGuildSss = 100,
        AlreadyInGuildS = 101,
        AlreadyInvitedToGuildS = 102,
        InvitedToGuild = 103,
        AlreadyInGuild = 104,
        GuildAccept = 105,
        GuildDeclineS = 106,
        GuildDeclineAutoS = 107,
        GuildPermissions = 108,
        GuildJoinS = 109,
        GuildFounderS = 110,
        GuildPromoteSss = 111,
        GuildDemoteSs = 112,
        GuildDemoteSss = 113,
        GuildInviteSelf = 114,
        GuildQuitS = 115,
        GuildLeaveS = 116,
        GuildRemoveSs = 117,
        GuildRemoveSelf = 118,
        GuildDisbandS = 119,
        GuildDisbandSelf = 120,
        GuildLeaderS = 121,
        GuildLeaderSelf = 122,
        GuildPlayerNotFoundS = 123,
        GuildPlayerNotInGuildS = 124,
        GuildPlayerNotInGuild = 125,
        GuildCantPromoteS = 126,
        GuildCantDemoteS = 127,
        GuildNotInAGuild = 128,
        GuildInternal = 129,
        GuildLeaderIsS = 130,
        GuildLeaderChangedSs = 131,
        GuildDisbanded = 132,
        GuildNotAllied = 133,
        GuildLeaderLeave = 134,
        GuildRanksLocked = 135,
        GuildRankInUse = 136,
        GuildRankTooHighS = 137,
        GuildRankTooLowS = 138,
        GuildNameExistsS = 139,
        GuildWithdrawLimit = 140,
        GuildNotEnoughMoney = 141,
        GuildTooMuchMoney = 142,
        GuildBankConjuredItem = 143,
        GuildBankEquippedItem = 144,
        GuildBankBoundItem = 145,
        GuildBankQuestItem = 146,
        GuildBankWrappedItem = 147,
        GuildBankFull = 148,
        GuildBankWrongTab = 149,
        NoGuildCharter = 150,
        OutOfRange = 151,
        PlayerDead = 152,
        ClientLockedOut = 153,
        ClientOnTransport = 154,
        KilledByS = 155,
        LootLocked = 156,
        LootTooFar = 157,
        LootDidntKill = 158,
        LootBadFacing = 159,
        LootNotstanding = 160,
        LootStunned = 161,
        LootNoUi = 162,
        LootWhileInvulnerable = 163,
        NoLoot = 164,
        QuestAcceptedS = 165,
        QuestCompleteS = 166,
        QuestFailedS = 167,
        QuestFailedBagFullS = 168,
        QuestFailedMaxCountS = 169,
        QuestFailedLowLevel = 170,
        QuestFailedMissingItems = 171,
        QuestFailedWrongRace = 172,
        QuestFailedNotEnoughMoney = 173,
        QuestFailedExpansion = 174,
        QuestOnlyOneTimed = 175,
        QuestNeedPrereqs = 176,
        QuestNeedPrereqsCustom = 177,
        QuestAlreadyOn = 178,
        QuestAlreadyDone = 179,
        QuestAlreadyDoneDaily = 180,
        QuestHasInProgress = 181,
        QuestRewardExpI = 182,
        QuestRewardMoneyS = 183,
        QuestMustChoose = 184,
        QuestLogFull = 185,
        CombatDamageSsi = 186,
        InspectS = 187,
        CantUseItem = 188,
        CantUseItemInArena = 189,
        CantUseItemInRatedBattleground = 190,
        MustEquipItem = 191,
        PassiveAbility = 192,
        SkillNotFound2h = 193,
        NoAttackTarget = 194,
        InvalidAttackTarget = 195,
        AttackPvpTargetWhileUnflagged = 196,
        AttackStunned = 197,
        AttackPacified = 198,
        AttackMounted = 199,
        AttackFleeing = 200,
        AttackConfused = 201,
        AttackCharmed = 202,
        AttackDead = 203,
        AttackPreventedByMechanicS = 204,
        AttackChannel = 205,
        Taxisamenode = 206,
        Taxinosuchpath = 207,
        Taxiunspecifiedservererror = 208,
        Taxinotenoughmoney = 209,
        Taxitoofaraway = 210,
        Taxinovendornearby = 211,
        Taxinotvisited = 212,
        Taxiplayerbusy = 213,
        Taxiplayeralreadymounted = 214,
        Taxiplayershapeshifted = 215,
        Taxiplayermoving = 216,
        Taxinopaths = 217,
        Taxinoteligible = 218,
        Taxinotstanding = 219,
        NoReplyTarget = 220,
        GenericNoTarget = 221,
        InitiateTradeS = 222,
        TradeRequestS = 223,
        TradeBlockedS = 224,
        TradeTargetDead = 225,
        TradeTooFar = 226,
        TradeCancelled = 227,
        TradeComplete = 228,
        TradeBagFull = 229,
        TradeTargetBagFull = 230,
        TradeMaxCountExceeded = 231,
        TradeTargetMaxCountExceeded = 232,
        InventoryTradeTooManyUniqueItem = 233,
        AlreadyTrading = 234,
        MountInvalidmountee = 235,
        MountToofaraway = 236,
        MountAlreadymounted = 237,
        MountNotmountable = 238,
        MountNotyourpet = 239,
        MountOther = 240,
        MountLooting = 241,
        MountRacecantmount = 242,
        MountShapeshifted = 243,
        MountNoFavorites = 244,
        DismountNopet = 245,
        DismountNotmounted = 246,
        DismountNotyourpet = 247,
        SpellFailedTotems = 248,
        SpellFailedReagents = 249,
        SpellFailedReagentsGeneric = 250,
        SpellFailedOptionalReagents = 251,
        CantTradeGold = 252,
        SpellFailedEquippedItem = 253,
        SpellFailedEquippedItemClassS = 254,
        SpellFailedShapeshiftFormS = 255,
        SpellFailedAnotherInProgress = 256,
        Badattackfacing = 257,
        Badattackpos = 258,
        ChestInUse = 259,
        UseCantOpen = 260,
        UseLocked = 261,
        DoorLocked = 262,
        ButtonLocked = 263,
        UseLockedWithItemS = 264,
        UseLockedWithSpellS = 265,
        UseLockedWithSpellKnownSi = 266,
        UseTooFar = 267,
        UseBadAngle = 268,
        UseObjectMoving = 269,
        UseSpellFocus = 270,
        UseDestroyed = 271,
        SetLootFreeforall = 272,
        SetLootRoundrobin = 273,
        SetLootMaster = 274,
        SetLootGroup = 275,
        SetLootThresholdS = 276,
        NewLootMasterS = 277,
        SpecifyMasterLooter = 278,
        LootSpecChangedS = 279,
        TameFailed = 280,
        ChatWhileDead = 281,
        ChatPlayerNotFoundS = 282,
        Newtaxipath = 283,
        NoPet = 284,
        Notyourpet = 285,
        PetNotRenameable = 286,
        QuestObjectiveCompleteS = 287,
        QuestUnknownComplete = 288,
        QuestAddKillSii = 289,
        QuestAddFoundSii = 290,
        QuestAddItemSii = 291,
        QuestAddPlayerKillSii = 292,
        Cannotcreatedirectory = 293,
        Cannotcreatefile = 294,
        PlayerWrongFaction = 295,
        PlayerIsNeutral = 296,
        BankslotFailedTooMany = 297,
        BankslotInsufficientFunds = 298,
        BankslotNotbanker = 299,
        FriendDbError = 300,
        FriendListFull = 301,
        FriendAddedS = 302,
        BattletagFriendAddedS = 303,
        FriendOnlineSs = 304,
        FriendOfflineS = 305,
        FriendNotFound = 306,
        FriendWrongFaction = 307,
        FriendRemovedS = 308,
        BattletagFriendRemovedS = 309,
        FriendError = 310,
        FriendAlreadyS = 311,
        FriendSelf = 312,
        FriendDeleted = 313,
        IgnoreFull = 314,
        IgnoreSelf = 315,
        IgnoreNotFound = 316,
        IgnoreAlreadyS = 317,
        IgnoreAddedS = 318,
        IgnoreRemovedS = 319,
        IgnoreAmbiguous = 320,
        IgnoreDeleted = 321,
        OnlyOneBolt = 322,
        OnlyOneAmmo = 323,
        SpellFailedEquippedSpecificItem = 324,
        WrongBagTypeSubclass = 325,
        CantWrapStackable = 326,
        CantWrapEquipped = 327,
        CantWrapWrapped = 328,
        CantWrapBound = 329,
        CantWrapUnique = 330,
        CantWrapBags = 331,
        OutOfMana = 332,
        OutOfRage = 333,
        OutOfFocus = 334,
        OutOfEnergy = 335,
        OutOfChi = 336,
        OutOfHealth = 337,
        OutOfRunes = 338,
        OutOfRunicPower = 339,
        OutOfSoulShards = 340,
        OutOfLunarPower = 341,
        OutOfHolyPower = 342,
        OutOfMaelstrom = 343,
        OutOfComboPoints = 344,
        OutOfInsanity = 345,
        OutOfArcaneCharges = 346,
        OutOfFury = 347,
        OutOfPain = 348,
        OutOfPowerDisplay = 349,
        LootGone = 350,
        MountForceddismount = 351,
        AutofollowTooFar = 352,
        UnitNotFound = 353,
        InvalidFollowTarget = 354,
        InvalidFollowPvpCombat = 355,
        InvalidFollowTargetPvpCombat = 356,
        InvalidInspectTarget = 357,
        GuildemblemSuccess = 358,
        GuildemblemInvalidTabardColors = 359,
        GuildemblemNoguild = 360,
        GuildemblemNotguildmaster = 361,
        GuildemblemNotenoughmoney = 362,
        GuildemblemInvalidvendor = 363,
        EmblemerrorNotabardgeoset = 364,
        SpellOutOfRange = 365,
        CommandNeedsTarget = 366,
        NoammoS = 367,
        Toobusytofollow = 368,
        DuelRequested = 369,
        DuelCancelled = 370,
        Deathbindalreadybound = 371,
        DeathbindSuccessS = 372,
        Noemotewhilerunning = 373,
        ZoneExplored = 374,
        ZoneExploredXp = 375,
        InvalidItemTarget = 376,
        InvalidQuestTarget = 377,
        IgnoringYouS = 378,
        FishNotHooked = 379,
        FishEscaped = 380,
        SpellFailedNotunsheathed = 381,
        PetitionOfferedS = 382,
        PetitionSigned = 383,
        PetitionSignedS = 384,
        PetitionDeclinedS = 385,
        PetitionAlreadySigned = 386,
        PetitionRestrictedAccountTrial = 387,
        PetitionAlreadySignedOther = 388,
        PetitionInGuild = 389,
        PetitionCreator = 390,
        PetitionNotEnoughSignatures = 391,
        PetitionNotSameServer = 392,
        PetitionFull = 393,
        PetitionAlreadySignedByS = 394,
        GuildNameInvalid = 395,
        SpellUnlearnedS = 396,
        PetSpellRooted = 397,
        PetSpellAffectingCombat = 398,
        PetSpellOutOfRange = 399,
        PetSpellNotBehind = 400,
        PetSpellTargetsDead = 401,
        PetSpellDead = 402,
        PetSpellNopath = 403,
        ItemCantBeDestroyed = 404,
        TicketAlreadyExists = 405,
        TicketCreateError = 406,
        TicketUpdateError = 407,
        TicketDbError = 408,
        TicketNoText = 409,
        TicketTextTooLong = 410,
        ObjectIsBusy = 411,
        ExhaustionWellrested = 412,
        ExhaustionRested = 413,
        ExhaustionNormal = 414,
        ExhaustionTired = 415,
        ExhaustionExhausted = 416,
        NoItemsWhileShapeshifted = 417,
        CantInteractShapeshifted = 418,
        RealmNotFound = 419,
        MailQuestItem = 420,
        MailBoundItem = 421,
        MailConjuredItem = 422,
        MailBag = 423,
        MailToSelf = 424,
        MailTargetNotFound = 425,
        MailDatabaseError = 426,
        MailDeleteItemError = 427,
        MailWrappedCod = 428,
        MailCantSendRealm = 429,
        MailTempReturnOutage = 430,
        MailSent = 431,
        NotHappyEnough = 432,
        UseCantImmune = 433,
        CantBeDisenchanted = 434,
        CantUseDisarmed = 435,
        AuctionQuestItem = 436,
        AuctionBoundItem = 437,
        AuctionConjuredItem = 438,
        AuctionLimitedDurationItem = 439,
        AuctionWrappedItem = 440,
        AuctionLootItem = 441,
        AuctionBag = 442,
        AuctionEquippedBag = 443,
        AuctionDatabaseError = 444,
        AuctionBidOwn = 445,
        AuctionBidIncrement = 446,
        AuctionHigherBid = 447,
        AuctionMinBid = 448,
        AuctionRepairItem = 449,
        AuctionUsedCharges = 450,
        AuctionAlreadyBid = 451,
        AuctionHouseUnavailable = 452,
        AuctionItemHasQuote = 453,
        AuctionHouseBusy = 454,
        AuctionStarted = 455,
        AuctionRemoved = 456,
        AuctionOutbidS = 457,
        AuctionWonS = 458,
        AuctionCommodityWonS = 459,
        AuctionSoldS = 460,
        AuctionExpiredS = 461,
        AuctionRemovedS = 462,
        AuctionBidPlaced = 463,
        LogoutFailed = 464,
        QuestPushSuccessS = 465,
        QuestPushInvalidS = 466,
        QuestPushAcceptedS = 467,
        QuestPushDeclinedS = 468,
        QuestPushBusyS = 469,
        QuestPushDeadS = 470,
        QuestPushLogFullS = 471,
        QuestPushOnquestS = 472,
        QuestPushAlreadyDoneS = 473,
        QuestPushNotDailyS = 474,
        QuestPushTimerExpiredS = 475,
        QuestPushNotInPartyS = 476,
        QuestPushDifferentServerDailyS = 477,
        QuestPushNotAllowedS = 478,
        RaidGroupLowlevel = 479,
        RaidGroupOnly = 480,
        RaidGroupFull = 481,
        RaidGroupRequirementsUnmatch = 482,
        CorpseIsNotInInstance = 483,
        PvpKillHonorable = 484,
        PvpKillDishonorable = 485,
        SpellFailedAlreadyAtFullHealth = 486,
        SpellFailedAlreadyAtFullMana = 487,
        SpellFailedAlreadyAtFullPowerS = 488,
        AutolootMoneyS = 489,
        GenericStunned = 490,
        GenericThrottle = 491,
        ClubFinderSearchingTooFast = 492,
        TargetStunned = 493,
        MustRepairDurability = 494,
        RaidYouJoined = 495,
        RaidYouLeft = 496,
        InstanceGroupJoinedWithParty = 497,
        InstanceGroupJoinedWithRaid = 498,
        RaidMemberAddedS = 499,
        RaidMemberRemovedS = 500,
        InstanceGroupAddedS = 501,
        InstanceGroupRemovedS = 502,
        ClickOnItemToFeed = 503,
        TooManyChatChannels = 504,
        LootRollPending = 505,
        LootPlayerNotFound = 506,
        NotInRaid = 507,
        LoggingOut = 508,
        TargetLoggingOut = 509,
        NotWhileMounted = 510,
        NotWhileShapeshifted = 511,
        NotInCombat = 512,
        NotWhileDisarmed = 513,
        PetBroken = 514,
        TalentWipeError = 515,
        SpecWipeError = 516,
        GlyphWipeError = 517,
        PetSpecWipeError = 518,
        FeignDeathResisted = 519,
        MeetingStoneInQueueS = 520,
        MeetingStoneLeftQueueS = 521,
        MeetingStoneOtherMemberLeft = 522,
        MeetingStonePartyKickedFromQueue = 523,
        MeetingStoneMemberStillInQueue = 524,
        MeetingStoneSuccess = 525,
        MeetingStoneInProgress = 526,
        MeetingStoneMemberAddedS = 527,
        MeetingStoneGroupFull = 528,
        MeetingStoneNotLeader = 529,
        MeetingStoneInvalidLevel = 530,
        MeetingStoneTargetNotInParty = 531,
        MeetingStoneTargetInvalidLevel = 532,
        MeetingStoneMustBeLeader = 533,
        MeetingStoneNoRaidGroup = 534,
        MeetingStoneNeedParty = 535,
        MeetingStoneNotFound = 536,
        MeetingStoneTargetInVehicle = 537,
        GuildemblemSame = 538,
        EquipTradeItem = 539,
        PvpToggleOn = 540,
        PvpToggleOff = 541,
        GroupJoinBattlegroundDeserters = 542,
        GroupJoinBattlegroundDead = 543,
        GroupJoinBattlegroundS = 544,
        GroupJoinBattlegroundFail = 545,
        GroupJoinBattlegroundTooMany = 546,
        SoloJoinBattlegroundS = 547,
        JoinSingleScenarioS = 548,
        BattlegroundTooManyQueues = 549,
        BattlegroundCannotQueueForRated = 550,
        BattledgroundQueuedForRated = 551,
        BattlegroundTeamLeftQueue = 552,
        BattlegroundNotInBattleground = 553,
        AlreadyInArenaTeamS = 554,
        InvalidPromotionCode = 555,
        BgPlayerJoinedSs = 556,
        BgPlayerLeftS = 557,
        RestrictedAccount = 558,
        RestrictedAccountTrial = 559,
        PlayTimeExceeded = 560,
        ApproachingPartialPlayTime = 561,
        ApproachingPartialPlayTime2 = 562,
        ApproachingNoPlayTime = 563,
        ApproachingNoPlayTime2 = 564,
        UnhealthyTime = 565,
        ChatRestrictedTrial = 566,
        ChatThrottled = 567,
        MailReachedCap = 568,
        InvalidRaidTarget = 569,
        RaidLeaderReadyCheckStartS = 570,
        ReadyCheckInProgress = 571,
        ReadyCheckThrottled = 572,
        DungeonDifficultyFailed = 573,
        DungeonDifficultyChangedS = 574,
        TradeWrongRealm = 575,
        TradeNotOnTaplist = 576,
        ChatPlayerAmbiguousS = 577,
        LootCantLootThatNow = 578,
        LootMasterInvFull = 579,
        LootMasterUniqueItem = 580,
        LootMasterOther = 581,
        FilteringYouS = 582,
        UsePreventedByMechanicS = 583,
        ItemUniqueEquippable = 584,
        LfgLeaderIsLfmS = 585,
        LfgPending = 586,
        CantSpeakLangage = 587,
        VendorMissingTurnins = 588,
        BattlegroundNotInTeam = 589,
        NotInBattleground = 590,
        NotEnoughHonorPoints = 591,
        NotEnoughArenaPoints = 592,
        SocketingRequiresMetaGem = 593,
        SocketingMetaGemOnlyInMetaslot = 594,
        SocketingRequiresHydraulicGem = 595,
        SocketingHydraulicGemOnlyInHydraulicslot = 596,
        SocketingRequiresCogwheelGem = 597,
        SocketingCogwheelGemOnlyInCogwheelslot = 598,
        SocketingItemTooLowLevel = 599,
        ItemMaxCountSocketed = 600,
        SystemDisabled = 601,
        QuestFailedTooManyDailyQuestsI = 602,
        ItemMaxCountEquippedSocketed = 603,
        ItemUniqueEquippableSocketed = 604,
        UserSquelched = 605,
        AccountSilenced = 606,
        PartyMemberSilenced = 607,
        PartyMemberSilencedLfgDelist = 608,
        TooMuchGold = 609,
        NotBarberSitting = 610,
        QuestFailedCais = 611,
        InviteRestrictedTrial = 612,
        VoiceIgnoreFull = 613,
        VoiceIgnoreSelf = 614,
        VoiceIgnoreNotFound = 615,
        VoiceIgnoreAlreadyS = 616,
        VoiceIgnoreAddedS = 617,
        VoiceIgnoreRemovedS = 618,
        VoiceIgnoreAmbiguous = 619,
        VoiceIgnoreDeleted = 620,
        UnknownMacroOptionS = 621,
        NotDuringArenaMatch = 622,
        PlayerSilenced = 623,
        PlayerUnsilenced = 624,
        ComsatDisconnect = 625,
        ComsatReconnectAttempt = 626,
        ComsatConnectFail = 627,
        MailInvalidAttachmentSlot = 628,
        MailTooManyAttachments = 629,
        MailInvalidAttachment = 630,
        MailAttachmentExpired = 631,
        VoiceChatParentalDisableMic = 632,
        ProfaneChatName = 633,
        PlayerSilencedEcho = 634,
        PlayerUnsilencedEcho = 635,
        LootCantLootThat = 636,
        ArenaExpiredCais = 637,
        GroupActionThrottled = 638,
        AlreadyPickpocketed = 639,
        NameInvalid = 640,
        NameNoName = 641,
        NameTooShort = 642,
        NameTooLong = 643,
        NameMixedLanguages = 644,
        NameProfane = 645,
        NameReserved = 646,
        NameThreeConsecutive = 647,
        NameInvalidSpace = 648,
        NameConsecutiveSpaces = 649,
        NameRussianConsecutiveSilentCharacters = 650,
        NameRussianSilentCharacterAtBeginningOrEnd = 651,
        NameDeclensionDoesntMatchBaseName = 652,
        RecruitAFriendNotLinked = 653,
        RecruitAFriendNotNow = 654,
        RecruitAFriendSummonLevelMax = 655,
        RecruitAFriendSummonCooldown = 656,
        RecruitAFriendSummonOffline = 657,
        RecruitAFriendInsufExpanLvl = 658,
        RecruitAFriendMapIncomingTransferNotAllowed = 659,
        NotSameAccount = 660,
        BadOnUseEnchant = 661,
        TradeSelf = 662,
        TooManySockets = 663,
        ItemMaxLimitCategoryCountExceededIs = 664,
        TradeTargetMaxLimitCategoryCountExceededIs = 665,
        ItemMaxLimitCategorySocketedExceededIs = 666,
        ItemMaxLimitCategoryEquippedExceededIs = 667,
        ShapeshiftFormCannotEquip = 668,
        ItemInventoryFullSatchel = 669,
        ScalingStatItemLevelExceeded = 670,
        ScalingStatItemLevelTooLow = 671,
        PurchaseLevelTooLow = 672,
        GroupSwapFailed = 673,
        InviteInCombat = 674,
        InvalidGlyphSlot = 675,
        GenericNoValidTargets = 676,
        CalendarEventAlertS = 677,
        PetLearnSpellS = 678,
        PetLearnAbilityS = 679,
        PetSpellUnlearnedS = 680,
        InviteUnknownRealm = 681,
        InviteNoPartyServer = 682,
        InvitePartyBusy = 683,
        PartyTargetAmbiguous = 684,
        PartyLfgInviteRaidLocked = 685,
        PartyLfgBootLimit = 686,
        PartyLfgBootCooldownS = 687,
        PartyLfgBootNotEligibleS = 688,
        PartyLfgBootInpatientTimerS = 689,
        PartyLfgBootInProgress = 690,
        PartyLfgBootTooFewPlayers = 691,
        PartyLfgBootVoteSucceeded = 692,
        PartyLfgBootVoteFailed = 693,
        PartyLfgBootInCombat = 694,
        PartyLfgBootDungeonComplete = 695,
        PartyLfgBootLootRolls = 696,
        PartyLfgBootVoteRegistered = 697,
        PartyPrivateGroupOnly = 698,
        PartyLfgTeleportInCombat = 699,
        RaidDisallowedByLevel = 700,
        RaidDisallowedByCrossRealm = 701,
        PartyRoleNotAvailable = 702,
        JoinLfgObjectFailed = 703,
        LfgRemovedLevelup = 704,
        LfgRemovedXpToggle = 705,
        LfgRemovedFactionChange = 706,
        BattlegroundInfoThrottled = 707,
        BattlegroundAlreadyIn = 708,
        ArenaTeamChangeFailedQueued = 709,
        ArenaTeamPermissions = 710,
        NotWhileFalling = 711,
        NotWhileMoving = 712,
        NotWhileFatigued = 713,
        MaxSockets = 714,
        MultiCastActionTotemS = 715,
        BattlegroundJoinLevelup = 716,
        RemoveFromPvpQueueXpGain = 717,
        BattlegroundJoinXpGain = 718,
        BattlegroundJoinMercenary = 719,
        BattlegroundJoinTooManyHealers = 720,
        BattlegroundJoinRatedTooManyHealers = 721,
        BattlegroundJoinTooManyTanks = 722,
        BattlegroundJoinTooManyDamage = 723,
        RaidDifficultyFailed = 724,
        RaidDifficultyChangedS = 725,
        LegacyRaidDifficultyChangedS = 726,
        RaidLockoutChangedS = 727,
        RaidConvertedToParty = 728,
        PartyConvertedToRaid = 729,
        PlayerDifficultyChangedS = 730,
        GmresponseDbError = 731,
        BattlegroundJoinRangeIndex = 732,
        ArenaJoinRangeIndex = 733,
        RemoveFromPvpQueueFactionChange = 734,
        BattlegroundJoinFailed = 735,
        BattlegroundJoinNoValidSpecForRole = 736,
        BattlegroundJoinRespec = 737,
        BattlegroundInvitationDeclined = 738,
        BattlegroundJoinTimedOut = 739,
        BattlegroundDupeQueue = 740,
        BattlegroundJoinMustCompleteQuest = 741,
        InBattlegroundRespec = 742,
        MailLimitedDurationItem = 743,
        YellRestrictedTrial = 744,
        ChatRaidRestrictedTrial = 745,
        LfgRoleCheckFailed = 746,
        LfgRoleCheckFailedTimeout = 747,
        LfgRoleCheckFailedNotViable = 748,
        LfgReadyCheckFailed = 749,
        LfgReadyCheckFailedTimeout = 750,
        LfgGroupFull = 751,
        LfgNoLfgObject = 752,
        LfgNoSlotsPlayer = 753,
        LfgNoSlotsParty = 754,
        LfgNoSpec = 755,
        LfgMismatchedSlots = 756,
        LfgMismatchedSlotsLocalXrealm = 757,
        LfgPartyPlayersFromDifferentRealms = 758,
        LfgMembersNotPresent = 759,
        LfgGetInfoTimeout = 760,
        LfgInvalidSlot = 761,
        LfgDeserterPlayer = 762,
        LfgDeserterParty = 763,
        LfgDead = 764,
        LfgRandomCooldownPlayer = 765,
        LfgRandomCooldownParty = 766,
        LfgTooManyMembers = 767,
        LfgTooFewMembers = 768,
        LfgProposalFailed = 769,
        LfgProposalDeclinedSelf = 770,
        LfgProposalDeclinedParty = 771,
        LfgNoSlotsSelected = 772,
        LfgNoRolesSelected = 773,
        LfgRoleCheckInitiated = 774,
        LfgReadyCheckInitiated = 775,
        LfgPlayerDeclinedRoleCheck = 776,
        LfgPlayerDeclinedReadyCheck = 777,
        LfgJoinedQueue = 778,
        LfgJoinedFlexQueue = 779,
        LfgJoinedRfQueue = 780,
        LfgJoinedScenarioQueue = 781,
        LfgJoinedWorldPvpQueue = 782,
        LfgJoinedBattlefieldQueue = 783,
        LfgJoinedList = 784,
        LfgLeftQueue = 785,
        LfgLeftList = 786,
        LfgRoleCheckAborted = 787,
        LfgReadyCheckAborted = 788,
        LfgCantUseBattleground = 789,
        LfgCantUseDungeons = 790,
        LfgReasonTooManyLfg = 791,
        InvalidTeleportLocation = 792,
        TooFarToInteract = 793,
        BattlegroundPlayersFromDifferentRealms = 794,
        DifficultyChangeCooldownS = 795,
        DifficultyChangeCombatCooldownS = 796,
        DifficultyChangeWorldstate = 797,
        DifficultyChangeEncounter = 798,
        DifficultyChangeCombat = 799,
        DifficultyChangePlayerBusy = 800,
        DifficultyChangeAlreadyStarted = 801,
        DifficultyChangeOtherHeroicS = 802,
        DifficultyChangeHeroicInstanceAlreadyRunning = 803,
        ArenaTeamPartySize = 804,
        QuestForceRemovedS = 805,
        AttackNoActions = 806,
        InRandomBg = 807,
        InNonRandomBg = 808,
        AuctionEnoughItems = 809,
        BnFriendSelf = 810,
        BnFriendAlready = 811,
        BnFriendBlocked = 812,
        BnFriendListFull = 813,
        BnFriendRequestSent = 814,
        BnBroadcastThrottle = 815,
        BgDeveloperOnly = 816,
        CurrencySpellSlotMismatch = 817,
        CurrencyNotTradable = 818,
        RequiresExpansionS = 819,
        QuestFailedSpell = 820,
        TalentFailedNotEnoughTalentsInPrimaryTree = 821,
        TalentFailedNoPrimaryTreeSelected = 822,
        TalentFailedCantRemoveTalent = 823,
        TalentFailedUnknown = 824,
        WargameRequestFailure = 825,
        RankRequiresAuthenticator = 826,
        GuildBankVoucherFailed = 827,
        WargameRequestSent = 828,
        RequiresAchievementI = 829,
        RefundResultExceedMaxCurrency = 830,
        CantBuyQuantity = 831,
        ItemIsBattlePayLocked = 832,
        PartyAlreadyInBattlegroundQueue = 833,
        PartyConfirmingBattlegroundQueue = 834,
        BattlefieldTeamPartySize = 835,
        InsuffTrackedCurrencyIs = 836,
        NotOnTournamentRealm = 837,
        GuildTrialAccountTrial = 838,
        GuildTrialAccountVeteran = 839,
        GuildUndeletableDueToLevel = 840,
        CantDoThatInAGroup = 841,
        GuildLeaderReplaced = 842,
        TransmogrifyCantEquip = 843,
        TransmogrifyInvalidItemType = 844,
        TransmogrifyNotSoulbound = 845,
        TransmogrifyInvalidSource = 846,
        TransmogrifyInvalidDestination = 847,
        TransmogrifyMismatch = 848,
        TransmogrifyLegendary = 849,
        TransmogrifySameItem = 850,
        TransmogrifySameAppearance = 851,
        TransmogrifyNotEquipped = 852,
        VoidDepositFull = 853,
        VoidWithdrawFull = 854,
        VoidStorageWrapped = 855,
        VoidStorageStackable = 856,
        VoidStorageUnbound = 857,
        VoidStorageRepair = 858,
        VoidStorageCharges = 859,
        VoidStorageQuest = 860,
        VoidStorageConjured = 861,
        VoidStorageMail = 862,
        VoidStorageBag = 863,
        VoidTransferStorageFull = 864,
        VoidTransferInvFull = 865,
        VoidTransferInternalError = 866,
        VoidTransferItemInvalid = 867,
        DifficultyDisabledInLfg = 868,
        VoidStorageUnique = 869,
        VoidStorageLoot = 870,
        VoidStorageHoliday = 871,
        VoidStorageDuration = 872,
        VoidStorageLoadFailed = 873,
        VoidStorageInvalidItem = 874,
        ParentalControlsChatMuted = 875,
        SorStartExperienceIncomplete = 876,
        SorInvalidEmail = 877,
        SorInvalidComment = 878,
        ChallengeModeResetCooldownS = 879,
        ChallengeModeResetKeystone = 880,
        PetJournalAlreadyInLoadout = 881,
        ReportSubmittedSuccessfully = 882,
        ReportSubmissionFailed = 883,
        SuggestionSubmittedSuccessfully = 884,
        BugSubmittedSuccessfully = 885,
        ChallengeModeEnabled = 886,
        ChallengeModeDisabled = 887,
        PetbattleCreateFailed = 888,
        PetbattleNotHere = 889,
        PetbattleNotHereOnTransport = 890,
        PetbattleNotHereUnevenGround = 891,
        PetbattleNotHereObstructed = 892,
        PetbattleNotWhileInCombat = 893,
        PetbattleNotWhileDead = 894,
        PetbattleNotWhileFlying = 895,
        PetbattleTargetInvalid = 896,
        PetbattleTargetOutOfRange = 897,
        PetbattleTargetNotCapturable = 898,
        PetbattleNotATrainer = 899,
        PetbattleDeclined = 900,
        PetbattleInBattle = 901,
        PetbattleInvalidLoadout = 902,
        PetbattleAllPetsDead = 903,
        PetbattleNoPetsInSlots = 904,
        PetbattleNoAccountLock = 905,
        PetbattleWildPetTapped = 906,
        PetbattleRestrictedAccount = 907,
        PetbattleOpponentNotAvailable = 908,
        PetbattleNotWhileInMatchedBattle = 909,
        CantHaveMorePetsOfThatType = 910,
        CantHaveMorePets = 911,
        PvpMapNotFound = 912,
        PvpMapNotSet = 913,
        PetbattleQueueQueued = 914,
        PetbattleQueueAlreadyQueued = 915,
        PetbattleQueueJoinFailed = 916,
        PetbattleQueueJournalLock = 917,
        PetbattleQueueRemoved = 918,
        PetbattleQueueProposalDeclined = 919,
        PetbattleQueueProposalTimeout = 920,
        PetbattleQueueOpponentDeclined = 921,
        PetbattleQueueRequeuedInternal = 922,
        PetbattleQueueRequeuedRemoved = 923,
        PetbattleQueueSlotLocked = 924,
        PetbattleQueueSlotEmpty = 925,
        PetbattleQueueSlotNoTracker = 926,
        PetbattleQueueSlotNoSpecies = 927,
        PetbattleQueueSlotCantBattle = 928,
        PetbattleQueueSlotRevoked = 929,
        PetbattleQueueSlotDead = 930,
        PetbattleQueueSlotNoPet = 931,
        PetbattleQueueNotWhileNeutral = 932,
        PetbattleGameTimeLimitWarning = 933,
        PetbattleGameRoundsLimitWarning = 934,
        HasRestriction = 935,
        ItemUpgradeItemTooLowLevel = 936,
        ItemUpgradeNoPath = 937,
        ItemUpgradeNoMoreUpgrades = 938,
        BonusRollEmpty = 939,
        ChallengeModeFull = 940,
        ChallengeModeInProgress = 941,
        ChallengeModeIncorrectKeystone = 942,
        BattletagFriendNotFound = 943,
        BattletagFriendNotValid = 944,
        BattletagFriendNotAllowed = 945,
        BattletagFriendThrottled = 946,
        BattletagFriendSuccess = 947,
        PetTooHighLevelToUncage = 948,
        PetbattleInternal = 949,
        CantCagePetYet = 950,
        NoLootInChallengeMode = 951,
        QuestPetBattleVictoriesPvpIi = 952,
        RoleCheckAlreadyInProgress = 953,
        RecruitAFriendAccountLimit = 954,
        RecruitAFriendFailed = 955,
        SetLootPersonal = 956,
        SetLootMethodFailedCombat = 957,
        ReagentBankFull = 958,
        ReagentBankLocked = 959,
        GarrisonBuildingExists = 960,
        GarrisonInvalidPlot = 961,
        GarrisonInvalidBuildingid = 962,
        GarrisonInvalidPlotBuilding = 963,
        GarrisonRequiresBlueprint = 964,
        GarrisonNotEnoughCurrency = 965,
        GarrisonNotEnoughGold = 966,
        GarrisonCompleteMissionWrongFollowerType = 967,
        AlreadyUsingLfgList = 968,
        RestrictedAccountLfgListTrial = 969,
        ToyUseLimitReached = 970,
        ToyAlreadyKnown = 971,
        TransmogSetAlreadyKnown = 972,
        NotEnoughCurrency = 973,
        SpecIsDisabled = 974,
        FeatureRestrictedTrial = 975,
        CantBeObliterated = 976,
        CantBeScrapped = 977,
        ArtifactRelicDoesNotMatchArtifact = 978,
        MustEquipArtifact = 979,
        CantDoThatRightNow = 980,
        AffectingCombat = 981,
        EquipmentManagerCombatSwapS = 982,
        EquipmentManagerBagsFull = 983,
        EquipmentManagerMissingItemS = 984,
        MovieRecordingWarningPerf = 985,
        MovieRecordingWarningDiskFull = 986,
        MovieRecordingWarningNoMovie = 987,
        MovieRecordingWarningRequirements = 988,
        MovieRecordingWarningCompressing = 989,
        NoChallengeModeReward = 990,
        ClaimedChallengeModeReward = 991,
        ChallengeModePeriodResetSs = 992,
        CantDoThatChallengeModeActive = 993,
        TalentFailedRestArea = 994,
        CannotAbandonLastPet = 995,
        TestCvarSetSss = 996,
        QuestTurnInFailReason = 997,
        ClaimedChallengeModeRewardOld = 998,
        TalentGrantedByAura = 999,
        ChallengeModeAlreadyComplete = 1000,
        GlyphTargetNotAvailable = 1001,
        PvpWarmodeToggleOn = 1002,
        PvpWarmodeToggleOff = 1003,
        SpellFailedLevelRequirement = 1004,
        BattlegroundJoinRequiresLevel = 1005,
        BattlegroundJoinDisqualified = 1006,
        BattlegroundJoinDisqualifiedNoName = 1007,
        VoiceChatGenericUnableToConnect = 1008,
        VoiceChatServiceLost = 1009,
        VoiceChatChannelNameTooShort = 1010,
        VoiceChatChannelNameTooLong = 1011,
        VoiceChatChannelAlreadyExists = 1012,
        VoiceChatTargetNotFound = 1013,
        VoiceChatTooManyRequests = 1014,
        VoiceChatPlayerSilenced = 1015,
        VoiceChatParentalDisableAll = 1016,
        VoiceChatDisabled = 1017,
        NoPvpReward = 1018,
        ClaimedPvpReward = 1019,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1020,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1021,
        AzeriteEssenceSelectionFailedConditionFailed = 1022,
        AzeriteEssenceSelectionFailedRestArea = 1023,
        AzeriteEssenceSelectionFailedSlotLocked = 1024,
        AzeriteEssenceSelectionFailedNotAtForge = 1025,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1026,
        AzeriteEssenceSelectionFailedNotEquipped = 1027,
        SocketingRequiresPunchcardredGem = 1028,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1029,
        SocketingRequiresPunchcardyellowGem = 1030,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1031,
        SocketingRequiresPunchcardblueGem = 1032,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1033,
        SocketingRequiresDominationShard = 1034,
        SocketingDominationShardOnlyInDominationslot = 1035,
        LevelLinkingResultLinked = 1036,
        LevelLinkingResultUnlinked = 1037,
        ClubFinderErrorPostClub = 1038,
        ClubFinderErrorApplyClub = 1039,
        ClubFinderErrorRespondApplicant = 1040,
        ClubFinderErrorCancelApplication = 1041,
        ClubFinderErrorTypeAcceptApplication = 1042,
        ClubFinderErrorTypeNoInvitePermissions = 1043,
        ClubFinderErrorTypeNoPostingPermissions = 1044,
        ClubFinderErrorTypeApplicantList = 1045,
        ClubFinderErrorTypeApplicantListNoPerm = 1046,
        ClubFinderErrorTypeFinderNotAvailable = 1047,
        ClubFinderErrorTypeGetPostingIds = 1048,
        ClubFinderErrorTypeJoinApplication = 1049,
        ClubFinderErrorTypeRealmNotEligible = 1050,
        ClubFinderErrorTypeFlaggedRename = 1051,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1052,
        ItemInteractionNotEnoughGold = 1053,
        ItemInteractionNotEnoughCurrency = 1054,
        PlayerChoiceErrorPendingChoice = 1055,
        SoulbindInvalidConduit = 1056,
        SoulbindInvalidConduitItem = 1057,
        SoulbindInvalidTalent = 1058,
        SoulbindDuplicateConduit = 1059,
        ActivateSoulbindS = 1060,
        ActivateSoulbindFailedRestArea = 1061,
        CantUseProfanity = 1062,
        NotInPetBattle = 1063,
        NotInNpe = 1064,
        NoSpec = 1065,
        NoDominationshardOverwrite = 1066,
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
        WorldObjectActor = 0,
        CreatureActor = 1
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

    public enum SceneType
    {
        Normal = 0,
        PetBattle = 1
    }
}
