using Framework.Constants;
using Game.Entities;
using Game.Groups;

namespace Game.Scripting.Interfaces.IPlayer
{
	public interface IPlayerOnChatGroup : IScriptObject
	{
		void OnChat(Player player, ChatMsg type, Language lang, string msg, Group group);
	}
}