// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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

			CanDuelResult response = new();
			response.TargetGUID = packet.TargetGUID;
			response.Result     = player.Duel == null;
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
			if (duelResponse.Accepted &&
			    !duelResponse.Forfeited)
				HandleDuelAccepted(duelResponse.ArbiterGUID);
			else
				HandleDuelCancelled();
		}

		private void HandleDuelAccepted(ObjectGuid arbiterGuid)
		{
			Player player = GetPlayer();

			if (player.Duel == null ||
			    player == player.Duel.Initiator ||
			    player.Duel.State != DuelState.Challenged)
				return;

			Player target = player.Duel.Opponent;

			if (target.PlayerData.DuelArbiter != arbiterGuid)
				return;

			Log.outDebug(LogFilter.Network, "Player 1 is: {0} ({1})", player.GetGUID().ToString(), player.GetName());
			Log.outDebug(LogFilter.Network, "Player 2 is: {0} ({1})", target.GetGUID().ToString(), target.GetName());

			long now = GameTime.GetGameTime();
			player.Duel.StartTime = now + 3;
			target.Duel.StartTime = now + 3;

			player.Duel.State = DuelState.Countdown;
			target.Duel.State = DuelState.Countdown;

			DuelCountdown packet = new(3000);

			player.SendPacket(packet);
			target.SendPacket(packet);

			player.EnablePvpRules();
			target.EnablePvpRules();
		}

		private void HandleDuelCancelled()
		{
			Player player = GetPlayer();

			// no Duel requested
			if (player.Duel == null ||
			    player.Duel.State == DuelState.Completed)
				return;

			// player surrendered in a Duel using /forfeit
			if (player.Duel.State == DuelState.InProgress)
			{
				player.CombatStopWithPets(true);
				player.Duel.Opponent.CombatStopWithPets(true);

				player.CastSpell(GetPlayer(), 7267, true); // beg
				player.DuelComplete(DuelCompleteType.Won);

				return;
			}

			player.DuelComplete(DuelCompleteType.Interrupted);
		}
	}
}