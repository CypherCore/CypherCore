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
    // Aura of Sacrifice - 183416
    // AreaTriggerID - 100102 (custom)
    [Script]
    public class at_pal_aura_of_sacrifice : AreaTriggerAI
    {
        public at_pal_aura_of_sacrifice(AreaTrigger areatrigger) : base(areatrigger)
        {
            at.SetPeriodicProcTimer(1000);
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                if (unit.IsPlayer() && caster.IsPlayer() && caster != unit)
                {
                    if (caster.ToPlayer().IsInSameRaidWith(unit.ToPlayer()))
                    {
                        caster.CastSpell(unit, PaladinSpells.AURA_OF_SACRIFICE_ALLY, true);
                    }
                }
            }
        }

        public override void OnUnitExit(Unit unit)
        {
            unit.RemoveAurasDueToSpell(PaladinSpells.AURA_OF_SACRIFICE_ALLY);
        }
    }
}
