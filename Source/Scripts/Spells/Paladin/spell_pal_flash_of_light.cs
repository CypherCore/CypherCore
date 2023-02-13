using Framework.Constants;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // Flash of Light - 19750
    [SpellScript(19750)]
    public class spell_pal_flash_of_light : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            GetCaster().RemoveAurasDueToSpell(PaladinSpells.InfusionOfLightAura);
        }
    }
}
