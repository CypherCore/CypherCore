using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    //17364
    [SpellScript(17364)]
    public class spell_sha_stormstrike : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit target = GetHitUnit();
            if (target == null)
            {
                return;
            }

            if (GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_CRASHING_STORM_DUMMY) && GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_CRASH_LIGTHNING_AURA))
            {
                GetCaster().CastSpell(target, ShamanSpells.SPELL_SHAMAN_CRASHING_LIGHTNING_DAMAGE, true);
            }

            if (GetCaster() && GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_CRASH_LIGTHNING_AURA))
            {
                GetCaster().CastSpell(null, ShamanSpells.SPELL_SHAMAN_CRASH_LIGHTNING_PROC, true);
            }
        }
    }
}
