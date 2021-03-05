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
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.CanDuel)]
        private void HandleCanDuel(CanDuel packet)
        {
            Player player = Global.ObjAccessor.FindPlayer(packet.TargetGUID);

            if (!player)
                return;

            var response = new CanDuelResult();
            response.TargetGUID = packet.TargetGUID;
            response.Result = player.duel == null;
            SendPacket(response);

            if (response.Result)
            {
                if (GetPlayer().IsMounted())
                    GetPlayer().CastSpell(player, 62875);
                else
                    GetPlayer().CastSpell(player, 7266);
            }
        }

        [WorldPacketHandler(ClientOpcodes.DuelResponse)]
        private void HandleDuelResponse(DuelResponse duelResponse)
        {
            if (duelResponse.Accepted && !duelResponse.Forfeited)
                HandleDuelAccepted();
            else
                HandleDuelCancelled();
        }

        private void HandleDuelAccepted()
        {
            if (GetPlayer().duel == null)                                  // ignore accept from duel-sender
                return;

            var player = GetPlayer();
            var plTarget = player.duel.opponent;

            if (player == player.duel.initiator || !plTarget || player == plTarget || player.duel.startTime != 0 || plTarget.duel.startTime != 0)
                return;

            Log.outDebug(LogFilter.Network, "Player 1 is: {0} ({1})", player.GetGUID().ToString(), player.GetName());
            Log.outDebug(LogFilter.Network, "Player 2 is: {0} ({1})", plTarget.GetGUID().ToString(), plTarget.GetName());

            var now = Time.UnixTime;
            player.duel.startTimer = now;
            plTarget.duel.startTimer = now;

            var packet = new DuelCountdown(3000);

            player.SendPacket(packet);
            plTarget.SendPacket(packet);

            player.EnablePvpRules();
            plTarget.EnablePvpRules();
        }

        private void HandleDuelCancelled()
        {
            // no duel requested
            if (GetPlayer().duel == null)
                return;

            // player surrendered in a duel using /forfeit
            if (GetPlayer().duel.startTime != 0)
            {
                GetPlayer().CombatStopWithPets(true);
                if (GetPlayer().duel.opponent)
                    GetPlayer().duel.opponent.CombatStopWithPets(true);

                GetPlayer().CastSpell(GetPlayer(), 7267, true);    // beg
                GetPlayer().DuelComplete(DuelCompleteType.Won);
                return;
            }

            GetPlayer().DuelComplete(DuelCompleteType.Interrupted);
        }
    }
}
