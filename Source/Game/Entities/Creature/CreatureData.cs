// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Maps;

namespace Game.Entities
{
    public class CreatureData : SpawnData
    {
        public uint Curhealth { get; set; }
        public uint Curmana { get; set; }
        public uint Currentwaypoint { get; set; }
        public uint Displayid { get; set; }
        public uint Dynamicflags { get; set; }
        public sbyte EquipmentId { get; set; }
        public byte MovementType { get; set; }
        public ulong Npcflag { get; set; }
        public uint UnitFlags { get; set; }  // enum UnitFlags mask values
        public uint UnitFlags2 { get; set; } // enum UnitFlags2 mask values
        public uint UnitFlags3 { get; set; } // enum UnitFlags3 mask values
        public float WanderDistance { get; set; }

        public CreatureData() : base(SpawnObjectType.Creature)
        {
        }
    }
}