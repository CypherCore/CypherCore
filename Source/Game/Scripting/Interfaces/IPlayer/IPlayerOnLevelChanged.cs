using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Called when a player's level changes (after the level is applied);
	public interface IPlayerOnLevelChanged : IScriptObject
	{
		void OnLevelChanged(Player player, byte oldLevel);
	}
}