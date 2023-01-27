using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Called when a player kills a creature
	public interface IPlayerOnCreatureKill : IScriptObject
	{
		void OnCreatureKill(Player killer, Creature killed);
	}
}