using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 47750 - Penance (Healing)
internal class spell_pri_power_of_the_dark_side_healing_bonus : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.PowerOfTheDarkSide);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.Heal, SpellScriptHookType.LaunchTarget));
	}

	private void HandleLaunchTarget(int effIndex)
	{
		var powerOfTheDarkSide = GetCaster().GetAuraEffect(PriestSpells.PowerOfTheDarkSide, 0);

		if (powerOfTheDarkSide != null)
		{
			PreventHitDefaultEffect(effIndex);

			float healingBonus = GetCaster().SpellHealingBonusDone(GetHitUnit(), GetSpellInfo(), (uint)GetEffectValue(), DamageEffectType.Heal, GetEffectInfo());
			var   value        = healingBonus + healingBonus * GetEffectVariance();
			value *= 1.0f + (powerOfTheDarkSide.GetAmount() / 100.0f);
			value =  GetHitUnit().SpellHealingBonusTaken(GetCaster(), GetSpellInfo(), (uint)value, DamageEffectType.Heal);
			SetHitHeal((int)value);
		}
	}
}