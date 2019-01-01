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
using System;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AttackSwing, Processing = PacketProcessing.Inplace)]
        void HandleAttackSwing(AttackSwing packet)
        {
            Unit enemy = Global.ObjAccessor.GetUnit(GetPlayer(), packet.Victim);
            if (!enemy)
            {
                // stop attack state at client
                SendAttackStop(null);
                return;
            }

            if (!GetPlayer().IsValidAttackTarget(enemy))
            {
                // stop attack state at client
                SendAttackStop(enemy);
                return;
            }

            //! Client explicitly checks the following before sending CMSG_ATTACKSWING packet,
            //! so we'll place the same check here. Note that it might be possible to reuse this snippet
            //! in other places as well.
            Vehicle vehicle = GetPlayer().GetVehicle();
            if (vehicle)
            {
                VehicleSeatRecord seat = vehicle.GetSeatForPassenger(GetPlayer());
                Cypher.Assert(seat != null);
                if (!seat.Flags.HasAnyFlag(VehicleSeatFlags.CanAttack))
                {
                    SendAttackStop(enemy);
                    return;
                }
            }

            GetPlayer().Attack(enemy, true);
        }

        [WorldPacketHandler(ClientOpcodes.AttackStop, Processing = PacketProcessing.Inplace)]
        void HandleAttackStop(AttackStop packet)
        {
            GetPlayer().AttackStop();
        }

        [WorldPacketHandler(ClientOpcodes.SetSheathed, Processing = PacketProcessing.Inplace)]
        void HandleSetSheathed(SetSheathed packet)
        {
            if (packet.CurrentSheathState >= (int)SheathState.Max)
            {
                Log.outError(LogFilter.Network, "Unknown sheath state {0} ??", packet.CurrentSheathState);
                return;
            }

            GetPlayer().SetSheath((SheathState)packet.CurrentSheathState);
        }

        void SendAttackStop(Unit enemy)
        {
            SendPacket(new SAttackStop(GetPlayer(), enemy));
        }
    }
}
