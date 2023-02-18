// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207311)]
public class spell_dk_clawing_shadows : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();
		var target = caster.ToPlayer().GetSelectedUnit();

		if (caster == null || target == null)
			return;

		caster.CastSpell(target, DeathKnightSpells.FESTERING_WOUND_DAMAGE, true);
	}
}