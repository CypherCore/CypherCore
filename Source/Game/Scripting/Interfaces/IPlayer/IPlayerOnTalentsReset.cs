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
    // Called when a player's talent points are reset (right before the reset is done);
    public interface IPlayerOnTalentsReset : IScriptObject
    {
        void OnTalentsReset(Player player, bool noCost);
    }

}

