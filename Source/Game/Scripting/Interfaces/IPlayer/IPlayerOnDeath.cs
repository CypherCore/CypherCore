using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    public interface IPlayerOnDeath : IScriptObject, IClassRescriction
    {
        void OnDeath(Player player);
    }
}
