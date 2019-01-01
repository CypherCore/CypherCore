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
using Game.DataStorage;
using Game.Entities;
using Game.Network;
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.MoveDismissVehicle)]
        void HandleMoveDismissVehicle(MoveDismissVehicle packet)
        {
            ObjectGuid vehicleGUID = GetPlayer().GetCharmGUID();
            if (vehicleGUID.IsEmpty())                                       // something wrong here...
                return;

            GetPlayer().ValidateMovementInfo(packet.Status);
            GetPlayer().m_movementInfo = packet.Status;

            GetPlayer().ExitVehicle();
        }

        [WorldPacketHandler(ClientOpcodes.RequestVehiclePrevSeat)]
        void HandleRequestVehiclePrevSeat(RequestVehiclePrevSeat packet)
        {
            Unit vehicle_base = GetPlayer().GetVehicleBase();
            if (!vehicle_base)
                return;

            VehicleSeatRecord seat = GetPlayer().GetVehicle().GetSeatForPassenger(GetPlayer());
            if (!seat.CanSwitchFromSeat())
            {
                Log.outError(LogFilter.Network, "HandleRequestVehiclePrevSeat: {0} tried to switch seats but current seatflags {1} don't permit that.",
                    GetPlayer().GetGUID().ToString(), seat.Flags);
                return;
            }

            GetPlayer().ChangeSeat(-1, false);
        }

        [WorldPacketHandler(ClientOpcodes.RequestVehicleNextSeat)]
        void HandleRequestVehicleNextSeat(RequestVehicleNextSeat packet)
        {
            Unit vehicle_base = GetPlayer().GetVehicleBase();
            if (!vehicle_base)
                return;

            VehicleSeatRecord seat = GetPlayer().GetVehicle().GetSeatForPassenger(GetPlayer());
            if (!seat.CanSwitchFromSeat())
            {
                Log.outError(LogFilter.Network, "HandleRequestVehicleNextSeat: {0} tried to switch seats but current seatflags {1} don't permit that.",
                    GetPlayer().GetGUID().ToString(), seat.Flags);
                return;
            }

            GetPlayer().ChangeSeat(-1, true);
        }

        [WorldPacketHandler(ClientOpcodes.MoveChangeVehicleSeats)]
        void HandleMoveChangeVehicleSeats(MoveChangeVehicleSeats packet)
        {
            Unit vehicle_base = GetPlayer().GetVehicleBase();
            if (!vehicle_base)
                return;

            VehicleSeatRecord seat = GetPlayer().GetVehicle().GetSeatForPassenger(GetPlayer());
            if (!seat.CanSwitchFromSeat())
            {
                Log.outError(LogFilter.Network, "HandleMoveChangeVehicleSeats, {0} tried to switch seats but current seatflags {1} don't permit that.",
                    GetPlayer().GetGUID().ToString(), seat.Flags);
                return;
            }

            GetPlayer().ValidateMovementInfo(packet.Status);

            if (vehicle_base.GetGUID() != packet.Status.Guid)
                return;

            vehicle_base.m_movementInfo = packet.Status;

            if (packet.DstVehicle.IsEmpty())
                GetPlayer().ChangeSeat(-1, packet.DstSeatIndex != 255);
            else
            {
                Unit vehUnit = Global.ObjAccessor.GetUnit(GetPlayer(), packet.DstVehicle);
                if (vehUnit)
                {
                    Vehicle vehicle = vehUnit.GetVehicleKit();
                    if (vehicle)
                        if (vehicle.HasEmptySeat((sbyte) packet.DstSeatIndex))
                            vehUnit.HandleSpellClick(GetPlayer(), (sbyte) packet.DstSeatIndex);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.RequestVehicleSwitchSeat)]
        void HandleRequestVehicleSwitchSeat(RequestVehicleSwitchSeat packet)
        {
            Unit vehicle_base = GetPlayer().GetVehicleBase();
            if (!vehicle_base)
                return;

            VehicleSeatRecord seat = GetPlayer().GetVehicle().GetSeatForPassenger(GetPlayer());
            if (!seat.CanSwitchFromSeat())
            {
                Log.outError(LogFilter.Network, "HandleRequestVehicleSwitchSeat: {0} tried to switch seats but current seatflags {1} don't permit that.",
                    GetPlayer().GetGUID().ToString(), seat.Flags);
                return;
            }

            if (vehicle_base.GetGUID() == packet.Vehicle)
                GetPlayer().ChangeSeat((sbyte)packet.SeatIndex);
            else
            {
                Unit vehUnit = Global.ObjAccessor.GetUnit(GetPlayer(), packet.Vehicle);
                if (vehUnit)
                {
                    Vehicle vehicle = vehUnit.GetVehicleKit();
                    if (vehicle)
                        if (vehicle.HasEmptySeat((sbyte) packet.SeatIndex))
                            vehUnit.HandleSpellClick(GetPlayer(), (sbyte) packet.SeatIndex);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.RideVehicleInteract)]
        void HandleRideVehicleInteract(RideVehicleInteract packet)
        {
            Player player = Global.ObjAccessor.FindPlayer(packet.Vehicle);
            if (player)
            {
                if (!player.GetVehicleKit())
                    return;
                if (!player.IsInRaidWith(GetPlayer()))
                    return;
                if (!player.IsWithinDistInMap(GetPlayer(), SharedConst.InteractionDistance))
                    return;

                GetPlayer().EnterVehicle(player);
            }
        }

        [WorldPacketHandler(ClientOpcodes.EjectPassenger)]
        void HandleEjectPassenger(EjectPassenger packet)
        {
            Vehicle vehicle = GetPlayer().GetVehicleKit();
            if (!vehicle)
            {
                Log.outError(LogFilter.Network, "HandleEjectPassenger: {0} is not in a vehicle!", GetPlayer().GetGUID().ToString());
                return;
            }

            if (packet.Passenger.IsUnit())
            {
                Unit unit = Global.ObjAccessor.GetUnit(GetPlayer(), packet.Passenger);
                if (!unit)
                {
                    Log.outError(LogFilter.Network, "{0} tried to eject {1} from vehicle, but the latter was not found in world!", GetPlayer().GetGUID().ToString(), packet.Passenger.ToString());
                    return;
                }

                if (!unit.IsOnVehicle(vehicle.GetBase()))
                {
                    Log.outError(LogFilter.Network, "{0} tried to eject {1}, but they are not in the same vehicle", GetPlayer().GetGUID().ToString(), packet.Passenger.ToString());
                    return;
                }

                VehicleSeatRecord seat = vehicle.GetSeatForPassenger(unit);
                Cypher.Assert(seat != null);
                if (seat.IsEjectable())
                    unit.ExitVehicle();
                else
                    Log.outError(LogFilter.Network, "{0} attempted to eject {1} from non-ejectable seat.", GetPlayer().GetGUID().ToString(), packet.Passenger.ToString());
            }

            else
                Log.outError(LogFilter.Network, "HandleEjectPassenger: {0} tried to eject invalid {1}", GetPlayer().GetGUID().ToString(), packet.Passenger.ToString());
        }

        [WorldPacketHandler(ClientOpcodes.RequestVehicleExit)]
        void HandleRequestVehicleExit(RequestVehicleExit packet)
        {
            Vehicle vehicle = GetPlayer().GetVehicle();
            if (vehicle)
            {
                VehicleSeatRecord seat = vehicle.GetSeatForPassenger(GetPlayer());
                if (seat != null)
                {
                    if (seat.CanEnterOrExit())
                        GetPlayer().ExitVehicle();
                    else
                        Log.outError(LogFilter.Network, "{0} tried to exit vehicle, but seatflags {1} (ID: {2}) don't permit that.",
                        GetPlayer().GetGUID().ToString(), seat.Id, seat.Flags);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.MoveSetVehicleRecIdAck)]
        void HandleMoveSetVehicleRecAck(MoveSetVehicleRecIdAck setVehicleRecIdAck)
        {
            GetPlayer().ValidateMovementInfo(setVehicleRecIdAck.Data.Status);
        }
    }
}
