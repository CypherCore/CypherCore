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
        public const int MaxMailItems = 12;
        public const int MaxDeclinedNameCases = 5;
        public const int MaxHolidayDurations = 10;
        public const int MaxHolidayDates = 26;
        public const int MaxHolidayFlags = 10;
        public const int DefaultMaxLevel = 120;
        public const int MaxLevel = 120;
        public const int StrongMaxLevel = 255;
        public const int MaxOverrideSpell = 10;
        public const int MaxWorldMapOverlayArea = 4;
        public const int MaxMountCapabilities = 24;
        public const int MaxLockCase = 8;
        public const int MaxAzeriteEmpoweredTier = 5;
        public const int MaxAzeriteEssenceSlot = 3;
        public const int MaxAzeriteEssenceRank = 4;

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
        HeroicWarfront = 149,
        LFR15thAnniversary = 151,

        Max
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
        Equipped2Handed = 35,
        VendorNotInterested = 36,
        VendorRefuseScrappableAzerite = 37,
        VendorHatesYou = 38,
        VendorSoldOut = 39,
        VendorTooFar = 40,
        VendorDoesntBuy = 41,
        NotEnoughMoney = 42,
        ReceiveItemS = 43,
        DropBoundItem = 44,
        TradeBoundItem = 45,
        TradeQuestItem = 46,
        TradeTempEnchantBound = 47,
        TradeGroundItem = 48,
        TradeBag = 49,
        SpellFailedS = 50,
        ItemCooldown = 51,
        PotionCooldown = 52,
        FoodCooldown = 53,
        SpellCooldown = 54,
        AbilityCooldown = 55,
        SpellAlreadyKnownS = 56,
        PetSpellAlreadyKnownS = 57,
        ProficiencyGainedS = 58,
        SkillGainedS = 59,
        SkillUpSi = 60,
        LearnSpellS = 61,
        LearnAbilityS = 62,
        LearnPassiveS = 63,
        LearnRecipeS = 64,
        LearnCompanionS = 65,
        LearnMountS = 66,
        LearnToyS = 67,
        LearnHeirloomS = 68,
        LearnTransmogS = 69,
        CompletedTransmogSetS = 70,
        RevokeTransmogS = 71,
        InvitePlayerS = 72,
        InviteSelf = 73,
        InvitedToGroupSs = 74,
        InvitedAlreadyInGroupSs = 75,
        AlreadyInGroupS = 76,
        CrossRealmRaidInvite = 77,
        PlayerBusyS = 78,
        NewLeaderS = 79,
        NewLeaderYou = 80,
        NewGuideS = 81,
        NewGuideYou = 82,
        LeftGroupS = 83,
        LeftGroupYou = 84,
        GroupDisbanded = 85,
        DeclineGroupS = 86,
        JoinedGroupS = 87,
        UninviteYou = 88,
        BadPlayerNameS = 89,
        NotInGroup = 90,
        TargetNotInGroupS = 91,
        TargetNotInInstanceS = 92,
        NotInInstanceGroup = 93,
        GroupFull = 94,
        NotLeader = 95,
        PlayerDiedS = 96,
        GuildCreateS = 97,
        GuildInviteS = 98,
        InvitedToGuildSss = 99,
        AlreadyInGuildS = 100,
        AlreadyInvitedToGuildS = 101,
        InvitedToGuild = 102,
        AlreadyInGuild = 103,
        GuildAccept = 104,
        GuildDeclineS = 105,
        GuildDeclineAutoS = 106,
        GuildPermissions = 107,
        GuildJoinS = 108,
        GuildFounderS = 109,
        GuildPromoteSss = 110,
        GuildDemoteSs = 111,
        GuildDemoteSss = 112,
        GuildInviteSelf = 113,
        GuildQuitS = 114,
        GuildLeaveS = 115,
        GuildRemoveSs = 116,
        GuildRemoveSelf = 117,
        GuildDisbandS = 118,
        GuildDisbandSelf = 119,
        GuildLeaderS = 120,
        GuildLeaderSelf = 121,
        GuildPlayerNotFoundS = 122,
        GuildPlayerNotInGuildS = 123,
        GuildPlayerNotInGuild = 124,
        GuildCantPromoteS = 125,
        GuildCantDemoteS = 126,
        GuildNotInAGuild = 127,
        GuildInternal = 128,
        GuildLeaderIsS = 129,
        GuildLeaderChangedSs = 130,
        GuildDisbanded = 131,
        GuildNotAllied = 132,
        GuildLeaderLeave = 133,
        GuildRanksLocked = 134,
        GuildRankInUse = 135,
        GuildRankTooHighS = 136,
        GuildRankTooLowS = 137,
        GuildNameExistsS = 138,
        GuildWithdrawLimit = 139,
        GuildNotEnoughMoney = 140,
        GuildTooMuchMoney = 141,
        GuildBankConjuredItem = 142,
        GuildBankEquippedItem = 143,
        GuildBankBoundItem = 144,
        GuildBankQuestItem = 145,
        GuildBankWrappedItem = 146,
        GuildBankFull = 147,
        GuildBankWrongTab = 148,
        NoGuildCharter = 149,
        OutOfRange = 150,
        PlayerDead = 151,
        ClientLockedOut = 152,
        ClientOnTransport = 153,
        KilledByS = 154,
        LootLocked = 155,
        LootTooFar = 156,
        LootDidntKill = 157,
        LootBadFacing = 158,
        LootNotstanding = 159,
        LootStunned = 160,
        LootNoUi = 161,
        LootWhileInvulnerable = 162,
        NoLoot = 163,
        QuestAcceptedS = 164,
        QuestCompleteS = 165,
        QuestFailedS = 166,
        QuestFailedBagFullS = 167,
        QuestFailedMaxCountS = 168,
        QuestFailedLowLevel = 169,
        QuestFailedMissingItems = 170,
        QuestFailedWrongRace = 171,
        QuestFailedNotEnoughMoney = 172,
        QuestFailedExpansion = 173,
        QuestOnlyOneTimed = 174,
        QuestNeedPrereqs = 175,
        QuestNeedPrereqsCustom = 176,
        QuestAlreadyOn = 177,
        QuestAlreadyDone = 178,
        QuestAlreadyDoneDaily = 179,
        QuestHasInProgress = 180,
        QuestRewardExpI = 181,
        QuestRewardMoneyS = 182,
        QuestMustChoose = 183,
        QuestLogFull = 184,
        CombatDamageSsi = 185,
        InspectS = 186,
        CantUseItem = 187,
        CantUseItemInArena = 188,
        CantUseItemInRatedBattleground = 189,
        MustEquipItem = 190,
        PassiveAbility = 191,
        Skill2hNotFound = 192,
        NoAttackTarget = 193,
        InvalidAttackTarget = 194,
        AttackPvpTargetWhileUnflagged = 195,
        AttackStunned = 196,
        AttackPacified = 197,
        AttackMounted = 198,
        AttackFleeing = 199,
        AttackConfused = 200,
        AttackCharmed = 201,
        AttackDead = 202,
        AttackPreventedByMechanicS = 203,
        AttackChannel = 204,
        Taxisamenode = 205,
        Taxinosuchpath = 206,
        Taxiunspecifiedservererror = 207,
        Taxinotenoughmoney = 208,
        Taxitoofaraway = 209,
        Taxinovendornearby = 210,
        Taxinotvisited = 211,
        Taxiplayerbusy = 212,
        Taxiplayeralreadymounted = 213,
        Taxiplayershapeshifted = 214,
        Taxiplayermoving = 215,
        Taxinopaths = 216,
        Taxinoteligible = 217,
        Taxinotstanding = 218,
        NoReplyTarget = 219,
        GenericNoTarget = 220,
        InitiateTradeS = 221,
        TradeRequestS = 222,
        TradeBlockedS = 223,
        TradeTargetDead = 224,
        TradeTooFar = 225,
        TradeCancelled = 226,
        TradeComplete = 227,
        TradeBagFull = 228,
        TradeTargetBagFull = 229,
        TradeMaxCountExceeded = 230,
        TradeTargetMaxCountExceeded = 231,
        AlreadyTrading = 232,
        MountInvalidmountee = 233,
        MountToofaraway = 234,
        MountAlreadymounted = 235,
        MountNotmountable = 236,
        MountNotyourpet = 237,
        MountOther = 238,
        MountLooting = 239,
        MountRacecantmount = 240,
        MountShapeshifted = 241,
        MountNoFavorites = 242,
        DismountNopet = 243,
        DismountNotmounted = 244,
        DismountNotyourpet = 245,
        SpellFailedTotems = 246,
        SpellFailedReagents = 247,
        SpellFailedReagentsGeneric = 248,
        CantTradeGold = 249,
        SpellFailedEquippedItem = 250,
        SpellFailedEquippedItemClassS = 251,
        SpellFailedShapeshiftFormS = 252,
        SpellFailedAnotherInProgress = 253,
        Badattackfacing = 254,
        Badattackpos = 255,
        ChestInUse = 256,
        UseCantOpen = 257,
        UseLocked = 258,
        DoorLocked = 259,
        ButtonLocked = 260,
        UseLockedWithItemS = 261,
        UseLockedWithSpellS = 262,
        UseLockedWithSpellKnownSi = 263,
        UseTooFar = 264,
        UseBadAngle = 265,
        UseObjectMoving = 266,
        UseSpellFocus = 267,
        UseDestroyed = 268,
        SetLootFreeforall = 269,
        SetLootRoundrobin = 270,
        SetLootMaster = 271,
        SetLootGroup = 272,
        SetLootThresholdS = 273,
        NewLootMasterS = 274,
        SpecifyMasterLooter = 275,
        LootSpecChangedS = 276,
        TameFailed = 277,
        ChatWhileDead = 278,
        ChatPlayerNotFoundS = 279,
        Newtaxipath = 280,
        NoPet = 281,
        Notyourpet = 282,
        PetNotRenameable = 283,
        QuestObjectiveCompleteS = 284,
        QuestUnknownComplete = 285,
        QuestAddKillSii = 286,
        QuestAddFoundSii = 287,
        QuestAddItemSii = 288,
        QuestAddPlayerKillSii = 289,
        Cannotcreatedirectory = 290,
        Cannotcreatefile = 291,
        PlayerWrongFaction = 292,
        PlayerIsNeutral = 293,
        BankslotFailedTooMany = 294,
        BankslotInsufficientFunds = 295,
        BankslotNotbanker = 296,
        FriendDbError = 297,
        FriendListFull = 298,
        FriendAddedS = 299,
        BattletagFriendAddedS = 300,
        FriendOnlineSs = 301,
        FriendOfflineS = 302,
        FriendNotFound = 303,
        FriendWrongFaction = 304,
        FriendRemovedS = 305,
        BattletagFriendRemovedS = 306,
        FriendError = 307,
        FriendAlreadyS = 308,
        FriendSelf = 309,
        FriendDeleted = 310,
        IgnoreFull = 311,
        IgnoreSelf = 312,
        IgnoreNotFound = 313,
        IgnoreAlreadyS = 314,
        IgnoreAddedS = 315,
        IgnoreRemovedS = 316,
        IgnoreAmbiguous = 317,
        IgnoreDeleted = 318,
        OnlyOneBolt = 319,
        OnlyOneAmmo = 320,
        SpellFailedEquippedSpecificItem = 321,
        WrongBagTypeSubclass = 322,
        CantWrapStackable = 323,
        CantWrapEquipped = 324,
        CantWrapWrapped = 325,
        CantWrapBound = 326,
        CantWrapUnique = 327,
        CantWrapBags = 328,
        OutOfMana = 329,
        OutOfRage = 330,
        OutOfFocus = 331,
        OutOfEnergy = 332,
        OutOfChi = 333,
        OutOfHealth = 334,
        OutOfRunes = 335,
        OutOfRunicPower = 336,
        OutOfSoulShards = 337,
        OutOfLunarPower = 338,
        OutOfHolyPower = 339,
        OutOfMaelstrom = 340,
        OutOfComboPoints = 341,
        OutOfInsanity = 342,
        OutOfArcaneCharges = 343,
        OutOfFury = 344,
        OutOfPain = 345,
        OutOfPowerDisplay = 346,
        LootGone = 347,
        MountForceddismount = 348,
        AutofollowTooFar = 349,
        UnitNotFound = 350,
        InvalidFollowTarget = 351,
        InvalidFollowPvpCombat = 352,
        InvalidFollowTargetPvpCombat = 353,
        InvalidInspectTarget = 354,
        GuildemblemSuccess = 355,
        GuildemblemInvalidTabardColors = 356,
        GuildemblemNoguild = 357,
        GuildemblemNotguildmaster = 358,
        GuildemblemNotenoughmoney = 359,
        GuildemblemInvalidvendor = 360,
        EmblemerrorNotabardgeoset = 361,
        SpellOutOfRange = 362,
        CommandNeedsTarget = 363,
        NoammoS = 364,
        Toobusytofollow = 365,
        DuelRequested = 366,
        DuelCancelled = 367,
        Deathbindalreadybound = 368,
        DeathbindSuccessS = 369,
        Noemotewhilerunning = 370,
        ZoneExplored = 371,
        ZoneExploredXp = 372,
        InvalidItemTarget = 373,
        InvalidQuestTarget = 374,
        IgnoringYouS = 375,
        FishNotHooked = 376,
        FishEscaped = 377,
        SpellFailedNotunsheathed = 378,
        PetitionOfferedS = 379,
        PetitionSigned = 380,
        PetitionSignedS = 381,
        PetitionDeclinedS = 382,
        PetitionAlreadySigned = 383,
        PetitionRestrictedAccountTrial = 384,
        PetitionAlreadySignedOther = 385,
        PetitionInGuild = 386,
        PetitionCreator = 387,
        PetitionNotEnoughSignatures = 388,
        PetitionNotSameServer = 389,
        PetitionFull = 390,
        PetitionAlreadySignedByS = 391,
        GuildNameInvalid = 392,
        SpellUnlearnedS = 393,
        PetSpellRooted = 394,
        PetSpellAffectingCombat = 395,
        PetSpellOutOfRange = 396,
        PetSpellNotBehind = 397,
        PetSpellTargetsDead = 398,
        PetSpellDead = 399,
        PetSpellNopath = 400,
        ItemCantBeDestroyed = 401,
        TicketAlreadyExists = 402,
        TicketCreateError = 403,
        TicketUpdateError = 404,
        TicketDbError = 405,
        TicketNoText = 406,
        TicketTextTooLong = 407,
        ObjectIsBusy = 408,
        ExhaustionWellrested = 409,
        ExhaustionRested = 410,
        ExhaustionNormal = 411,
        ExhaustionTired = 412,
        ExhaustionExhausted = 413,
        NoItemsWhileShapeshifted = 414,
        CantInteractShapeshifted = 415,
        RealmNotFound = 416,
        MailQuestItem = 417,
        MailBoundItem = 418,
        MailConjuredItem = 419,
        MailBag = 420,
        MailToSelf = 421,
        MailTargetNotFound = 422,
        MailDatabaseError = 423,
        MailDeleteItemError = 424,
        MailWrappedCod = 425,
        MailCantSendRealm = 426,
        MailTempReturnOutage = 427,
        MailSent = 428,
        NotHappyEnough = 429,
        UseCantImmune = 430,
        CantBeDisenchanted = 431,
        CantUseDisarmed = 432,
        AuctionQuestItem = 433,
        AuctionBoundItem = 434,
        AuctionConjuredItem = 435,
        AuctionLimitedDurationItem = 436,
        AuctionWrappedItem = 437,
        AuctionLootItem = 438,
        AuctionBag = 439,
        AuctionEquippedBag = 440,
        AuctionDatabaseError = 441,
        AuctionBidOwn = 442,
        AuctionBidIncrement = 443,
        AuctionHigherBid = 444,
        AuctionMinBid = 445,
        AuctionRepairItem = 446,
        AuctionUsedCharges = 447,
        AuctionAlreadyBid = 448,
        AuctionHouseUnavailable = 449,
        AuctionHouseBusy = 450,
        AuctionStarted = 451,
        AuctionRemoved = 452,
        AuctionOutbidS = 453,
        AuctionWonS = 454,
        AuctionSoldS = 455,
        AuctionExpiredS = 456,
        AuctionRemovedS = 457,
        AuctionBidPlaced = 458,
        LogoutFailed = 459,
        QuestPushSuccessS = 460,
        QuestPushInvalidS = 461,
        QuestPushAcceptedS = 462,
        QuestPushDeclinedS = 463,
        QuestPushBusyS = 464,
        QuestPushDeadS = 465,
        QuestPushLogFullS = 466,
        QuestPushOnquestS = 467,
        QuestPushAlreadyDoneS = 468,
        QuestPushNotDailyS = 469,
        QuestPushTimerExpiredS = 470,
        QuestPushNotInPartyS = 471,
        QuestPushDifferentServerDailyS = 472,
        QuestPushNotAllowedS = 473,
        RaidGroupLowlevel = 474,
        RaidGroupOnly = 475,
        RaidGroupFull = 476,
        RaidGroupRequirementsUnmatch = 477,
        CorpseIsNotInInstance = 478,
        PvpKillHonorable = 479,
        PvpKillDishonorable = 480,
        SpellFailedAlreadyAtFullHealth = 481,
        SpellFailedAlreadyAtFullMana = 482,
        SpellFailedAlreadyAtFullPowerS = 483,
        AutolootMoneyS = 484,
        GenericStunned = 485,
        GenericThrottle = 486,
        ClubFinderSearchingTooFast = 487,
        TargetStunned = 488,
        MustRepairDurability = 489,
        RaidYouJoined = 490,
        RaidYouLeft = 491,
        InstanceGroupJoinedWithParty = 492,
        InstanceGroupJoinedWithRaid = 493,
        RaidMemberAddedS = 494,
        RaidMemberRemovedS = 495,
        InstanceGroupAddedS = 496,
        InstanceGroupRemovedS = 497,
        ClickOnItemToFeed = 498,
        TooManyChatChannels = 499,
        LootRollPending = 500,
        LootPlayerNotFound = 501,
        NotInRaid = 502,
        LoggingOut = 503,
        TargetLoggingOut = 504,
        NotWhileMounted = 505,
        NotWhileShapeshifted = 506,
        NotInCombat = 507,
        NotWhileDisarmed = 508,
        PetBroken = 509,
        TalentWipeError = 510,
        SpecWipeError = 511,
        GlyphWipeError = 512,
        PetSpecWipeError = 513,
        FeignDeathResisted = 514,
        MeetingStoneInQueueS = 515,
        MeetingStoneLeftQueueS = 516,
        MeetingStoneOtherMemberLeft = 517,
        MeetingStonePartyKickedFromQueue = 518,
        MeetingStoneMemberStillInQueue = 519,
        MeetingStoneSuccess = 520,
        MeetingStoneInProgress = 521,
        MeetingStoneMemberAddedS = 522,
        MeetingStoneGroupFull = 523,
        MeetingStoneNotLeader = 524,
        MeetingStoneInvalidLevel = 525,
        MeetingStoneTargetNotInParty = 526,
        MeetingStoneTargetInvalidLevel = 527,
        MeetingStoneMustBeLeader = 528,
        MeetingStoneNoRaidGroup = 529,
        MeetingStoneNeedParty = 530,
        MeetingStoneNotFound = 531,
        MeetingStoneTargetInVehicle = 532,
        GuildemblemSame = 533,
        EquipTradeItem = 534,
        PvpToggleOn = 535,
        PvpToggleOff = 536,
        GroupJoinBattlegroundDeserters = 537,
        GroupJoinBattlegroundDead = 538,
        GroupJoinBattlegroundS = 539,
        GroupJoinBattlegroundFail = 540,
        GroupJoinBattlegroundTooMany = 541,
        SoloJoinBattlegroundS = 542,
        JoinSingleScenarioS = 543,
        BattlegroundTooManyQueues = 544,
        BattlegroundCannotQueueForRated = 545,
        BattledgroundQueuedForRated = 546,
        BattlegroundTeamLeftQueue = 547,
        BattlegroundNotInBattleground = 548,
        AlreadyInArenaTeamS = 549,
        InvalidPromotionCode = 550,
        BgPlayerJoinedSs = 551,
        BgPlayerLeftS = 552,
        RestrictedAccount = 553,
        RestrictedAccountTrial = 554,
        PlayTimeExceeded = 555,
        ApproachingPartialPlayTime = 556,
        ApproachingPartialPlayTime2 = 557,
        ApproachingNoPlayTime = 558,
        ApproachingNoPlayTime2 = 559,
        UnhealthyTime = 560,
        ChatRestrictedTrial = 561,
        ChatThrottled = 562,
        MailReachedCap = 563,
        InvalidRaidTarget = 564,
        RaidLeaderReadyCheckStartS = 565,
        ReadyCheckInProgress = 566,
        ReadyCheckThrottled = 567,
        DungeonDifficultyFailed = 568,
        DungeonDifficultyChangedS = 569,
        TradeWrongRealm = 570,
        TradeNotOnTaplist = 571,
        ChatPlayerAmbiguousS = 572,
        LootCantLootThatNow = 573,
        LootMasterInvFull = 574,
        LootMasterUniqueItem = 575,
        LootMasterOther = 576,
        FilteringYouS = 577,
        UsePreventedByMechanicS = 578,
        ItemUniqueEquippable = 579,
        LfgLeaderIsLfmS = 580,
        LfgPending = 581,
        CantSpeakLangage = 582,
        VendorMissingTurnins = 583,
        BattlegroundNotInTeam = 584,
        NotInBattleground = 585,
        NotEnoughHonorPoints = 586,
        NotEnoughArenaPoints = 587,
        SocketingRequiresMetaGem = 588,
        SocketingMetaGemOnlyInMetaslot = 589,
        SocketingRequiresHydraulicGem = 590,
        SocketingHydraulicGemOnlyInHydraulicslot = 591,
        SocketingRequiresCogwheelGem = 592,
        SocketingCogwheelGemOnlyInCogwheelslot = 593,
        SocketingItemTooLowLevel = 594,
        ItemMaxCountSocketed = 595,
        SystemDisabled = 596,
        QuestFailedTooManyDailyQuestsI = 597,
        ItemMaxCountEquippedSocketed = 598,
        ItemUniqueEquippableSocketed = 599,
        UserSquelched = 600,
        AccountSilenced = 601,
        TooMuchGold = 602,
        NotBarberSitting = 603,
        QuestFailedCais = 604,
        InviteRestrictedTrial = 605,
        VoiceIgnoreFull = 606,
        VoiceIgnoreSelf = 607,
        VoiceIgnoreNotFound = 608,
        VoiceIgnoreAlreadyS = 609,
        VoiceIgnoreAddedS = 610,
        VoiceIgnoreRemovedS = 611,
        VoiceIgnoreAmbiguous = 612,
        VoiceIgnoreDeleted = 613,
        UnknownMacroOptionS = 614,
        NotDuringArenaMatch = 615,
        PlayerSilenced = 616,
        PlayerUnsilenced = 617,
        ComsatDisconnect = 618,
        ComsatReconnectAttempt = 619,
        ComsatConnectFail = 620,
        MailInvalidAttachmentSlot = 621,
        MailTooManyAttachments = 622,
        MailInvalidAttachment = 623,
        MailAttachmentExpired = 624,
        VoiceChatParentalDisableMic = 625,
        ProfaneChatName = 626,
        PlayerSilencedEcho = 627,
        PlayerUnsilencedEcho = 628,
        LootCantLootThat = 629,
        ArenaExpiredCais = 630,
        GroupActionThrottled = 631,
        AlreadyPickpocketed = 632,
        NameInvalid = 633,
        NameNoName = 634,
        NameTooShort = 635,
        NameTooLong = 636,
        NameMixedLanguages = 637,
        NameProfane = 638,
        NameReserved = 639,
        NameThreeConsecutive = 640,
        NameInvalidSpace = 641,
        NameConsecutiveSpaces = 642,
        NameRussianConsecutiveSilentCharacters = 643,
        NameRussianSilentCharacterAtBeginningOrEnd = 644,
        NameDeclensionDoesntMatchBaseName = 645,
        RecruitAFriendNotLinked = 646,
        RecruitAFriendNotNow = 647,
        RecruitAFriendSummonLevelMax = 648,
        RecruitAFriendSummonCooldown = 649,
        RecruitAFriendSummonOffline = 650,
        RecruitAFriendInsufExpanLvl = 651,
        RecruitAFriendMapIncomingTransferNotAllowed = 652,
        NotSameAccount = 653,
        BadOnUseEnchant = 654,
        TradeSelf = 655,
        TooManySockets = 656,
        ItemMaxLimitCategoryCountExceededIs = 657,
        TradeTargetMaxLimitCategoryCountExceededIs = 658,
        ItemMaxLimitCategorySocketedExceededIs = 659,
        ItemMaxLimitCategoryEquippedExceededIs = 660,
        ShapeshiftFormCannotEquip = 661,
        ItemInventoryFullSatchel = 662,
        ScalingStatItemLevelExceeded = 663,
        ScalingStatItemLevelTooLow = 664,
        PurchaseLevelTooLow = 665,
        GroupSwapFailed = 666,
        InviteInCombat = 667,
        InvalidGlyphSlot = 668,
        GenericNoValidTargets = 669,
        CalendarEventAlertS = 670,
        PetLearnSpellS = 671,
        PetLearnAbilityS = 672,
        PetSpellUnlearnedS = 673,
        InviteUnknownRealm = 674,
        InviteNoPartyServer = 675,
        InvitePartyBusy = 676,
        PartyTargetAmbiguous = 677,
        PartyLfgInviteRaidLocked = 678,
        PartyLfgBootLimit = 679,
        PartyLfgBootCooldownS = 680,
        PartyLfgBootNotEligibleS = 681,
        PartyLfgBootInpatientTimerS = 682,
        PartyLfgBootInProgress = 683,
        PartyLfgBootTooFewPlayers = 684,
        PartyLfgBootVoteSucceeded = 685,
        PartyLfgBootVoteFailed = 686,
        PartyLfgBootInCombat = 687,
        PartyLfgBootDungeonComplete = 688,
        PartyLfgBootLootRolls = 689,
        PartyLfgBootVoteRegistered = 690,
        PartyPrivateGroupOnly = 691,
        PartyLfgTeleportInCombat = 692,
        RaidDisallowedByLevel = 693,
        RaidDisallowedByCrossRealm = 694,
        PartyRoleNotAvailable = 695,
        JoinLfgObjectFailed = 696,
        LfgRemovedLevelup = 697,
        LfgRemovedXpToggle = 698,
        LfgRemovedFactionChange = 699,
        BattlegroundInfoThrottled = 700,
        BattlegroundAlreadyIn = 701,
        ArenaTeamChangeFailedQueued = 702,
        ArenaTeamPermissions = 703,
        NotWhileFalling = 704,
        NotWhileMoving = 705,
        NotWhileFatigued = 706,
        MaxSockets = 707,
        MultiCastActionTotemS = 708,
        BattlegroundJoinLevelup = 709,
        RemoveFromPvpQueueXpGain = 710,
        BattlegroundJoinXpGain = 711,
        BattlegroundJoinMercenary = 712,
        BattlegroundJoinTooManyHealers = 713,
        BattlegroundJoinRatedTooManyHealers = 714,
        BattlegroundJoinTooManyTanks = 715,
        BattlegroundJoinTooManyDamage = 716,
        RaidDifficultyFailed = 717,
        RaidDifficultyChangedS = 718,
        LegacyRaidDifficultyChangedS = 719,
        RaidLockoutChangedS = 720,
        RaidConvertedToParty = 721,
        PartyConvertedToRaid = 722,
        PlayerDifficultyChangedS = 723,
        GmresponseDbError = 724,
        BattlegroundJoinRangeIndex = 725,
        ArenaJoinRangeIndex = 726,
        RemoveFromPvpQueueFactionChange = 727,
        BattlegroundJoinFailed = 728,
        BattlegroundJoinNoValidSpecForRole = 729,
        BattlegroundJoinRespec = 730,
        BattlegroundInvitationDeclined = 731,
        BattlegroundJoinTimedOut = 732,
        BattlegroundDupeQueue = 733,
        BattlegroundJoinMustCompleteQuest = 734,
        InBattlegroundRespec = 735,
        MailLimitedDurationItem = 736,
        YellRestrictedTrial = 737,
        ChatRaidRestrictedTrial = 738,
        LfgRoleCheckFailed = 739,
        LfgRoleCheckFailedTimeout = 740,
        LfgRoleCheckFailedNotViable = 741,
        LfgReadyCheckFailed = 742,
        LfgReadyCheckFailedTimeout = 743,
        LfgGroupFull = 744,
        LfgNoLfgObject = 745,
        LfgNoSlotsPlayer = 746,
        LfgNoSlotsParty = 747,
        LfgNoSpec = 748,
        LfgMismatchedSlots = 749,
        LfgMismatchedSlotsLocalXrealm = 750,
        LfgPartyPlayersFromDifferentRealms = 751,
        LfgMembersNotPresent = 752,
        LfgGetInfoTimeout = 753,
        LfgInvalidSlot = 754,
        LfgDeserterPlayer = 755,
        LfgDeserterParty = 756,
        LfgDead = 757,
        LfgRandomCooldownPlayer = 758,
        LfgRandomCooldownParty = 759,
        LfgTooManyMembers = 760,
        LfgTooFewMembers = 761,
        LfgProposalFailed = 762,
        LfgProposalDeclinedSelf = 763,
        LfgProposalDeclinedParty = 764,
        LfgNoSlotsSelected = 765,
        LfgNoRolesSelected = 766,
        LfgRoleCheckInitiated = 767,
        LfgReadyCheckInitiated = 768,
        LfgPlayerDeclinedRoleCheck = 769,
        LfgPlayerDeclinedReadyCheck = 770,
        LfgJoinedQueue = 771,
        LfgJoinedFlexQueue = 772,
        LfgJoinedRfQueue = 773,
        LfgJoinedScenarioQueue = 774,
        LfgJoinedWorldPvpQueue = 775,
        LfgJoinedBattlefieldQueue = 776,
        LfgJoinedList = 777,
        LfgLeftQueue = 778,
        LfgLeftList = 779,
        LfgRoleCheckAborted = 780,
        LfgReadyCheckAborted = 781,
        LfgCantUseBattleground = 782,
        LfgCantUseDungeons = 783,
        LfgReasonTooManyLfg = 784,
        InvalidTeleportLocation = 785,
        TooFarToInteract = 786,
        BattlegroundPlayersFromDifferentRealms = 787,
        DifficultyChangeCooldownS = 788,
        DifficultyChangeCombatCooldownS = 789,
        DifficultyChangeWorldstate = 790,
        DifficultyChangeEncounter = 791,
        DifficultyChangeCombat = 792,
        DifficultyChangePlayerBusy = 793,
        DifficultyChangeAlreadyStarted = 794,
        DifficultyChangeOtherHeroicS = 795,
        DifficultyChangeHeroicInstanceAlreadyRunning = 796,
        ArenaTeamPartySize = 797,
        QuestForceRemovedS = 798,
        AttackNoActions = 799,
        InRandomBg = 800,
        InNonRandomBg = 801,
        AuctionEnoughItems = 802,
        BnFriendSelf = 803,
        BnFriendAlready = 804,
        BnFriendBlocked = 805,
        BnFriendListFull = 806,
        BnFriendRequestSent = 807,
        BnBroadcastThrottle = 808,
        BgDeveloperOnly = 809,
        CurrencySpellSlotMismatch = 810,
        CurrencyNotTradable = 811,
        RequiresExpansionS = 812,
        QuestFailedSpell = 813,
        TalentFailedNotEnoughTalentsInPrimaryTree = 814,
        TalentFailedNoPrimaryTreeSelected = 815,
        TalentFailedCantRemoveTalent = 816,
        TalentFailedUnknown = 817,
        WargameRequestFailure = 818,
        RankRequiresAuthenticator = 819,
        GuildBankVoucherFailed = 820,
        WargameRequestSent = 821,
        RequiresAchievementI = 822,
        RefundResultExceedMaxCurrency = 823,
        CantBuyQuantity = 824,
        ItemIsBattlePayLocked = 825,
        PartyAlreadyInBattlegroundQueue = 826,
        PartyConfirmingBattlegroundQueue = 827,
        BattlefieldTeamPartySize = 828,
        InsuffTrackedCurrencyIs = 829,
        NotOnTournamentRealm = 830,
        GuildTrialAccountTrial = 831,
        GuildTrialAccountVeteran = 832,
        GuildUndeletableDueToLevel = 833,
        CantDoThatInAGroup = 834,
        GuildLeaderReplaced = 835,
        TransmogrifyCantEquip = 836,
        TransmogrifyInvalidItemType = 837,
        TransmogrifyNotSoulbound = 838,
        TransmogrifyInvalidSource = 839,
        TransmogrifyInvalidDestination = 840,
        TransmogrifyMismatch = 841,
        TransmogrifyLegendary = 842,
        TransmogrifySameItem = 843,
        TransmogrifySameAppearance = 844,
        TransmogrifyNotEquipped = 845,
        VoidDepositFull = 846,
        VoidWithdrawFull = 847,
        VoidStorageWrapped = 848,
        VoidStorageStackable = 849,
        VoidStorageUnbound = 850,
        VoidStorageRepair = 851,
        VoidStorageCharges = 852,
        VoidStorageQuest = 853,
        VoidStorageConjured = 854,
        VoidStorageMail = 855,
        VoidStorageBag = 856,
        VoidTransferStorageFull = 857,
        VoidTransferInvFull = 858,
        VoidTransferInternalError = 859,
        VoidTransferItemInvalid = 860,
        DifficultyDisabledInLfg = 861,
        VoidStorageUnique = 862,
        VoidStorageLoot = 863,
        VoidStorageHoliday = 864,
        VoidStorageDuration = 865,
        VoidStorageLoadFailed = 866,
        VoidStorageInvalidItem = 867,
        ParentalControlsChatMuted = 868,
        SorStartExperienceIncomplete = 869,
        SorInvalidEmail = 870,
        SorInvalidComment = 871,
        ChallengeModeResetCooldownS = 872,
        ChallengeModeResetKeystone = 873,
        PetJournalAlreadyInLoadout = 874,
        ReportSubmittedSuccessfully = 875,
        ReportSubmissionFailed = 876,
        SuggestionSubmittedSuccessfully = 877,
        BugSubmittedSuccessfully = 878,
        ChallengeModeEnabled = 879,
        ChallengeModeDisabled = 880,
        PetbattleCreateFailed = 881,
        PetbattleNotHere = 882,
        PetbattleNotHereOnTransport = 883,
        PetbattleNotHereUnevenGround = 884,
        PetbattleNotHereObstructed = 885,
        PetbattleNotWhileInCombat = 886,
        PetbattleNotWhileDead = 887,
        PetbattleNotWhileFlying = 888,
        PetbattleTargetInvalid = 889,
        PetbattleTargetOutOfRange = 890,
        PetbattleTargetNotCapturable = 891,
        PetbattleNotATrainer = 892,
        PetbattleDeclined = 893,
        PetbattleInBattle = 894,
        PetbattleInvalidLoadout = 895,
        PetbattleAllPetsDead = 896,
        PetbattleNoPetsInSlots = 897,
        PetbattleNoAccountLock = 898,
        PetbattleWildPetTapped = 899,
        PetbattleRestrictedAccount = 900,
        PetbattleOpponentNotAvailable = 901,
        PetbattleNotWhileInMatchedBattle = 902,
        CantHaveMorePetsOfThatType = 903,
        CantHaveMorePets = 904,
        PvpMapNotFound = 905,
        PvpMapNotSet = 906,
        PetbattleQueueQueued = 907,
        PetbattleQueueAlreadyQueued = 908,
        PetbattleQueueJoinFailed = 909,
        PetbattleQueueJournalLock = 910,
        PetbattleQueueRemoved = 911,
        PetbattleQueueProposalDeclined = 912,
        PetbattleQueueProposalTimeout = 913,
        PetbattleQueueOpponentDeclined = 914,
        PetbattleQueueRequeuedInternal = 915,
        PetbattleQueueRequeuedRemoved = 916,
        PetbattleQueueSlotLocked = 917,
        PetbattleQueueSlotEmpty = 918,
        PetbattleQueueSlotNoTracker = 919,
        PetbattleQueueSlotNoSpecies = 920,
        PetbattleQueueSlotCantBattle = 921,
        PetbattleQueueSlotRevoked = 922,
        PetbattleQueueSlotDead = 923,
        PetbattleQueueSlotNoPet = 924,
        PetbattleQueueNotWhileNeutral = 925,
        PetbattleGameTimeLimitWarning = 926,
        PetbattleGameRoundsLimitWarning = 927,
        HasRestriction = 928,
        ItemUpgradeItemTooLowLevel = 929,
        ItemUpgradeNoPath = 930,
        ItemUpgradeNoMoreUpgrades = 931,
        BonusRollEmpty = 932,
        ChallengeModeFull = 933,
        ChallengeModeInProgress = 934,
        ChallengeModeIncorrectKeystone = 935,
        BattletagFriendNotFound = 936,
        BattletagFriendNotValid = 937,
        BattletagFriendNotAllowed = 938,
        BattletagFriendThrottled = 939,
        BattletagFriendSuccess = 940,
        PetTooHighLevelToUncage = 941,
        PetbattleInternal = 942,
        CantCagePetYet = 943,
        NoLootInChallengeMode = 944,
        QuestPetBattleVictoriesPvpIi = 945,
        RoleCheckAlreadyInProgress = 946,
        RecruitAFriendAccountLimit = 947,
        RecruitAFriendFailed = 948,
        SetLootPersonal = 949,
        SetLootMethodFailedCombat = 950,
        ReagentBankFull = 951,
        ReagentBankLocked = 952,
        GarrisonBuildingExists = 953,
        GarrisonInvalidPlot = 954,
        GarrisonInvalidBuildingid = 955,
        GarrisonInvalidPlotBuilding = 956,
        GarrisonRequiresBlueprint = 957,
        GarrisonNotEnoughCurrency = 958,
        GarrisonNotEnoughGold = 959,
        GarrisonCompleteMissionWrongFollowerType = 960,
        AlreadyUsingLfgList = 961,
        RestrictedAccountLfgListTrial = 962,
        ToyUseLimitReached = 963,
        ToyAlreadyKnown = 964,
        TransmogSetAlreadyKnown = 965,
        NotEnoughCurrency = 966,
        SpecIsDisabled = 967,
        FeatureRestrictedTrial = 968,
        CantBeObliterated = 969,
        CantBeScrapped = 970,
        ArtifactRelicDoesNotMatchArtifact = 971,
        MustEquipArtifact = 972,
        CantDoThatRightNow = 973,
        AffectingCombat = 974,
        EquipmentManagerCombatSwapS = 975,
        EquipmentManagerBagsFull = 976,
        EquipmentManagerMissingItemS = 977,
        MovieRecordingWarningPerf = 978,
        MovieRecordingWarningDiskFull = 979,
        MovieRecordingWarningNoMovie = 980,
        MovieRecordingWarningRequirements = 981,
        MovieRecordingWarningCompressing = 982,
        NoChallengeModeReward = 983,
        ClaimedChallengeModeReward = 984,
        ChallengeModePeriodResetSs = 985,
        CantDoThatChallengeModeActive = 986,
        TalentFailedRestArea = 987,
        CannotAbandonLastPet = 988,
        TestCvarSetSss = 989,
        QuestTurnInFailReason = 990,
        ClaimedChallengeModeRewardOld = 991,
        TalentGrantedByAura = 992,
        ChallengeModeAlreadyComplete = 993,
        GlyphTargetNotAvailable = 994,
        PvpWarmodeToggleOn = 995,
        PvpWarmodeToggleOff = 996,
        SpellFailedLevelRequirement = 997,
        BattlegroundJoinRequiresLevel = 998,
        BattlegroundJoinDisqualified = 999,
        BattlegroundJoinDisqualifiedNoName = 1000,
        VoiceChatGenericUnableToConnect = 1001,
        VoiceChatServiceLost = 1002,
        VoiceChatChannelNameTooShort = 1003,
        VoiceChatChannelNameTooLong = 1004,
        VoiceChatChannelAlreadyExists = 1005,
        VoiceChatTargetNotFound = 1006,
        VoiceChatTooManyRequests = 1007,
        VoiceChatPlayerSilenced = 1008,
        VoiceChatParentalDisableAll = 1009,
        VoiceChatDisabled = 1010,
        NoPvpReward = 1011,
        ClaimedPvpReward = 1012,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1013,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1014,
        AzeriteEssenceSelectionFailedConditionFailed = 1015,
        AzeriteEssenceSelectionFailedRestArea = 1016,
        AzeriteEssenceSelectionFailedSlotLocked = 1017,
        AzeriteEssenceSelectionFailedNotAtForge = 1018,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1019,
        AzeriteEssenceSelectionFailedNotEquipped = 1020,
        SocketingRequiresPunchcardredGem = 1021,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1022,
        SocketingRequiresPunchcardyellowGem = 1023,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1024,
        SocketingRequiresPunchcardblueGem = 1025,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1026,
        LevelLinkingResultLinked = 1027,
        LevelLinkingResultUnlinked = 1028,
        ClubFinderErrorPostClub = 1029,
        ClubFinderErrorApplyClub = 1030,
        ClubFinderErrorRespondApplicant = 1031,
        ClubFinderErrorCancelApplication = 1032,
        ClubFinderErrorTypeAcceptApplication = 1033,
        ClubFinderErrorTypeNoInvitePermissions = 1034,
        ClubFinderErrorTypeNoPostingPermissions = 1035,
        ClubFinderErrorTypeApplicantList = 1036,
        ClubFinderErrorTypeApplicantListNoPerm = 1037,
        ClubFinderErrorTypeFinderNotAvailable = 1038,
        ClubFinderErrorTypeGetPostingIds = 1039,
        ClubFinderErrorTypeJoinApplication = 1040,
        CantUseProfanity = 1041
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
