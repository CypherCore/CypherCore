using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IPlayer
{
    public interface IPlayerOnDealDamage : IScriptObject, IClassRescriction
    {
        void OnDamage(Player caster, Unit target, ref uint damage, SpellInfo spellProto);
    }
}
