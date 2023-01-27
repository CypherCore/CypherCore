using Framework.Constants;
using Game.Chat;
using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
	public interface IPlayerOnChatChannel : IScriptObject
	{
		void OnChat(Player player, ChatMsg type, Language lang, string msg, Channel channel);
	}
}