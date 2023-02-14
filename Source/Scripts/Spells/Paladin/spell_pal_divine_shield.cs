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
    [SpellScript(642)] // 642 - Divine Shield
    internal class spell_pal_divine_shield : SpellScript, ISpellCheckCast, ISpellAfterCast
    {
        public void AfterCast()
        {
            Unit caster = GetCaster();

            if (caster.HasAura(PaladinSpells.FinalStand))
                caster.CastSpell((Unit)null, PaladinSpells.FinalStandEffect, true);


            caster.CastSpell(caster, PaladinSpells.Forbearance, true);
            caster.CastSpell(caster, PaladinSpells.ImmuneShieldMarker, true);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.FinalStand, PaladinSpells.FinalStandEffect, PaladinSpells.Forbearance) //, SpellIds._PALADIN_IMMUNE_SHIELD_MARKER // uncomment when we have serverside only spells
                   &&
                   spellInfo.ExcludeCasterAuraSpell == PaladinSpells.ImmuneShieldMarker;
        }

        public SpellCastResult CheckCast()
        {
            if (GetCaster().HasAura(PaladinSpells.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }
    }
}
