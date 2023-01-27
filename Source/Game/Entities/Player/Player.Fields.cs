// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using Framework.Constants;
using Game.Achievements;
using Game.BattleGrounds;
using Game.Chat;
using Game.DataStorage;
using Game.Garrisons;
using Game.Groups;
using Game.Loots;
using Game.Mails;
using Game.Misc;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Entities
{
	public partial class Player
	{
		private PlayerAchievementMgr _achievementSys;

		private Dictionary<byte, ActionButton> _actionButtons = new();

		private PlayerCommandStates _activeCheats;
		public ActivePlayerData _activePlayerData;

		private bool _advancedCombatLoggingEnabled;

		private Dictionary<ObjectGuid, Loot> _AELootView = new();
		private uint _areaUpdateId;
		private uint _ArenaTeamIdInvited;
		private uint _ArmorProficiency;
		private float[] _auraBaseFlatMod = new float[(int)BaseModGroup.End];
		private float[] _auraBasePctMod = new float[(int)BaseModGroup.End];
		private uint _baseHealthRegen;
		private uint _baseManaRegen;

		//Stats
		private uint _baseSpellPower;
		private bool _bCanDelayTeleport;

		//PVP
		private BgBattlegroundQueueID_Rec[] _bgBattlegroundQueueID = new BgBattlegroundQueueID_Rec[SharedConst.MaxPlayerBGQueues];
		private BGData _bgData;
		private bool _bHasDelayedTeleport;
		private bool _bPassOnGroupLoot;
		private bool _canBlock;
		private bool _canParry;
		private bool _canTitanGrip;

		private uint _ChampioningFaction;
		private List<Channel> _channels = new();
		private byte _cinematic;

		private CinematicManager _cinematicMgr;
		public List<ObjectGuid> _clientGUIDs = new();
		private uint _combatExitTime;
		private uint _contestedPvPTimer;

		private WorldLocation _corpseLocation;
		private PlayerCreateMode _createMode;

		private long _createTime;

		private CUFProfile[] _CUFProfiles = new CUFProfile[PlayerConst.MaxCUFProfiles];
		private Dictionary<uint, PlayerCurrency> _currencyStorage = new();
		private uint _currentBuybackSlot;
		private bool _customizationsChanged;

		private bool _DailyQuestChanged;
		private long _deathExpireTime;
		private uint _deathTimer;
		private DeclinedName _declinedname;
		private PlayerDelayedOperations _DelayedOperations;
		private List<uint> _DFQuests = new();
		private uint _drunkTimer;

		private Difficulty _dungeonDifficulty;
		private List<EnchantDuration> _enchantDuration = new();

		//Inventory
		private Dictionary<ulong, EquipmentSetInfo> _equipmentSets = new();

		private PlayerExtraFlags _ExtraFlags;
		private byte _fishingSteps;
		private uint _foodEmoteTimerCount;
		public byte[] _forced_speed_changes = new byte[(int)UnitMoveType.Max];

		private Garrison _garrison;

		//Groups/Raids
		private GroupReference _group = new();
		private Group _groupInvite;
		private GroupUpdateFlags _groupUpdateMask;
		private GroupUpdateCounter[] _groupUpdateSequences = new GroupUpdateCounter[2];

		private TimeTracker _groupUpdateTimer;

		private ulong _GuildIdInvited;
		private uint _HomebindTimer;
		private uint _hostileReferenceCheckTimer;
		private uint _ingametime;
		private Dictionary<uint, long> _instanceResetTimes = new();
		public bool _InstanceValid;
		private bool _IsBGRandomWinner;
		private List<Item> _itemDuration = new();
		private Item[] _items = new Item[(int)PlayerSlots.Count];
		private List<ObjectGuid> _itemSoulboundTradeable = new();
		public bool _itemUpdateQueueBlocked;
		private long _Last_tick;
		private long _lastDailyQuestTime;
		private uint _lastFallTime;
		private float _lastFallZ;
		private long _lastHonorUpdateTime;
		private uint _lastpetnumber;
		private uint _lastPotionId;
		private Difficulty _legacyRaidDifficulty;
		private long _logintime;
		private List<LootRoll> _lootRolls = new(); // loot rolls waiting for answer

		//Mail
		private List<Mail> _mail = new();
		public bool _mailsUpdated;
		private int[] _MirrorTimer = new int[3];

		private PlayerUnderwaterState _MirrorTimerFlags;
		private PlayerUnderwaterState _MirrorTimerFlagsLast;
		private bool _MonthlyQuestChanged;
		private List<uint> _monthlyquests = new();
		public byte _movementForceModMagnitudeChanges;

		private uint _movie;
		private long _nextMailDelivereTime;

		private uint _nextSave;
		private uint _oldpetspell;
		private GroupReference _originalGroup = new();
		private MultiMap<uint, uint> _overrideSpells = new();
		private uint _pendingBindId;
		private uint _pendingBindTimer;
		public List<PetAura> _petAuras = new();

		//Pets
		private PetStable _petStable;
		private uint _PlayedTimeLevel;
		private uint _PlayedTimeTotal;

		public PlayerData _playerData;
		private ObjectGuid _playerSharingQuest;
		private float[] _powerFraction = new float[(int)PowerType.MaxPerClass];
		private QuestObjectiveCriteriaManager _questObjectiveCriteriaMgr;
		private MultiMap<(QuestObjectiveType Type, int ObjectID), QuestObjectiveStatusData> _questObjectiveStatus = new();
		private Dictionary<uint, QuestStatusData> _QuestStatus = new();
		private Dictionary<uint, QuestSaveType> _QuestStatusSave = new();
		private Difficulty _raidDifficulty;
		private uint _recall_instanceId;

		// Recall position
		private WorldLocation _recall_location;

		private Dictionary<uint, uint> _recentInstances = new();
		private List<ObjectGuid> _refundableItems = new();
		private uint _regenTimerCount;

		private RestMgr _restMgr;

		private ResurrectionData _resurrectionData;
		private List<uint> _RewardedQuests = new();
		private Dictionary<uint, QuestSaveType> _RewardedQuestsSave = new();
		private Runes _runes = new();

		private SceneMgr _sceneMgr;
		private bool _SeasonalQuestChanged;
		private Dictionary<uint, Dictionary<uint, long>> _seasonalquests = new();
		private uint _sharedQuestId;
		private PlayerSocial _social;

		private SpecializationInfo _specializationInfo;
		private List<SpellModifier>[][] _spellMods = new List<SpellModifier>[(int)SpellModOp.Max][];
		public Spell _spellModTakingSpell;
		private int _spellPenetrationItemMod;

		//Spell
		private Dictionary<uint, PlayerSpell> _spells = new();
		private Dictionary<uint, StoredAuraTeleportLocation> _storedAuraTeleportLocations = new();

		// Player summoning
		private long _summon_expire;
		private uint _summon_instanceId;
		private WorldLocation _summon_location;
		private byte _swingErrorMsg;

		//Movement
		public PlayerTaxi _taxi = new();
		private Team _team;
		private uint? _teleport_instanceId;
		private TeleportToOptions _teleport_options;
		private uint _temporaryUnsummonedPetNumber;

		//Quest
		private List<uint> _timedquests = new();
		private uint _titanGripPenaltySpellId;
		private TradeData _trade;

		private Dictionary<int, PlayerSpellState> _traitConfigStates = new();
		private bool _usePvpItemLevels;
		public List<ObjectGuid> _visibleTransports = new();
		private VoidStorageItem[] _voidStorageItems = new VoidStorageItem[SharedConst.VoidStorageMaxSlot];
		private uint _weaponChangeTimer;
		private uint _WeaponProficiency;
		private bool _WeeklyQuestChanged;
		private List<uint> _weeklyquests = new();
		private uint _zoneUpdateId;
		private uint _zoneUpdateTimer;
		public AtLoginFlags atLoginFlags;
		public string autoReplyMsg;

		//Combat
		private int[] baseRatingValue = new int[(int)CombatRating.Max];
		public DuelInfo duel;

		// variables to save health and mana before duel and restore them after duel
		private ulong healthBeforeDuel;
		private WorldLocation homebind = new();
		private uint homebindAreaId;
		public List<ItemSetEffect> ItemSetEff = new();
		public List<Item> ItemUpdateQueue = new();
		private uint manaBeforeDuel;
		private Dictionary<ulong, Item> mMitems = new();
		private bool mSemaphoreTeleport_Far;
		private bool mSemaphoreTeleport_Near;
		private Dictionary<uint, SkillStatusData> mSkillStatus = new();

		//Gossip
		public PlayerMenu PlayerTalkClass;
		public PvPInfo pvpInfo;
		private ReputationMgr reputationMgr;
		public WorldObject seerView;

		//Core
		private WorldSession Session;
		private WorldLocation teleportDest;
		public byte unReadMails;
		private List<ObjectGuid> WhisperList = new();

		public bool IsDebugAreaTriggers { get; set; }

		public WorldSession GetSession()
		{
			return Session;
		}

		public PlayerSocial GetSocial()
		{
			return _social;
		}

		private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
		{
			private ActivePlayerData ActivePlayerMask = new();
			private ObjectFieldData ObjectMask = new();
			private Player Owner;
			private PlayerData PlayerMask = new();
			private UnitData UnitMask = new();

			public ValuesUpdateForPlayerWithMaskSender(Player owner)
			{
				Owner = owner;
			}

			public void Invoke(Player player)
			{
				UpdateData udata = new(Owner.GetMapId());

				Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), UnitMask.GetUpdateMask(), PlayerMask.GetUpdateMask(), ActivePlayerMask.GetUpdateMask(), player);

				udata.BuildPacket(out UpdateObject packet);
				player.SendPacket(packet);
			}
		}
	}

	public class PlayerInfo
	{
		public List<PlayerCreateInfoAction> action = new();
		public List<uint>[] castSpells = new List<uint>[(int)PlayerCreateMode.Max];
		public CreatePosition createPosition;
		public CreatePosition? createPositionNPE;
		public List<uint> customSpells = new();

		public uint? introMovieId;
		public uint? introSceneId;
		public uint? introSceneIdNPE;
		public List<PlayerCreateInfoItem> item = new();

		public ItemContext itemContext;

		public PlayerLevelInfo[] levelInfo = new PlayerLevelInfo[WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel)];
		public List<SkillRaceClassInfoRecord> skills = new();

		public PlayerInfo()
		{
			for (var i = 0; i < castSpells.Length; ++i)
				castSpells[i] = new List<uint>();
		}

		public struct CreatePosition
		{
			public WorldLocation Loc;
			public ulong? TransportGuid;
		}
	}

	public class PlayerCreateInfoItem
	{
		public uint item_amount;

		public uint item_id;

		public PlayerCreateInfoItem(uint id, uint amount)
		{
			item_id     = id;
			item_amount = amount;
		}
	}

	public class PlayerCreateInfoAction
	{
		public uint action;

		public byte button;
		public byte type;

		public PlayerCreateInfoAction() : this(0, 0, 0)
		{
		}

		public PlayerCreateInfoAction(byte _button, uint _action, byte _type)
		{
			button = _button;
			type   = _type;
			action = _action;
		}
	}

	public class PlayerLevelInfo
	{
		public int[] stats = new int[(int)Stats.Max];
	}

	public class PlayerCurrency
	{
		public byte Flags;
		public uint Quantity;
		public PlayerCurrencyState state;
		public uint TrackedQuantity;
		public uint WeeklyQuantity;
	}

	public class SpecializationInfo
	{
		public byte ActiveGroup;
		public List<uint>[] Glyphs = new List<uint>[PlayerConst.MaxSpecializations];
		public uint[][] PvpTalents = new uint[PlayerConst.MaxSpecializations][];
		public uint ResetTalentsCost;
		public long ResetTalentsTime;

		public Dictionary<uint, PlayerSpellState>[] Talents = new Dictionary<uint, PlayerSpellState>[PlayerConst.MaxSpecializations];

		public SpecializationInfo()
		{
			for (byte i = 0; i < PlayerConst.MaxSpecializations; ++i)
			{
				Talents[i]    = new Dictionary<uint, PlayerSpellState>();
				PvpTalents[i] = new uint[PlayerConst.MaxPvpTalentSlots];
				Glyphs[i]     = new List<uint>();
			}
		}
	}

	public class Runes
	{
		public uint[] Cooldown = new uint[PlayerConst.MaxRunes];

		public List<byte> CooldownOrder = new();
		public byte RuneState; // mask of available runes

		public void SetRuneState(byte index, bool set = true)
		{
			bool foundRune = CooldownOrder.Contains(index);

			if (set)
			{
				RuneState |= (byte)(1 << index); // usable

				if (foundRune)
					CooldownOrder.Remove(index);
			}
			else
			{
				RuneState &= (byte)~(1 << index); // on cooldown

				if (!foundRune)
					CooldownOrder.Add(index);
			}
		}
	}

	public class ActionButton
	{
		public ulong packedData;
		public ActionButtonUpdateState uState;

		public ActionButton()
		{
			packedData = 0;
			uState     = ActionButtonUpdateState.New;
		}

		public ActionButtonType GetButtonType()
		{
			return (ActionButtonType)((packedData & 0xFF00000000000000) >> 56);
		}

		public ulong GetAction()
		{
			return (packedData & 0x00FFFFFFFFFFFFFF);
		}

		public void SetActionAndType(ulong action, ActionButtonType type)
		{
			ulong newData = action | ((ulong)type << 56);

			if (newData != packedData ||
			    uState == ActionButtonUpdateState.Deleted)
			{
				packedData = newData;

				if (uState != ActionButtonUpdateState.New)
					uState = ActionButtonUpdateState.Changed;
			}
		}
	}

	public class ResurrectionData
	{
		public uint Aura;
		public ObjectGuid GUID;
		public uint Health;
		public WorldLocation Location = new();
		public uint Mana;
	}

	public struct PvPInfo
	{
		public bool IsHostile;
		public bool IsInHostileArea; //> Marks if player is in an area which forces PvP flag
		public bool IsInNoPvPArea;   //> Marks if player is in a sanctuary or friendly capital city
		public bool IsInFFAPvPArea;  //> Marks if player is in an FFAPvP area (such as Gurubashi Arena)
		public long EndTimer;        //> Time when player unflags himself for PvP (flag removed after 5 minutes)
	}

	public class DuelInfo
	{
		public Player Initiator;
		public bool IsMounted;
		public Player Opponent;
		public long OutOfBoundsTime;
		public long StartTime;
		public DuelState State;

		public DuelInfo(Player opponent, Player initiator, bool isMounted)
		{
			Opponent  = opponent;
			Initiator = initiator;
			IsMounted = isMounted;
		}
	}

	public class AccessRequirement
	{
		public uint achievement;
		public uint item;
		public uint item2;
		public byte levelMax;
		public byte levelMin;
		public uint quest_A;
		public uint quest_H;
		public string questFailedText;
	}

	public class EnchantDuration
	{
		public Item item;
		public uint leftduration;
		public EnchantmentSlot slot;

		public EnchantDuration(Item _item = null, EnchantmentSlot _slot = EnchantmentSlot.Max, uint _leftduration = 0)
		{
			item         = _item;
			slot         = _slot;
			leftduration = _leftduration;
		}
	}

	public class VoidStorageItem
	{
		public uint ArtifactKnowledgeLevel;
		public List<uint> BonusListIDs = new();
		public ItemContext Context;
		public ObjectGuid CreatorGuid;
		public uint FixedScalingLevel;
		public uint ItemEntry;

		public ulong ItemId;
		public uint RandomBonusListId;

		public VoidStorageItem(ulong id, uint entry, ObjectGuid creator, uint randomBonusListId, uint fixedScalingLevel, uint artifactKnowledgeLevel, ItemContext context, List<uint> bonuses)
		{
			ItemId                 = id;
			ItemEntry              = entry;
			CreatorGuid            = creator;
			RandomBonusListId      = randomBonusListId;
			FixedScalingLevel      = fixedScalingLevel;
			ArtifactKnowledgeLevel = artifactKnowledgeLevel;
			Context                = context;

			foreach (var value in bonuses)
				BonusListIDs.Add(value);
		}
	}

	public class EquipmentSetInfo
	{
		public enum EquipmentSetType
		{
			Equipment = 0,
			Transmog = 1
		}

		public EquipmentSetData Data;

		public EquipmentSetUpdateState state;

		public EquipmentSetInfo()
		{
			state = EquipmentSetUpdateState.New;
			Data  = new EquipmentSetData();
		}

		// Data sent in EquipmentSet related packets
		public class EquipmentSetData
		{
			public int[] Appearances = new int[EquipmentSlot.End]; // ItemModifiedAppearanceID
			public int AssignedSpecIndex = -1;                     // Index of character specialization that this set is automatically equipped for
			public int[] Enchants = new int[2];                    // SpellItemEnchantmentID
			public ulong Guid;                                     // Set Identifier
			public uint IgnoreMask;                                // Mask of EquipmentSlot
			public ObjectGuid[] Pieces = new ObjectGuid[EquipmentSlot.End];
			public int SecondaryShoulderApparanceID; // Secondary shoulder appearance
			public int SecondaryShoulderSlot;        // Always 2 if secondary shoulder apperance is used
			public int SecondaryWeaponAppearanceID;  // For legion artifacts: linked child item appearance
			public int SecondaryWeaponSlot;          // For legion artifacts: which slot is used by child item
			public string SetIcon = "";
			public uint SetID; // Index
			public string SetName = "";
			public EquipmentSetType Type;
		}
	}

	public class BgBattlegroundQueueID_Rec
	{
		public BattlegroundQueueTypeId bgQueueTypeId;
		public uint invitedToInstance;
		public uint joinTime;
		public bool mercenary;
	}

	// Holder for Battlegrounddata
	public class BGData
	{
		public byte bgAfkReportedCount;
		public long bgAfkReportedTimer;

		public List<ObjectGuid> bgAfkReporter = new();

		public uint bgInstanceID; //< This variable is set to bg._InstanceID,

		public uint bgTeam; //< What side the player will be added to

		//  when player is teleported to BG - (it is Battleground's GUID)
		public BattlegroundTypeId bgTypeID;

		public WorldLocation joinPos; //< From where player entered BG

		public uint mountSpell;
		public uint[] taxiPath = new uint[2];

		public BGData()
		{
			bgTypeID = BattlegroundTypeId.None;
			ClearTaxiPath();
			joinPos = new WorldLocation();
		}

		public void ClearTaxiPath()
		{
			taxiPath[0] = taxiPath[1] = 0;
		}

		public bool HasTaxiPath()
		{
			return taxiPath[0] != 0 && taxiPath[1] != 0;
		}
	}

	public class CUFProfile
	{
		public BitSet BoolOptions;
		public ushort BottomOffset;
		public byte BottomPoint;
		public ushort FrameHeight;
		public ushort FrameWidth;
		public byte HealthText;
		public ushort LeftOffset;
		public byte LeftPoint;

		public string ProfileName;
		public byte SortBy;

		// LeftOffset, TopOffset and BottomOffset
		public ushort TopOffset;

		// LeftAlign, TopAlight, BottomAlign
		public byte TopPoint;

		public CUFProfile()
		{
			BoolOptions = new BitSet((int)CUFBoolOptions.BoolOptionsCount);
		}

		public CUFProfile(string name, ushort frameHeight, ushort frameWidth, byte sortBy, byte healthText, uint boolOptions,
		                  byte topPoint, byte bottomPoint, byte leftPoint, ushort topOffset, ushort bottomOffset, ushort leftOffset)
		{
			ProfileName = name;

			BoolOptions = new BitSet(new uint[]
			                         {
				                         boolOptions
			                         });

			FrameHeight  = frameHeight;
			FrameWidth   = frameWidth;
			SortBy       = sortBy;
			HealthText   = healthText;
			TopPoint     = topPoint;
			BottomPoint  = bottomPoint;
			LeftPoint    = leftPoint;
			TopOffset    = topOffset;
			BottomOffset = bottomOffset;
			LeftOffset   = leftOffset;
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

		// More fields can be added to BoolOptions without changing DB schema (up to 32, currently 27)
	}

	internal struct GroupUpdateCounter
	{
		public ObjectGuid GroupGuid;
		public int UpdateSequenceNumber;
	}

	internal class StoredAuraTeleportLocation
	{
		public enum State
		{
			Unchanged,
			Changed,
			Deleted
		}

		public State CurrentState;
		public WorldLocation Loc;
	}

	internal struct QuestObjectiveStatusData
	{
		public (uint QuestID, QuestStatusData Status) QuestStatusPair;
		public QuestObjective Objective;
	}
}