// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class WaypointNode
    {
        public uint Delay { get; set; }
        public byte EventChance { get; set; }
        public uint EventId { get; set; }

        public uint Id { get; set; }
        public WaypointMoveType MoveType { get; set; }
        public float? Orientation;
        public float X, Y, Z;

        public WaypointNode()
        {
            MoveType = WaypointMoveType.Run;
        }

        public WaypointNode(uint _id, float _x, float _y, float _z, float? _orientation = null, uint _delay = 0)
        {
            Id = _id;
            X = _x;
            Y = _y;
            Z = _z;
            Orientation = _orientation;
            Delay = _delay;
            EventId = 0;
            MoveType = WaypointMoveType.Walk;
            EventChance = 100;
        }
    }
}