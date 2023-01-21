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
    public interface IPlayerOnChatWhisper : IScriptObject
    {
        void OnChat(Player player, ChatMsg type, Language lang, string msg, Player receiver);

    }

}

