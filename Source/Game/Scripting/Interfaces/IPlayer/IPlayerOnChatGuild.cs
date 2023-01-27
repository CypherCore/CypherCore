using Framework.Constants;
using Game.Entities;
using Game.Guilds;

namespace Game.Scripting.Interfaces.IPlayer
{
	public interface IPlayerOnChatGuild : IScriptObject
	{
		void OnChat(Player player, ChatMsg type, Language lang, string msg, Guild guild);
	}
}