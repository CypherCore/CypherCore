using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Both of the below are called on emote opcodes.
	public interface IPlayerOnClearEmote : IScriptObject
	{
		void OnClearEmote(Player player);
	}
}