using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // 1022 - Blessing of Protection
    // 204018 - Blessing of Spellwarding
    [SpellScript(new uint[] { 1022, 204018 })]
    internal class spell_pal_blessing_of_protection : SpellScript, ISpellCheckCast, ISpellAfterHit
    {
        public void AfterHit()
        {
            Unit target = GetHitUnit();

            if (target)
            {
                GetCaster().CastSpell(target, PaladinSpells.Forbearance, true);
                GetCaster().CastSpell(target, PaladinSpells.ImmuneShieldMarker, true);
            }
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.Forbearance) //, SpellIds._PALADIN_IMMUNE_SHIELD_MARKER) // uncomment when we have serverside only spells
                   &&
                   spellInfo.ExcludeTargetAuraSpell == PaladinSpells.ImmuneShieldMarker;
        }

        public SpellCastResult CheckCast()
        {
            Unit target = GetExplTargetUnit();

            if (!target ||
                target.HasAura(PaladinSpells.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }
    }
}
