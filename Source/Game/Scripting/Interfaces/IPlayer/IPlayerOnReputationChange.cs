using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Called when a player's reputation changes (before it is actually changed);
	public interface IPlayerOnReputationChange : IScriptObject
	{
		void OnReputationChange(Player player, uint factionId, int standing, bool incremental);
	}
}