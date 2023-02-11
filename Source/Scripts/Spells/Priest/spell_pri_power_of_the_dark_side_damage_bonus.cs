﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 47666 - Penance (Damage)
internal class spell_pri_power_of_the_dark_side_damage_bonus : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.PowerOfTheDarkSide);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.LaunchTarget));
	}

	private void HandleLaunchTarget(uint effIndex)
	{
		var powerOfTheDarkSide = GetCaster().GetAuraEffect(PriestSpells.PowerOfTheDarkSide, 0);

		if (powerOfTheDarkSide != null)
		{
			PreventHitDefaultEffect(effIndex);

			float damageBonus = GetCaster().SpellDamageBonusDone(GetHitUnit(), GetSpellInfo(), (uint)GetEffectValue(), DamageEffectType.SpellDirect, GetEffectInfo());
			var   value       = damageBonus + damageBonus * GetEffectVariance();
			value *= 1.0f + (powerOfTheDarkSide.GetAmount() / 100.0f);
			value =  GetHitUnit().SpellDamageBonusTaken(GetCaster(), GetSpellInfo(), (uint)value, DamageEffectType.SpellDirect);
			SetHitDamage((int)value);
		}
	}
}