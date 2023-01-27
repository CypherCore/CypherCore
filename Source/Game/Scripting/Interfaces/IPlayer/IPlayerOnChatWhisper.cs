using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	public interface IPlayerOnChatWhisper : IScriptObject
	{
		void OnChat(Player player, ChatMsg type, Language lang, string msg, Player receiver);
	}
}