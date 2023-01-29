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
        private PlayerAchievementMgr _achievementSys;

        private readonly Dictionary<byte, ActionButton> _actionButtons = new();

        private PlayerCommandStates _activeCheats;
        public ActivePlayerData ActivePlayerData { get; set; }

        private bool _advancedCombatLoggingEnabled;

        private readonly Dictionary<ObjectGuid, Loot> _aELootView = new();
        private uint _areaUpdateId;
        private uint _arenaTeamIdInvited;
        private uint _armorProficiency;
        private readonly float[] _auraBaseFlatMod = new float[(int)BaseModGroup.End];
        private readonly float[] _auraBasePctMod = new float[(int)BaseModGroup.End];
        private uint _baseHealthRegen;
        private uint _baseManaRegen;

        //Stats
        private uint _baseSpellPower;
        private bool _bCanDelayTeleport;

        //PVP
        private readonly BgBattlegroundQueueID_Rec[] _bgBattlegroundQueueID = new BgBattlegroundQueueID_Rec[SharedConst.MaxPlayerBGQueues];
        private readonly BGData _bgData;
        private bool _bHasDelayedTeleport;
        private bool _bPassOnGroupLoot;
        private bool _canBlock;
        private bool _canParry;
        private bool _canTitanGrip;

        private uint _championingFaction;
        private readonly List<Channel> _channels = new();
        private byte _cinematic;

        private readonly CinematicManager _cinematicMgr;
        public List<ObjectGuid> ClientGUIDs { get; set; } = new();
        private uint _combatExitTime;
        private uint _contestedPvPTimer;

        private WorldLocation _corpseLocation;
        private PlayerCreateMode _createMode;

        private long _createTime;

        private readonly CUFProfile[] _cUFProfiles = new CUFProfile[PlayerConst.MaxCUFProfiles];
        private readonly Dictionary<uint, PlayerCurrency> _currencyStorage = new();
        private uint _currentBuybackSlot;
        private bool _customizationsChanged;

        private bool _dailyQuestChanged;
        private long _deathExpireTime;
        private uint _deathTimer;
        private DeclinedName _declinedname;
        private PlayerDelayedOperations _delayedOperations;
        private readonly List<uint> _dfQuests = new();
        private uint _drunkTimer;

        private Difficulty _dungeonDifficulty;
        private readonly List<EnchantDuration> _enchantDuration = new();

        //Inventory
        private readonly Dictionary<ulong, EquipmentSetInfo> _equipmentSets = new();

        private PlayerExtraFlags _extraFlags;
        private byte _fishingSteps;
        private uint _foodEmoteTimerCount;
        public byte[] ForcedSpeedChanges = new byte[(int)UnitMoveType.Max];

        private Garrison _garrison;

        //Groups/Raids
        private readonly GroupReference _group = new();
        private Group _groupInvite;
        private GroupUpdateFlags _groupUpdateMask;
        private readonly GroupUpdateCounter[] _groupUpdateSequences = new GroupUpdateCounter[2];

        private readonly TimeTracker _groupUpdateTimer;

        private ulong _guildIdInvited;
        private uint _homebindTimer;
        private uint _hostileReferenceCheckTimer;
        private uint _ingametime;
        private readonly Dictionary<uint, long> _instanceResetTimes = new();
        public bool InstanceValid { get; set; }
        private bool _isBGRandomWinner;
        private readonly List<Item> _itemDuration = new();
        private readonly Item[] _items = new Item[(int)PlayerSlots.Count];
        private readonly List<ObjectGuid> _itemSoulboundTradeable = new();
        public bool ItemUpdateQueueBlocked { get; set; }
        private long _last_tick;
        private long _lastDailyQuestTime;
        private uint _lastFallTime;
        private float _lastFallZ;
        private long _lastHonorUpdateTime;
        private uint _lastpetnumber;
        private uint _lastPotionId;
        private Difficulty _legacyRaidDifficulty;
        private readonly long _logintime;
        private readonly List<LootRoll> _lootRolls = new(); // loot rolls waiting for answer

        //Mail
        private readonly List<Mail> _mail = new();
        public bool MailUpdated { get; set; }
        private readonly int[] _mirrorTimer = new int[3];

        private PlayerUnderwaterState _mirrorTimerFlags;
        private PlayerUnderwaterState _mirrorTimerFlagsLast;
        private bool _monthlyQuestChanged;
        private readonly List<uint> _monthlyquests = new();
        public byte MovementForceModMagnitudeChanges;

        private uint _movie;
        private long _nextMailDelivereTime;

        private uint _nextSave;
        private uint _oldpetspell;
        private readonly GroupReference _originalGroup = new();
        private readonly MultiMap<uint, uint> _overrideSpells = new();
        private uint _pendingBindId;
        private uint _pendingBindTimer;
        public List<PetAura> PetAuras = new();

        //Pets
        private PetStable _petStable;
        private uint _playedTimeLevel;
        private uint _playedTimeTotal;

        public PlayerData PlayerData { get; set; }
        private ObjectGuid _playerSharingQuest;
        private readonly float[] _powerFraction = new float[(int)PowerType.MaxPerClass];
        private readonly QuestObjectiveCriteriaManager _questObjectiveCriteriaMgr;
        private readonly MultiMap<(QuestObjectiveType Type, int ObjectID), QuestObjectiveStatusData> _questObjectiveStatus = new();
        private readonly Dictionary<uint, QuestStatusData> _questStatus = new();
        private readonly Dictionary<uint, QuestSaveType> _questStatusSave = new();
        private Difficulty _raidDifficulty;
        private uint _recall_instanceId;

        // Recall position
        private WorldLocation _recall_location;

        private readonly Dictionary<uint, uint> _recentInstances = new();
        private readonly List<ObjectGuid> _refundableItems = new();
        private uint _regenTimerCount;

        private readonly RestMgr _restMgr;

        private ResurrectionData _resurrectionData;
        private readonly List<uint> _rewardedQuests = new();
        private readonly Dictionary<uint, QuestSaveType> _rewardedQuestsSave = new();
        private Runes _runes = new();

        private readonly SceneMgr _sceneMgr;
        private bool _seasonalQuestChanged;
        private readonly Dictionary<uint, Dictionary<uint, long>> _seasonalquests = new();
        private uint _sharedQuestId;
        private PlayerSocial _social;

        private SpecializationInfo _specializationInfo;
        private readonly List<SpellModifier>[][] _spellMods = new List<SpellModifier>[(int)SpellModOp.Max][];
        public Spell SpellModTakingSpell { get; set; }
        private int _spellPenetrationItemMod;

        //Spell
        private readonly Dictionary<uint, PlayerSpell> _spells = new();
        private readonly Dictionary<uint, StoredAuraTeleportLocation> _storedAuraTeleportLocations = new();

        // Player summoning
        private long _summon_expire;
        private uint _summon_instanceId;
        private WorldLocation _summon_location;
        private byte _swingErrorMsg;

        //Movement
        public PlayerTaxi Taxi { get; set; } = new();
        private Team _team;
        private uint? _teleport_instanceId;
        private TeleportToOptions _teleport_options;
        private uint _temporaryUnsummonedPetNumber;

        //Quest
        private readonly List<uint> _timedquests = new();
        private uint _titanGripPenaltySpellId;
        private TradeData _trade;

        private readonly Dictionary<int, PlayerSpellState> _traitConfigStates = new();
        private bool _usePvpItemLevels;
        public List<ObjectGuid> VisibleTransports { get; set; } = new();
        private readonly VoidStorageItem[] _voidStorageItems = new VoidStorageItem[SharedConst.VoidStorageMaxSlot];
        private uint _weaponChangeTimer;
        private uint _weaponProficiency;
        private bool _weeklyQuestChanged;
        private readonly List<uint> _weeklyquests = new();
        private uint _zoneUpdateId;
        private uint _zoneUpdateTimer;
        public AtLoginFlags AtLoginFlags { get; set; }
        public string AutoReplyMsg { get; set; }

        //Combat
        private readonly int[] _baseRatingValue = new int[(int)CombatRating.Max];
        public DuelInfo Duel { get; set; }

        // variables to save health and mana before Duel and restore them after Duel
        private ulong _healthBeforeDuel;
        private readonly WorldLocation _homebind = new();
        private uint _homebindAreaId;
        public List<ItemSetEffect> ItemSetEff { get; set; } = new();
        public List<Item> ItemUpdateQueue { get; set; } = new();
        private uint _manaBeforeDuel;
        private readonly Dictionary<ulong, Item> _mMitems = new();
        private bool _mSemaphoreTeleport_Far;
        private bool _mSemaphoreTeleport_Near;
        private readonly Dictionary<uint, SkillStatusData> _mSkillStatus = new();

        //Gossip
        public PlayerMenu PlayerTalkClass { get; set; }
        public PvPInfo PvpInfo;
        private ReputationMgr _reputationMgr;
        public WorldObject SeerView { get; set; }

        //Core
        private readonly WorldSession _session;
        private WorldLocation _teleportDest;
        public byte UnReadMails { get; set; }
        private readonly List<ObjectGuid> _whisperList = new();

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