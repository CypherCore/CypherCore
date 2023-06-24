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
        CreatureDifficulty m_creatureDifficulty;

        string[] m_stringIds = new string[3];
        string m_scriptStringId;

        SpellFocusInfo _spellFocusInfo;

        long _lastDamagedTime; // Part of Evade mechanics
        MultiMap<byte, byte> m_textRepeat = new();

        CreatureStaticFlagsHolder _staticFlags;

        // Regenerate health
        bool _regenerateHealth; // Set on creation

        bool _isMissingCanSwimFlagOutOfCombat;

        uint _gossipMenuId;
        uint? _trainerId;
        float _sparringHealthPct;

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

        uint? _lootId;
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
        bool m_dontClearTapListOnEvade;
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

    public struct CreatureStaticFlagsHolder
    {
        CreatureStaticFlags _flags;
        CreatureStaticFlags2 _flags2;
        CreatureStaticFlags3 _flags3;
        CreatureStaticFlags4 _flags4;
        CreatureStaticFlags5 _flags5;
        CreatureStaticFlags6 _flags6;
        CreatureStaticFlags7 _flags7;
        CreatureStaticFlags8 _flags8;

        public CreatureStaticFlagsHolder(uint flags, uint flags2, uint flags3, uint flags4, uint flags5, uint flags6, uint flags7, uint flags8)
        {
            _flags = (CreatureStaticFlags)flags;
            _flags2 = (CreatureStaticFlags2)flags2;
            _flags3 = (CreatureStaticFlags3)flags3;
            _flags4 = (CreatureStaticFlags4)flags4;
            _flags5 = (CreatureStaticFlags5)flags5;
            _flags6 = (CreatureStaticFlags6)flags6;
            _flags7 = (CreatureStaticFlags7)flags7;
            _flags8 = (CreatureStaticFlags8)flags8;
        }

        public bool HasFlag(CreatureStaticFlags flag) { return _flags.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags2 flag) { return _flags2.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags3 flag) { return _flags3.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags4 flag) { return _flags4.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags5 flag) { return _flags5.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags6 flag) { return _flags6.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags7 flag) { return _flags7.HasFlag(flag); }
        public bool HasFlag(CreatureStaticFlags8 flag) { return _flags8.HasFlag(flag); }

        public void ApplyFlag(CreatureStaticFlags flag, bool apply) { if (apply) _flags |= flag; else _flags &= ~flag; }
        public void ApplyFlag(CreatureStaticFlags2 flag, bool apply) { if (apply) _flags2 |= flag; else _flags2 &= ~flag; }
        public void ApplyFlag(CreatureStaticFlags3 flag, bool apply) { if (apply) _flags3 |= flag; else _flags3 &= ~flag; }
        public void ApplyFlag(CreatureStaticFlags4 flag, bool apply) { if (apply) _flags4 |= flag; else _flags4 &= ~flag; }
        public void ApplyFlag(CreatureStaticFlags5 flag, bool apply) { if (apply) _flags5 |= flag; else _flags5 &= ~flag; }
        public void ApplyFlag(CreatureStaticFlags6 flag, bool apply) { if (apply) _flags6 |= flag; else _flags6 &= ~flag; }
        public void ApplyFlag(CreatureStaticFlags7 flag, bool apply) { if (apply) _flags7 |= flag; else _flags7 &= ~flag; }
        public void ApplyFlag(CreatureStaticFlags8 flag, bool apply) { if (apply) _flags8 |= flag; else _flags8 &= ~flag; }
    }
}
