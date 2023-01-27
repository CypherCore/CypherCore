using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Called when a player choose a response from a PlayerChoice
	public interface IPlayerOnPlayerChoiceResponse : IScriptObject
	{
		void OnPlayerChoiceResponse(Player player, uint choiceId, uint responseId);
	}
}