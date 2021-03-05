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

using Framework.Constants;
using Game.Achievements;
using Game.Chat;
using Game.DataStorage;
using Game.Garrisons;
using Game.Groups;
using Game.Mails;
using Game.Maps;
using Game.Misc;
using Game.Spells;
using System.Collections;
using System.Collections.Generic;
using Game.BattleGrounds;

namespace Game.Entities
{
    public partial class Player
    {
        public WorldSession GetSession() { return Session; }
        public PlayerSocial GetSocial() { return m_social; }

        //Gossip
        public PlayerMenu PlayerTalkClass;
        private PlayerSocial m_social;
        private List<Channel> m_channels = new List<Channel>();
        private List<ObjectGuid> WhisperList = new List<ObjectGuid>();
        public string autoReplyMsg;

        //Inventory
        private Dictionary<ulong, EquipmentSetInfo> _equipmentSets = new Dictionary<ulong, EquipmentSetInfo>();
        public List<ItemSetEffect> ItemSetEff = new List<ItemSetEffect>();
        private List<EnchantDuration> m_enchantDuration = new List<EnchantDuration>();
        private List<Item> m_itemDuration = new List<Item>();
        private List<ObjectGuid> m_itemSoulboundTradeable = new List<ObjectGuid>();
        private List<ObjectGuid> m_refundableItems = new List<ObjectGuid>();
        public List<Item> ItemUpdateQueue = new List<Item>();
        private VoidStorageItem[] _voidStorageItems = new VoidStorageItem[SharedConst.VoidStorageMaxSlot];
        private Item[] m_items = new Item[(int)PlayerSlots.Count];
        private uint m_WeaponProficiency;
        private uint m_ArmorProficiency;
        private uint m_currentBuybackSlot;
        private TradeData m_trade;

        //PVP
        private BgBattlegroundQueueID_Rec[] m_bgBattlegroundQueueID = new BgBattlegroundQueueID_Rec[SharedConst.MaxPlayerBGQueues];
        private BGData m_bgData;
        private bool m_IsBGRandomWinner;
        public PvPInfo pvpInfo;
        private uint m_ArenaTeamIdInvited;
        private long m_lastHonorUpdateTime;
        private uint m_contestedPvPTimer;
        private bool _usePvpItemLevels;

        //Groups/Raids
        private GroupReference m_group = new GroupReference();
        private GroupReference m_originalGroup = new GroupReference();
        private Group m_groupInvite;
        private GroupUpdateFlags m_groupUpdateMask;
        private bool m_bPassOnGroupLoot;
        private GroupUpdateCounter[] m_groupUpdateSequences = new GroupUpdateCounter[2];

        public Dictionary<Difficulty, Dictionary<uint, InstanceBind>> m_boundInstances = new Dictionary<Difficulty, Dictionary<uint, InstanceBind>>();
        private Dictionary<uint, long> _instanceResetTimes = new Dictionary<uint, long>();
        private uint _pendingBindId;
        private uint _pendingBindTimer;
        public bool m_InstanceValid;

        private Difficulty m_dungeonDifficulty;
        private Difficulty m_raidDifficulty;
        private Difficulty m_legacyRaidDifficulty;
        private Difficulty m_prevMapDifficulty;

        //Movement
        public PlayerTaxi m_taxi = new PlayerTaxi();
        public byte[] m_forced_speed_changes = new byte[(int)UnitMoveType.Max];
        public byte m_movementForceModMagnitudeChanges;
        private uint m_lastFallTime;
        private float m_lastFallZ;
        private WorldLocation teleportDest;
        private TeleportToOptions m_teleport_options;
        private bool mSemaphoreTeleport_Near;
        private bool mSemaphoreTeleport_Far;
        private PlayerDelayedOperations m_DelayedOperations;
        private bool m_bCanDelayTeleport;
        private bool m_bHasDelayedTeleport;

        private PlayerUnderwaterState m_MirrorTimerFlags;
        private PlayerUnderwaterState m_MirrorTimerFlagsLast;
        private bool m_isInWater;

        //Stats
        private uint m_baseSpellPower;
        private uint m_baseManaRegen;
        private uint m_baseHealthRegen;
        private int m_spellPenetrationItemMod;
        private uint m_lastPotionId;

        //Spell
        private Dictionary<uint, PlayerSpell> m_spells = new Dictionary<uint, PlayerSpell>();
        private Dictionary<uint, SkillStatusData> mSkillStatus = new Dictionary<uint, SkillStatusData>();
        private Dictionary<uint, PlayerCurrency> _currencyStorage = new Dictionary<uint, PlayerCurrency>();
        private List<SpellModifier>[][] m_spellMods = new List<SpellModifier>[(int)SpellModOp.Max][];
        private MultiMap<uint, uint> m_overrideSpells = new MultiMap<uint, uint>();
        public Spell m_spellModTakingSpell;
        private uint m_oldpetspell;

        //Mail
        private List<Mail> m_mail = new List<Mail>();
        private Dictionary<ulong, Item> mMitems = new Dictionary<ulong, Item>();
        public byte unReadMails;
        private long m_nextMailDelivereTime;
        public bool m_mailsLoaded;
        public bool m_mailsUpdated;

        //Pets
        public List<PetAura> m_petAuras = new List<PetAura>();
        public uint m_stableSlots;
        private uint m_temporaryUnsummonedPetNumber;
        private uint m_lastpetnumber;

        // Player summoning
        private long m_summon_expire;
        private WorldLocation m_summon_location;

        private RestMgr _restMgr;

        //Combat 
        private int[] baseRatingValue = new int[(int)CombatRating.Max];
        private float[] m_auraBaseFlatMod = new float[(int)BaseModGroup.End];
        private float[] m_auraBasePctMod = new float[(int)BaseModGroup.End];
        public DuelInfo duel;
        private bool m_canParry;
        private bool m_canBlock;
        private bool m_canTitanGrip;
        private uint m_titanGripPenaltySpellId;
        private uint m_deathTimer;
        private long m_deathExpireTime;
        private byte m_swingErrorMsg;
        private uint m_combatExitTime;
        private uint m_regenTimerCount;
        private uint m_weaponChangeTimer;

        //Quest
        private List<uint> m_timedquests = new List<uint>();
        private List<uint> m_weeklyquests = new List<uint>();
        private List<uint> m_monthlyquests = new List<uint>();
        private MultiMap<uint, uint> m_seasonalquests = new MultiMap<uint, uint>();
        private Dictionary<uint, QuestStatusData> m_QuestStatus = new Dictionary<uint, QuestStatusData>();
        private Dictionary<uint, QuestSaveType> m_QuestStatusSave = new Dictionary<uint, QuestSaveType>();
        private List<uint> m_DFQuests = new List<uint>();
        private List<uint> m_RewardedQuests = new List<uint>();
        private Dictionary<uint, QuestSaveType> m_RewardedQuestsSave = new Dictionary<uint, QuestSaveType>();

        private bool m_DailyQuestChanged;
        private bool m_WeeklyQuestChanged;
        private bool m_MonthlyQuestChanged;
        private bool m_SeasonalQuestChanged;
        private long m_lastDailyQuestTime;

        private Garrison _garrison;

        private CinematicManager _cinematicMgr;

        // variables to save health and mana before duel and restore them after duel
        private ulong healthBeforeDuel;
        private uint manaBeforeDuel;

        private bool _advancedCombatLoggingEnabled;

        private WorldLocation _corpseLocation;

        //Core
        private WorldSession Session;

        public PlayerData m_playerData;
        public ActivePlayerData m_activePlayerData;

        private uint m_nextSave;
        private byte m_cinematic;

        private uint m_movie;
        private bool m_customizationsChanged;

        private SpecializationInfo _specializationInfo;
        public List<ObjectGuid> m_clientGUIDs = new List<ObjectGuid>();
        public List<ObjectGuid> m_visibleTransports = new List<ObjectGuid>();
        public WorldObject seerView;
        // only changed for direct client control (possess, vehicle etc.), not stuff you control using pet commands
        public Unit m_unitMovedByMe;
        private Team m_team;
        public Stack<uint> m_timeSyncQueue = new Stack<uint>();
        private uint m_timeSyncTimer;
        public uint m_timeSyncClient;
        public uint m_timeSyncServer;
        private ReputationMgr reputationMgr;
        private QuestObjectiveCriteriaManager m_questObjectiveCriteriaMgr;
        public AtLoginFlags atLoginFlags;
        public bool m_itemUpdateQueueBlocked;

        private PlayerExtraFlags m_ExtraFlags;

        public bool IsDebugAreaTriggers { get; set; }
        private uint m_zoneUpdateId;
        private uint m_areaUpdateId;
        private uint m_zoneUpdateTimer;

        private uint m_ChampioningFaction;
        private byte m_fishingSteps;

        // Recall position
        private WorldLocation m_recall_location;
        private WorldLocation homebind;
        private uint homebindAreaId;
        private uint m_HomebindTimer;

        private ResurrectionData _resurrectionData;

        private PlayerAchievementMgr m_achievementSys;

        private SceneMgr m_sceneMgr;

        private Dictionary<ObjectGuid /*LootObject*/, ObjectGuid /*WorldObject*/> m_AELootView = new Dictionary<ObjectGuid, ObjectGuid>();

        private CUFProfile[] _CUFProfiles = new CUFProfile[PlayerConst.MaxCUFProfiles];
        private float[] m_powerFraction = new float[(int)PowerType.MaxPerClass];
        private int[] m_MirrorTimer = new int[3];

        private ulong m_GuildIdInvited;
        private DeclinedName _declinedname;
        private Runes m_runes = new Runes();
        private uint m_hostileReferenceCheckTimer;
        private uint m_drunkTimer;
        private long m_logintime;
        private long m_Last_tick;
        private uint m_PlayedTimeTotal;
        private uint m_PlayedTimeLevel;

        private Dictionary<byte, ActionButton> m_actionButtons = new Dictionary<byte, ActionButton>();
        private ObjectGuid m_playerSharingQuest;
        private uint m_sharedQuestId;
        private uint m_ingametime;

        private PlayerCommandStates _activeCheats;
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

        public List<PlayerCreateInfoItem> item = new List<PlayerCreateInfoItem>();
        public List<uint> customSpells = new List<uint>();
        public List<uint> castSpells = new List<uint>();
        public List<PlayerCreateInfoAction> action = new List<PlayerCreateInfoAction>();
        public List<SkillRaceClassInfoRecord> skills = new List<SkillRaceClassInfoRecord>();

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
        public ushort[] stats = new ushort[(int)Stats.Max];
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
            var foundRune = CooldownOrder.Contains(index);
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

        public List<byte> CooldownOrder = new List<byte>();
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
            var newData = action | ((ulong)type << 56);
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
        public WorldLocation Location = new WorldLocation();
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
        public List<uint> BonusListIDs = new List<uint>();
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
            public Array<ObjectGuid> Pieces = new Array<ObjectGuid>(EquipmentSlot.End);
            public Array<int> Appearances = new Array<int>(EquipmentSlot.End);  // ItemModifiedAppearanceID
            public Array<int> Enchants = new Array<int>(2);  // SpellItemEnchantmentID
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

        public List<ObjectGuid> bgAfkReporter = new List<ObjectGuid>();
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
            var array = new uint[1];
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

    internal struct GroupUpdateCounter
    {
        public ObjectGuid GroupGuid;
        public int UpdateSequenceNumber;
    }
}
