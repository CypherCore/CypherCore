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
        public const int ReputationCap = 42999;
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

        /// <summary>
        /// BattlePets Const
        /// </summary>
        public const int MaxBattlePetSpeciesId = 3084;
        public const int MaxPetBattleSlots = 3;
        public const int MaxBattlePetsPerSpecies = 3;
        public const int BattlePetCageItemId = 82800;
        public const int DefaultSummonBattlePetSpell = 118301;

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
        public const float PetFollowAngle = MathFunctions.PiOver2;
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
        MaxPerClass = 6
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
        AlreadyTrading = 233,
        MountInvalidmountee = 234,
        MountToofaraway = 235,
        MountAlreadymounted = 236,
        MountNotmountable = 237,
        MountNotyourpet = 238,
        MountOther = 239,
        MountLooting = 240,
        MountRacecantmount = 241,
        MountShapeshifted = 242,
        MountNoFavorites = 243,
        DismountNopet = 244,
        DismountNotmounted = 245,
        DismountNotyourpet = 246,
        SpellFailedTotems = 247,
        SpellFailedReagents = 248,
        SpellFailedReagentsGeneric = 249,
        SpellFailedOptionalReagents = 250,
        CantTradeGold = 251,
        SpellFailedEquippedItem = 252,
        SpellFailedEquippedItemClassS = 253,
        SpellFailedShapeshiftFormS = 254,
        SpellFailedAnotherInProgress = 255,
        Badattackfacing = 256,
        Badattackpos = 257,
        ChestInUse = 258,
        UseCantOpen = 259,
        UseLocked = 260,
        DoorLocked = 261,
        ButtonLocked = 262,
        UseLockedWithItemS = 263,
        UseLockedWithSpellS = 264,
        UseLockedWithSpellKnownSi = 265,
        UseTooFar = 266,
        UseBadAngle = 267,
        UseObjectMoving = 268,
        UseSpellFocus = 269,
        UseDestroyed = 270,
        SetLootFreeforall = 271,
        SetLootRoundrobin = 272,
        SetLootMaster = 273,
        SetLootGroup = 274,
        SetLootThresholdS = 275,
        NewLootMasterS = 276,
        SpecifyMasterLooter = 277,
        LootSpecChangedS = 278,
        TameFailed = 279,
        ChatWhileDead = 280,
        ChatPlayerNotFoundS = 281,
        Newtaxipath = 282,
        NoPet = 283,
        Notyourpet = 284,
        PetNotRenameable = 285,
        QuestObjectiveCompleteS = 286,
        QuestUnknownComplete = 287,
        QuestAddKillSii = 288,
        QuestAddFoundSii = 289,
        QuestAddItemSii = 290,
        QuestAddPlayerKillSii = 291,
        Cannotcreatedirectory = 292,
        Cannotcreatefile = 293,
        PlayerWrongFaction = 294,
        PlayerIsNeutral = 295,
        BankslotFailedTooMany = 296,
        BankslotInsufficientFunds = 297,
        BankslotNotbanker = 298,
        FriendDbError = 299,
        FriendListFull = 300,
        FriendAddedS = 301,
        BattletagFriendAddedS = 302,
        FriendOnlineSs = 303,
        FriendOfflineS = 304,
        FriendNotFound = 305,
        FriendWrongFaction = 306,
        FriendRemovedS = 307,
        BattletagFriendRemovedS = 308,
        FriendError = 309,
        FriendAlreadyS = 310,
        FriendSelf = 311,
        FriendDeleted = 312,
        IgnoreFull = 313,
        IgnoreSelf = 314,
        IgnoreNotFound = 315,
        IgnoreAlreadyS = 316,
        IgnoreAddedS = 317,
        IgnoreRemovedS = 318,
        IgnoreAmbiguous = 319,
        IgnoreDeleted = 320,
        OnlyOneBolt = 321,
        OnlyOneAmmo = 322,
        SpellFailedEquippedSpecificItem = 323,
        WrongBagTypeSubclass = 324,
        CantWrapStackable = 325,
        CantWrapEquipped = 326,
        CantWrapWrapped = 327,
        CantWrapBound = 328,
        CantWrapUnique = 329,
        CantWrapBags = 330,
        OutOfMana = 331,
        OutOfRage = 332,
        OutOfFocus = 333,
        OutOfEnergy = 334,
        OutOfChi = 335,
        OutOfHealth = 336,
        OutOfRunes = 337,
        OutOfRunicPower = 338,
        OutOfSoulShards = 339,
        OutOfLunarPower = 340,
        OutOfHolyPower = 341,
        OutOfMaelstrom = 342,
        OutOfComboPoints = 343,
        OutOfInsanity = 344,
        OutOfArcaneCharges = 345,
        OutOfFury = 346,
        OutOfPain = 347,
        OutOfPowerDisplay = 348,
        LootGone = 349,
        MountForceddismount = 350,
        AutofollowTooFar = 351,
        UnitNotFound = 352,
        InvalidFollowTarget = 353,
        InvalidFollowPvpCombat = 354,
        InvalidFollowTargetPvpCombat = 355,
        InvalidInspectTarget = 356,
        GuildemblemSuccess = 357,
        GuildemblemInvalidTabardColors = 358,
        GuildemblemNoguild = 359,
        GuildemblemNotguildmaster = 360,
        GuildemblemNotenoughmoney = 361,
        GuildemblemInvalidvendor = 362,
        EmblemerrorNotabardgeoset = 363,
        SpellOutOfRange = 364,
        CommandNeedsTarget = 365,
        NoammoS = 366,
        Toobusytofollow = 367,
        DuelRequested = 368,
        DuelCancelled = 369,
        Deathbindalreadybound = 370,
        DeathbindSuccessS = 371,
        Noemotewhilerunning = 372,
        ZoneExplored = 373,
        ZoneExploredXp = 374,
        InvalidItemTarget = 375,
        InvalidQuestTarget = 376,
        IgnoringYouS = 377,
        FishNotHooked = 378,
        FishEscaped = 379,
        SpellFailedNotunsheathed = 380,
        PetitionOfferedS = 381,
        PetitionSigned = 382,
        PetitionSignedS = 383,
        PetitionDeclinedS = 384,
        PetitionAlreadySigned = 385,
        PetitionRestrictedAccountTrial = 386,
        PetitionAlreadySignedOther = 387,
        PetitionInGuild = 388,
        PetitionCreator = 389,
        PetitionNotEnoughSignatures = 390,
        PetitionNotSameServer = 391,
        PetitionFull = 392,
        PetitionAlreadySignedByS = 393,
        GuildNameInvalid = 394,
        SpellUnlearnedS = 395,
        PetSpellRooted = 396,
        PetSpellAffectingCombat = 397,
        PetSpellOutOfRange = 398,
        PetSpellNotBehind = 399,
        PetSpellTargetsDead = 400,
        PetSpellDead = 401,
        PetSpellNopath = 402,
        ItemCantBeDestroyed = 403,
        TicketAlreadyExists = 404,
        TicketCreateError = 405,
        TicketUpdateError = 406,
        TicketDbError = 407,
        TicketNoText = 408,
        TicketTextTooLong = 409,
        ObjectIsBusy = 410,
        ExhaustionWellrested = 411,
        ExhaustionRested = 412,
        ExhaustionNormal = 413,
        ExhaustionTired = 414,
        ExhaustionExhausted = 415,
        NoItemsWhileShapeshifted = 416,
        CantInteractShapeshifted = 417,
        RealmNotFound = 418,
        MailQuestItem = 419,
        MailBoundItem = 420,
        MailConjuredItem = 421,
        MailBag = 422,
        MailToSelf = 423,
        MailTargetNotFound = 424,
        MailDatabaseError = 425,
        MailDeleteItemError = 426,
        MailWrappedCod = 427,
        MailCantSendRealm = 428,
        MailTempReturnOutage = 429,
        MailSent = 430,
        NotHappyEnough = 431,
        UseCantImmune = 432,
        CantBeDisenchanted = 433,
        CantUseDisarmed = 434,
        AuctionQuestItem = 435,
        AuctionBoundItem = 436,
        AuctionConjuredItem = 437,
        AuctionLimitedDurationItem = 438,
        AuctionWrappedItem = 439,
        AuctionLootItem = 440,
        AuctionBag = 441,
        AuctionEquippedBag = 442,
        AuctionDatabaseError = 443,
        AuctionBidOwn = 444,
        AuctionBidIncrement = 445,
        AuctionHigherBid = 446,
        AuctionMinBid = 447,
        AuctionRepairItem = 448,
        AuctionUsedCharges = 449,
        AuctionAlreadyBid = 450,
        AuctionHouseUnavailable = 451,
        AuctionItemHasQuote = 452,
        AuctionHouseBusy = 453,
        AuctionStarted = 454,
        AuctionRemoved = 455,
        AuctionOutbidS = 456,
        AuctionWonS = 457,
        AuctionCommodityWonS = 458,
        AuctionSoldS = 459,
        AuctionExpiredS = 460,
        AuctionRemovedS = 461,
        AuctionBidPlaced = 462,
        LogoutFailed = 463,
        QuestPushSuccessS = 464,
        QuestPushInvalidS = 465,
        QuestPushAcceptedS = 466,
        QuestPushDeclinedS = 467,
        QuestPushBusyS = 468,
        QuestPushDeadS = 469,
        QuestPushLogFullS = 470,
        QuestPushOnquestS = 471,
        QuestPushAlreadyDoneS = 472,
        QuestPushNotDailyS = 473,
        QuestPushTimerExpiredS = 474,
        QuestPushNotInPartyS = 475,
        QuestPushDifferentServerDailyS = 476,
        QuestPushNotAllowedS = 477,
        RaidGroupLowlevel = 478,
        RaidGroupOnly = 479,
        RaidGroupFull = 480,
        RaidGroupRequirementsUnmatch = 481,
        CorpseIsNotInInstance = 482,
        PvpKillHonorable = 483,
        PvpKillDishonorable = 484,
        SpellFailedAlreadyAtFullHealth = 485,
        SpellFailedAlreadyAtFullMana = 486,
        SpellFailedAlreadyAtFullPowerS = 487,
        AutolootMoneyS = 488,
        GenericStunned = 489,
        GenericThrottle = 490,
        ClubFinderSearchingTooFast = 491,
        TargetStunned = 492,
        MustRepairDurability = 493,
        RaidYouJoined = 494,
        RaidYouLeft = 495,
        InstanceGroupJoinedWithParty = 496,
        InstanceGroupJoinedWithRaid = 497,
        RaidMemberAddedS = 498,
        RaidMemberRemovedS = 499,
        InstanceGroupAddedS = 500,
        InstanceGroupRemovedS = 501,
        ClickOnItemToFeed = 502,
        TooManyChatChannels = 503,
        LootRollPending = 504,
        LootPlayerNotFound = 505,
        NotInRaid = 506,
        LoggingOut = 507,
        TargetLoggingOut = 508,
        NotWhileMounted = 509,
        NotWhileShapeshifted = 510,
        NotInCombat = 511,
        NotWhileDisarmed = 512,
        PetBroken = 513,
        TalentWipeError = 514,
        SpecWipeError = 515,
        GlyphWipeError = 516,
        PetSpecWipeError = 517,
        FeignDeathResisted = 518,
        MeetingStoneInQueueS = 519,
        MeetingStoneLeftQueueS = 520,
        MeetingStoneOtherMemberLeft = 521,
        MeetingStonePartyKickedFromQueue = 522,
        MeetingStoneMemberStillInQueue = 523,
        MeetingStoneSuccess = 524,
        MeetingStoneInProgress = 525,
        MeetingStoneMemberAddedS = 526,
        MeetingStoneGroupFull = 527,
        MeetingStoneNotLeader = 528,
        MeetingStoneInvalidLevel = 529,
        MeetingStoneTargetNotInParty = 530,
        MeetingStoneTargetInvalidLevel = 531,
        MeetingStoneMustBeLeader = 532,
        MeetingStoneNoRaidGroup = 533,
        MeetingStoneNeedParty = 534,
        MeetingStoneNotFound = 535,
        MeetingStoneTargetInVehicle = 536,
        GuildemblemSame = 537,
        EquipTradeItem = 538,
        PvpToggleOn = 539,
        PvpToggleOff = 540,
        GroupJoinBattlegroundDeserters = 541,
        GroupJoinBattlegroundDead = 542,
        GroupJoinBattlegroundS = 543,
        GroupJoinBattlegroundFail = 544,
        GroupJoinBattlegroundTooMany = 545,
        SoloJoinBattlegroundS = 546,
        JoinSingleScenarioS = 547,
        BattlegroundTooManyQueues = 548,
        BattlegroundCannotQueueForRated = 549,
        BattledgroundQueuedForRated = 550,
        BattlegroundTeamLeftQueue = 551,
        BattlegroundNotInBattleground = 552,
        AlreadyInArenaTeamS = 553,
        InvalidPromotionCode = 554,
        BgPlayerJoinedSs = 555,
        BgPlayerLeftS = 556,
        RestrictedAccount = 557,
        RestrictedAccountTrial = 558,
        PlayTimeExceeded = 559,
        ApproachingPartialPlayTime = 560,
        ApproachingPartialPlayTime2 = 561,
        ApproachingNoPlayTime = 562,
        ApproachingNoPlayTime2 = 563,
        UnhealthyTime = 564,
        ChatRestrictedTrial = 565,
        ChatThrottled = 566,
        MailReachedCap = 567,
        InvalidRaidTarget = 568,
        RaidLeaderReadyCheckStartS = 569,
        ReadyCheckInProgress = 570,
        ReadyCheckThrottled = 571,
        DungeonDifficultyFailed = 572,
        DungeonDifficultyChangedS = 573,
        TradeWrongRealm = 574,
        TradeNotOnTaplist = 575,
        ChatPlayerAmbiguousS = 576,
        LootCantLootThatNow = 577,
        LootMasterInvFull = 578,
        LootMasterUniqueItem = 579,
        LootMasterOther = 580,
        FilteringYouS = 581,
        UsePreventedByMechanicS = 582,
        ItemUniqueEquippable = 583,
        LfgLeaderIsLfmS = 584,
        LfgPending = 585,
        CantSpeakLangage = 586,
        VendorMissingTurnins = 587,
        BattlegroundNotInTeam = 588,
        NotInBattleground = 589,
        NotEnoughHonorPoints = 590,
        NotEnoughArenaPoints = 591,
        SocketingRequiresMetaGem = 592,
        SocketingMetaGemOnlyInMetaslot = 593,
        SocketingRequiresHydraulicGem = 594,
        SocketingHydraulicGemOnlyInHydraulicslot = 595,
        SocketingRequiresCogwheelGem = 596,
        SocketingCogwheelGemOnlyInCogwheelslot = 597,
        SocketingItemTooLowLevel = 598,
        ItemMaxCountSocketed = 599,
        SystemDisabled = 600,
        QuestFailedTooManyDailyQuestsI = 601,
        ItemMaxCountEquippedSocketed = 602,
        ItemUniqueEquippableSocketed = 603,
        UserSquelched = 604,
        AccountSilenced = 605,
        TooMuchGold = 606,
        NotBarberSitting = 607,
        QuestFailedCais = 608,
        InviteRestrictedTrial = 609,
        VoiceIgnoreFull = 610,
        VoiceIgnoreSelf = 611,
        VoiceIgnoreNotFound = 612,
        VoiceIgnoreAlreadyS = 613,
        VoiceIgnoreAddedS = 614,
        VoiceIgnoreRemovedS = 615,
        VoiceIgnoreAmbiguous = 616,
        VoiceIgnoreDeleted = 617,
        UnknownMacroOptionS = 618,
        NotDuringArenaMatch = 619,
        PlayerSilenced = 620,
        PlayerUnsilenced = 621,
        ComsatDisconnect = 622,
        ComsatReconnectAttempt = 623,
        ComsatConnectFail = 624,
        MailInvalidAttachmentSlot = 625,
        MailTooManyAttachments = 626,
        MailInvalidAttachment = 627,
        MailAttachmentExpired = 628,
        VoiceChatParentalDisableMic = 629,
        ProfaneChatName = 630,
        PlayerSilencedEcho = 631,
        PlayerUnsilencedEcho = 632,
        LootCantLootThat = 633,
        ArenaExpiredCais = 634,
        GroupActionThrottled = 635,
        AlreadyPickpocketed = 636,
        NameInvalid = 637,
        NameNoName = 638,
        NameTooShort = 639,
        NameTooLong = 640,
        NameMixedLanguages = 641,
        NameProfane = 642,
        NameReserved = 643,
        NameThreeConsecutive = 644,
        NameInvalidSpace = 645,
        NameConsecutiveSpaces = 646,
        NameRussianConsecutiveSilentCharacters = 647,
        NameRussianSilentCharacterAtBeginningOrEnd = 648,
        NameDeclensionDoesntMatchBaseName = 649,
        RecruitAFriendNotLinked = 650,
        RecruitAFriendNotNow = 651,
        RecruitAFriendSummonLevelMax = 652,
        RecruitAFriendSummonCooldown = 653,
        RecruitAFriendSummonOffline = 654,
        RecruitAFriendInsufExpanLvl = 655,
        RecruitAFriendMapIncomingTransferNotAllowed = 656,
        NotSameAccount = 657,
        BadOnUseEnchant = 658,
        TradeSelf = 659,
        TooManySockets = 660,
        ItemMaxLimitCategoryCountExceededIs = 661,
        TradeTargetMaxLimitCategoryCountExceededIs = 662,
        ItemMaxLimitCategorySocketedExceededIs = 663,
        ItemMaxLimitCategoryEquippedExceededIs = 664,
        ShapeshiftFormCannotEquip = 665,
        ItemInventoryFullSatchel = 666,
        ScalingStatItemLevelExceeded = 667,
        ScalingStatItemLevelTooLow = 668,
        PurchaseLevelTooLow = 669,
        GroupSwapFailed = 670,
        InviteInCombat = 671,
        InvalidGlyphSlot = 672,
        GenericNoValidTargets = 673,
        CalendarEventAlertS = 674,
        PetLearnSpellS = 675,
        PetLearnAbilityS = 676,
        PetSpellUnlearnedS = 677,
        InviteUnknownRealm = 678,
        InviteNoPartyServer = 679,
        InvitePartyBusy = 680,
        PartyTargetAmbiguous = 681,
        PartyLfgInviteRaidLocked = 682,
        PartyLfgBootLimit = 683,
        PartyLfgBootCooldownS = 684,
        PartyLfgBootNotEligibleS = 685,
        PartyLfgBootInpatientTimerS = 686,
        PartyLfgBootInProgress = 687,
        PartyLfgBootTooFewPlayers = 688,
        PartyLfgBootVoteSucceeded = 689,
        PartyLfgBootVoteFailed = 690,
        PartyLfgBootInCombat = 691,
        PartyLfgBootDungeonComplete = 692,
        PartyLfgBootLootRolls = 693,
        PartyLfgBootVoteRegistered = 694,
        PartyPrivateGroupOnly = 695,
        PartyLfgTeleportInCombat = 696,
        RaidDisallowedByLevel = 697,
        RaidDisallowedByCrossRealm = 698,
        PartyRoleNotAvailable = 699,
        JoinLfgObjectFailed = 700,
        LfgRemovedLevelup = 701,
        LfgRemovedXpToggle = 702,
        LfgRemovedFactionChange = 703,
        BattlegroundInfoThrottled = 704,
        BattlegroundAlreadyIn = 705,
        ArenaTeamChangeFailedQueued = 706,
        ArenaTeamPermissions = 707,
        NotWhileFalling = 708,
        NotWhileMoving = 709,
        NotWhileFatigued = 710,
        MaxSockets = 711,
        MultiCastActionTotemS = 712,
        BattlegroundJoinLevelup = 713,
        RemoveFromPvpQueueXpGain = 714,
        BattlegroundJoinXpGain = 715,
        BattlegroundJoinMercenary = 716,
        BattlegroundJoinTooManyHealers = 717,
        BattlegroundJoinRatedTooManyHealers = 718,
        BattlegroundJoinTooManyTanks = 719,
        BattlegroundJoinTooManyDamage = 720,
        RaidDifficultyFailed = 721,
        RaidDifficultyChangedS = 722,
        LegacyRaidDifficultyChangedS = 723,
        RaidLockoutChangedS = 724,
        RaidConvertedToParty = 725,
        PartyConvertedToRaid = 726,
        PlayerDifficultyChangedS = 727,
        GmresponseDbError = 728,
        BattlegroundJoinRangeIndex = 729,
        ArenaJoinRangeIndex = 730,
        RemoveFromPvpQueueFactionChange = 731,
        BattlegroundJoinFailed = 732,
        BattlegroundJoinNoValidSpecForRole = 733,
        BattlegroundJoinRespec = 734,
        BattlegroundInvitationDeclined = 735,
        BattlegroundJoinTimedOut = 736,
        BattlegroundDupeQueue = 737,
        BattlegroundJoinMustCompleteQuest = 738,
        InBattlegroundRespec = 739,
        MailLimitedDurationItem = 740,
        YellRestrictedTrial = 741,
        ChatRaidRestrictedTrial = 742,
        LfgRoleCheckFailed = 743,
        LfgRoleCheckFailedTimeout = 744,
        LfgRoleCheckFailedNotViable = 745,
        LfgReadyCheckFailed = 746,
        LfgReadyCheckFailedTimeout = 747,
        LfgGroupFull = 748,
        LfgNoLfgObject = 749,
        LfgNoSlotsPlayer = 750,
        LfgNoSlotsParty = 751,
        LfgNoSpec = 752,
        LfgMismatchedSlots = 753,
        LfgMismatchedSlotsLocalXrealm = 754,
        LfgPartyPlayersFromDifferentRealms = 755,
        LfgMembersNotPresent = 756,
        LfgGetInfoTimeout = 757,
        LfgInvalidSlot = 758,
        LfgDeserterPlayer = 759,
        LfgDeserterParty = 760,
        LfgDead = 761,
        LfgRandomCooldownPlayer = 762,
        LfgRandomCooldownParty = 763,
        LfgTooManyMembers = 764,
        LfgTooFewMembers = 765,
        LfgProposalFailed = 766,
        LfgProposalDeclinedSelf = 767,
        LfgProposalDeclinedParty = 768,
        LfgNoSlotsSelected = 769,
        LfgNoRolesSelected = 770,
        LfgRoleCheckInitiated = 771,
        LfgReadyCheckInitiated = 772,
        LfgPlayerDeclinedRoleCheck = 773,
        LfgPlayerDeclinedReadyCheck = 774,
        LfgJoinedQueue = 775,
        LfgJoinedFlexQueue = 776,
        LfgJoinedRfQueue = 777,
        LfgJoinedScenarioQueue = 778,
        LfgJoinedWorldPvpQueue = 779,
        LfgJoinedBattlefieldQueue = 780,
        LfgJoinedList = 781,
        LfgLeftQueue = 782,
        LfgLeftList = 783,
        LfgRoleCheckAborted = 784,
        LfgReadyCheckAborted = 785,
        LfgCantUseBattleground = 786,
        LfgCantUseDungeons = 787,
        LfgReasonTooManyLfg = 788,
        InvalidTeleportLocation = 789,
        TooFarToInteract = 790,
        BattlegroundPlayersFromDifferentRealms = 791,
        DifficultyChangeCooldownS = 792,
        DifficultyChangeCombatCooldownS = 793,
        DifficultyChangeWorldstate = 794,
        DifficultyChangeEncounter = 795,
        DifficultyChangeCombat = 796,
        DifficultyChangePlayerBusy = 797,
        DifficultyChangeAlreadyStarted = 798,
        DifficultyChangeOtherHeroicS = 799,
        DifficultyChangeHeroicInstanceAlreadyRunning = 800,
        ArenaTeamPartySize = 801,
        QuestForceRemovedS = 802,
        AttackNoActions = 803,
        InRandomBg = 804,
        InNonRandomBg = 805,
        AuctionEnoughItems = 806,
        BnFriendSelf = 807,
        BnFriendAlready = 808,
        BnFriendBlocked = 809,
        BnFriendListFull = 810,
        BnFriendRequestSent = 811,
        BnBroadcastThrottle = 812,
        BgDeveloperOnly = 813,
        CurrencySpellSlotMismatch = 814,
        CurrencyNotTradable = 815,
        RequiresExpansionS = 816,
        QuestFailedSpell = 817,
        TalentFailedNotEnoughTalentsInPrimaryTree = 818,
        TalentFailedNoPrimaryTreeSelected = 819,
        TalentFailedCantRemoveTalent = 820,
        TalentFailedUnknown = 821,
        WargameRequestFailure = 822,
        RankRequiresAuthenticator = 823,
        GuildBankVoucherFailed = 824,
        WargameRequestSent = 825,
        RequiresAchievementI = 826,
        RefundResultExceedMaxCurrency = 827,
        CantBuyQuantity = 828,
        ItemIsBattlePayLocked = 829,
        PartyAlreadyInBattlegroundQueue = 830,
        PartyConfirmingBattlegroundQueue = 831,
        BattlefieldTeamPartySize = 832,
        InsuffTrackedCurrencyIs = 833,
        NotOnTournamentRealm = 834,
        GuildTrialAccountTrial = 835,
        GuildTrialAccountVeteran = 836,
        GuildUndeletableDueToLevel = 837,
        CantDoThatInAGroup = 838,
        GuildLeaderReplaced = 839,
        TransmogrifyCantEquip = 840,
        TransmogrifyInvalidItemType = 841,
        TransmogrifyNotSoulbound = 842,
        TransmogrifyInvalidSource = 843,
        TransmogrifyInvalidDestination = 844,
        TransmogrifyMismatch = 845,
        TransmogrifyLegendary = 846,
        TransmogrifySameItem = 847,
        TransmogrifySameAppearance = 848,
        TransmogrifyNotEquipped = 849,
        VoidDepositFull = 850,
        VoidWithdrawFull = 851,
        VoidStorageWrapped = 852,
        VoidStorageStackable = 853,
        VoidStorageUnbound = 854,
        VoidStorageRepair = 855,
        VoidStorageCharges = 856,
        VoidStorageQuest = 857,
        VoidStorageConjured = 858,
        VoidStorageMail = 859,
        VoidStorageBag = 860,
        VoidTransferStorageFull = 861,
        VoidTransferInvFull = 862,
        VoidTransferInternalError = 863,
        VoidTransferItemInvalid = 864,
        DifficultyDisabledInLfg = 865,
        VoidStorageUnique = 866,
        VoidStorageLoot = 867,
        VoidStorageHoliday = 868,
        VoidStorageDuration = 869,
        VoidStorageLoadFailed = 870,
        VoidStorageInvalidItem = 871,
        ParentalControlsChatMuted = 872,
        SorStartExperienceIncomplete = 873,
        SorInvalidEmail = 874,
        SorInvalidComment = 875,
        ChallengeModeResetCooldownS = 876,
        ChallengeModeResetKeystone = 877,
        PetJournalAlreadyInLoadout = 878,
        ReportSubmittedSuccessfully = 879,
        ReportSubmissionFailed = 880,
        SuggestionSubmittedSuccessfully = 881,
        BugSubmittedSuccessfully = 882,
        ChallengeModeEnabled = 883,
        ChallengeModeDisabled = 884,
        PetbattleCreateFailed = 885,
        PetbattleNotHere = 886,
        PetbattleNotHereOnTransport = 887,
        PetbattleNotHereUnevenGround = 888,
        PetbattleNotHereObstructed = 889,
        PetbattleNotWhileInCombat = 890,
        PetbattleNotWhileDead = 891,
        PetbattleNotWhileFlying = 892,
        PetbattleTargetInvalid = 893,
        PetbattleTargetOutOfRange = 894,
        PetbattleTargetNotCapturable = 895,
        PetbattleNotATrainer = 896,
        PetbattleDeclined = 897,
        PetbattleInBattle = 898,
        PetbattleInvalidLoadout = 899,
        PetbattleAllPetsDead = 900,
        PetbattleNoPetsInSlots = 901,
        PetbattleNoAccountLock = 902,
        PetbattleWildPetTapped = 903,
        PetbattleRestrictedAccount = 904,
        PetbattleOpponentNotAvailable = 905,
        PetbattleNotWhileInMatchedBattle = 906,
        CantHaveMorePetsOfThatType = 907,
        CantHaveMorePets = 908,
        PvpMapNotFound = 909,
        PvpMapNotSet = 910,
        PetbattleQueueQueued = 911,
        PetbattleQueueAlreadyQueued = 912,
        PetbattleQueueJoinFailed = 913,
        PetbattleQueueJournalLock = 914,
        PetbattleQueueRemoved = 915,
        PetbattleQueueProposalDeclined = 916,
        PetbattleQueueProposalTimeout = 917,
        PetbattleQueueOpponentDeclined = 918,
        PetbattleQueueRequeuedInternal = 919,
        PetbattleQueueRequeuedRemoved = 920,
        PetbattleQueueSlotLocked = 921,
        PetbattleQueueSlotEmpty = 922,
        PetbattleQueueSlotNoTracker = 923,
        PetbattleQueueSlotNoSpecies = 924,
        PetbattleQueueSlotCantBattle = 925,
        PetbattleQueueSlotRevoked = 926,
        PetbattleQueueSlotDead = 927,
        PetbattleQueueSlotNoPet = 928,
        PetbattleQueueNotWhileNeutral = 929,
        PetbattleGameTimeLimitWarning = 930,
        PetbattleGameRoundsLimitWarning = 931,
        HasRestriction = 932,
        ItemUpgradeItemTooLowLevel = 933,
        ItemUpgradeNoPath = 934,
        ItemUpgradeNoMoreUpgrades = 935,
        BonusRollEmpty = 936,
        ChallengeModeFull = 937,
        ChallengeModeInProgress = 938,
        ChallengeModeIncorrectKeystone = 939,
        BattletagFriendNotFound = 940,
        BattletagFriendNotValid = 941,
        BattletagFriendNotAllowed = 942,
        BattletagFriendThrottled = 943,
        BattletagFriendSuccess = 944,
        PetTooHighLevelToUncage = 945,
        PetbattleInternal = 946,
        CantCagePetYet = 947,
        NoLootInChallengeMode = 948,
        QuestPetBattleVictoriesPvpIi = 949,
        RoleCheckAlreadyInProgress = 950,
        RecruitAFriendAccountLimit = 951,
        RecruitAFriendFailed = 952,
        SetLootPersonal = 953,
        SetLootMethodFailedCombat = 954,
        ReagentBankFull = 955,
        ReagentBankLocked = 956,
        GarrisonBuildingExists = 957,
        GarrisonInvalidPlot = 958,
        GarrisonInvalidBuildingid = 959,
        GarrisonInvalidPlotBuilding = 960,
        GarrisonRequiresBlueprint = 961,
        GarrisonNotEnoughCurrency = 962,
        GarrisonNotEnoughGold = 963,
        GarrisonCompleteMissionWrongFollowerType = 964,
        AlreadyUsingLfgList = 965,
        RestrictedAccountLfgListTrial = 966,
        ToyUseLimitReached = 967,
        ToyAlreadyKnown = 968,
        TransmogSetAlreadyKnown = 969,
        NotEnoughCurrency = 970,
        SpecIsDisabled = 971,
        FeatureRestrictedTrial = 972,
        CantBeObliterated = 973,
        CantBeScrapped = 974,
        ArtifactRelicDoesNotMatchArtifact = 975,
        MustEquipArtifact = 976,
        CantDoThatRightNow = 977,
        AffectingCombat = 978,
        EquipmentManagerCombatSwapS = 979,
        EquipmentManagerBagsFull = 980,
        EquipmentManagerMissingItemS = 981,
        MovieRecordingWarningPerf = 982,
        MovieRecordingWarningDiskFull = 983,
        MovieRecordingWarningNoMovie = 984,
        MovieRecordingWarningRequirements = 985,
        MovieRecordingWarningCompressing = 986,
        NoChallengeModeReward = 987,
        ClaimedChallengeModeReward = 988,
        ChallengeModePeriodResetSs = 989,
        CantDoThatChallengeModeActive = 990,
        TalentFailedRestArea = 991,
        CannotAbandonLastPet = 992,
        TestCvarSetSss = 993,
        QuestTurnInFailReason = 994,
        ClaimedChallengeModeRewardOld = 995,
        TalentGrantedByAura = 996,
        ChallengeModeAlreadyComplete = 997,
        GlyphTargetNotAvailable = 998,
        PvpWarmodeToggleOn = 999,
        PvpWarmodeToggleOff = 1000,
        SpellFailedLevelRequirement = 1001,
        BattlegroundJoinRequiresLevel = 1002,
        BattlegroundJoinDisqualified = 1003,
        BattlegroundJoinDisqualifiedNoName = 1004,
        VoiceChatGenericUnableToConnect = 1005,
        VoiceChatServiceLost = 1006,
        VoiceChatChannelNameTooShort = 1007,
        VoiceChatChannelNameTooLong = 1008,
        VoiceChatChannelAlreadyExists = 1009,
        VoiceChatTargetNotFound = 1010,
        VoiceChatTooManyRequests = 1011,
        VoiceChatPlayerSilenced = 1012,
        VoiceChatParentalDisableAll = 1013,
        VoiceChatDisabled = 1014,
        NoPvpReward = 1015,
        ClaimedPvpReward = 1016,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1017,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1018,
        AzeriteEssenceSelectionFailedConditionFailed = 1019,
        AzeriteEssenceSelectionFailedRestArea = 1020,
        AzeriteEssenceSelectionFailedSlotLocked = 1021,
        AzeriteEssenceSelectionFailedNotAtForge = 1022,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1023,
        AzeriteEssenceSelectionFailedNotEquipped = 1024,
        SocketingRequiresPunchcardredGem = 1025,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1026,
        SocketingRequiresPunchcardyellowGem = 1027,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1028,
        SocketingRequiresPunchcardblueGem = 1029,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1030,
        LevelLinkingResultLinked = 1031,
        LevelLinkingResultUnlinked = 1032,
        ClubFinderErrorPostClub = 1033,
        ClubFinderErrorApplyClub = 1034,
        ClubFinderErrorRespondApplicant = 1035,
        ClubFinderErrorCancelApplication = 1036,
        ClubFinderErrorTypeAcceptApplication = 1037,
        ClubFinderErrorTypeNoInvitePermissions = 1038,
        ClubFinderErrorTypeNoPostingPermissions = 1039,
        ClubFinderErrorTypeApplicantList = 1040,
        ClubFinderErrorTypeApplicantListNoPerm = 1041,
        ClubFinderErrorTypeFinderNotAvailable = 1042,
        ClubFinderErrorTypeGetPostingIds = 1043,
        ClubFinderErrorTypeJoinApplication = 1044,
        ClubFinderErrorTypeFlaggedRename = 1045,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1046,
        ItemInteractionNotEnoughGold = 1047,
        ItemInteractionNotEnoughCurrency = 1048,
        PlayerChoiceErrorPendingChoice = 1049,
        SoulbindInvalidConduit = 1050,
        SoulbindInvalidConduitItem = 1051,
        SoulbindInvalidTalent = 1052,
        SoulbindDuplicateConduit = 1053,
        ActivateSoulbindFailedRestArea = 1054,
        CantUseProfanity = 1055,
        NotInPetBattle = 1056,
        NotInNpe = 1057,
    }

    public enum SceneFlags
    {
        Unk1 = 0x01,
        CancelAtEnd = 0x02,
        NotCancelable = 0x04,
        Unk8 = 0x08,
        Unk16 = 0x10, // 16, most common value
        Unk32 = 0x20
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
}
