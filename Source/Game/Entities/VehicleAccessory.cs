// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public struct VehicleAccessory
    {
        public VehicleAccessory(uint entry, sbyte seatId, bool isMinion, byte summonType, uint summonTime)
        {
            AccessoryEntry = entry;
            IsMinion = isMinion;
            SummonTime = summonTime;
            SeatId = seatId;
            SummonedType = summonType;
        }

        public uint AccessoryEntry { get; set; }
        public bool IsMinion { get; set; }
        public uint SummonTime { get; set; }
        public sbyte SeatId { get; set; }
        public byte SummonedType { get; set; }
    }
}