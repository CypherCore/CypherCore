using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{

    // Demonwrath periodic - 193440
    [SpellScript(193440)]
    public class spell_warl_demonwrath_periodic : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandlePeriodic(AuraEffect UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            int rollChance = GetSpellInfo().GetEffect(2).BasePoints;
            if (RandomHelper.randChance(rollChance))
            {
                caster.CastSpell(caster, WarlockSpells.DEMONWRATH_SOULSHARD, true);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicTriggerSpell));
        }
    }
}
