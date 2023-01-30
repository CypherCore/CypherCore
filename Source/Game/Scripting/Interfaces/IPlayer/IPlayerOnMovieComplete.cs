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

    // Called when a player completes a movie
    public interface IPlayerOnMovieComplete : IScriptObject
    {
        void OnMovieComplete(Player player, uint movieId);

    }


}

