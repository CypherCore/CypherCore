// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Achievements;
using Game.Chat;
using Game.Garrisons;
using Game.Groups;
using Game.Loots;
using Game.Mails;
using Game.Misc;
using Game.Spells;

namespace Game.Entities
{
    public partial class Player
    {
        public byte[] ForcedSpeedChanges = new byte[(int)UnitMoveType.Max];
        public byte MovementForceModMagnitudeChanges;
        public List<PetAura> PetAuras = new();
        public PvPInfo PvpInfo;

        private readonly Dictionary<byte, ActionButton> _actionButtons = new();

        private readonly Dictionary<ObjectGuid, Loot> _aELootView = new();
        private readonly float[] _auraBaseFlatMod = new float[(int)BaseModGroup.End];
        private readonly float[] _auraBasePctMod = new float[(int)BaseModGroup.End];

        //PVP
        private readonly BgBattlegroundQueueID_Rec[] _bgBattlegroundQueueID = new BgBattlegroundQueueID_Rec[SharedConst.MaxPlayerBGQueues];
        private readonly BGData _bgData;
        private readonly List<Channel> _channels = new();

        private readonly CinematicManager _cinematicMgr;

        private readonly CUFProfile[] _cUFProfiles = new CUFProfile[PlayerConst.MaxCUFProfiles];
        private readonly Dictionary<uint, PlayerCurrency> _currencyStorage = new();
        private readonly List<uint> _dfQuests = new();
        private readonly List<EnchantDuration> _enchantDuration = new();

        //Inventory
        private readonly Dictionary<ulong, EquipmentSetInfo> _equipmentSets = new();

        //Groups/Raids
        private readonly GroupReference _group = new();
        private readonly GroupUpdateCounter[] _groupUpdateSequences = new GroupUpdateCounter[2];

        private readonly TimeTracker _groupUpdateTimer;
        private readonly Dictionary<uint, long> _instanceResetTimes = new();
        private readonly List<Item> _itemDuration = new();
        private readonly Item[] _items = new Item[(int)PlayerSlots.Count];
        private readonly List<ObjectGuid> _itemSoulboundTradeable = new();
        private readonly long _logintime;
        private readonly List<LootRoll> _lootRolls = new(); // loot rolls waiting for answer

        //Mail
        private readonly List<Mail> _mail = new();
        private readonly int[] _mirrorTimer = new int[3];
        private readonly List<uint> _monthlyquests = new();
        private readonly GroupReference _originalGroup = new();
        private readonly MultiMap<uint, uint> _overrideSpells = new();
        private readonly float[] _powerFraction = new float[(int)PowerType.MaxPerClass];
        private readonly QuestObjectiveCriteriaManager _questObjectiveCriteriaMgr;
        private readonly MultiMap<(QuestObjectiveType Type, int ObjectID), QuestObjectiveStatusData> _questObjectiveStatus = new();
        private readonly Dictionary<uint, QuestStatusData> _questStatus = new();
        private readonly Dictionary<uint, QuestSaveType> _questStatusSave = new();

        private readonly Dictionary<uint, uint> _recentInstances = new();
        private readonly List<ObjectGuid> _refundableItems = new();

        private readonly RestMgr _restMgr;
        private readonly List<uint> _rewardedQuests = new();
        private readonly Dictionary<uint, QuestSaveType> _rewardedQuestsSave = new();

        private readonly SceneMgr _sceneMgr;
        private readonly Dictionary<uint, Dictionary<uint, long>> _seasonalquests = new();
        private readonly List<SpellModifier>[][] _spellMods = new List<SpellModifier>[(int)SpellModOp.Max][];

        //Spell
        private readonly Dictionary<uint, PlayerSpell> _spells = new();
        private readonly Dictionary<uint, StoredAuraTeleportLocation> _storedAuraTeleportLocations = new();

        //Quest
        private readonly List<uint> _timedquests = new();

        private readonly Dictionary<int, PlayerSpellState> _traitConfigStates = new();
        private readonly VoidStorageItem[] _voidStorageItems = new VoidStorageItem[SharedConst.VoidStorageMaxSlot];
        private readonly List<uint> _weeklyquests = new();

        //Combat
        private readonly int[] _baseRatingValue = new int[(int)CombatRating.Max];
        private readonly WorldLocation _homebind = new();
        private readonly Dictionary<ulong, Item> _mMitems = new();
        private readonly Dictionary<uint, SkillStatusData> _mSkillStatus = new();

        //Core
        private readonly WorldSession _session;
        private readonly List<ObjectGuid> _whisperList = new();
        private PlayerAchievementMgr _achievementSys;

        private PlayerCommandStates _activeCheats;

        private bool _advancedCombatLoggingEnabled;
        private uint _areaUpdateId;
        private uint _arenaTeamIdInvited;
        private uint _armorProficiency;
        private uint _baseHealthRegen;
        private uint _baseManaRegen;

        //Stats
        private uint _baseSpellPower;
        private bool _bCanDelayTeleport;
        private bool _bHasDelayedTeleport;
        private bool _bPassOnGroupLoot;
        private bool _canBlock;
        private bool _canParry;
        private bool _canTitanGrip;

        private uint _championingFaction;
        private byte _cinematic;
        private uint _combatExitTime;
        private uint _contestedPvPTimer;

        private WorldLocation _corpseLocation;
        private PlayerCreateMode _createMode;

        private long _createTime;
        private uint _currentBuybackSlot;
        private bool _customizationsChanged;

        private bool _dailyQuestChanged;
        private long _deathExpireTime;
        private uint _deathTimer;
        private DeclinedName _declinedname;
        private PlayerDelayedOperations _delayedOperations;
        private uint _drunkTimer;

        private Difficulty _dungeonDifficulty;

        private PlayerExtraFlags _extraFlags;
        private byte _fishingSteps;
        private uint _foodEmoteTimerCount;

        private Garrison _garrison;
        private Group _groupInvite;
        private GroupUpdateFlags _groupUpdateMask;

        private ulong _guildIdInvited;
        private uint _homebindTimer;
        private uint _hostileReferenceCheckTimer;
        private uint _ingametime;
        private bool _isBGRandomWinner;
        private long _last_tick;
        private long _lastDailyQuestTime;
        private uint _lastFallTime;
        private float _lastFallZ;
        private long _lastHonorUpdateTime;
        private uint _lastpetnumber;
        private uint _lastPotionId;
        private Difficulty _legacyRaidDifficulty;

        private PlayerUnderwaterState _mirrorTimerFlags;
        private PlayerUnderwaterState _mirrorTimerFlagsLast;
        private bool _monthlyQuestChanged;

        private uint _movie;
        private long _nextMailDelivereTime;

        private uint _nextSave;
        private uint _oldpetspell;
        private uint _pendingBindId;
        private uint _pendingBindTimer;

        //Pets
        private PetStable _petStable;
        private uint _playedTimeLevel;
        private uint _playedTimeTotal;
        private ObjectGuid _playerSharingQuest;
        private Difficulty _raidDifficulty;
        private uint _recall_instanceId;

        // Recall position
        private WorldLocation _recall_location;
        private uint _regenTimerCount;

        private ResurrectionData _resurrectionData;
        private Runes _runes = new();
        private bool _seasonalQuestChanged;
        private uint _sharedQuestId;
        private PlayerSocial _social;

        private SpecializationInfo _specializationInfo;
        private int _spellPenetrationItemMod;

        // Player summoning
        private long _summon_expire;
        private uint _summon_instanceId;
        private WorldLocation _summon_location;
        private byte _swingErrorMsg;
        private Team _team;
        private uint? _teleport_instanceId;
        private TeleportToOptions _teleport_options;
        private uint _temporaryUnsummonedPetNumber;
        private uint _titanGripPenaltySpellId;
        private TradeData _trade;
        private bool _usePvpItemLevels;
        private uint _weaponChangeTimer;
        private uint _weaponProficiency;
        private bool _weeklyQuestChanged;
        private uint _zoneUpdateId;
        private uint _zoneUpdateTimer;

        // variables to save health and mana before Duel and restore them after Duel
        private ulong _healthBeforeDuel;
        private uint _homebindAreaId;
        private uint _manaBeforeDuel;
        private bool _mSemaphoreTeleport_Far;
        private bool _mSemaphoreTeleport_Near;
        private ReputationMgr _reputationMgr;
        private WorldLocation _teleportDest;
        public ActivePlayerData ActivePlayerData { get; set; }
        public List<ObjectGuid> ClientGUIDs { get; set; } = new();
        public bool InstanceValid { get; set; }
        public bool ItemUpdateQueueBlocked { get; set; }
        public bool MailUpdated { get; set; }

        public PlayerData PlayerData { get; set; }
        public Spell SpellModTakingSpell { get; set; }

        //Movement
        public PlayerTaxi Taxi { get; set; } = new();
        public List<ObjectGuid> VisibleTransports { get; set; } = new();
        public AtLoginFlags AtLoginFlags { get; set; }
        public string AutoReplyMsg { get; set; }
        public DuelInfo Duel { get; set; }
        public List<ItemSetEffect> ItemSetEff { get; set; } = new();
        public List<Item> ItemUpdateQueue { get; set; } = new();

        //Gossip
        public PlayerMenu PlayerTalkClass { get; set; }
        public WorldObject SeerView { get; set; }
        public byte UnReadMails { get; set; }

        public bool IsDebugAreaTriggers { get; set; }

        public WorldSession GetSession()
        {
            return _session;
        }

        public PlayerSocial GetSocial()
        {
            return _social;
        }
    }
}