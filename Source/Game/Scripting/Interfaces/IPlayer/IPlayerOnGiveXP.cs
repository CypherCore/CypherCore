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
    // Called when a player gains XP (before anything is given);
    public interface IPlayerOnGiveXP : IScriptObject
    {
        void OnGiveXP(Player player, ref uint amount, Unit victim);
    }
}

