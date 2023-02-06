using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 211219 - The Expendables
    [SpellScript(211219)]
    public class spell_warlock_artifact_the_expendables : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (caster.ToPlayer())
            {
                return;
            }

            Player player = caster.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (player == null)
            {
                return;
            }

            foreach (Unit unit in player.m_Controlled)
            {
                player.CastSpell(unit, WarlockSpells.THE_EXPANDABLES_BUFF, true);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}
