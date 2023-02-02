// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

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
                if (!seat.HasFlag(VehicleSeatFlags.CanAttack))
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
