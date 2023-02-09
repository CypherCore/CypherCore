using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // Spell 196935 - Voodoo Totem
    // AT - 11577
    [Script]
    public class at_sha_voodoo_totem : AreaTriggerAI
    {
        public at_sha_voodoo_totem(AreaTrigger areaTrigger) : base(areaTrigger)
        {
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster == null || unit == null)
            {
                return;
            }

            if (caster.IsValidAttackTarget(unit))
            {
                caster.CastSpell(unit, TotemSpells.SPELL_TOTEM_VOODOO_EFFECT, true);
                caster.CastSpell(unit, TotemSpells.SPELL_TOTEM_VOODOO_COOLDOWN, true);
            }
        }

        public override void OnUnitExit(Unit unit)
        {
            unit.RemoveAurasDueToSpell(TotemSpells.SPELL_TOTEM_VOODOO_EFFECT, at.GetCasterGuid());
        }
    }
}
