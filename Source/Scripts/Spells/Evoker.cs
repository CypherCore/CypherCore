// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.Spell;
using Game.Spells;
using System;

namespace Scripts.Spells.Evoker
{
    struct SpellIds
    {
        public const uint GlideKnockback = 358736;
        public const uint Hover = 358267;
        public const uint SoarRacial = 369536;
    }

    [Script] // 358733 - Glide (Racial)
    class spell_evo_glide : SpellScript, ICheckCastHander, IOnCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlideKnockback, SpellIds.Hover, SpellIds.SoarRacial);
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();

            if (!caster.IsFalling())
                return SpellCastResult.NotOnGround;

            return SpellCastResult.SpellCastOk;
        }

        public void OnCast()
        {
            Player caster = GetCaster().ToPlayer();
            if (caster == null)
                return;

            caster.CastSpell(caster, SpellIds.GlideKnockback, true);

            caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(SpellIds.Hover, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
            caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(SpellIds.SoarRacial, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
        }
    }
}
