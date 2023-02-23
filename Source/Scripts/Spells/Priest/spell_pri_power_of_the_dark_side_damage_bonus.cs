// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
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
		return ValidateSpellInfo(PriestSpells.POWER_OF_THE_DARK_SIDE);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.LaunchTarget));
	}

	private void HandleLaunchTarget(int effIndex)
	{
		var powerOfTheDarkSide = GetCaster().GetAuraEffect(PriestSpells.POWER_OF_THE_DARK_SIDE, 0);

		if (powerOfTheDarkSide != null)
		{
			PreventHitDefaultEffect(effIndex);

			double damageBonus = GetCaster().SpellDamageBonusDone(GetHitUnit(), GetSpellInfo(), (uint)GetEffectValue(), DamageEffectType.SpellDirect, GetEffectInfo(), 1, GetSpell());
			var   value       = damageBonus + damageBonus * GetEffectVariance();
			value *= 1.0f + (powerOfTheDarkSide.GetAmount() / 100.0f);
			value =  GetHitUnit().SpellDamageBonusTaken(GetCaster(), GetSpellInfo(), (uint)value, DamageEffectType.SpellDirect);
			SetHitDamage((int)value);
		}
	}
}