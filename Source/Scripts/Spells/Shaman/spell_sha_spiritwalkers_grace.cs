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
    [SpellScript(79206)]
    public class spell_sha_spiritwalkers_grace : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            if (caster.HasAura(159651))
            {
                caster.CastSpell(caster, 159652, true);
            }
        }
    }
}
