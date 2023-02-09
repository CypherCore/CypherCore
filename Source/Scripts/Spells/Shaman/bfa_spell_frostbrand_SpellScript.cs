using Framework.Constants;
using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Shaman
{
    // Frostbrand - 196834
    [SpellScript(196834)]
    public class bfa_spell_frostbrand_SpellScript : SpellScript, ISpellOnHit
    {
        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            if (caster == null || target == null)
            {
                return;
            }

            caster.CastSpell(target, ShamanSpells.SPELL_FROSTBRAND_SLOW, true);
        }
    }
}
