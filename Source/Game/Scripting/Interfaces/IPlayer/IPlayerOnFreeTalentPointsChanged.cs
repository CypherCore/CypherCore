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
    // Called when a player's free talent points change (right before the change is applied);
    public interface IPlayerOnFreeTalentPointsChanged : IScriptObject
    {
        void OnFreeTalentPointsChanged(Player player, uint points);

    }


}

