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
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;
using Framework.Dynamic;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.ElwynnForest
{
    partial struct PathIds
    {
        public const uint STORMWIND_PATH = 80500;
        public const uint GOLDSHIRE_PATH = 80501;
        public const uint WOODS_PATH = 80502;
        public const uint HOUSE_PATH = 80503;
        public const uint LISA_PATH = 8070;
    };

    partial struct WaypointIds
    {
        public const uint STORMWIND_WAYPOINT = 57;
        public const uint GOLDSHIRE_WAYPOINT = 32;
        public const uint WOODS_WAYPOINT = 22;
        public const uint HOUSE_WAYPOINT = 35;
        public const uint LISA_WAYPOINT = 4;
    };

    partial struct SoundIds
    {
        public const uint BANSHEE_DEATH = 1171;
        public const uint BANSHEE_PRE_AGGRO = 1172;
        public const uint CTHUN_YOU_WILL_DIE = 8585;
        public const uint CTHUN_DEATH_IS_CLOSE = 8580;
        public const uint HUMAN_FEMALE_EMOTE_CRY = 6916;
        public const uint GHOST_DEATH = 3416;
    };

    partial struct CreatureIds
    {
        public const uint NPC_DANA = 804;
        public const uint NPC_CAMERON = 805;
        public const uint NPC_JOHN = 806;
        public const uint NPC_LISA = 807;
        public const uint NPC_AARON = 810;
        public const uint NPC_JOSE = 811;
    };

    partial struct EventIds
    {
        public const uint EVENT_WP_START_GOLDSHIRE = 1;
        public const uint EVENT_WP_START_WOODS = 2;
        public const uint EVENT_WP_START_HOUSE = 3;
        public const uint EVENT_WP_START_LISA = 4;
        public const uint EVENT_PLAY_SOUNDS = 5;
        public const uint EVENT_BEGIN_EVENT = 6;
    };

    partial struct GameEventIds
    {
        public const uint GAME_EVENT_CHILDREN_OF_GOLDSHIRE = 76;
    };

    [Script]
    class npc_cameron : ScriptedAI
    {
        public npc_cameron(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _started = false;
            _childrenGUIDs = new List<ObjectGuid>();
        }

        static uint SoundPicker()
        {
            return RandomHelper.RAND(SoundIds.BANSHEE_DEATH, SoundIds.BANSHEE_PRE_AGGRO, SoundIds.CTHUN_YOU_WILL_DIE,
                SoundIds.CTHUN_DEATH_IS_CLOSE, SoundIds.HUMAN_FEMALE_EMOTE_CRY, SoundIds.GHOST_DEATH);
        }

        void MoveTheChildren()
        {
            List<Position> MovePosPositions =
                new List<Position>()
                {
                    new Position(-9373.521f, -67.71767f, 69.201965f, 1.117011f),
                    new Position(-9374.94f, -62.51654f, 69.201965f, 5.201081f),
                    new Position(-9371.013f, -71.20811f, 69.201965f, 1.937315f),
                    new Position(-9368.419f, -66.47543f, 69.201965f, 3.141593f),
                    new Position(-9372.376f, -65.49946f, 69.201965f, 4.206244f),
                    new Position(-9377.477f, -67.8297f, 69.201965f, 0.296706f)
                };

            MovePosPositions.Shuffle();
            // first we break formation because children will need to move on their own now
            foreach (ObjectGuid guid in _childrenGUIDs)
            {
                Creature child = ObjectAccessor.GetCreature(me, guid);
                if (child != null)
                    if (child.GetFormation() != null)
                        child.GetFormation().RemoveMember(child);
            }

            // Move each child to an random position
            for (int i = 0; i < _childrenGUIDs.Count; ++i)
            {
                Creature children = ObjectAccessor.GetCreature(me, _childrenGUIDs[i]);
                if (children != null)
                {
                    children.SetWalk(true);
                    children.GetMotionMaster().MovePoint(0, MovePosPositions[i], true,
                        MovePosPositions[i].GetOrientation());
                }
            }

            me.SetWalk(true);
            me.GetMotionMaster().MovePoint(0, MovePosPositions[MovePosPositions.Count - 1], true,
                MovePosPositions[MovePosPositions.Count - 1].GetOrientation());
        }

        public override void WaypointReached(uint waypointId, uint pathId)
        {
            switch (pathId)
            {
                case PathIds.STORMWIND_PATH:
                {
                    if (waypointId == WaypointIds.STORMWIND_WAYPOINT)
                    {
                        me.GetMotionMaster().MoveRandom(10f);
                        _events.ScheduleEvent(EventIds.EVENT_WP_START_GOLDSHIRE, TimeSpan.FromMinutes(11));
                    }

                    break;
                }
                case PathIds.GOLDSHIRE_PATH:
                {
                    if (waypointId == WaypointIds.GOLDSHIRE_WAYPOINT)
                    {
                        me.GetMotionMaster().MoveRandom(10f);
                        _events.ScheduleEvent(EventIds.EVENT_WP_START_WOODS, TimeSpan.FromMinutes(15));
                    }

                    break;
                }
                case PathIds.WOODS_PATH:
                {
                    if (waypointId == WaypointIds.WOODS_WAYPOINT)
                    {
                        me.GetMotionMaster().MoveRandom(10f);
                        _events.ScheduleEvent(EventIds.EVENT_WP_START_HOUSE, TimeSpan.FromMinutes(6));
                        _events.ScheduleEvent(EventIds.EVENT_WP_START_LISA, TimeSpan.FromSeconds(2));
                    }

                    break;
                }
                case PathIds.HOUSE_PATH:
                {
                    if (waypointId == WaypointIds.HOUSE_WAYPOINT)
                    {
                        // Move childeren at last point
                        MoveTheChildren();

                        // After 30 seconds a random sound should play
                        _events.ScheduleEvent(EventIds.EVENT_PLAY_SOUNDS, TimeSpan.FromSeconds(30));
                    }

                    break;
                }
            }
        }

        public override void OnGameEvent(bool start, ushort eventId)
        {
            if (start && eventId == GameEventIds.GAME_EVENT_CHILDREN_OF_GOLDSHIRE)
            {
                // Start event at 7 am
                // Begin pathing
                _events.ScheduleEvent(EventIds.EVENT_BEGIN_EVENT, TimeSpan.FromSeconds(2));
                _started = true;
            }
            else if (!start && eventId == GameEventIds.GAME_EVENT_CHILDREN_OF_GOLDSHIRE)
            {
                // Reset event at 8 am
                _started = false;
                _events.Reset();
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!_started)
                return;

            _events.Update(diff);
            uint eventId;
            while ((eventId = _events.ExecuteEvent()) != 0)
            {
                switch (eventId)
                {
                    case EventIds.EVENT_WP_START_GOLDSHIRE:
                        me.GetMotionMaster().MovePath(PathIds.GOLDSHIRE_PATH, false);
                        break;
                    case EventIds.EVENT_WP_START_WOODS:
                        me.GetMotionMaster().MovePath(PathIds.WOODS_PATH, false);
                        break;
                    case EventIds.EVENT_WP_START_HOUSE:
                        me.GetMotionMaster().MovePath(PathIds.HOUSE_PATH, false);
                        break;
                    case EventIds.EVENT_WP_START_LISA:
                        foreach (ObjectGuid guid in _childrenGUIDs)
                        {
                            Creature child = ObjectAccessor.GetCreature(me, guid);
                            if (child != null)
                            {
                                if (child.GetEntry() == CreatureIds.NPC_LISA)
                                {
                                    child.GetMotionMaster().MovePath(PathIds.LISA_PATH, false);
                                    break;
                                }
                            }
                        }

                        break;
                    case EventIds.EVENT_PLAY_SOUNDS:
                        me.PlayDistanceSound(SoundPicker());
                        break;
                    case EventIds.EVENT_BEGIN_EVENT:
                    {
                        _childrenGUIDs.Clear();
                        Creature dana = me.FindNearestCreature(CreatureIds.NPC_DANA, 25.0f);
                        Creature john = me.FindNearestCreature(CreatureIds.NPC_JOHN, 25.0f);
                        Creature lisa = me.FindNearestCreature(CreatureIds.NPC_LISA, 25.0f);
                        Creature aaron = me.FindNearestCreature(CreatureIds.NPC_AARON, 25.0f);
                        Creature jose = me.FindNearestCreature(CreatureIds.NPC_JOSE, 25.0f);
                        // Get all childeren's guid's.
                        if (dana != null)
                            _childrenGUIDs.Add(dana.GetGUID());
                        if (john != null)
                            _childrenGUIDs.Add(john.GetGUID());
                        if (lisa != null)
                            _childrenGUIDs.Add(lisa.GetGUID());
                        if (aaron != null)
                            _childrenGUIDs.Add(aaron.GetGUID());
                        if (jose != null)
                            _childrenGUIDs.Add(jose.GetGUID());

                        // If Formation was disbanded, remake.
                        if (!me.GetFormation().IsFormed())
                        {
                            foreach (ObjectGuid guid in _childrenGUIDs)
                            {
                                Creature child = ObjectAccessor.GetCreature(me, guid);
                                if (child != null)
                                    child.SearchFormation();
                            }
                        }

                        // Start movement
                        me.GetMotionMaster().MovePath(PathIds.STORMWIND_PATH, false);

                        break;
                    }
                    default:
                        break;
                }
            }
        }

        //   private EventMap _events;
        private bool _started;
        private List<ObjectGuid> _childrenGUIDs;
    };
}