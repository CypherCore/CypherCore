using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{

    //Second wind - 29838
    [SpellScript(29838)]

    public class spell_warr_second_wind_proc : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (caster.IsInCombat())
            {
                caster.CastSpell(caster, WarriorSpells.SECOND_WIND_DAMAGED, true);
            }
        }

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (!caster.IsInCombat())
            {
                caster.RemoveAura(WarriorSpells.SECOND_WIND_DAMAGED);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
            AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}
