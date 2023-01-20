using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting.Interfaces
{
    public interface IHasSpellEffects
    {
        List<ISpellEffect> SpellEffects { get; }
    }
}
