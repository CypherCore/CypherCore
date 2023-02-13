﻿using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(189110)]
public class spell_dh_infernal_strike : SpellScript, ISpellOnCast, ISpellOnHit
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(DemonHunterSpells.SPELL_DH_INFERNAL_STRIKE_JUMP, DemonHunterSpells.SPELL_DH_INFERNAL_STRIKE_DAMAGE);
	}

	public void OnHit()
	{
		var caster = GetCaster();
		var dest   = GetHitDest();
		var target = GetHitUnit();

		if (caster == null || dest == null || target == null)
			return;

		if (target.IsHostileTo(caster))
		{
			caster.CastSpell(new Position(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ()), DemonHunterSpells.SPELL_DH_INFERNAL_STRIKE_JUMP, true);
			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_INFERNAL_STRIKE_VISUAL, true);
		}
	}

	public void OnCast()
	{
		var caster = GetCaster();

		if (caster != null)
			caster.m_Events.AddEventAtOffset(new event_dh_infernal_strike(caster), TimeSpan.FromMilliseconds(750));
	}
}