/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
        public const int MaxMailItems = 12;
        public const int MaxDeclinedNameCases = 5;
        public const int MaxHolidayDurations = 10;
        public const int MaxHolidayDates = 16;
        public const int MaxHolidayFlags = 10;
        public const int DefaultMaxLevel = 120;
        public const int MaxLevel = 120;
        public const int StrongMaxLevel = 255;
        public const int MaxOverrideSpell = 10;
        public const int MaxWorldMapOverlayArea = 4;
        public const int MaxMountCapabilities = 24;
        public const int MaxLockCase = 8;

        /// <summary>
        /// BattlePets Const
        /// </summary>
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
        public const LocaleConstant DefaultLocale = LocaleConstant.enUS;
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
        public const uint MaxEquipmentItems = 3;

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
        public const int CreatureNoPathEvadeTime = 5 * Time.InMilliseconds;
        public const int PetFocusRegenInterval = 4 * Time.InMilliseconds;
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
        public const float DefaultWorldObjectSize = 0.388999998569489f;      // player size, also currently used (correctly?) for any non Unit world objects
        public const float AttackDistance = 5.0f;
        public const float DefaultCombatReach = 1.5f;
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
    }

    public enum LocaleConstant
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
        EnemySpar = 0x20,   // guessed, sparring with enemies?
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
    public enum FactionFlags
    {
        None = 0x00,                 // no faction flag
        Visible = 0x01,                 // makes visible in client (set or can be set at interaction with target of this faction)
        AtWar = 0x02,                 // enable AtWar-button in client. player controlled (except opposition team always war state), Flag only set on initial creation
        Hidden = 0x04,                 // hidden faction from reputation pane in client (player can gain reputation, but this update not sent to client)
        InvisibleForced = 0x08,                 // always overwrite FACTION_FLAG_VISIBLE and hide faction in rep.list, used for hide opposite team factions
        PeaceForced = 0x10,                 // always overwrite FACTION_FLAG_AT_WAR, used for prevent war with own team factions
        Inactive = 0x20,                 // player controlled, state stored in characters.data (CMSG_SET_FACTION_INACTIVE)
        Rival = 0x40,                 // flag for the two competing outland factions
        Special = 0x80                  // horde and alliance home cities and their northrend allies have this flag
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
        //RACE_ZANDALARI_TROLL    = 31,
        //RACE_KUL_TIRAN          = 32,
        //RACE_THIN_HUMAN         = 33,
        DarkIronDwarf = 34,
        //RACE_VULPERA            = 35,
        MagharOrc = 36,
        Max,

        RaceMaskAllPlayable = ((1 << (Human - 1)) | (1 << (Orc - 1)) | (1 << (Dwarf - 1)) | (1 << (NightElf - 1)) | (1 << (Undead - 1))
            | (1 << (Tauren - 1)) | (1 << (Gnome - 1)) | (1 << (Troll - 1)) | (1 << (BloodElf - 1)) | (1 << (Draenei - 1))
            | (1 << (Goblin - 1)) | (1 << (Worgen - 1)) | (1 << (PandarenNeutral - 1)) | (1 << (PandarenAlliance - 1)) | (1 << (PandarenHorde - 1))
            | (1 << (Nightborne - 1)) | (1 << (HighmountainTauren - 1)) | (1 << (VoidElf - 1)) | (1 << (LightforgedDraenei - 1)) | (1 << (DarkIronDwarf - 1)) | (1 << (MagharOrc - 1))),

        RaceMaskAlliance = ((1 << (Human - 1)) | (1 << (Dwarf - 1)) | (1 << (NightElf - 1)) | (1 << (Gnome - 1))
            | (1 << (Draenei - 1)) | (1 << (Worgen - 1)) | (1 << (PandarenAlliance - 1)) | (1 << (VoidElf - 1)) | (1 << (LightforgedDraenei - 1)) | (1 << (DarkIronDwarf - 1))),

        RaceMaskHorde = RaceMaskAllPlayable & ~RaceMaskAlliance
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

        Max
    }

    public enum DifficultyFlags : byte
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
        NobleGarden = 181,
        ChildrensWeek = 201,
        CallToArmsAv = 283,
        CallToArmsWs = 284,
        CallToArmsAb = 285,
        FishingExtravaganza = 301,
        HarvestFestival = 321,
        HallowsEnd = 324,
        LunarFestival = 327,
        LoveIsInTheAir = 335,
        FireFestival = 341,
        CallToArmsEy = 353,
        Brewfest = 372,
        DarkmoonFaireElwynn = 374,
        DarkmoonFaireThunder = 375,
        DarkmoonFaireShattrath = 376,
        PiratesDay = 398,
        CallToArmsSa = 400,
        PilgrimsBounty = 404,
        WotlkLaunch = 406,
        DayOfDead = 409,
        CallToArmsIc = 420,
        //LoveIsInTheAir = 423,
        KaluAkFishingDerby = 424,
        CallToArmsBfg = 435,
        CallToArmsTp = 436,
        RatedBg15Vs15 = 442,
        RatedBg25Vs25 = 443,
        Anniversary7Years = 467,
        DarkmoonFaireTerokkar = 479,
        Anniversary8Years = 484,
        CallToArmsSm = 488,
        CallToArmsTk = 489,
        //CallToArmsAv        = 490,
        //CallToArmsAb        = 491,
        //CallToArmsEy        = 492,
        //CallToArmsAv        = 493,
        //CallToArmsSm        = 494,
        //CallToArmsSa        = 495,
        //CallToArmsTk        = 496,
        //CallToArmsBfg       = 497,
        //CallToArmsTp        = 498,
        //CallToArmsWs        = 499,
        Anniversary9Years = 509,
        Anniversary10Years = 514,
        CallToArmsDg = 515,
        //CallToArmsDg        = 516
        TimewalkingOutlands = 559,
        ApexisBonusEvent = 560,
        ArenaSkirmishBonusEvent = 561,
        TimewalkingNorthrend = 562,
        BattlegroundBonusEvent = 563,
        DraenorDungeonEvent = 564,
        PetBattleBonusEvent = 565,
        Anniversary11Years = 566,
        TimewalkingCataclysm = 587
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
        Talk = 0,                // Source/Target = Creature, Target = Any, Datalong = Talk Type (0=Say, 1=Whisper, 2=Yell, 3=Emote Text, 4=Boss Emote Text), Datalong2 & 1 = Player Talk (Instead Of Creature), Dataint = StringId
        Emote = 1,                // Source/Target = Creature, Datalong = Emote Id, Datalong2 = 0: Set Emote State; > 0: Play Emote State
        FieldSet = 2,                // Source/Target = Creature, Datalong = Field Id, Datalog2 = Value
        MoveTo = 3,                // Source/Target = Creature, Datalong2 = Time To Reach, X/Y/Z = Destination
        FlagSet = 4,                // Source/Target = Creature, Datalong = Field Id, Datalog2 = Bitmask
        FlagRemove = 5,                // Source/Target = Creature, Datalong = Field Id, Datalog2 = Bitmask
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
        AlwaysMaxskill,
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
        AuctionGetallDelay,
        AuctionLevelReq,
        AuctionSearchDelay,
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
        CleanCharacterDb,
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
        DemonHuntersPerRealm,
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
        HotfixCacheVersion,
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
        IntervalLogUpdate,
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
        MinDualspecLevel,
        MinLevelStatSave,
        MinLogUpdate,
        MinPetName,
        MinPetitionSigns,
        MinPlayerName,
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
        RateTargetPosRecalculationRange,
        RateXpExplore,
        RateXpGuildModifier,
        RateXpKill,
        RateXpBgKill,
        RateXpQuest,
        RealmZone,
        ResetDuelCooldowns,
        ResetDuelHealthMana,
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
        SocketTimeouttime,
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
        UiQuestLevelsInDialogs,     // Should We Add Quest Levels To The Title In The Npc Dialogs?
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
        InvFull = 1,
        BankFull = 2,
        CantEquipLevelI = 3,
        CantEquipSkill = 4,
        CantEquipEver = 5,
        CantEquipRank = 6,
        CantEquipRating = 7,
        CantEquipReputation = 8,
        ProficiencyNeeded = 9,
        WrongSlot = 10,
        CantEquipNeedTalent = 11,
        BagFull = 12,
        InternalBagError = 13,
        DestroyNonemptyBag = 14,
        BagInBag = 15,
        TooManySpecialBags = 16,
        TradeEquippedBag = 17,
        AmmoOnly = 18,
        NoSlotAvailable = 19,
        WrongBagType = 20,
        ItemMaxCount = 21,
        NotEquippable = 22,
        CantStack = 23,
        CantSwap = 24,
        SlotEmpty = 25,
        ItemNotFound = 26,
        TooFewToSplit = 27,
        SplitFailed = 28,
        NotABag = 29,
        NotOwner = 30,
        OnlyOneQuiver = 31,
        NoBankSlot = 32,
        NoBankHere = 33,
        ItemLocked = 34,
        TwoHandedEquipped = 35,
        VendorNotInterested = 36,
        VendorHatesYou = 37,
        VendorSoldOut = 38,
        VendorTooFar = 39,
        VendorDoesntBuy = 40,
        NotEnoughMoney = 41,
        ReceiveItemS = 42,
        DropBoundItem = 43,
        TradeBoundItem = 44,
        TradeQuestItem = 45,
        TradeTempEnchantBound = 46,
        TradeGroundItem = 47,
        TradeBag = 48,
        SpellFailedS = 49,
        ItemCooldown = 50,
        PotionCooldown = 51,
        FoodCooldown = 52,
        SpellCooldown = 53,
        AbilityCooldown = 54,
        SpellAlreadyKnownS = 55,
        PetSpellAlreadyKnownS = 56,
        ProficiencyGainedS = 57,
        SkillGainedS = 58,
        SkillUpSi = 59,
        LearnSpellS = 60,
        LearnAbilityS = 61,
        LearnPassiveS = 62,
        LearnRecipeS = 63,
        LearnCompanionS = 64,
        LearnMountS = 65,
        LearnToyS = 66,
        LearnHeirloomS = 67,
        LearnTransmogS = 68,
        CompletedTransmogSetS = 69,
        RevokeTransmogS = 70,
        InvitePlayerS = 71,
        InviteSelf = 72,
        InvitedToGroupSs = 73,
        InvitedAlreadyInGroupSs = 74,
        AlreadyInGroupS = 75,
        CrossRealmRaidInvite = 76,
        PlayerBusyS = 77,
        NewLeaderS = 78,
        NewLeaderYou = 79,
        NewGuideS = 80,
        NewGuideYou = 81,
        LeftGroupS = 82,
        LeftGroupYou = 83,
        GroupDisbanded = 84,
        DeclineGroupS = 85,
        JoinedGroupS = 86,
        UninviteYou = 87,
        BadPlayerNameS = 88,
        NotInGroup = 89,
        TargetNotInGroupS = 90,
        TargetNotInInstanceS = 91,
        NotInInstanceGroup = 92,
        GroupFull = 93,
        NotLeader = 94,
        PlayerDiedS = 95,
        GuildCreateS = 96,
        GuildInviteS = 97,
        InvitedToGuildSss = 98,
        AlreadyInGuildS = 99,
        AlreadyInvitedToGuildS = 100,
        InvitedToGuild = 101,
        AlreadyInGuild = 102,
        GuildAccept = 103,
        GuildDeclineS = 104,
        GuildDeclineAutoS = 105,
        GuildPermissions = 106,
        GuildJoinS = 107,
        GuildFounderS = 108,
        GuildPromoteSss = 109,
        GuildDemoteSs = 110,
        GuildDemoteSss = 111,
        GuildInviteSelf = 112,
        GuildQuitS = 113,
        GuildLeaveS = 114,
        GuildRemoveSs = 115,
        GuildRemoveSelf = 116,
        GuildDisbandS = 117,
        GuildDisbandSelf = 118,
        GuildLeaderS = 119,
        GuildLeaderSelf = 120,
        GuildPlayerNotFoundS = 121,
        GuildPlayerNotInGuildS = 122,
        GuildPlayerNotInGuild = 123,
        GuildCantPromoteS = 124,
        GuildCantDemoteS = 125,
        GuildNotInAGuild = 126,
        GuildInternal = 127,
        GuildLeaderIsS = 128,
        GuildLeaderChangedSs = 129,
        GuildDisbanded = 130,
        GuildNotAllied = 131,
        GuildLeaderLeave = 132,
        GuildRanksLocked = 133,
        GuildRankInUse = 134,
        GuildRankTooHighS = 135,
        GuildRankTooLowS = 136,
        GuildNameExistsS = 137,
        GuildWithdrawLimit = 138,
        GuildNotEnoughMoney = 139,
        GuildTooMuchMoney = 140,
        GuildBankConjuredItem = 141,
        GuildBankEquippedItem = 142,
        GuildBankBoundItem = 143,
        GuildBankQuestItem = 144,
        GuildBankWrappedItem = 145,
        GuildBankFull = 146,
        GuildBankWrongTab = 147,
        NoGuildCharter = 148,
        OutOfRange = 149,
        PlayerDead = 150,
        ClientLockedOut = 151,
        ClientOnTransport = 152,
        KilledByS = 153,
        LootLocked = 154,
        LootTooFar = 155,
        LootDidntKill = 156,
        LootBadFacing = 157,
        LootNotstanding = 158,
        LootStunned = 159,
        LootNoUi = 160,
        LootWhileInvulnerable = 161,
        NoLoot = 162,
        QuestAcceptedS = 163,
        QuestCompleteS = 164,
        QuestFailedS = 165,
        QuestFailedBagFullS = 166,
        QuestFailedMaxCountS = 167,
        QuestFailedLowLevel = 168,
        QuestFailedMissingItems = 169,
        QuestFailedWrongRace = 170,
        QuestFailedNotEnoughMoney = 171,
        QuestFailedExpansion = 172,
        QuestOnlyOneTimed = 173,
        QuestNeedPrereqs = 174,
        QuestNeedPrereqsCustom = 175,
        QuestAlreadyOn = 176,
        QuestAlreadyDone = 177,
        QuestAlreadyDoneDaily = 178,
        QuestHasInProgress = 179,
        QuestRewardExpI = 180,
        QuestRewardMoneyS = 181,
        QuestMustChoose = 182,
        QuestLogFull = 183,
        CombatDamageSsi = 184,
        InspectS = 185,
        CantUseItem = 186,
        CantUseItemInArena = 187,
        CantUseItemInRatedBattleground = 188,
        MustEquipItem = 189,
        PassiveAbility = 190,
        Hand2skillnotfound = 191,
        NoAttackTarget = 192,
        InvalidAttackTarget = 193,
        AttackPvpTargetWhileUnflagged = 194,
        AttackStunned = 195,
        AttackPacified = 196,
        AttackMounted = 197,
        AttackFleeing = 198,
        AttackConfused = 199,
        AttackCharmed = 200,
        AttackDead = 201,
        AttackPreventedByMechanicS = 202,
        AttackChannel = 203,
        Taxisamenode = 204,
        Taxinosuchpath = 205,
        Taxiunspecifiedservererror = 206,
        Taxinotenoughmoney = 207,
        Taxitoofaraway = 208,
        Taxinovendornearby = 209,
        Taxinotvisited = 210,
        Taxiplayerbusy = 211,
        Taxiplayeralreadymounted = 212,
        Taxiplayershapeshifted = 213,
        Taxiplayermoving = 214,
        Taxinopaths = 215,
        Taxinoteligible = 216,
        Taxinotstanding = 217,
        NoReplyTarget = 218,
        GenericNoTarget = 219,
        InitiateTradeS = 220,
        TradeRequestS = 221,
        TradeBlockedS = 222,
        TradeTargetDead = 223,
        TradeTooFar = 224,
        TradeCancelled = 225,
        TradeComplete = 226,
        TradeBagFull = 227,
        TradeTargetBagFull = 228,
        TradeMaxCountExceeded = 229,
        TradeTargetMaxCountExceeded = 230,
        AlreadyTrading = 231,
        MountInvalidmountee = 232,
        MountToofaraway = 233,
        MountAlreadymounted = 234,
        MountNotmountable = 235,
        MountNotyourpet = 236,
        MountOther = 237,
        MountLooting = 238,
        MountRacecantmount = 239,
        MountShapeshifted = 240,
        MountNoFavorites = 241,
        DismountNopet = 242,
        DismountNotmounted = 243,
        DismountNotyourpet = 244,
        SpellFailedTotems = 245,
        SpellFailedReagents = 246,
        SpellFailedReagentsGeneric = 247,
        CantTradeGold = 248,
        SpellFailedEquippedItem = 249,
        SpellFailedEquippedItemClassS = 250,
        SpellFailedShapeshiftFormS = 251,
        SpellFailedAnotherInProgress = 252,
        Badattackfacing = 253,
        Badattackpos = 254,
        ChestInUse = 255,
        UseCantOpen = 256,
        UseLocked = 257,
        DoorLocked = 258,
        ButtonLocked = 259,
        UseLockedWithItemS = 260,
        UseLockedWithSpellS = 261,
        UseLockedWithSpellKnownSi = 262,
        UseTooFar = 263,
        UseBadAngle = 264,
        UseObjectMoving = 265,
        UseSpellFocus = 266,
        UseDestroyed = 267,
        SetLootFreeforall = 268,
        SetLootRoundrobin = 269,
        SetLootMaster = 270,
        SetLootGroup = 271,
        SetLootThresholdS = 272,
        NewLootMasterS = 273,
        SpecifyMasterLooter = 274,
        LootSpecChangedS = 275,
        TameFailed = 276,
        ChatWhileDead = 277,
        ChatPlayerNotFoundS = 278,
        Newtaxipath = 279,
        NoPet = 280,
        Notyourpet = 281,
        PetNotRenameable = 282,
        QuestObjectiveCompleteS = 283,
        QuestUnknownComplete = 284,
        QuestAddKillSii = 285,
        QuestAddFoundSii = 286,
        QuestAddItemSii = 287,
        QuestAddPlayerKillSii = 288,
        Cannotcreatedirectory = 289,
        Cannotcreatefile = 290,
        PlayerWrongFaction = 291,
        PlayerIsNeutral = 292,
        BankslotFailedTooMany = 293,
        BankslotInsufficientFunds = 294,
        BankslotNotbanker = 295,
        FriendDbError = 296,
        FriendListFull = 297,
        FriendAddedS = 298,
        BattletagFriendAddedS = 299,
        FriendOnlineSs = 300,
        FriendOfflineS = 301,
        FriendNotFound = 302,
        FriendWrongFaction = 303,
        FriendRemovedS = 304,
        BattletagFriendRemovedS = 305,
        FriendError = 306,
        FriendAlreadyS = 307,
        FriendSelf = 308,
        FriendDeleted = 309,
        IgnoreFull = 310,
        IgnoreSelf = 311,
        IgnoreNotFound = 312,
        IgnoreAlreadyS = 313,
        IgnoreAddedS = 314,
        IgnoreRemovedS = 315,
        IgnoreAmbiguous = 316,
        IgnoreDeleted = 317,
        OnlyOneBolt = 318,
        OnlyOneAmmo = 319,
        SpellFailedEquippedSpecificItem = 320,
        WrongBagTypeSubclass = 321,
        CantWrapStackable = 322,
        CantWrapEquipped = 323,
        CantWrapWrapped = 324,
        CantWrapBound = 325,
        CantWrapUnique = 326,
        CantWrapBags = 327,
        OutOfMana = 328,
        OutOfRage = 329,
        OutOfFocus = 330,
        OutOfEnergy = 331,
        OutOfChi = 332,
        OutOfHealth = 333,
        OutOfRunes = 334,
        OutOfRunicPower = 335,
        OutOfSoulShards = 336,
        OutOfLunarPower = 337,
        OutOfHolyPower = 338,
        OutOfMaelstrom = 339,
        OutOfComboPoints = 340,
        OutOfInsanity = 341,
        OutOfArcaneCharges = 342,
        OutOfFury = 343,
        OutOfPain = 344,
        OutOfPowerDisplay = 345,
        LootGone = 346,
        MountForceddismount = 347,
        AutofollowTooFar = 348,
        UnitNotFound = 349,
        InvalidFollowTarget = 350,
        InvalidFollowPvpCombat = 351,
        InvalidFollowTargetPvpCombat = 352,
        InvalidInspectTarget = 353,
        GuildemblemSuccess = 354,
        GuildemblemInvalidTabardColors = 355,
        GuildemblemNoguild = 356,
        GuildemblemNotguildmaster = 357,
        GuildemblemNotenoughmoney = 358,
        GuildemblemInvalidvendor = 359,
        EmblemerrorNotabardgeoset = 360,
        SpellOutOfRange = 361,
        CommandNeedsTarget = 362,
        NoammoS = 363,
        Toobusytofollow = 364,
        DuelRequested = 365,
        DuelCancelled = 366,
        Deathbindalreadybound = 367,
        DeathbindSuccessS = 368,
        Noemotewhilerunning = 369,
        ZoneExplored = 370,
        ZoneExploredXp = 371,
        InvalidItemTarget = 372,
        InvalidQuestTarget = 373,
        IgnoringYouS = 374,
        FishNotHooked = 375,
        FishEscaped = 376,
        SpellFailedNotunsheathed = 377,
        PetitionOfferedS = 378,
        PetitionSigned = 379,
        PetitionSignedS = 380,
        PetitionDeclinedS = 381,
        PetitionAlreadySigned = 382,
        PetitionRestrictedAccountTrial = 383,
        PetitionAlreadySignedOther = 384,
        PetitionInGuild = 385,
        PetitionCreator = 386,
        PetitionNotEnoughSignatures = 387,
        PetitionNotSameServer = 388,
        PetitionFull = 389,
        PetitionAlreadySignedByS = 390,
        GuildNameInvalid = 391,
        SpellUnlearnedS = 392,
        PetSpellRooted = 393,
        PetSpellAffectingCombat = 394,
        PetSpellOutOfRange = 395,
        PetSpellNotBehind = 396,
        PetSpellTargetsDead = 397,
        PetSpellDead = 398,
        PetSpellNopath = 399,
        ItemCantBeDestroyed = 400,
        TicketAlreadyExists = 401,
        TicketCreateError = 402,
        TicketUpdateError = 403,
        TicketDbError = 404,
        TicketNoText = 405,
        TicketTextTooLong = 406,
        ObjectIsBusy = 407,
        ExhaustionWellrested = 408,
        ExhaustionRested = 409,
        ExhaustionNormal = 410,
        ExhaustionTired = 411,
        ExhaustionExhausted = 412,
        NoItemsWhileShapeshifted = 413,
        CantInteractShapeshifted = 414,
        RealmNotFound = 415,
        MailQuestItem = 416,
        MailBoundItem = 417,
        MailConjuredItem = 418,
        MailBag = 419,
        MailToSelf = 420,
        MailTargetNotFound = 421,
        MailDatabaseError = 422,
        MailDeleteItemError = 423,
        MailWrappedCod = 424,
        MailCantSendRealm = 425,
        MailSent = 426,
        NotHappyEnough = 427,
        UseCantImmune = 428,
        CantBeDisenchanted = 429,
        CantUseDisarmed = 430,
        AuctionQuestItem = 431,
        AuctionBoundItem = 432,
        AuctionConjuredItem = 433,
        AuctionLimitedDurationItem = 434,
        AuctionWrappedItem = 435,
        AuctionLootItem = 436,
        AuctionBag = 437,
        AuctionEquippedBag = 438,
        AuctionDatabaseError = 439,
        AuctionBidOwn = 440,
        AuctionBidIncrement = 441,
        AuctionHigherBid = 442,
        AuctionMinBid = 443,
        AuctionRepairItem = 444,
        AuctionUsedCharges = 445,
        AuctionAlreadyBid = 446,
        AuctionStarted = 447,
        AuctionRemoved = 448,
        AuctionOutbidS = 449,
        AuctionWonS = 450,
        AuctionSoldS = 451,
        AuctionExpiredS = 452,
        AuctionRemovedS = 453,
        AuctionBidPlaced = 454,
        LogoutFailed = 455,
        QuestPushSuccessS = 456,
        QuestPushInvalidS = 457,
        QuestPushAcceptedS = 458,
        QuestPushDeclinedS = 459,
        QuestPushBusyS = 460,
        QuestPushDeadS = 461,
        QuestPushLogFullS = 462,
        QuestPushOnquestS = 463,
        QuestPushAlreadyDoneS = 464,
        QuestPushNotDailyS = 465,
        QuestPushTimerExpiredS = 466,
        QuestPushNotInPartyS = 467,
        QuestPushDifferentServerDailyS = 468,
        QuestPushNotAllowedS = 469,
        RaidGroupLowlevel = 470,
        RaidGroupOnly = 471,
        RaidGroupFull = 472,
        RaidGroupRequirementsUnmatch = 473,
        CorpseIsNotInInstance = 474,
        PvpKillHonorable = 475,
        PvpKillDishonorable = 476,
        SpellFailedAlreadyAtFullHealth = 477,
        SpellFailedAlreadyAtFullMana = 478,
        SpellFailedAlreadyAtFullPowerS = 479,
        AutolootMoneyS = 480,
        GenericStunned = 481,
        TargetStunned = 482,
        MustRepairDurability = 483,
        RaidYouJoined = 484,
        RaidYouLeft = 485,
        InstanceGroupJoinedWithParty = 486,
        InstanceGroupJoinedWithRaid = 487,
        RaidMemberAddedS = 488,
        RaidMemberRemovedS = 489,
        InstanceGroupAddedS = 490,
        InstanceGroupRemovedS = 491,
        ClickOnItemToFeed = 492,
        TooManyChatChannels = 493,
        LootRollPending = 494,
        LootPlayerNotFound = 495,
        NotInRaid = 496,
        LoggingOut = 497,
        TargetLoggingOut = 498,
        NotWhileMounted = 499,
        NotWhileShapeshifted = 500,
        NotInCombat = 501,
        NotWhileDisarmed = 502,
        PetBroken = 503,
        TalentWipeError = 504,
        SpecWipeError = 505,
        GlyphWipeError = 506,
        PetSpecWipeError = 507,
        FeignDeathResisted = 508,
        MeetingStoneInQueueS = 509,
        MeetingStoneLeftQueueS = 510,
        MeetingStoneOtherMemberLeft = 511,
        MeetingStonePartyKickedFromQueue = 512,
        MeetingStoneMemberStillInQueue = 513,
        MeetingStoneSuccess = 514,
        MeetingStoneInProgress = 515,
        MeetingStoneMemberAddedS = 516,
        MeetingStoneGroupFull = 517,
        MeetingStoneNotLeader = 518,
        MeetingStoneInvalidLevel = 519,
        MeetingStoneTargetNotInParty = 520,
        MeetingStoneTargetInvalidLevel = 521,
        MeetingStoneMustBeLeader = 522,
        MeetingStoneNoRaidGroup = 523,
        MeetingStoneNeedParty = 524,
        MeetingStoneNotFound = 525,
        GuildemblemSame = 526,
        EquipTradeItem = 527,
        PvpToggleOn = 528,
        PvpToggleOff = 529,
        GroupJoinBattlegroundDeserters = 530,
        GroupJoinBattlegroundDead = 531,
        GroupJoinBattlegroundS = 532,
        GroupJoinBattlegroundFail = 533,
        GroupJoinBattlegroundTooMany = 534,
        SoloJoinBattlegroundS = 535,
        JoinSingleScenarioS = 536,
        BattlegroundTooManyQueues = 537,
        BattlegroundCannotQueueForRated = 538,
        BattledgroundQueuedForRated = 539,
        BattlegroundTeamLeftQueue = 540,
        BattlegroundNotInBattleground = 541,
        AlreadyInArenaTeamS = 542,
        InvalidPromotionCode = 543,
        BgPlayerJoinedSs = 544,
        BgPlayerLeftS = 545,
        RestrictedAccount = 546,
        RestrictedAccountTrial = 547,
        PlayTimeExceeded = 548,
        ApproachingPartialPlayTime = 549,
        ApproachingPartialPlayTime2 = 550,
        ApproachingNoPlayTime = 551,
        ApproachingNoPlayTime2 = 552,
        UnhealthyTime = 553,
        ChatRestrictedTrial = 554,
        ChatThrottled = 555,
        MailReachedCap = 556,
        InvalidRaidTarget = 557,
        RaidLeaderReadyCheckStartS = 558,
        ReadyCheckInProgress = 559,
        ReadyCheckThrottled = 560,
        DungeonDifficultyFailed = 561,
        DungeonDifficultyChangedS = 562,
        TradeWrongRealm = 563,
        TradeNotOnTaplist = 564,
        ChatPlayerAmbiguousS = 565,
        LootCantLootThatNow = 566,
        LootMasterInvFull = 567,
        LootMasterUniqueItem = 568,
        LootMasterOther = 569,
        FilteringYouS = 570,
        UsePreventedByMechanicS = 571,
        ItemUniqueEquippable = 572,
        LfgLeaderIsLfmS = 573,
        LfgPending = 574,
        CantSpeakLangage = 575,
        VendorMissingTurnins = 576,
        BattlegroundNotInTeam = 577,
        NotInBattleground = 578,
        NotEnoughHonorPoints = 579,
        NotEnoughArenaPoints = 580,
        SocketingRequiresMetaGem = 581,
        SocketingMetaGemOnlyInMetaslot = 582,
        SocketingRequiresHydraulicGem = 583,
        SocketingHydraulicGemOnlyInHydraulicslot = 584,
        SocketingRequiresCogwheelGem = 585,
        SocketingCogwheelGemOnlyInCogwheelslot = 586,
        SocketingItemTooLowLevel = 587,
        ItemMaxCountSocketed = 588,
        SystemDisabled = 589,
        QuestFailedTooManyDailyQuestsI = 590,
        ItemMaxCountEquippedSocketed = 591,
        ItemUniqueEquippableSocketed = 592,
        UserSquelched = 593,
        TooMuchGold = 594,
        NotBarberSitting = 595,
        QuestFailedCais = 596,
        InviteRestrictedTrial = 597,
        VoiceIgnoreFull = 598,
        VoiceIgnoreSelf = 599,
        VoiceIgnoreNotFound = 600,
        VoiceIgnoreAlreadyS = 601,
        VoiceIgnoreAddedS = 602,
        VoiceIgnoreRemovedS = 603,
        VoiceIgnoreAmbiguous = 604,
        VoiceIgnoreDeleted = 605,
        UnknownMacroOptionS = 606,
        NotDuringArenaMatch = 607,
        PlayerSilenced = 608,
        PlayerUnsilenced = 609,
        ComsatDisconnect = 610,
        ComsatReconnectAttempt = 611,
        ComsatConnectFail = 612,
        MailInvalidAttachmentSlot = 613,
        MailTooManyAttachments = 614,
        MailInvalidAttachment = 615,
        MailAttachmentExpired = 616,
        VoiceChatParentalDisableMic = 617,
        ProfaneChatName = 618,
        PlayerSilencedEcho = 619,
        PlayerUnsilencedEcho = 620,
        LootCantLootThat = 621,
        ArenaExpiredCais = 622,
        GroupActionThrottled = 623,
        AlreadyPickpocketed = 624,
        NameInvalid = 625,
        NameNoName = 626,
        NameTooShort = 627,
        NameTooLong = 628,
        NameMixedLanguages = 629,
        NameProfane = 630,
        NameReserved = 631,
        NameThreeConsecutive = 632,
        NameInvalidSpace = 633,
        NameConsecutiveSpaces = 634,
        NameRussianConsecutiveSilentCharacters = 635,
        NameRussianSilentCharacterAtBeginningOrEnd = 636,
        NameDeclensionDoesntMatchBaseName = 637,
        ReferAFriendNotReferredBy = 638,
        ReferAFriendTargetTooHigh = 639,
        ReferAFriendInsufficientGrantableLevels = 640,
        ReferAFriendTooFar = 641,
        ReferAFriendDifferentFaction = 642,
        ReferAFriendNotNow = 643,
        ReferAFriendGrantLevelMaxI = 644,
        ReferAFriendSummonLevelMaxI = 645,
        ReferAFriendSummonCooldown = 646,
        ReferAFriendSummonOfflineS = 647,
        ReferAFriendInsufExpanLvl = 648,
        ReferAFriendNotInLfg = 649,
        ReferAFriendNoXrealm = 650,
        ReferAFriendMapIncomingTransferNotAllowed = 651,
        NotSameAccount = 652,
        BadOnUseEnchant = 653,
        TradeSelf = 654,
        TooManySockets = 655,
        ItemMaxLimitCategoryCountExceededIs = 656,
        TradeTargetMaxLimitCategoryCountExceededIs = 657,
        ItemMaxLimitCategorySocketedExceededIs = 658,
        ItemMaxLimitCategoryEquippedExceededIs = 659,
        ShapeshiftFormCannotEquip = 660,
        ItemInventoryFullSatchel = 661,
        ScalingStatItemLevelExceeded = 662,
        ScalingStatItemLevelTooLow = 663,
        PurchaseLevelTooLow = 664,
        GroupSwapFailed = 665,
        InviteInCombat = 666,
        InvalidGlyphSlot = 667,
        GenericNoValidTargets = 668,
        CalendarEventAlertS = 669,
        PetLearnSpellS = 670,
        PetLearnAbilityS = 671,
        PetSpellUnlearnedS = 672,
        InviteUnknownRealm = 673,
        InviteNoPartyServer = 674,
        InvitePartyBusy = 675,
        PartyTargetAmbiguous = 676,
        PartyLfgInviteRaidLocked = 677,
        PartyLfgBootLimit = 678,
        PartyLfgBootCooldownS = 679,
        PartyLfgBootNotEligibleS = 680,
        PartyLfgBootInpatientTimerS = 681,
        PartyLfgBootInProgress = 682,
        PartyLfgBootTooFewPlayers = 683,
        PartyLfgBootVoteSucceeded = 684,
        PartyLfgBootVoteFailed = 685,
        PartyLfgBootInCombat = 686,
        PartyLfgBootDungeonComplete = 687,
        PartyLfgBootLootRolls = 688,
        PartyLfgBootVoteRegistered = 689,
        PartyPrivateGroupOnly = 690,
        PartyLfgTeleportInCombat = 691,
        RaidDisallowedByLevel = 692,
        RaidDisallowedByCrossRealm = 693,
        PartyRoleNotAvailable = 694,
        JoinLfgObjectFailed = 695,
        LfgRemovedLevelup = 696,
        LfgRemovedXpToggle = 697,
        LfgRemovedFactionChange = 698,
        BattlegroundInfoThrottled = 699,
        BattlegroundAlreadyIn = 700,
        ArenaTeamChangeFailedQueued = 701,
        ArenaTeamPermissions = 702,
        NotWhileFalling = 703,
        NotWhileMoving = 704,
        NotWhileFatigued = 705,
        MaxSockets = 706,
        MultiCastActionTotemS = 707,
        BattlegroundJoinLevelup = 708,
        RemoveFromPvpQueueXpGain = 709,
        BattlegroundJoinXpGain = 710,
        BattlegroundJoinMercenary = 711,
        BattlegroundJoinTooManyHealers = 712,
        BattlegroundJoinTooManyTanks = 713,
        BattlegroundJoinTooManyDamage = 714,
        RaidDifficultyFailed = 715,
        RaidDifficultyChangedS = 716,
        LegacyRaidDifficultyChangedS = 717,
        RaidLockoutChangedS = 718,
        RaidConvertedToParty = 719,
        PartyConvertedToRaid = 720,
        PlayerDifficultyChangedS = 721,
        GmresponseDbError = 722,
        BattlegroundJoinRangeIndex = 723,
        ArenaJoinRangeIndex = 724,
        RemoveFromPvpQueueFactionChange = 725,
        BattlegroundJoinFailed = 726,
        BattlegroundJoinNoValidSpecForRole = 727,
        BattlegroundJoinRespec = 728,
        BattlegroundInvitationDeclined = 729,
        BattlegroundJoinTimedOut = 730,
        BattlegroundDupeQueue = 731,
        BattlegroundJoinMustCompleteQuest = 732,
        InBattlegroundRespec = 733,
        MailLimitedDurationItem = 734,
        YellRestrictedTrial = 735,
        ChatRaidRestrictedTrial = 736,
        LfgRoleCheckFailed = 737,
        LfgRoleCheckFailedTimeout = 738,
        LfgRoleCheckFailedNotViable = 739,
        LfgReadyCheckFailed = 740,
        LfgReadyCheckFailedTimeout = 741,
        LfgGroupFull = 742,
        LfgNoLfgObject = 743,
        LfgNoSlotsPlayer = 744,
        LfgNoSlotsParty = 745,
        LfgNoSpec = 746,
        LfgMismatchedSlots = 747,
        LfgMismatchedSlotsLocalXrealm = 748,
        LfgPartyPlayersFromDifferentRealms = 749,
        LfgMembersNotPresent = 750,
        LfgGetInfoTimeout = 751,
        LfgInvalidSlot = 752,
        LfgDeserterPlayer = 753,
        LfgDeserterParty = 754,
        LfgDead = 755,
        LfgRandomCooldownPlayer = 756,
        LfgRandomCooldownParty = 757,
        LfgTooManyMembers = 758,
        LfgTooFewMembers = 759,
        LfgProposalFailed = 760,
        LfgProposalDeclinedSelf = 761,
        LfgProposalDeclinedParty = 762,
        LfgNoSlotsSelected = 763,
        LfgNoRolesSelected = 764,
        LfgRoleCheckInitiated = 765,
        LfgReadyCheckInitiated = 766,
        LfgPlayerDeclinedRoleCheck = 767,
        LfgPlayerDeclinedReadyCheck = 768,
        LfgJoinedQueue = 769,
        LfgJoinedFlexQueue = 770,
        LfgJoinedRfQueue = 771,
        LfgJoinedScenarioQueue = 772,
        LfgJoinedWorldPvpQueue = 773,
        LfgJoinedBattlefieldQueue = 774,
        LfgJoinedList = 775,
        LfgLeftQueue = 776,
        LfgLeftList = 777,
        LfgRoleCheckAborted = 778,
        LfgReadyCheckAborted = 779,
        LfgCantUseBattleground = 780,
        LfgCantUseDungeons = 781,
        LfgReasonTooManyLfg = 782,
        InvalidTeleportLocation = 783,
        TooFarToInteract = 784,
        BattlegroundPlayersFromDifferentRealms = 785,
        DifficultyChangeCooldownS = 786,
        DifficultyChangeCombatCooldownS = 787,
        DifficultyChangeWorldstate = 788,
        DifficultyChangeEncounter = 789,
        DifficultyChangeCombat = 790,
        DifficultyChangePlayerBusy = 791,
        DifficultyChangeAlreadyStarted = 792,
        DifficultyChangeOtherHeroicS = 793,
        DifficultyChangeHeroicInstanceAlreadyRunning = 794,
        ArenaTeamPartySize = 795,
        QuestForceRemovedS = 796,
        AttackNoActions = 797,
        InRandomBg = 798,
        InNonRandomBg = 799,
        AuctionEnoughItems = 800,
        BnFriendSelf = 801,
        BnFriendAlready = 802,
        BnFriendBlocked = 803,
        BnFriendListFull = 804,
        BnFriendRequestSent = 805,
        BnBroadcastThrottle = 806,
        BgDeveloperOnly = 807,
        CurrencySpellSlotMismatch = 808,
        CurrencyNotTradable = 809,
        RequiresExpansionS = 810,
        QuestFailedSpell = 811,
        TalentFailedNotEnoughTalentsInPrimaryTree = 812,
        TalentFailedNoPrimaryTreeSelected = 813,
        TalentFailedCantRemoveTalent = 814,
        TalentFailedUnknown = 815,
        WargameRequestFailure = 816,
        RankRequiresAuthenticator = 817,
        GuildBankVoucherFailed = 818,
        WargameRequestSent = 819,
        RequiresAchievementI = 820,
        RefundResultExceedMaxCurrency = 821,
        CantBuyQuantity = 822,
        ItemIsBattlePayLocked = 823,
        PartyAlreadyInBattlegroundQueue = 824,
        PartyConfirmingBattlegroundQueue = 825,
        BattlefieldTeamPartySize = 826,
        InsuffTrackedCurrencyIs = 827,
        NotOnTournamentRealm = 828,
        GuildTrialAccountTrial = 829,
        GuildTrialAccountVeteran = 830,
        GuildUndeletableDueToLevel = 831,
        CantDoThatInAGroup = 832,
        GuildLeaderReplaced = 833,
        TransmogrifyCantEquip = 834,
        TransmogrifyInvalidItemType = 835,
        TransmogrifyNotSoulbound = 836,
        TransmogrifyInvalidSource = 837,
        TransmogrifyInvalidDestination = 838,
        TransmogrifyMismatch = 839,
        TransmogrifyLegendary = 840,
        TransmogrifySameItem = 841,
        TransmogrifySameAppearance = 842,
        TransmogrifyNotEquipped = 843,
        VoidDepositFull = 844,
        VoidWithdrawFull = 845,
        VoidStorageWrapped = 846,
        VoidStorageStackable = 847,
        VoidStorageUnbound = 848,
        VoidStorageRepair = 849,
        VoidStorageCharges = 850,
        VoidStorageQuest = 851,
        VoidStorageConjured = 852,
        VoidStorageMail = 853,
        VoidStorageBag = 854,
        VoidTransferStorageFull = 855,
        VoidTransferInvFull = 856,
        VoidTransferInternalError = 857,
        VoidTransferItemInvalid = 858,
        DifficultyDisabledInLfg = 859,
        VoidStorageUnique = 860,
        VoidStorageLoot = 861,
        VoidStorageHoliday = 862,
        VoidStorageDuration = 863,
        VoidStorageLoadFailed = 864,
        VoidStorageInvalidItem = 865,
        ParentalControlsChatMuted = 866,
        SorStartExperienceIncomplete = 867,
        SorInvalidEmail = 868,
        SorInvalidComment = 869,
        ChallengeModeResetCooldownS = 870,
        ChallengeModeResetKeystone = 871,
        PetJournalAlreadyInLoadout = 872,
        ReportSubmittedSuccessfully = 873,
        ReportSubmissionFailed = 874,
        SuggestionSubmittedSuccessfully = 875,
        BugSubmittedSuccessfully = 876,
        ChallengeModeEnabled = 877,
        ChallengeModeDisabled = 878,
        PetbattleCreateFailed = 879,
        PetbattleNotHere = 880,
        PetbattleNotHereOnTransport = 881,
        PetbattleNotHereUnevenGround = 882,
        PetbattleNotHereObstructed = 883,
        PetbattleNotWhileInCombat = 884,
        PetbattleNotWhileDead = 885,
        PetbattleNotWhileFlying = 886,
        PetbattleTargetInvalid = 887,
        PetbattleTargetOutOfRange = 888,
        PetbattleTargetNotCapturable = 889,
        PetbattleNotATrainer = 890,
        PetbattleDeclined = 891,
        PetbattleInBattle = 892,
        PetbattleInvalidLoadout = 893,
        PetbattleAllPetsDead = 894,
        PetbattleNoPetsInSlots = 895,
        PetbattleNoAccountLock = 896,
        PetbattleWildPetTapped = 897,
        PetbattleRestrictedAccount = 898,
        PetbattleOpponentNotAvailable = 899,
        PetbattleNotWhileInMatchedBattle = 900,
        CantHaveMorePetsOfThatType = 901,
        CantHaveMorePets = 902,
        PvpMapNotFound = 903,
        PvpMapNotSet = 904,
        PetbattleQueueQueued = 905,
        PetbattleQueueAlreadyQueued = 906,
        PetbattleQueueJoinFailed = 907,
        PetbattleQueueJournalLock = 908,
        PetbattleQueueRemoved = 909,
        PetbattleQueueProposalDeclined = 910,
        PetbattleQueueProposalTimeout = 911,
        PetbattleQueueOpponentDeclined = 912,
        PetbattleQueueRequeuedInternal = 913,
        PetbattleQueueRequeuedRemoved = 914,
        PetbattleQueueSlotLocked = 915,
        PetbattleQueueSlotEmpty = 916,
        PetbattleQueueSlotNoTracker = 917,
        PetbattleQueueSlotNoSpecies = 918,
        PetbattleQueueSlotCantBattle = 919,
        PetbattleQueueSlotRevoked = 920,
        PetbattleQueueSlotDead = 921,
        PetbattleQueueSlotNoPet = 922,
        PetbattleQueueNotWhileNeutral = 923,
        PetbattleGameTimeLimitWarning = 924,
        PetbattleGameRoundsLimitWarning = 925,
        HasRestriction = 926,
        ItemUpgradeItemTooLowLevel = 927,
        ItemUpgradeNoPath = 928,
        ItemUpgradeNoMoreUpgrades = 929,
        BonusRollEmpty = 930,
        ChallengeModeFull = 931,
        ChallengeModeInProgress = 932,
        ChallengeModeIncorrectKeystone = 933,
        BattletagFriendNotFound = 934,
        BattletagFriendNotValid = 935,
        BattletagFriendNotAllowed = 936,
        BattletagFriendThrottled = 937,
        BattletagFriendSuccess = 938,
        PetTooHighLevelToUncage = 939,
        PetbattleInternal = 940,
        CantCagePetYet = 941,
        NoLootInChallengeMode = 942,
        QuestPetBattleVictoriesPvpIi = 943,
        RoleCheckAlreadyInProgress = 944,
        RecruitAFriendAccountLimit = 945,
        RecruitAFriendFailed = 946,
        SetLootPersonal = 947,
        SetLootMethodFailedCombat = 948,
        ReagentBankFull = 949,
        ReagentBankLocked = 950,
        GarrisonBuildingExists = 951,
        GarrisonInvalidPlot = 952,
        GarrisonInvalidBuildingid = 953,
        GarrisonInvalidPlotBuilding = 954,
        GarrisonRequiresBlueprint = 955,
        GarrisonNotEnoughCurrency = 956,
        GarrisonNotEnoughGold = 957,
        GarrisonCompleteMissionWrongFollowerType = 958,
        AlreadyUsingLfgList = 959,
        RestrictedAccountLfgListTrial = 960,
        ToyUseLimitReached = 961,
        ToyAlreadyKnown = 962,
        TransmogSetAlreadyKnown = 963,
        NotEnoughCurrency = 964,
        SpecIsDisabled = 965,
        FeatureRestrictedTrial = 966,
        CantBeObliterated = 967,
        CantBeScrapped = 968,
        ArtifactRelicDoesNotMatchArtifact = 969,
        MustEquipArtifact = 970,
        CantDoThatRightNow = 971,
        AffectingCombat = 972,
        EquipmentManagerCombatSwapS = 973,
        EquipmentManagerBagsFull = 974,
        EquipmentManagerMissingItemS = 975,
        MovieRecordingWarningPerf = 976,
        MovieRecordingWarningDiskFull = 977,
        MovieRecordingWarningNoMovie = 978,
        MovieRecordingWarningRequirements = 979,
        MovieRecordingWarningCompressing = 980,
        NoChallengeModeReward = 981,
        ClaimedChallengeModeReward = 982,
        ChallengeModePeriodResetSs = 983,
        CantDoThatChallengeModeActive = 984,
        TalentFailedRestArea = 985,
        CannotAbandonLastPet = 986,
        TestCvarSetSss = 987,
        QuestTurnInFailReason = 988,
        ClaimedChallengeModeRewardOld = 989,
        TalentGrantedByAura = 990,
        ChallengeModeAlreadyComplete = 991,
        GlyphTargetNotAvailable = 992,
        PvpWarmodeToggleOn = 993,
        PvpWarmodeToggleOff = 994,
        SpellFailedLevelRequirement = 995,
        BattlegroundJoinRequiresLevel = 996,
        BattlegroundJoinDisqualified = 997,
        VoiceChatGenericUnableToConnect = 998,
        VoiceChatServiceLost = 999,
        VoiceChatChannelNameTooShort = 1000,
        VoiceChatChannelNameTooLong = 1001,
        VoiceChatChannelAlreadyExists = 1002,
        VoiceChatTargetNotFound = 1003,
        VoiceChatTooManyRequests = 1004,
        VoiceChatPlayerSilenced = 1005,
        VoiceChatParentalDisableAll = 1006,
        VoiceChatDisabled = 1007,
        NoPvpReward = 1008,
        ClaimedPvpReward = 1009
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
}
