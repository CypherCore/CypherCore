using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	public interface IPlayerOnPVPKill : IScriptObject
	{
		void OnPVPKill(Player killer, Player killed);
	}
}