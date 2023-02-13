using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // 26573 - Consecration
    [Script] //  9228 - AreaTriggerId
    internal class areatrigger_pal_consecration : AreaTriggerAI
    {
        public areatrigger_pal_consecration(AreaTrigger areatrigger) : base(areatrigger)
        {
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();

            if (caster != null)
            {
                // 243597 is also being cast as protection, but CreateObject is not sent, either serverside areatrigger for this aura or unused - also no visual is seen
                if (unit == caster &&
                    caster.IsPlayer() &&
                    caster.ToPlayer().GetPrimarySpecialization() == TalentSpecialization.PaladinProtection)
                    caster.CastSpell(caster, PaladinSpells.ConsecrationProtectionAura);

                if (caster.IsValidAttackTarget(unit))
                    if (caster.HasAura(PaladinSpells.ConsecratedGroundPassive))
                        caster.CastSpell(unit, PaladinSpells.ConsecratedGroundSlow);
            }
        }

        public override void OnUnitExit(Unit unit)
        {
            if (at.GetCasterGuid() == unit.GetGUID())
                unit.RemoveAurasDueToSpell(PaladinSpells.ConsecrationProtectionAura, at.GetCasterGuid());

            unit.RemoveAurasDueToSpell(PaladinSpells.ConsecratedGroundSlow, at.GetCasterGuid());
        }
    }
}
