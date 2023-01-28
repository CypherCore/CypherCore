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
    // Called when a Duel ends
    public interface IPlayerOnDuelEnd : IScriptObject
    {
        void OnDuelEnd(Player winner, Player loser, DuelCompleteType type);

    }
}

