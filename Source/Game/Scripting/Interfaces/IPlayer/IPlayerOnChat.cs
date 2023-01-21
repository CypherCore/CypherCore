using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Chat;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Spells;

namespace Game.Scripting.Interfaces.IPlayer
{
    // The following methods are called when a player sends a chat message.
    public interface IPlayerOnChat : IScriptObject
    {
        void OnChat(Player player, ChatMsg type, Language lang, string msg);

    }
}

