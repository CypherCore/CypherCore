// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47468)]
public class spell_dk_ghoul_claw : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();
		var target = GetExplTargetUnit();

		if (caster == null || target == null)
			return;

		Unit owner = caster.GetOwner().ToPlayer();

		if (owner != null)
			if (owner.HasAura(DeathKnightSpells.SPELL_DK_INFECTED_CLAWS))
				if (RandomHelper.randChance(30))
					caster.CastSpell(target, DeathKnightSpells.SPELL_DK_FESTERING_WOUND_DAMAGE, true);
	}
}