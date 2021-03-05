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
using Game.Loots;
using Game.Spells;
using System.Collections.Generic;

namespace Game.Entities
{
    public partial class Creature
    {
        private CreatureTemplate m_creatureInfo;
        private CreatureData m_creatureData;

        private Spell _focusSpell;   // Locks the target during spell cast for proper facing
        private uint _focusDelay;
        private bool m_shouldReacquireTarget;
        private ObjectGuid m_suppressedTarget; // Stores the creature's "real" target while casting
        private float m_suppressedOrientation; // Stores the creature's "real" orientation while casting

        private long _lastDamagedTime; // Part of Evade mechanics
        private MultiMap<byte, byte> m_textRepeat = new MultiMap<byte, byte>();

        // Regenerate health
        private bool _regenerateHealth; // Set on creation
        private bool _regenerateHealthLock; // Dynamically set

        public ulong m_PlayerDamageReq;
        public float m_SightDistance;
        public float m_CombatDistance;
        public bool m_isTempWorldObject; //true when possessed

        private ReactStates reactState;                           // for AI, not charmInfo
        public MovementGeneratorType DefaultMovementType { get; set; }
        public ulong m_spawnId;
        private byte m_equipmentId;
        private sbyte m_originalEquipmentId; // can be -1

        private bool m_AlreadyCallAssistance;
        private bool m_AlreadySearchedAssistance;
        private bool m_cannotReachTarget;
        private uint m_cannotReachTimer;
        private bool m_AI_locked;

        private SpellSchoolMask m_meleeDamageSchoolMask;
        public uint m_originalEntry;

        private Position m_homePosition;
        private Position m_transportHomePosition = new Position();

        private bool DisableReputationGain;

        private LootModes m_LootMode;                                  // Bitmask (default: LOOT_MODE_DEFAULT) that determines what loot will be lootable

        // Waypoint path
        private uint _waypointPathId;
        private (uint nodeId, uint pathId) _currentWaypointNodeInfo;

        //Formation var
        private CreatureGroup m_formation;
        private bool triggerJustAppeared;
        private bool m_respawnCompatibilityMode;

        public uint[] m_spells = new uint[SharedConst.MaxCreatureSpells];

        // Timers
        private long _pickpocketLootRestore;
        public long m_corpseRemoveTime;                          // (msecs)timer for death or corpse disappearance
        private long m_respawnTime;                               // (secs) time of next respawn
        private uint m_respawnDelay;                              // (secs) delay between corpse disappearance and respawning
        private uint m_corpseDelay;                               // (secs) delay between death and corpse disappearance
        private float m_respawnradius;
        private uint m_boundaryCheckTime;                         // (msecs) remaining time for next evade boundary check
        private uint m_combatPulseTime;                           // (msecs) remaining time for next zone-in-combat pulse
        private uint m_combatPulseDelay;                          // (secs) how often the creature puts the entire zone in combat (only works in dungeons)

        // vendor items
        private List<VendorItemCount> m_vendorItemCounts = new List<VendorItemCount>();

        public Loot loot = new Loot();
        public uint m_groupLootTimer;                            // (msecs)timer used for group loot
        public ObjectGuid lootingGroupLowGUID;                         // used to find group which is looting corpse
        private ObjectGuid m_lootRecipient;
        private ObjectGuid m_lootRecipientGroup;
    }

    public enum ObjectCellMoveState
    {
        None,    // not in move list
        Active,  // in move list
        Inactive // in move list but should not move
    }
}
