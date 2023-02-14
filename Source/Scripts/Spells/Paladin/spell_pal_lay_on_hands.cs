// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
    [SpellScript(633)] // 633 - Lay on Hands
    internal class spell_pal_lay_on_hands : SpellScript, ISpellCheckCast, ISpellAfterHit
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
            return ValidateSpellInfo(PaladinSpells.Forbearance) //, PaladinSpells.ImmuneShieldMarker);
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
