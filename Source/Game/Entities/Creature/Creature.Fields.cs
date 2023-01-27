// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Loots;
using Game.Spells;

namespace Game.Entities
{
	public partial class Creature
	{
		private bool _AlreadyCallAssistance;
		private bool _AlreadySearchedAssistance;
		private uint _boundaryCheckTime; // (msecs) remaining time for next evade boundary check
		private bool _cannotReachTarget;
		private uint _cannotReachTimer;
		public float _CombatDistance;
		private uint _combatPulseDelay; // (secs) how often the creature puts the entire zone in combat (only works in dungeons)
		private uint _combatPulseTime;  // (msecs) remaining time for next zone-in-combat pulse
		private uint _corpseDelay;      // (secs) delay between death and corpse disappearance
		public long _corpseRemoveTime;  // (msecs)timer for death or corpse disappearance
		private CreatureData _creatureData;
		private CreatureTemplate _creatureInfo;
		private (uint nodeId, uint pathId) _currentWaypointNodeInfo;
		private byte _equipmentId;

		//Formation var
		private CreatureGroup _formation;

		private Position _homePosition;
		private bool _ignoreCorpseDecayRatio;

		private bool _isMissingCanSwimFlagOutOfCombat;
		public bool _isTempWorldObject; //true when possessed

		private long _lastDamagedTime; // Part of Evade mechanics
		public Loot _loot;

		private LootModes _LootMode; // Bitmask (default: LOOT_MODE_DEFAULT) that determines what loot will be lootable

		private SpellSchoolMask _meleeDamageSchoolMask;
		public uint _originalEntry;
		private sbyte _originalEquipmentId; // can be -1

		internal Dictionary<ObjectGuid, Loot> _personalLoot = new();

		// Timers
		private long _pickpocketLootRestore;

		public ulong _PlayerDamageReq;

		// Regenerate health
		private bool _regenerateHealth;     // Set on creation
		private bool _regenerateHealthLock; // Dynamically set
		private bool _respawnCompatibilityMode;
		private uint _respawnDelay; // (secs) delay between corpse disappearance and respawning
		private long _respawnTime;  // (secs) time of next respawn
		private string _scriptStringId;
		public float _SightDistance;
		public ulong _spawnId;

		private SpellFocusInfo _spellFocusInfo;

		public uint[] _spells = new uint[SharedConst.MaxCreatureSpells];

		private string[] _stringIds = new string[3];
		private HashSet<ObjectGuid> _tapList = new();
		private MultiMap<byte, byte> _textRepeat = new();
		private Position _transportHomePosition = new();

		// vendor items
		private List<VendorItemCount> _vendorItemCounts = new();
		private float _wanderDistance;

		// Waypoint path
		private uint _waypointPathId;

		private bool DisableReputationGain;

		private ReactStates reactState; // for AI, not charmInfo
		private bool triggerJustAppeared;
		public MovementGeneratorType DefaultMovementType { get; set; }
	}

	public enum ObjectCellMoveState
	{
		None,    // not in move list
		Active,  // in move list
		Inactive // in move list but should not move
	}

	internal struct SpellFocusInfo
	{
		public Spell Spell;
		public uint Delay;        // ms until the creature's target should snap back (0 = no snapback scheduled)
		public ObjectGuid Target; // the creature's "real" target while casting
		public float Orientation; // the creature's "real" orientation while casting
	}
}