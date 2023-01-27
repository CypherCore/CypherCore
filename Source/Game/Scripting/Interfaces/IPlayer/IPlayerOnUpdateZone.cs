using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	// Called when a player switches to a new zone
	public interface IPlayerOnUpdateZone : IScriptObject
	{
		void OnUpdateZone(Player player, uint newZone, uint newArea);
	}
}