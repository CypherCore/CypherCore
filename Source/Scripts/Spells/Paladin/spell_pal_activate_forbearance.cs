using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // Activate Forbearance
    // Called by Blessing of Protection - 1022, Lay on Hands - 633, Blessing of Spellwarding - 204018
    [SpellScript(new uint[] { 1022, 633, 204018 })]
    public class spell_pal_activate_forbearance : SpellScript, ISpellOnHit, ISpellCheckCast
    {
        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(PaladinSpells.Forbearance);
        }

        public SpellCastResult CheckCast()
        {
            Unit target = GetExplTargetUnit();
            if (target != null)
            {
                if (target.HasAura(PaladinSpells.Forbearance))
                {
                    return SpellCastResult.TargetAurastate;
                }
            }
            return SpellCastResult.SpellCastOk;
        }

        public void OnHit()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                Unit target = GetHitUnit();
                if (target != null)
                {
                    player.CastSpell(target, PaladinSpells.Forbearance, true);
                }
            }
        }
    }
}
