using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IPlayer
{
    public interface IPlayerOnCooldownEnd : IScriptObject, IClassRescriction
    {
        void OnCooldownEnd(Player player, SpellInfo spellInfo, uint itemId, uint categoryId);
    }
}
