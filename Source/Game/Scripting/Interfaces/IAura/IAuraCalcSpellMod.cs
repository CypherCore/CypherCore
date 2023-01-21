using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.Aura
{
    public interface IAuraCalcSpellMod : IAuraEffectHandler
    {
        void CalcSpellMod(AuraEffect aura, ref SpellModifier spellMod);
    }

    public class EffectCalcSpellModHandler : AuraEffectHandler, IAuraCalcSpellMod
    {
        public delegate void AuraEffectCalcSpellModDelegate(AuraEffect aura, ref SpellModifier spellMod);
        AuraEffectCalcSpellModDelegate _fn;

        public EffectCalcSpellModHandler(AuraEffectCalcSpellModDelegate fn, uint effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcSpellmod)
        {
            _fn = fn;
        }

        public void CalcSpellMod(AuraEffect aura, ref SpellModifier spellMod)
        {
            _fn(aura, ref spellMod);
        }
    }
}
