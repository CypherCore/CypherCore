using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Called when a player is killed by a creature
	public interface IPlayerOnPlayerKilledByCreature : IScriptObject
	{
		void OnPlayerKilledByCreature(Creature killer, Player killed);
	}
}