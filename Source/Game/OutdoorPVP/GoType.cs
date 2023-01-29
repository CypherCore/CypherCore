// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Game.Entities;

namespace Game.PvP
{
    public class GoType
    {
        public uint Entry { get; set; }
        public uint Map { get; set; }
        public Position Pos { get; set; }
        public Quaternion Rot;

        public GoType(uint _entry, uint _map, float _x, float _y, float _z, float _o, float _rot0, float _rot1, float _rot2, float _rot3)
        {
            Entry = _entry;
            Map = _map;
            Pos = new Position(_x, _y, _z, _o);
            Rot = new Quaternion(_rot0, _rot1, _rot2, _rot3);
        }
    }
}