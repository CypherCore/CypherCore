// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Loots;

namespace Game.Entities
{
    public partial class Creature
    {
        internal Dictionary<ObjectGuid, Loot> _personalLoot = new();
        private readonly Position _homePosition;

        private readonly string[] _stringIds = new string[3];
        private readonly MultiMap<byte, byte> _textRepeat = new();
        private readonly Position _transportHomePosition = new();

        // vendor items
        private readonly List<VendorItemCount> _vendorItemCounts = new();
        private bool _alreadyCallAssistance;
        private bool _alreadySearchedAssistance;
        private uint _boundaryCheckTime; // (msecs) remaining Time for next evade boundary check
        private bool _cannotReachTarget;
        private uint _cannotReachTimer;
        private uint _combatPulseDelay; // (secs) how often the creature puts the entire zone in combat (only works in dungeons)
        private uint _combatPulseTime;  // (msecs) remaining Time for next zone-in-combat pulse
        private uint _corpseDelay;      // (secs) delay between death and corpse disappearance
        private CreatureData _creatureData;
        private CreatureTemplate _creatureInfo;
        private (uint nodeId, uint pathId) _currentWaypointNodeInfo;

        private bool _disableReputationGain;
        private byte _equipmentId;

        //Formation var
        private CreatureGroup _formation;
        private bool _ignoreCorpseDecayRatio;

        private bool _isMissingCanSwimFlagOutOfCombat;

        private long _lastDamagedTime; // Part of Evade mechanics

        private LootModes _lootMode; // Bitmask (default: LOOT_MODE_DEFAULT) that determines what loot will be lootable

        private SpellSchoolMask _meleeDamageSchoolMask;
        private sbyte _originalEquipmentId; // can be -1

        // Timers
        private long _pickpocketLootRestore;

        private ReactStates _reactState; // for AI, not charmInfo

        // Regenerate health
        private bool _regenerateHealth;     // Set on creation
        private bool _regenerateHealthLock; // Dynamically set
        private bool _respawnCompatibilityMode;
        private uint _respawnDelay; // (secs) delay between corpse disappearance and respawning
        private long _respawnTime;  // (secs) Time of next respawn
        private string _scriptStringId;

        private SpellFocusInfo _spellFocusInfo;
        private HashSet<ObjectGuid> _tapList = new();
        private bool _triggerJustAppeared;
        private float _wanderDistance;

        // Waypoint path
        private uint _waypointPathId;
        public float CombatDistance { get; set; }
        public long CorpseRemoveTime { get; set; }  // (msecs)timer for death or corpse disappearance
        public bool IsTempWorldObject { get; set; } //true when possessed
        public Loot Loot { get; set; }
        public uint OriginalEntry { get; set; }

        public ulong PlayerDamageReq { get; set; }
        public float SightDistance { get; set; }
        public ulong SpawnId { get; set; }

        public uint[] Spells { get; set; } = new uint[SharedConst.MaxCreatureSpells];
        public MovementGeneratorType DefaultMovementType { get; set; }
    }
}