using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{

    //Second wind (triggered) - 202149
    [SpellScript(202149)]
    public class spell_warr_second_wind_damaged : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (caster.IsInCombat())
            {
                caster.RemoveAura(WarriorSpells.SECOND_WIND_HEAL);
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
                caster.CastSpell(caster, WarriorSpells.SECOND_WIND_HEAL, true);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
            AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}
