using System.Collections.Generic;
using Game.Scripting.Interfaces.ISpell;

namespace Game.Scripting.Interfaces
{
    public interface IHasSpellEffects
    {
        List<ISpellEffect> SpellEffects { get; }
    }
}