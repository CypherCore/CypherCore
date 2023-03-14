// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Loots;
using Game.Spells;
using System.Collections.Generic;

namespace Game.Entities
{
    public partial class Creature
    {
        CreatureTemplate m_creatureInfo;
        CreatureData m_creatureData;

        string[] m_stringIds = new string[3];
        string m_scriptStringId;

        SpellFocusInfo _spellFocusInfo;

        long _lastDamagedTime; // Part of Evade mechanics
        MultiMap<byte, byte> m_textRepeat = new();

        // Regenerate health
        bool _regenerateHealth; // Set on creation
        bool _regenerateHealthLock; // Dynamically set

        bool _isMissingCanSwimFlagOutOfCombat;

        uint? _trainerId;

        public ulong m_PlayerDamageReq;
        public float m_SightDistance;
        public float m_CombatDistance;
        public bool m_isTempWorldObject; //true when possessed

        ReactStates reactState;                           // for AI, not charmInfo
        public MovementGeneratorType DefaultMovementType { get; set; }
        public ulong m_spawnId;
        byte m_equipmentId;
        sbyte m_originalEquipmentId; // can be -1

        bool m_AlreadyCallAssistance;
        bool m_AlreadySearchedAssistance;
        bool m_cannotReachTarget;
        uint m_cannotReachTimer;

        SpellSchoolMask m_meleeDamageSchoolMask;
        public uint m_originalEntry;

        Position m_homePosition;
        Position m_transportHomePosition = new();

        bool DisableReputationGain;

        LootModes m_LootMode;                                  // Bitmask (default: LOOT_MODE_DEFAULT) that determines what loot will be lootable

        // Waypoint path
        uint _waypointPathId;
        (uint nodeId, uint pathId) _currentWaypointNodeInfo;

        //Formation var
        CreatureGroup m_formation;
        bool triggerJustAppeared;
        bool m_respawnCompatibilityMode;

        public uint[] m_spells = new uint[SharedConst.MaxCreatureSpells];

        // Timers
        long _pickpocketLootRestore;
        public long m_corpseRemoveTime;                          // (msecs)timer for death or corpse disappearance
        long m_respawnTime;                               // (secs) time of next respawn
        uint m_respawnDelay;                              // (secs) delay between corpse disappearance and respawning
        uint m_corpseDelay;                               // (secs) delay between death and corpse disappearance
        bool m_ignoreCorpseDecayRatio;
        float m_wanderDistance;
        uint m_boundaryCheckTime;                         // (msecs) remaining time for next evade boundary check
        uint m_combatPulseTime;                           // (msecs) remaining time for next zone-in-combat pulse
        uint m_combatPulseDelay;                          // (secs) how often the creature puts the entire zone in combat (only works in dungeons)

        // vendor items
        List<VendorItemCount> m_vendorItemCounts = new();

        internal Dictionary<ObjectGuid, Loot> m_personalLoot = new();
        public Loot _loot;
        HashSet<ObjectGuid> m_tapList = new();
    }

    public enum ObjectCellMoveState
    {
        None,    // not in move list
        Active,  // in move list
        Inactive // in move list but should not move
    }

    struct SpellFocusInfo
    {
        public Spell Spell;
        public uint Delay;         // ms until the creature's target should snap back (0 = no snapback scheduled)
        public ObjectGuid Target;        // the creature's "real" target while casting
        public float Orientation; // the creature's "real" orientation while casting
    }
}
