using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellEffectHandler : ISpellEffect
    {
        SpellEffectName EffectName { get; }

        void CallEffect(uint effIndex);
    }

    public class EffectHandler : SpellEffect, ISpellEffectHandler
    {
        public delegate void SpellEffectFn(uint index);

        public EffectHandler(SpellEffectFn callEffect, uint effectIndex, SpellEffectName spellEffectName, SpellScriptHookType hookType) : base(effectIndex, hookType)
        {
            EffectName = spellEffectName;
            _callEffect = callEffect;
        }

        public SpellEffectName EffectName { get; private set; }

        private readonly SpellEffectFn _callEffect;

        public void CallEffect(uint effIndex)
        {
            _callEffect(EffectIndex);
        }
    }
}
