// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.DataStorage;

namespace Game.Entities
{
    public class VehicleSeat
    {
        public PassengerInfo Passenger;

        public VehicleSeat(VehicleSeatRecord seatInfo, VehicleSeatAddon seatAddon)
        {
            SeatInfo = seatInfo;
            SeatAddon = seatAddon;
            Passenger.Reset();
        }

        public VehicleSeatAddon SeatAddon { get; set; }

        public VehicleSeatRecord SeatInfo { get; set; }

        public bool IsEmpty()
        {
            return Passenger.Guid.IsEmpty();
        }
    }
}