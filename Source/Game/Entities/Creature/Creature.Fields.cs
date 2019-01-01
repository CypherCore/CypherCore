/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
        CreatureTemplate m_creatureInfo;
        CreatureData m_creatureData;

        Spell _focusSpell;   // Locks the target during spell cast for proper facing
        uint _focusDelay;
        bool m_shouldReacquireTarget;
        ObjectGuid m_suppressedTarget; // Stores the creature's "real" target while casting
        float m_suppressedOrientation; // Stores the creature's "real" orientation while casting

        MultiMap<byte, byte> m_textRepeat = new MultiMap<byte, byte>();

        public ulong m_PlayerDamageReq;
        public float m_SightDistance;
        public float m_CombatDistance;
        public bool m_isTempWorldObject; //true when possessed

        ReactStates reactState;                           // for AI, not charmInfo
        public MovementGeneratorType m_defaultMovementType { get; set; }
        public ulong m_spawnId;
        byte m_equipmentId;
        sbyte m_originalEquipmentId; // can be -1

        bool m_AlreadyCallAssistance;
        bool m_AlreadySearchedAssistance;
        bool m_regenHealth;
        bool m_cannotReachTarget;
        uint m_cannotReachTimer;
        bool m_AI_locked;

        SpellSchoolMask m_meleeDamageSchoolMask;
        public uint m_originalEntry;

        Position m_homePosition;
        Position m_transportHomePosition = new Position();

        bool DisableReputationGain;

        LootModes m_LootMode;                                  // Bitmask (default: LOOT_MODE_DEFAULT) that determines what loot will be lootable

        //WaypointMovementGenerator vars
        uint m_waypointID;
        uint m_path_id;

        //Formation var
        CreatureGroup m_formation;
        bool TriggerJustRespawned;

        public uint[] m_spells = new uint[SharedConst.MaxCreatureSpells];

        // Timers
        long _pickpocketLootRestore;
        public long m_corpseRemoveTime;                          // (msecs)timer for death or corpse disappearance
        long m_respawnTime;                               // (secs) time of next respawn
        uint m_respawnDelay;                              // (secs) delay between corpse disappearance and respawning
        uint m_corpseDelay;                               // (secs) delay between death and corpse disappearance
        float m_respawnradius;
        uint m_boundaryCheckTime;                         // (msecs) remaining time for next evade boundary check
        uint m_combatPulseTime;                           // (msecs) remaining time for next zone-in-combat pulse
        uint m_combatPulseDelay;                          // (secs) how often the creature puts the entire zone in combat (only works in dungeons)

        // vendor items
        List<VendorItemCount> m_vendorItemCounts = new List<VendorItemCount>();

        public Loot loot = new Loot();
        public uint m_groupLootTimer;                            // (msecs)timer used for group loot
        public ObjectGuid lootingGroupLowGUID;                         // used to find group which is looting corpse
        ObjectGuid m_lootRecipient;
        ObjectGuid m_lootRecipientGroup;
        ObjectGuid _skinner;
    }

    public enum ObjectCellMoveState
    {
        None,    // not in move list
        Active,  // in move list
        Inactive // in move list but should not move
    }
}
