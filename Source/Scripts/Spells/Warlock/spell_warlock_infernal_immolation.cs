using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Warlock
{

    // 19483 - Immolation
    public class spell_warlock_infernal_immolation : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


        private void PeriodicTick(AuraEffect UnnamedParameter)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            caster.CastSpell(caster, WarlockSpells.IMMOLATION_TRIGGERED, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(caster.GetOwnerGUID()));
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
        }
    }
}
