// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(185358)]
public class spell_hun_arcane_shot : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		if (caster.HasAura(HunterSpells.MARKING_TARGETS))
		{
			caster.CastSpell(target, HunterSpells.HUNTERS_MARK_AURA, true);
			caster.CastSpell(caster, HunterSpells.HUNTERS_MARK_AURA_2, true);
			caster.RemoveAura(HunterSpells.MARKING_TARGETS);
		}

		if (caster.HasAura(HunterSpells.LETHAL_SHOTS) && RandomHelper.randChance(20))
			if (caster.GetSpellHistory().HasCooldown(HunterSpells.RAPID_FIRE))
				caster.GetSpellHistory().ModifyCooldown(HunterSpells.RAPID_FIRE, TimeSpan.FromSeconds(-5000));

		if (caster.HasAura(HunterSpells.CALLING_THE_SHOTS))
			if (caster.GetSpellHistory().HasCooldown(HunterSpells.TRUESHOT))
				caster.GetSpellHistory().ModifyCooldown(HunterSpells.TRUESHOT, TimeSpan.FromSeconds(-2500));
	}
}