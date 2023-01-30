// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.PvP
{
    internal class CreatureType
    {
        public uint Entry { get; set; }
        public uint Map { get; set; }
        private readonly Position _pos;

        public CreatureType(uint _entry, uint _map, float _x, float _y, float _z, float _o)
        {
            Entry = _entry;
            Map = _map;
            _pos = new Position(_x, _y, _z, _o);
        }
    }
}