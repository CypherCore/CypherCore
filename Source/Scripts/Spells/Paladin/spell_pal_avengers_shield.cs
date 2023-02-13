using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // 31935 - Avenger's Shield
    [SpellScript(31935)]
    public class spell_pal_avengers_shield : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target == null)
            {
                return;
            }

            if (caster.HasAura(PaladinSpells.GRAND_CRUSADER_PROC))
            {
                caster.RemoveAurasDueToSpell(PaladinSpells.GRAND_CRUSADER_PROC);
            }

            int damage = GetHitDamage();

            if (caster.HasAura(PaladinSpells.FIRST_AVENGER))
            {
                MathFunctions.AddPct(ref damage, 50);
            }

            SetHitDamage(damage);
        }
    }
}
