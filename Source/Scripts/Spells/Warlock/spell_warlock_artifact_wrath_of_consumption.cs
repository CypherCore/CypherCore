using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 199472 - Wrath of Consumption
    [SpellScript(199472)]
    public class spell_warlock_artifact_wrath_of_consumption : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                caster.CastSpell(caster, WarlockSpells.WRATH_OF_CONSUMPTION_PROC, true);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
