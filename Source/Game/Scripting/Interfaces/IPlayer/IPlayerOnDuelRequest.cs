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
    // Called when a Duel is requested
    public interface IPlayerOnDuelRequest : IScriptObject
    {
        void OnDuelRequest(Player target, Player challenger);
    }
}

