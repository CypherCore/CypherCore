using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Called when a player is about to be saved.
	public interface IPlayerOnSave : IScriptObject
	{
		void OnSave(Player player);
	}
}