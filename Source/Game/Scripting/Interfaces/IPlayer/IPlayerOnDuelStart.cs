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
    // Called when a duel starts (after 3s countdown);
    public interface IPlayerOnDuelStart : IScriptObject
    {
        void OnDuelStart(Player player1, Player player2);

    }
}

