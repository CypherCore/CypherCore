// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Game.AI
{
    [StructLayout(LayoutKind.Explicit)]
    public struct SmartEvent
    {
        [FieldOffset(0)] public SmartEvents type;

        [FieldOffset(4)] public uint event_phase_mask;

        [FieldOffset(8)] public uint event_chance;

        [FieldOffset(12)] public SmartEventFlags event_flags;

        [FieldOffset(16)] public MinMaxRepeat minMaxRepeat;

        [FieldOffset(16)] public Kill kill;

        [FieldOffset(16)] public SpellHit spellHit;

        [FieldOffset(16)] public Los los;

        [FieldOffset(16)] public Respawn respawn;

        [FieldOffset(16)] public MinMax minMax;

        [FieldOffset(16)] public TargetCasting targetCasting;

        [FieldOffset(16)] public FriendlyCC friendlyCC;

        [FieldOffset(16)] public MissingBuff missingBuff;

        [FieldOffset(16)] public Summoned summoned;

        [FieldOffset(16)] public Quest quest;

        [FieldOffset(16)] public QuestObjective questObjective;

        [FieldOffset(16)] public Emote emote;

        [FieldOffset(16)] public Aura aura;

        [FieldOffset(16)] public Charm charm;

        [FieldOffset(16)] public MovementInform movementInform;

        [FieldOffset(16)] public DataSet dataSet;

        [FieldOffset(16)] public Waypoint waypoint;

        [FieldOffset(16)] public TransportAddCreature transportAddCreature;

        [FieldOffset(16)] public TransportRelocate transportRelocate;

        [FieldOffset(16)] public InstancePlayerEnter instancePlayerEnter;

        [FieldOffset(16)] public Areatrigger areatrigger;

        [FieldOffset(16)] public TextOver textOver;

        [FieldOffset(16)] public TimedEvent timedEvent;

        [FieldOffset(16)] public GossipHello gossipHello;

        [FieldOffset(16)] public Gossip gossip;

        [FieldOffset(16)] public GameEvent gameEvent;

        [FieldOffset(16)] public GoLootStateChanged goLootStateChanged;

        [FieldOffset(16)] public EventInform eventInform;

        [FieldOffset(16)] public DoAction doAction;

        [FieldOffset(16)] public FriendlyHealthPct friendlyHealthPct;

        [FieldOffset(16)] public Distance distance;

        [FieldOffset(16)] public Counter counter;

        [FieldOffset(16)] public SpellCast spellCast;

        [FieldOffset(16)] public Spell spell;

        [FieldOffset(16)] public Raw raw;

        [FieldOffset(40)] public string param_string;

        #region Structs

        public struct MinMaxRepeat
        {
            public uint min;
            public uint max;
            public uint repeatMin;
            public uint repeatMax;
        }

        public struct Kill
        {
            public uint cooldownMin;
            public uint cooldownMax;
            public uint playerOnly;
            public uint creature;
        }

        public struct SpellHit
        {
            public uint spell;
            public uint school;
            public uint cooldownMin;
            public uint cooldownMax;
        }

        public struct Los
        {
            public uint hostilityMode;
            public uint maxDist;
            public uint cooldownMin;
            public uint cooldownMax;
            public uint playerOnly;
        }

        public struct Respawn
        {
            public uint type;
            public uint map;
            public uint area;
        }

        public struct MinMax
        {
            public uint repeatMin;
            public uint repeatMax;
        }

        public struct TargetCasting
        {
            public uint repeatMin;
            public uint repeatMax;
            public uint spellId;
        }

        public struct FriendlyCC
        {
            public uint radius;
            public uint repeatMin;
            public uint repeatMax;
        }

        public struct MissingBuff
        {
            public uint spell;
            public uint radius;
            public uint repeatMin;
            public uint repeatMax;
        }

        public struct Summoned
        {
            public uint creature;
            public uint cooldownMin;
            public uint cooldownMax;
        }

        public struct Quest
        {
            public uint questId;
            public uint cooldownMin;
            public uint cooldownMax;
        }

        public struct QuestObjective
        {
            public uint id;
        }

        public struct Emote
        {
            public uint emoteId;
            public uint cooldownMin;
            public uint cooldownMax;
        }

        public struct Aura
        {
            public uint spell;
            public uint count;
            public uint repeatMin;
            public uint repeatMax;
        }

        public struct Charm
        {
            public uint onRemove;
        }

        public struct MovementInform
        {
            public uint type;
            public uint id;
        }

        public struct DataSet
        {
            public uint id;
            public uint value;
            public uint cooldownMin;
            public uint cooldownMax;
        }

        public struct Waypoint
        {
            public uint pointID;
            public uint pathID;
        }

        public struct TransportAddCreature
        {
            public uint creature;
        }

        public struct TransportRelocate
        {
            public uint pointID;
        }

        public struct InstancePlayerEnter
        {
            public uint team;
            public uint cooldownMin;
            public uint cooldownMax;
        }

        public struct Areatrigger
        {
            public uint id;
        }

        public struct TextOver
        {
            public uint textGroupID;
            public uint creatureEntry;
        }

        public struct TimedEvent
        {
            public uint id;
        }

        public struct GossipHello
        {
            public uint filter;
        }

        public struct Gossip
        {
            public uint sender;
            public uint action;
        }

        public struct GameEvent
        {
            public uint gameEventId;
        }

        public struct GoLootStateChanged
        {
            public uint lootState;
        }

        public struct EventInform
        {
            public uint eventId;
        }

        public struct DoAction
        {
            public uint eventId;
        }

        public struct FriendlyHealthPct
        {
            public uint minHpPct;
            public uint maxHpPct;
            public uint repeatMin;
            public uint repeatMax;
            public uint radius;
        }

        public struct Distance
        {
            public uint guid;
            public uint entry;
            public uint dist;
            public uint repeat;
        }

        public struct Counter
        {
            public uint id;
            public uint value;
            public uint cooldownMin;
            public uint cooldownMax;
        }

        public struct SpellCast
        {
            public uint spell;
            public uint cooldownMin;
            public uint cooldownMax;
        }

        public struct Spell
        {
            public uint effIndex;
        }

        public struct Raw
        {
            public uint param1;
            public uint param2;
            public uint param3;
            public uint param4;
            public uint param5;
        }

        #endregion
    }
}