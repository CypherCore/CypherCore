﻿/*
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

using Framework.Constants;
using Game.Achievements;
using Game.BattleGrounds;
using Game.Chat;
using Game.DataStorage;
using Game.Garrisons;
using Game.Groups;
using Game.Mails;
using Game.Maps;
using Game.Misc;
using Game.Spells;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Game.Entities
{
    public partial class Player
    {
        public WorldSession GetSession() { return Session; }
        public PlayerSocial GetSocial() { return m_social; }

        //Gossip
        public PlayerMenu PlayerTalkClass;
        PlayerSocial m_social;
        List<Channel> m_channels = new();
        List<ObjectGuid> WhisperList = new();
        public string autoReplyMsg;

        //Inventory
        Dictionary<ulong, EquipmentSetInfo> _equipmentSets = new();
        public List<ItemSetEffect> ItemSetEff = new();
        List<EnchantDuration> m_enchantDuration = new();
        List<Item> m_itemDuration = new();
        List<ObjectGuid> m_itemSoulboundTradeable = new();
        List<ObjectGuid> m_refundableItems = new();
        public List<Item> ItemUpdateQueue = new();
        VoidStorageItem[] _voidStorageItems = new VoidStorageItem[SharedConst.VoidStorageMaxSlot];
        Item[] m_items = new Item[(int)PlayerSlots.Count];
        uint m_WeaponProficiency;
        uint m_ArmorProficiency;
        uint m_currentBuybackSlot;
        TradeData m_trade;

        //PVP
        BgBattlegroundQueueID_Rec[] m_bgBattlegroundQueueID = new BgBattlegroundQueueID_Rec[SharedConst.MaxPlayerBGQueues];
        BGData m_bgData;
        bool m_IsBGRandomWinner;
        public PvPInfo pvpInfo;
        uint m_ArenaTeamIdInvited;
        long m_lastHonorUpdateTime;
        uint m_contestedPvPTimer;
        bool _usePvpItemLevels;

        //Groups/Raids
        GroupReference m_group = new();
        GroupReference m_originalGroup = new();
        Group m_groupInvite;
        GroupUpdateFlags m_groupUpdateMask;
        bool m_bPassOnGroupLoot;
        GroupUpdateCounter[] m_groupUpdateSequences = new GroupUpdateCounter[2];

        public Dictionary<Difficulty, Dictionary<uint, InstanceBind>> m_boundInstances = new();
        Dictionary<uint, long> _instanceResetTimes = new();
        uint _pendingBindId;
        uint _pendingBindTimer;
        public bool m_InstanceValid;

        Difficulty m_dungeonDifficulty;
        Difficulty m_raidDifficulty;
        Difficulty m_legacyRaidDifficulty;
        Difficulty m_prevMapDifficulty;

        //Movement
        public PlayerTaxi m_taxi = new();
        public byte[] m_forced_speed_changes = new byte[(int)UnitMoveType.Max];
        public byte m_movementForceModMagnitudeChanges;
        uint m_lastFallTime;
        float m_lastFallZ;
        WorldLocation teleportDest;
        TeleportToOptions m_teleport_options;
        bool mSemaphoreTeleport_Near;
        bool mSemaphoreTeleport_Far;
        PlayerDelayedOperations m_DelayedOperations;
        bool m_bCanDelayTeleport;
        bool m_bHasDelayedTeleport;

        PlayerUnderwaterState m_MirrorTimerFlags;
        PlayerUnderwaterState m_MirrorTimerFlagsLast;
        bool m_isInWater;

        //Stats
        uint m_baseSpellPower;
        uint m_baseManaRegen;
        uint m_baseHealthRegen;
        int m_spellPenetrationItemMod;
        uint m_lastPotionId;

        //Spell
        Dictionary<uint, PlayerSpell> m_spells = new();
        Dictionary<uint, SkillStatusData> mSkillStatus = new();
        Dictionary<uint, PlayerCurrency> _currencyStorage = new();
        List<SpellModifier>[][] m_spellMods = new List<SpellModifier>[(int)SpellModOp.Max][];
        MultiMap<uint, uint> m_overrideSpells = new();
        public Spell m_spellModTakingSpell;
        uint m_oldpetspell;
        Dictionary<uint, StoredAuraTeleportLocation> m_storedAuraTeleportLocations = new();

        //Mail
        List<Mail> m_mail = new();
        Dictionary<ulong, Item> mMitems = new();
        public byte unReadMails;
        long m_nextMailDelivereTime;
        public bool m_mailsLoaded;
        public bool m_mailsUpdated;

        //Pets
        public List<PetAura> m_petAuras = new();
        public uint m_stableSlots;
        uint m_temporaryUnsummonedPetNumber;
        uint m_lastpetnumber;

        // Player summoning
        long m_summon_expire;
        WorldLocation m_summon_location;

        RestMgr _restMgr;

        //Combat
        int[] baseRatingValue = new int[(int)CombatRating.Max];
        float[] m_auraBaseFlatMod = new float[(int)BaseModGroup.End];
        float[] m_auraBasePctMod = new float[(int)BaseModGroup.End];
        public DuelInfo duel;
        bool m_canParry;
        bool m_canBlock;
        bool m_canTitanGrip;
        uint m_titanGripPenaltySpellId;
        uint m_deathTimer;
        long m_deathExpireTime;
        byte m_swingErrorMsg;
        uint m_combatExitTime;
        uint m_regenTimerCount;
        uint m_weaponChangeTimer;

        //Quest
        List<uint> m_timedquests = new();
        List<uint> m_weeklyquests = new();
        List<uint> m_monthlyquests = new();
        MultiMap<uint, uint> m_seasonalquests = new();
        Dictionary<uint, QuestStatusData> m_QuestStatus = new();
        MultiMap<(QuestObjectiveType Type, int ObjectID), QuestObjectiveStatusData> m_questObjectiveStatus = new();
        Dictionary<uint, QuestSaveType> m_QuestStatusSave = new();
        List<uint> m_DFQuests = new();
        List<uint> m_RewardedQuests = new();
        Dictionary<uint, QuestSaveType> m_RewardedQuestsSave = new();

        bool m_DailyQuestChanged;
        bool m_WeeklyQuestChanged;
        bool m_MonthlyQuestChanged;
        bool m_SeasonalQuestChanged;
        long m_lastDailyQuestTime;

        Garrison _garrison;

        CinematicManager _cinematicMgr;

        // variables to save health and mana before duel and restore them after duel
        ulong healthBeforeDuel;
        uint manaBeforeDuel;

        bool _advancedCombatLoggingEnabled;

        WorldLocation _corpseLocation;

        //Core
        WorldSession Session;

        public PlayerData m_playerData;
        public ActivePlayerData m_activePlayerData;

        uint m_nextSave;
        byte m_cinematic;

        uint m_movie;
        bool m_customizationsChanged;

        SpecializationInfo _specializationInfo;
        public List<ObjectGuid> m_clientGUIDs = new();
        public List<ObjectGuid> m_visibleTransports = new();
        public WorldObject seerView;
        // only changed for direct client control (possess, vehicle etc.), not stuff you control using pet commands
        public Unit m_unitMovedByMe;
        Team m_team;
        public Stack<uint> m_timeSyncQueue = new();
        uint m_timeSyncTimer;
        public uint m_timeSyncClient;
        public uint m_timeSyncServer;
        ReputationMgr reputationMgr;
        QuestObjectiveCriteriaManager m_questObjectiveCriteriaMgr;
        public AtLoginFlags atLoginFlags;
        public bool m_itemUpdateQueueBlocked;

        PlayerExtraFlags m_ExtraFlags;

        public bool IsDebugAreaTriggers { get; set; }
        uint m_zoneUpdateId;
        uint m_areaUpdateId;
        uint m_zoneUpdateTimer;

        uint m_ChampioningFaction;
        byte m_fishingSteps;

        // Recall position
        WorldLocation m_recall_location;
        WorldLocation homebind;
        uint homebindAreaId;
        uint m_HomebindTimer;

        ResurrectionData _resurrectionData;

        PlayerAchievementMgr m_achievementSys;

        SceneMgr m_sceneMgr;

        Dictionary<ObjectGuid /*LootObject*/, ObjectGuid /*WorldObject*/> m_AELootView = new();

        CUFProfile[] _CUFProfiles = new CUFProfile[PlayerConst.MaxCUFProfiles];
        float[] m_powerFraction = new float[(int)PowerType.MaxPerClass];
        int[] m_MirrorTimer = new int[3];

        ulong m_GuildIdInvited;
        DeclinedName _declinedname;
        Runes m_runes = new();
        uint m_hostileReferenceCheckTimer;
        uint m_drunkTimer;
        long m_logintime;
        long m_Last_tick;
        uint m_PlayedTimeTotal;
        uint m_PlayedTimeLevel;

        Dictionary<byte, ActionButton> m_actionButtons = new();
        ObjectGuid m_playerSharingQuest;
        uint m_sharedQuestId;
        uint m_ingametime;

        PlayerCommandStates _activeCheats;
    }

    public class PlayerInfo
    {
        public uint MapId;
        public uint ZoneId;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float Orientation;

        public uint DisplayId_m;
        public uint DisplayId_f;

        public List<PlayerCreateInfoItem> item = new();
        public List<uint> customSpells = new();
        public List<uint> castSpells = new();
        public List<PlayerCreateInfoAction> action = new();
        public List<SkillRaceClassInfoRecord> skills = new();

        public PlayerLevelInfo[] levelInfo = new PlayerLevelInfo[WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel)];
    }

    public class PlayerCreateInfoItem
    {
        public PlayerCreateInfoItem(uint id, uint amount)
        {
            item_id = id;
            item_amount = amount;
        }

        public uint item_id;
        public uint item_amount;
    }

    public class PlayerCreateInfoAction
    {
        public PlayerCreateInfoAction() : this(0, 0, 0) { }
        public PlayerCreateInfoAction(byte _button, uint _action, byte _type)
        {
            button = _button;
            type = _type;
            action = _action;
        }

        public byte button;
        public byte type;
        public uint action;
    }

    public class PlayerLevelInfo
    {
        public int[] stats = new int[(int)Stats.Max];
    }

    public class PlayerCurrency
    {
        public PlayerCurrencyState state;
        public uint Quantity;
        public uint WeeklyQuantity;
        public uint TrackedQuantity;
        public byte Flags;
    }

    public class SpecializationInfo
    {
        public SpecializationInfo()
        {
            for (byte i = 0; i < PlayerConst.MaxSpecializations; ++i)
            {
                Talents[i] = new Dictionary<uint, PlayerSpellState>();
                PvpTalents[i] = new uint[PlayerConst.MaxPvpTalentSlots];
                Glyphs[i] = new List<uint>();
            }
        }

        public Dictionary<uint, PlayerSpellState>[] Talents = new Dictionary<uint, PlayerSpellState>[PlayerConst.MaxSpecializations];
        public uint[][] PvpTalents = new uint[PlayerConst.MaxSpecializations][];
        public List<uint>[] Glyphs = new List<uint>[PlayerConst.MaxSpecializations];
        public uint ResetTalentsCost;
        public long ResetTalentsTime;
        public byte ActiveGroup;
    }

    public class Runes
    {
        public void SetRuneState(byte index, bool set = true)
        {
            bool foundRune = CooldownOrder.Contains(index);
            if (set)
            {
                RuneState |= (byte)(1 << index);                      // usable
                if (foundRune)
                    CooldownOrder.Remove(index);
            }
            else
            {
                RuneState &= (byte)~(1 << index);                     // on cooldown
                if (!foundRune)
                    CooldownOrder.Add(index);
            }
        }

        public List<byte> CooldownOrder = new();
        public uint[] Cooldown = new uint[PlayerConst.MaxRunes];
        public byte RuneState;                                        // mask of available runes
    }

    public class ActionButton
    {
        public ActionButton()
        {
            packedData = 0;
            uState = ActionButtonUpdateState.New;
        }

        public ActionButtonType GetButtonType() { return (ActionButtonType)((packedData & 0xFFFFFFFF00000000) >> 56); }
        public uint GetAction() { return (uint)(packedData & 0x00000000FFFFFFFF); }
        public void SetActionAndType(ulong action, ActionButtonType type)
        {
            ulong newData = action | ((ulong)type << 56);
            if (newData != packedData || uState == ActionButtonUpdateState.Deleted)
            {
                packedData = newData;
                if (uState != ActionButtonUpdateState.New)
                    uState = ActionButtonUpdateState.Changed;
            }
        }

        public ulong packedData;
        public ActionButtonUpdateState uState;
    }

    public class ResurrectionData
    {
        public ObjectGuid GUID;
        public WorldLocation Location = new();
        public uint Health;
        public uint Mana;
        public uint Aura;
    }

    public struct PvPInfo
    {
        public bool IsHostile;
        public bool IsInHostileArea;               //> Marks if player is in an area which forces PvP flag
        public bool IsInNoPvPArea;                 //> Marks if player is in a sanctuary or friendly capital city
        public bool IsInFFAPvPArea;                //> Marks if player is in an FFAPvP area (such as Gurubashi Arena)
        public long EndTimer;                    //> Time when player unflags himself for PvP (flag removed after 5 minutes)
    }

    public class DuelInfo
    {
        public Player initiator;
        public Player opponent;
        public long startTimer;
        public long startTime;
        public long outOfBound;
        public bool isMounted;
        public bool isCompleted;

        public bool IsDueling() { return opponent != null; }
    }

    public class AccessRequirement
    {
        public byte levelMin;
        public byte levelMax;
        public uint item;
        public uint item2;
        public uint quest_A;
        public uint quest_H;
        public uint achievement;
        public string questFailedText;
    }

    public class EnchantDuration
    {
        public EnchantDuration(Item _item = null, EnchantmentSlot _slot = EnchantmentSlot.Max, uint _leftduration = 0)
        {
            item = _item;
            slot = _slot;
            leftduration = _leftduration;
        }

        public Item item;
        public EnchantmentSlot slot;
        public uint leftduration;
    }

    public class VoidStorageItem
    {
        public VoidStorageItem(ulong id, uint entry, ObjectGuid creator, uint randomBonusListId, uint fixedScalingLevel, uint artifactKnowledgeLevel, ItemContext context, List<uint> bonuses)
        {
            ItemId = id;
            ItemEntry = entry;
            CreatorGuid = creator;
            RandomBonusListId = randomBonusListId;
            FixedScalingLevel = fixedScalingLevel;
            ArtifactKnowledgeLevel = artifactKnowledgeLevel;
            Context = context;

            foreach (var value in bonuses)
                BonusListIDs.Add(value);
        }

        public ulong ItemId;
        public uint ItemEntry;
        public ObjectGuid CreatorGuid;
        public uint RandomBonusListId;
        public uint FixedScalingLevel;
        public uint ArtifactKnowledgeLevel;
        public ItemContext Context;
        public List<uint> BonusListIDs = new();
    }

    public class EquipmentSetInfo
    {
        public EquipmentSetInfo()
        {
            state = EquipmentSetUpdateState.New;
            Data = new EquipmentSetData();
        }

        public EquipmentSetUpdateState state;
        public EquipmentSetData Data;

        // Data sent in EquipmentSet related packets
        public class EquipmentSetData
        {
            public EquipmentSetType Type;
            public ulong Guid; // Set Identifier
            public uint SetID; // Index
            public uint IgnoreMask ; // Mask of EquipmentSlot
            public int AssignedSpecIndex = -1; // Index of character specialization that this set is automatically equipped for
            public string SetName = "";
            public string SetIcon = "";
            public ObjectGuid[] Pieces = new ObjectGuid[EquipmentSlot.End];
            public int[] Appearances = new int[EquipmentSlot.End];  // ItemModifiedAppearanceID
            public int[] Enchants = new int[2];  // SpellItemEnchantmentID
            public int Unknown901_1;
            public int Unknown901_2;
        }

        public enum EquipmentSetType
        {
            Equipment = 0,
            Transmog = 1
        }
    }

    public class BgBattlegroundQueueID_Rec
    {
        public BattlegroundQueueTypeId bgQueueTypeId;
        public uint invitedToInstance;
        public uint joinTime;
    }

    // Holder for Battlegrounddata
    public class BGData
    {
        public BGData()
        {
            bgTypeID = BattlegroundTypeId.None;
            ClearTaxiPath();
            joinPos = new WorldLocation();
        }

        public uint bgInstanceID;                    //< This variable is set to bg.m_InstanceID,
        //  when player is teleported to BG - (it is Battleground's GUID)
        public BattlegroundTypeId bgTypeID;

        public List<ObjectGuid> bgAfkReporter = new();
        public byte bgAfkReportedCount;
        public long bgAfkReportedTimer;

        public uint bgTeam;                          //< What side the player will be added to

        public uint mountSpell;
        public uint[] taxiPath = new uint[2];

        public WorldLocation joinPos;                  //< From where player entered BG

        public void ClearTaxiPath() { taxiPath[0] = taxiPath[1] = 0; }
        public bool HasTaxiPath() { return taxiPath[0] != 0 && taxiPath[1] != 0; }
    }

    public class CUFProfile
    {
        public CUFProfile()
        {
            BoolOptions = new BitSet((int)CUFBoolOptions.BoolOptionsCount);
        }

        public CUFProfile(string name, ushort frameHeight, ushort frameWidth, byte sortBy, byte healthText, uint boolOptions,
            byte topPoint, byte bottomPoint, byte leftPoint, ushort topOffset, ushort bottomOffset, ushort leftOffset)
        {
            ProfileName = name;
            BoolOptions = new BitSet(new uint[] { boolOptions });

            FrameHeight = frameHeight;
            FrameWidth = frameWidth;
            SortBy = sortBy;
            HealthText = healthText;
            TopPoint = topPoint;
            BottomPoint = bottomPoint;
            LeftPoint = leftPoint;
            TopOffset = topOffset;
            BottomOffset = bottomOffset;
            LeftOffset = leftOffset;
        }

        public void SetOption(CUFBoolOptions opt, byte arg)
        {
            BoolOptions.Set((int)opt, arg != 0);
        }
        public bool GetOption(CUFBoolOptions opt)
        {
            return BoolOptions.Get((int)opt);
        }
        public ulong GetUlongOptionValue()
        {
            uint[] array = new uint[1];
            BoolOptions.CopyTo(array, 0);
            return (ulong)array[0];
        }

        public string ProfileName;
        public ushort FrameHeight;
        public ushort FrameWidth;
        public byte SortBy;
        public byte HealthText;

        // LeftAlign, TopAlight, BottomAlign
        public byte TopPoint;
        public byte BottomPoint;
        public byte LeftPoint;

        // LeftOffset, TopOffset and BottomOffset
        public ushort TopOffset;
        public ushort BottomOffset;
        public ushort LeftOffset;

        public BitSet BoolOptions;

        // More fields can be added to BoolOptions without changing DB schema (up to 32, currently 27)
    }

    struct GroupUpdateCounter
    {
        public ObjectGuid GroupGuid;
        public int UpdateSequenceNumber;
    }

    class StoredAuraTeleportLocation
    {
        public WorldLocation Loc;
        public State CurrentState;

        public enum State
        {
            Unchanged,
            Changed,
            Deleted
        }
    }

    struct QuestObjectiveStatusData
    {
        public (uint QuestID, QuestStatusData Status) QuestStatusPair;
        public QuestObjective Objective;
    }
}
