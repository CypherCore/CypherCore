using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(196301)]
    public class spell_warlock_artifact_devourer_of_life : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (RandomHelper.randChance(aurEff.GetAmount()))
            {
                caster.CastSpell(caster, WarlockSpells.DEVOURER_OF_LIFE_PROC, true);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
