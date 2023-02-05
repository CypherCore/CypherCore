using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    //200860 Unrivaled Strenght
    [SpellScript(200860)]
    public class spell_warr_unrivaled_strenght : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
        {
            GetCaster().CastSpell(GetCaster(), 200977, true);
            if (GetCaster().HasAura(200977))
            {
                GetCaster().GetAura(200977).GetEffect(0).SetAmount(aurEff.GetBaseAmount());
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
