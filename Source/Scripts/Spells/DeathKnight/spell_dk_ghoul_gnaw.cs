// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47481)]
public class spell_dk_ghoul_gnaw : SpellScript, ISpellAfterHit
{

	public void AfterHit(){
        var caster = GetCaster();
        var target = GetExplTargetUnit();

        if (caster == null || target == null)
            return;

        Unit owner = caster.GetOwner().ToPlayer();
        if (owner != null)
        {
			caster.CastSpell(target, caster.HasAura(DeathKnightSpells.DARK_TRANSFORMATION) ? DeathKnightSpells.DT_GHOUL_GNAW : DeathKnightSpells.GHOUL_GNAW, true);
        }
    }
}