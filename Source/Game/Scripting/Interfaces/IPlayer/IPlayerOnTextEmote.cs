using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	public interface IPlayerOnTextEmote : IScriptObject
	{
		void OnTextEmote(Player player, uint textEmote, uint emoteNum, ObjectGuid guid);
	}
}