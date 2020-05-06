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
        public const int MaxAzeriteEssenceSlot = 4;
        public const int MaxAzeriteEssenceRank = 4;

        /// <summary>
        /// BattlePets Const
        /// </summary>
        public const int MaxBattlePetSpeciesId = 2873;
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

        ShadowLands = 8,

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
        AuctionItemHasQuote = 450,
        AuctionHouseBusy = 451,
        AuctionStarted = 452,
        AuctionRemoved = 453,
        AuctionOutbidS = 454,
        AuctionWonS = 455,
        AuctionSoldS = 456,
        AuctionExpiredS = 457,
        AuctionRemovedS = 458,
        AuctionBidPlaced = 459,
        LogoutFailed = 460,
        QuestPushSuccessS = 461,
        QuestPushInvalidS = 462,
        QuestPushAcceptedS = 463,
        QuestPushDeclinedS = 464,
        QuestPushBusyS = 465,
        QuestPushDeadS = 466,
        QuestPushLogFullS = 467,
        QuestPushOnquestS = 468,
        QuestPushAlreadyDoneS = 469,
        QuestPushNotDailyS = 470,
        QuestPushTimerExpiredS = 471,
        QuestPushNotInPartyS = 472,
        QuestPushDifferentServerDailyS = 473,
        QuestPushNotAllowedS = 474,
        RaidGroupLowlevel = 475,
        RaidGroupOnly = 476,
        RaidGroupFull = 477,
        RaidGroupRequirementsUnmatch = 478,
        CorpseIsNotInInstance = 479,
        PvpKillHonorable = 480,
        PvpKillDishonorable = 481,
        SpellFailedAlreadyAtFullHealth = 482,
        SpellFailedAlreadyAtFullMana = 483,
        SpellFailedAlreadyAtFullPowerS = 484,
        AutolootMoneyS = 485,
        GenericStunned = 486,
        GenericThrottle = 487,
        ClubFinderSearchingTooFast = 488,
        TargetStunned = 489,
        MustRepairDurability = 490,
        RaidYouJoined = 491,
        RaidYouLeft = 492,
        InstanceGroupJoinedWithParty = 493,
        InstanceGroupJoinedWithRaid = 494,
        RaidMemberAddedS = 495,
        RaidMemberRemovedS = 496,
        InstanceGroupAddedS = 497,
        InstanceGroupRemovedS = 498,
        ClickOnItemToFeed = 499,
        TooManyChatChannels = 500,
        LootRollPending = 501,
        LootPlayerNotFound = 502,
        NotInRaid = 503,
        LoggingOut = 504,
        TargetLoggingOut = 505,
        NotWhileMounted = 506,
        NotWhileShapeshifted = 507,
        NotInCombat = 508,
        NotWhileDisarmed = 509,
        PetBroken = 510,
        TalentWipeError = 511,
        SpecWipeError = 512,
        GlyphWipeError = 513,
        PetSpecWipeError = 514,
        FeignDeathResisted = 515,
        MeetingStoneInQueueS = 516,
        MeetingStoneLeftQueueS = 517,
        MeetingStoneOtherMemberLeft = 518,
        MeetingStonePartyKickedFromQueue = 519,
        MeetingStoneMemberStillInQueue = 520,
        MeetingStoneSuccess = 521,
        MeetingStoneInProgress = 522,
        MeetingStoneMemberAddedS = 523,
        MeetingStoneGroupFull = 524,
        MeetingStoneNotLeader = 525,
        MeetingStoneInvalidLevel = 526,
        MeetingStoneTargetNotInParty = 527,
        MeetingStoneTargetInvalidLevel = 528,
        MeetingStoneMustBeLeader = 529,
        MeetingStoneNoRaidGroup = 530,
        MeetingStoneNeedParty = 531,
        MeetingStoneNotFound = 532,
        MeetingStoneTargetInVehicle = 533,
        GuildemblemSame = 534,
        EquipTradeItem = 535,
        PvpToggleOn = 536,
        PvpToggleOff = 537,
        GroupJoinBattlegroundDeserters = 538,
        GroupJoinBattlegroundDead = 539,
        GroupJoinBattlegroundS = 540,
        GroupJoinBattlegroundFail = 541,
        GroupJoinBattlegroundTooMany = 542,
        SoloJoinBattlegroundS = 543,
        JoinSingleScenarioS = 544,
        BattlegroundTooManyQueues = 545,
        BattlegroundCannotQueueForRated = 546,
        BattledgroundQueuedForRated = 547,
        BattlegroundTeamLeftQueue = 548,
        BattlegroundNotInBattleground = 549,
        AlreadyInArenaTeamS = 550,
        InvalidPromotionCode = 551,
        BgPlayerJoinedSs = 552,
        BgPlayerLeftS = 553,
        RestrictedAccount = 554,
        RestrictedAccountTrial = 555,
        PlayTimeExceeded = 556,
        ApproachingPartialPlayTime = 557,
        ApproachingPartialPlayTime2 = 558,
        ApproachingNoPlayTime = 559,
        ApproachingNoPlayTime2 = 560,
        UnhealthyTime = 561,
        ChatRestrictedTrial = 562,
        ChatThrottled = 563,
        MailReachedCap = 564,
        InvalidRaidTarget = 565,
        RaidLeaderReadyCheckStartS = 566,
        ReadyCheckInProgress = 567,
        ReadyCheckThrottled = 568,
        DungeonDifficultyFailed = 569,
        DungeonDifficultyChangedS = 570,
        TradeWrongRealm = 571,
        TradeNotOnTaplist = 572,
        ChatPlayerAmbiguousS = 573,
        LootCantLootThatNow = 574,
        LootMasterInvFull = 575,
        LootMasterUniqueItem = 576,
        LootMasterOther = 577,
        FilteringYouS = 578,
        UsePreventedByMechanicS = 579,
        ItemUniqueEquippable = 580,
        LfgLeaderIsLfmS = 581,
        LfgPending = 582,
        CantSpeakLangage = 583,
        VendorMissingTurnins = 584,
        BattlegroundNotInTeam = 585,
        NotInBattleground = 586,
        NotEnoughHonorPoints = 587,
        NotEnoughArenaPoints = 588,
        SocketingRequiresMetaGem = 589,
        SocketingMetaGemOnlyInMetaslot = 590,
        SocketingRequiresHydraulicGem = 591,
        SocketingHydraulicGemOnlyInHydraulicslot = 592,
        SocketingRequiresCogwheelGem = 593,
        SocketingCogwheelGemOnlyInCogwheelslot = 594,
        SocketingItemTooLowLevel = 595,
        ItemMaxCountSocketed = 596,
        SystemDisabled = 597,
        QuestFailedTooManyDailyQuestsI = 598,
        ItemMaxCountEquippedSocketed = 599,
        ItemUniqueEquippableSocketed = 600,
        UserSquelched = 601,
        AccountSilenced = 602,
        TooMuchGold = 603,
        NotBarberSitting = 604,
        QuestFailedCais = 605,
        InviteRestrictedTrial = 606,
        VoiceIgnoreFull = 607,
        VoiceIgnoreSelf = 608,
        VoiceIgnoreNotFound = 609,
        VoiceIgnoreAlreadyS = 610,
        VoiceIgnoreAddedS = 611,
        VoiceIgnoreRemovedS = 612,
        VoiceIgnoreAmbiguous = 613,
        VoiceIgnoreDeleted = 614,
        UnknownMacroOptionS = 615,
        NotDuringArenaMatch = 616,
        PlayerSilenced = 617,
        PlayerUnsilenced = 618,
        ComsatDisconnect = 619,
        ComsatReconnectAttempt = 620,
        ComsatConnectFail = 621,
        MailInvalidAttachmentSlot = 622,
        MailTooManyAttachments = 623,
        MailInvalidAttachment = 624,
        MailAttachmentExpired = 625,
        VoiceChatParentalDisableMic = 626,
        ProfaneChatName = 627,
        PlayerSilencedEcho = 628,
        PlayerUnsilencedEcho = 629,
        LootCantLootThat = 630,
        ArenaExpiredCais = 631,
        GroupActionThrottled = 632,
        AlreadyPickpocketed = 633,
        NameInvalid = 634,
        NameNoName = 635,
        NameTooShort = 636,
        NameTooLong = 637,
        NameMixedLanguages = 638,
        NameProfane = 639,
        NameReserved = 640,
        NameThreeConsecutive = 641,
        NameInvalidSpace = 642,
        NameConsecutiveSpaces = 643,
        NameRussianConsecutiveSilentCharacters = 644,
        NameRussianSilentCharacterAtBeginningOrEnd = 645,
        NameDeclensionDoesntMatchBaseName = 646,
        RecruitAFriendNotLinked = 647,
        RecruitAFriendNotNow = 648,
        RecruitAFriendSummonLevelMax = 649,
        RecruitAFriendSummonCooldown = 650,
        RecruitAFriendSummonOffline = 651,
        RecruitAFriendInsufExpanLvl = 652,
        RecruitAFriendMapIncomingTransferNotAllowed = 653,
        NotSameAccount = 654,
        BadOnUseEnchant = 655,
        TradeSelf = 656,
        TooManySockets = 657,
        ItemMaxLimitCategoryCountExceededIs = 658,
        TradeTargetMaxLimitCategoryCountExceededIs = 659,
        ItemMaxLimitCategorySocketedExceededIs = 660,
        ItemMaxLimitCategoryEquippedExceededIs = 661,
        ShapeshiftFormCannotEquip = 662,
        ItemInventoryFullSatchel = 663,
        ScalingStatItemLevelExceeded = 664,
        ScalingStatItemLevelTooLow = 665,
        PurchaseLevelTooLow = 666,
        GroupSwapFailed = 667,
        InviteInCombat = 668,
        InvalidGlyphSlot = 669,
        GenericNoValidTargets = 670,
        CalendarEventAlertS = 671,
        PetLearnSpellS = 672,
        PetLearnAbilityS = 673,
        PetSpellUnlearnedS = 674,
        InviteUnknownRealm = 675,
        InviteNoPartyServer = 676,
        InvitePartyBusy = 677,
        PartyTargetAmbiguous = 678,
        PartyLfgInviteRaidLocked = 679,
        PartyLfgBootLimit = 680,
        PartyLfgBootCooldownS = 681,
        PartyLfgBootNotEligibleS = 682,
        PartyLfgBootInpatientTimerS = 683,
        PartyLfgBootInProgress = 684,
        PartyLfgBootTooFewPlayers = 685,
        PartyLfgBootVoteSucceeded = 686,
        PartyLfgBootVoteFailed = 687,
        PartyLfgBootInCombat = 688,
        PartyLfgBootDungeonComplete = 689,
        PartyLfgBootLootRolls = 690,
        PartyLfgBootVoteRegistered = 691,
        PartyPrivateGroupOnly = 692,
        PartyLfgTeleportInCombat = 693,
        RaidDisallowedByLevel = 694,
        RaidDisallowedByCrossRealm = 695,
        PartyRoleNotAvailable = 696,
        JoinLfgObjectFailed = 697,
        LfgRemovedLevelup = 698,
        LfgRemovedXpToggle = 699,
        LfgRemovedFactionChange = 700,
        BattlegroundInfoThrottled = 701,
        BattlegroundAlreadyIn = 702,
        ArenaTeamChangeFailedQueued = 703,
        ArenaTeamPermissions = 704,
        NotWhileFalling = 705,
        NotWhileMoving = 706,
        NotWhileFatigued = 707,
        MaxSockets = 708,
        MultiCastActionTotemS = 709,
        BattlegroundJoinLevelup = 710,
        RemoveFromPvpQueueXpGain = 711,
        BattlegroundJoinXpGain = 712,
        BattlegroundJoinMercenary = 713,
        BattlegroundJoinTooManyHealers = 714,
        BattlegroundJoinRatedTooManyHealers = 715,
        BattlegroundJoinTooManyTanks = 716,
        BattlegroundJoinTooManyDamage = 717,
        RaidDifficultyFailed = 718,
        RaidDifficultyChangedS = 719,
        LegacyRaidDifficultyChangedS = 720,
        RaidLockoutChangedS = 721,
        RaidConvertedToParty = 722,
        PartyConvertedToRaid = 723,
        PlayerDifficultyChangedS = 724,
        GmresponseDbError = 725,
        BattlegroundJoinRangeIndex = 726,
        ArenaJoinRangeIndex = 727,
        RemoveFromPvpQueueFactionChange = 728,
        BattlegroundJoinFailed = 729,
        BattlegroundJoinNoValidSpecForRole = 730,
        BattlegroundJoinRespec = 731,
        BattlegroundInvitationDeclined = 732,
        BattlegroundJoinTimedOut = 733,
        BattlegroundDupeQueue = 734,
        BattlegroundJoinMustCompleteQuest = 735,
        InBattlegroundRespec = 736,
        MailLimitedDurationItem = 737,
        YellRestrictedTrial = 738,
        ChatRaidRestrictedTrial = 739,
        LfgRoleCheckFailed = 740,
        LfgRoleCheckFailedTimeout = 741,
        LfgRoleCheckFailedNotViable = 742,
        LfgReadyCheckFailed = 743,
        LfgReadyCheckFailedTimeout = 744,
        LfgGroupFull = 745,
        LfgNoLfgObject = 746,
        LfgNoSlotsPlayer = 747,
        LfgNoSlotsParty = 748,
        LfgNoSpec = 749,
        LfgMismatchedSlots = 750,
        LfgMismatchedSlotsLocalXrealm = 751,
        LfgPartyPlayersFromDifferentRealms = 752,
        LfgMembersNotPresent = 753,
        LfgGetInfoTimeout = 754,
        LfgInvalidSlot = 755,
        LfgDeserterPlayer = 756,
        LfgDeserterParty = 757,
        LfgDead = 758,
        LfgRandomCooldownPlayer = 759,
        LfgRandomCooldownParty = 760,
        LfgTooManyMembers = 761,
        LfgTooFewMembers = 762,
        LfgProposalFailed = 763,
        LfgProposalDeclinedSelf = 764,
        LfgProposalDeclinedParty = 765,
        LfgNoSlotsSelected = 766,
        LfgNoRolesSelected = 767,
        LfgRoleCheckInitiated = 768,
        LfgReadyCheckInitiated = 769,
        LfgPlayerDeclinedRoleCheck = 770,
        LfgPlayerDeclinedReadyCheck = 771,
        LfgJoinedQueue = 772,
        LfgJoinedFlexQueue = 773,
        LfgJoinedRfQueue = 774,
        LfgJoinedScenarioQueue = 775,
        LfgJoinedWorldPvpQueue = 776,
        LfgJoinedBattlefieldQueue = 777,
        LfgJoinedList = 778,
        LfgLeftQueue = 779,
        LfgLeftList = 780,
        LfgRoleCheckAborted = 781,
        LfgReadyCheckAborted = 782,
        LfgCantUseBattleground = 783,
        LfgCantUseDungeons = 784,
        LfgReasonTooManyLfg = 785,
        InvalidTeleportLocation = 786,
        TooFarToInteract = 787,
        BattlegroundPlayersFromDifferentRealms = 788,
        DifficultyChangeCooldownS = 789,
        DifficultyChangeCombatCooldownS = 790,
        DifficultyChangeWorldstate = 791,
        DifficultyChangeEncounter = 792,
        DifficultyChangeCombat = 793,
        DifficultyChangePlayerBusy = 794,
        DifficultyChangeAlreadyStarted = 795,
        DifficultyChangeOtherHeroicS = 796,
        DifficultyChangeHeroicInstanceAlreadyRunning = 797,
        ArenaTeamPartySize = 798,
        QuestForceRemovedS = 799,
        AttackNoActions = 800,
        InRandomBg = 801,
        InNonRandomBg = 802,
        AuctionEnoughItems = 803,
        BnFriendSelf = 804,
        BnFriendAlready = 805,
        BnFriendBlocked = 806,
        BnFriendListFull = 807,
        BnFriendRequestSent = 808,
        BnBroadcastThrottle = 809,
        BgDeveloperOnly = 810,
        CurrencySpellSlotMismatch = 811,
        CurrencyNotTradable = 812,
        RequiresExpansionS = 813,
        QuestFailedSpell = 814,
        TalentFailedNotEnoughTalentsInPrimaryTree = 815,
        TalentFailedNoPrimaryTreeSelected = 816,
        TalentFailedCantRemoveTalent = 817,
        TalentFailedUnknown = 818,
        WargameRequestFailure = 819,
        RankRequiresAuthenticator = 820,
        GuildBankVoucherFailed = 821,
        WargameRequestSent = 822,
        RequiresAchievementI = 823,
        RefundResultExceedMaxCurrency = 824,
        CantBuyQuantity = 825,
        ItemIsBattlePayLocked = 826,
        PartyAlreadyInBattlegroundQueue = 827,
        PartyConfirmingBattlegroundQueue = 828,
        BattlefieldTeamPartySize = 829,
        InsuffTrackedCurrencyIs = 830,
        NotOnTournamentRealm = 831,
        GuildTrialAccountTrial = 832,
        GuildTrialAccountVeteran = 833,
        GuildUndeletableDueToLevel = 834,
        CantDoThatInAGroup = 835,
        GuildLeaderReplaced = 836,
        TransmogrifyCantEquip = 837,
        TransmogrifyInvalidItemType = 838,
        TransmogrifyNotSoulbound = 839,
        TransmogrifyInvalidSource = 840,
        TransmogrifyInvalidDestination = 841,
        TransmogrifyMismatch = 842,
        TransmogrifyLegendary = 843,
        TransmogrifySameItem = 844,
        TransmogrifySameAppearance = 845,
        TransmogrifyNotEquipped = 846,
        VoidDepositFull = 847,
        VoidWithdrawFull = 848,
        VoidStorageWrapped = 849,
        VoidStorageStackable = 850,
        VoidStorageUnbound = 851,
        VoidStorageRepair = 852,
        VoidStorageCharges = 853,
        VoidStorageQuest = 854,
        VoidStorageConjured = 855,
        VoidStorageMail = 856,
        VoidStorageBag = 857,
        VoidTransferStorageFull = 858,
        VoidTransferInvFull = 859,
        VoidTransferInternalError = 860,
        VoidTransferItemInvalid = 861,
        DifficultyDisabledInLfg = 862,
        VoidStorageUnique = 863,
        VoidStorageLoot = 864,
        VoidStorageHoliday = 865,
        VoidStorageDuration = 866,
        VoidStorageLoadFailed = 867,
        VoidStorageInvalidItem = 868,
        ParentalControlsChatMuted = 869,
        SorStartExperienceIncomplete = 870,
        SorInvalidEmail = 871,
        SorInvalidComment = 872,
        ChallengeModeResetCooldownS = 873,
        ChallengeModeResetKeystone = 874,
        PetJournalAlreadyInLoadout = 875,
        ReportSubmittedSuccessfully = 876,
        ReportSubmissionFailed = 877,
        SuggestionSubmittedSuccessfully = 878,
        BugSubmittedSuccessfully = 879,
        ChallengeModeEnabled = 880,
        ChallengeModeDisabled = 881,
        PetbattleCreateFailed = 882,
        PetbattleNotHere = 883,
        PetbattleNotHereOnTransport = 884,
        PetbattleNotHereUnevenGround = 885,
        PetbattleNotHereObstructed = 886,
        PetbattleNotWhileInCombat = 887,
        PetbattleNotWhileDead = 888,
        PetbattleNotWhileFlying = 889,
        PetbattleTargetInvalid = 890,
        PetbattleTargetOutOfRange = 891,
        PetbattleTargetNotCapturable = 892,
        PetbattleNotATrainer = 893,
        PetbattleDeclined = 894,
        PetbattleInBattle = 895,
        PetbattleInvalidLoadout = 896,
        PetbattleAllPetsDead = 897,
        PetbattleNoPetsInSlots = 898,
        PetbattleNoAccountLock = 899,
        PetbattleWildPetTapped = 900,
        PetbattleRestrictedAccount = 901,
        PetbattleOpponentNotAvailable = 902,
        PetbattleNotWhileInMatchedBattle = 903,
        CantHaveMorePetsOfThatType = 904,
        CantHaveMorePets = 905,
        PvpMapNotFound = 906,
        PvpMapNotSet = 907,
        PetbattleQueueQueued = 908,
        PetbattleQueueAlreadyQueued = 909,
        PetbattleQueueJoinFailed = 910,
        PetbattleQueueJournalLock = 911,
        PetbattleQueueRemoved = 912,
        PetbattleQueueProposalDeclined = 913,
        PetbattleQueueProposalTimeout = 914,
        PetbattleQueueOpponentDeclined = 915,
        PetbattleQueueRequeuedInternal = 916,
        PetbattleQueueRequeuedRemoved = 917,
        PetbattleQueueSlotLocked = 918,
        PetbattleQueueSlotEmpty = 919,
        PetbattleQueueSlotNoTracker = 920,
        PetbattleQueueSlotNoSpecies = 921,
        PetbattleQueueSlotCantBattle = 922,
        PetbattleQueueSlotRevoked = 923,
        PetbattleQueueSlotDead = 924,
        PetbattleQueueSlotNoPet = 925,
        PetbattleQueueNotWhileNeutral = 926,
        PetbattleGameTimeLimitWarning = 927,
        PetbattleGameRoundsLimitWarning = 928,
        HasRestriction = 929,
        ItemUpgradeItemTooLowLevel = 930,
        ItemUpgradeNoPath = 931,
        ItemUpgradeNoMoreUpgrades = 932,
        BonusRollEmpty = 933,
        ChallengeModeFull = 934,
        ChallengeModeInProgress = 935,
        ChallengeModeIncorrectKeystone = 936,
        BattletagFriendNotFound = 937,
        BattletagFriendNotValid = 938,
        BattletagFriendNotAllowed = 939,
        BattletagFriendThrottled = 940,
        BattletagFriendSuccess = 941,
        PetTooHighLevelToUncage = 942,
        PetbattleInternal = 943,
        CantCagePetYet = 944,
        NoLootInChallengeMode = 945,
        QuestPetBattleVictoriesPvpIi = 946,
        RoleCheckAlreadyInProgress = 947,
        RecruitAFriendAccountLimit = 948,
        RecruitAFriendFailed = 949,
        SetLootPersonal = 950,
        SetLootMethodFailedCombat = 951,
        ReagentBankFull = 952,
        ReagentBankLocked = 953,
        GarrisonBuildingExists = 954,
        GarrisonInvalidPlot = 955,
        GarrisonInvalidBuildingid = 956,
        GarrisonInvalidPlotBuilding = 957,
        GarrisonRequiresBlueprint = 958,
        GarrisonNotEnoughCurrency = 959,
        GarrisonNotEnoughGold = 960,
        GarrisonCompleteMissionWrongFollowerType = 961,
        AlreadyUsingLfgList = 962,
        RestrictedAccountLfgListTrial = 963,
        ToyUseLimitReached = 964,
        ToyAlreadyKnown = 965,
        TransmogSetAlreadyKnown = 966,
        NotEnoughCurrency = 967,
        SpecIsDisabled = 968,
        FeatureRestrictedTrial = 969,
        CantBeObliterated = 970,
        CantBeScrapped = 971,
        ArtifactRelicDoesNotMatchArtifact = 972,
        MustEquipArtifact = 973,
        CantDoThatRightNow = 974,
        AffectingCombat = 975,
        EquipmentManagerCombatSwapS = 976,
        EquipmentManagerBagsFull = 977,
        EquipmentManagerMissingItemS = 978,
        MovieRecordingWarningPerf = 979,
        MovieRecordingWarningDiskFull = 980,
        MovieRecordingWarningNoMovie = 981,
        MovieRecordingWarningRequirements = 982,
        MovieRecordingWarningCompressing = 983,
        NoChallengeModeReward = 984,
        ClaimedChallengeModeReward = 985,
        ChallengeModePeriodResetSs = 986,
        CantDoThatChallengeModeActive = 987,
        TalentFailedRestArea = 988,
        CannotAbandonLastPet = 989,
        TestCvarSetSss = 990,
        QuestTurnInFailReason = 991,
        ClaimedChallengeModeRewardOld = 992,
        TalentGrantedByAura = 993,
        ChallengeModeAlreadyComplete = 994,
        GlyphTargetNotAvailable = 995,
        PvpWarmodeToggleOn = 996,
        PvpWarmodeToggleOff = 997,
        SpellFailedLevelRequirement = 998,
        BattlegroundJoinRequiresLevel = 999,
        BattlegroundJoinDisqualified = 1000,
        BattlegroundJoinDisqualifiedNoName = 1001,
        VoiceChatGenericUnableToConnect = 1002,
        VoiceChatServiceLost = 1003,
        VoiceChatChannelNameTooShort = 1004,
        VoiceChatChannelNameTooLong = 1005,
        VoiceChatChannelAlreadyExists = 1006,
        VoiceChatTargetNotFound = 1007,
        VoiceChatTooManyRequests = 1008,
        VoiceChatPlayerSilenced = 1009,
        VoiceChatParentalDisableAll = 1010,
        VoiceChatDisabled = 1011,
        NoPvpReward = 1012,
        ClaimedPvpReward = 1013,
        AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1014,
        AzeriteEssenceSelectionFailedCantRemoveEssence = 1015,
        AzeriteEssenceSelectionFailedConditionFailed = 1016,
        AzeriteEssenceSelectionFailedRestArea = 1017,
        AzeriteEssenceSelectionFailedSlotLocked = 1018,
        AzeriteEssenceSelectionFailedNotAtForge = 1019,
        AzeriteEssenceSelectionFailedHeartLevelTooLow = 1020,
        AzeriteEssenceSelectionFailedNotEquipped = 1021,
        SocketingRequiresPunchcardredGem = 1022,
        SocketingPunchcardredGemOnlyInPunchcardredslot = 1023,
        SocketingRequiresPunchcardyellowGem = 1024,
        SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1025,
        SocketingRequiresPunchcardblueGem = 1026,
        SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1027,
        LevelLinkingResultLinked = 1028,
        LevelLinkingResultUnlinked = 1029,
        ClubFinderErrorPostClub = 1030,
        ClubFinderErrorApplyClub = 1031,
        ClubFinderErrorRespondApplicant = 1032,
        ClubFinderErrorCancelApplication = 1033,
        ClubFinderErrorTypeAcceptApplication = 1034,
        ClubFinderErrorTypeNoInvitePermissions = 1035,
        ClubFinderErrorTypeNoPostingPermissions = 1036,
        ClubFinderErrorTypeApplicantList = 1037,
        ClubFinderErrorTypeApplicantListNoPerm = 1038,
        ClubFinderErrorTypeFinderNotAvailable = 1039,
        ClubFinderErrorTypeGetPostingIds = 1040,
        ClubFinderErrorTypeJoinApplication = 1041,
        ClubFinderErrorTypeFlaggedRename = 1042,
        ClubFinderErrorTypeFlaggedDescriptionChange = 1043,
        ItemInteractionNotEnoughGold = 1044,
        ItemInteractionNotEnoughCurrency = 1045,
        CantUseProfanity = 1046,
        NotInPetBattle = 1047
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
