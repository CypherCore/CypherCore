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
        void HandleCanDuel(CanDuel packet)
        {
            Player player = Global.ObjAccessor.FindPlayer(packet.TargetGUID);

            if (!player)
                return;

            CanDuelResult response = new();
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
        void HandleDuelResponse(DuelResponse duelResponse)
        {
            if (duelResponse.Accepted && !duelResponse.Forfeited)
                HandleDuelAccepted(duelResponse.ArbiterGUID);
            else
                HandleDuelCancelled();
        }

        void HandleDuelAccepted(ObjectGuid arbiterGuid)
        {
            Player player = GetPlayer();
            if (player.duel == null || player == player.duel.Initiator || player.duel.State != DuelState.Challenged)
                return;

            Player target = player.duel.Opponent;
            if (target.m_playerData.DuelArbiter != arbiterGuid)
                return;

            Log.outDebug(LogFilter.Network, "Player 1 is: {0} ({1})", player.GetGUID().ToString(), player.GetName());
            Log.outDebug(LogFilter.Network, "Player 2 is: {0} ({1})", target.GetGUID().ToString(), target.GetName());

            long now = GameTime.GetGameTime();
            player.duel.StartTime = now + 3;
            target.duel.StartTime = now + 3;

            DuelCountdown packet = new(3000);

            player.SendPacket(packet);
            target.SendPacket(packet);

            player.EnablePvpRules();
            target.EnablePvpRules();
        }

        void HandleDuelCancelled()
        {
            Player player = GetPlayer();

            // no duel requested
            if (player.duel == null || player.duel.State == DuelState.Completed)
                return;

            // player surrendered in a duel using /forfeit
            if (player.duel.State == DuelState.InProgress)
            {
                player.CombatStopWithPets(true);
                player.duel.Opponent.CombatStopWithPets(true);

                player.CastSpell(GetPlayer(), 7267, true);    // beg
                player.DuelComplete(DuelCompleteType.Won);
                return;
            }

            player.DuelComplete(DuelCompleteType.Interrupted);
        }
    }
}
