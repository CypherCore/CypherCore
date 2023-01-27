using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Called after a player's quest status has been changed
	public interface IPlayerOnQuestStatusChange : IScriptObject
	{
		void OnQuestStatusChange(Player player, uint questId);
	}
}