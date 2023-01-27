using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Called when a duel starts (after 3s countdown);
	public interface IPlayerOnDuelStart : IScriptObject
	{
		void OnDuelStart(Player player1, Player player2);
	}
}