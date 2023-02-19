// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Spells;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCalcSpellMod : IAuraEffectHandler
    {
        void CalcSpellMod(AuraEffect aura, ref SpellModifier spellMod);
    }

    public class AuraEffectCalcSpellModHandler : AuraEffectHandler, IAuraCalcSpellMod
    {
        public delegate void AuraEffectCalcSpellModDelegate(AuraEffect aura, ref SpellModifier spellMod);

        private readonly AuraEffectCalcSpellModDelegate _fn;

        public AuraEffectCalcSpellModHandler(AuraEffectCalcSpellModDelegate fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcSpellmod)
        {
            _fn = fn;
        }

        public void CalcSpellMod(AuraEffect aura, ref SpellModifier spellMod)
        {
            _fn(aura, ref spellMod);
        }
    }
}