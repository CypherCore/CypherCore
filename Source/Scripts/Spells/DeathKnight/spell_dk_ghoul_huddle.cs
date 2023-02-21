// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47484)]
public class spell_dk_ghoul_huddle : SpellScript, ISpellAfterHit
{

	public void AfterHit(){
        var caster = GetCaster();

        if (caster == null)
            return;

        Unit owner = caster.GetOwner().ToPlayer();
        if (owner != null)
        {
			caster.CastSpell(caster.HasAura(DeathKnightSpells.DARK_TRANSFORMATION) ? DeathKnightSpells.DT_GHOUL_HUDDLE : DeathKnightSpells.GHOUL_HUDDLE,true);
        }
    }
}