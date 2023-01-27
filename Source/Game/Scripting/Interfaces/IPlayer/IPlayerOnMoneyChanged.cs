using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Called when a player's money is modified (before the modification is done);
	public interface IPlayerOnMoneyChanged : IScriptObject
	{
		void OnMoneyChanged(Player player, long amount);
	}
}