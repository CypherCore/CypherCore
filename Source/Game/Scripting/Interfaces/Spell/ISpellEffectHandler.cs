using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;

namespace Game.Scripting.Interfaces.Spell
{
    public interface ISpellEffectHandler : ISpellEffect
    {
        SpellEffectName EffectName { get; }
        SpellScriptHookType HookType { get; }
    }

    public class EffectHandler : SpellEffect, ISpellEffectHandler
    {
        public EffectHandler(SpellEffectFn callEffect, uint effectIndex, SpellEffectName spellEffectName, SpellScriptHookType hookType) : base(callEffect, effectIndex)
        {
            EffectName = spellEffectName;
            HookType = hookType;
        }

        public SpellEffectName EffectName { get; private set; }

        public SpellScriptHookType HookType { get; private set; }
    }
}
