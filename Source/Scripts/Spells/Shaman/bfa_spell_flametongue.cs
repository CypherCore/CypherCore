using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
    // Flametongue - 193796
    [SpellScript(193796)]
    public class bfa_spell_flametongue : SpellScript, ISpellOnHit
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

            if (caster.HasAura(ShamanSpells.SPELL_SEARING_ASSAULT_TALENT))
            {
                caster.CastSpell(target, ShamanSpells.SPELL_SEARING_ASSULAT_TALENT_PROC, true);
            }
        }
    }
}
