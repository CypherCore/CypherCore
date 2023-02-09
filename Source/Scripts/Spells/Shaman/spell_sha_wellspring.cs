using Framework.Constants;
using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Shaman
{
    //197995
    [SpellScript(197995)]
    public class spell_sha_wellspring : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            if (caster == null || target == null)
            {
                return;
            }

            caster.CastSpell(target, ShamanSpells.SPELL_SHAMAN_WELLSPRING_MISSILE, true);
        }
    }
}
