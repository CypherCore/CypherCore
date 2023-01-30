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
    // Called when a player switches to a new zone
    public interface IPlayerOnUpdateZone : IScriptObject
    {
        void OnUpdateZone(Player player, uint newZone, uint newArea);

    }

}

