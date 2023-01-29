using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // The following methods are called when a player sends a chat message.
    public interface IPlayerOnChat : IScriptObject
    {
        void OnChat(Player player, ChatMsg type, Language lang, string msg);
    }
}