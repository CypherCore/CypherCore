using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Paladin
{
    // Justicar's Vengeance - 215661
    [SpellScript(215661)]
    public class spell_pal_justicars_vengeance : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target == null)
            {
                return;
            }

            if (target.HasAuraType(AuraType.ModStun) || target.HasAuraWithMechanic(1 << (int)Mechanics.Stun))
            {
                int damage = GetHitDamage();
                MathFunctions.AddPct(ref damage, 50);

                SetHitDamage(damage);
                SetEffectValue(damage);
            }

            if (caster.HasAura(PaladinSpells.SPELL_PALADIN_DIVINE_PURPOSE_RET_AURA))
            {
                caster.RemoveAurasDueToSpell(PaladinSpells.SPELL_PALADIN_DIVINE_PURPOSE_RET_AURA);
            }

            if (caster.HasAura(PaladinSpells.SPELL_PALADIN_FIST_OF_JUSTICE_RETRI))
            {
                if (caster.GetSpellHistory().HasCooldown(PaladinSpells.SPELL_PALADIN_HAMMER_OF_JUSTICE))
                {
                    caster.GetSpellHistory().ModifyCooldown(PaladinSpells.SPELL_PALADIN_HAMMER_OF_JUSTICE, TimeSpan.FromSeconds(-10 * Time.InMilliseconds));
                }
            }
        }
    }
}
