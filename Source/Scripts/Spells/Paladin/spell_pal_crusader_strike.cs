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

            if (caster.HasAura(PaladinSpells.CRUSADERS_MIGHT))
            {
                if (caster.GetSpellHistory().HasCooldown(PaladinSpells.HolyShock))
                {
                    caster.GetSpellHistory().ModifyCooldown(PaladinSpells.HolyShock, TimeSpan.FromMilliseconds(-1 * Time.InMilliseconds));
                }

                if (caster.GetSpellHistory().HasCooldown(PaladinSpells.LIGHT_OF_DAWN))
                {
                    caster.GetSpellHistory().ModifyCooldown(PaladinSpells.LIGHT_OF_DAWN, TimeSpan.FromMilliseconds(-1 * Time.InMilliseconds));
                }
            }
        }
    }
}
