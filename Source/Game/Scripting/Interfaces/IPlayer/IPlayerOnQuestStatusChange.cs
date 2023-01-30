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
    // Called after a player's quest status has been changed
    public interface IPlayerOnQuestStatusChange : IScriptObject
    {
        void OnQuestStatusChange(Player player, uint questId);

    }

}

