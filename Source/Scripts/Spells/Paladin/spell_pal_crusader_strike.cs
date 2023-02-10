using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // Crusader Strike - 35395
    [SpellScript(35395)]
    public class spell_pal_crusader_strike : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();

            if (caster.HasAura(PaladinSpells.SPELL_PALADIN_CRUSADERS_MIGHT))
            {
                if (caster.GetSpellHistory().HasCooldown(PaladinSpells.SPELL_PALADIN_HOLY_SHOCK_GENERIC))
                {
                    caster.GetSpellHistory().ModifyCooldown(PaladinSpells.SPELL_PALADIN_HOLY_SHOCK_GENERIC, TimeSpan.FromMilliseconds(-1 * Time.InMilliseconds));
                }

                if (caster.GetSpellHistory().HasCooldown(PaladinSpells.SPELL_PALADIN_LIGHT_OF_DAWN))
                {
                    caster.GetSpellHistory().ModifyCooldown(PaladinSpells.SPELL_PALADIN_LIGHT_OF_DAWN, TimeSpan.FromMilliseconds(-1 * Time.InMilliseconds));
                }
            }
        }
    }
}
