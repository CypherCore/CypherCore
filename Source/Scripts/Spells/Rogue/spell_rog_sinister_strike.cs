using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 193315 - Sinister Strike
internal class spell_rog_sinister_strike : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(RogueSpells.T52pSetBonus);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		int        damagePerCombo = GetHitDamage();
		AuraEffect t5             = GetCaster().GetAuraEffect(RogueSpells.T52pSetBonus, 0);

		if (t5 != null)
			damagePerCombo += t5.GetAmount();

		int finalDamage = damagePerCombo;
		var costs       = GetSpell().GetPowerCost();
		var c           = costs.Find(cost => cost.Power == PowerType.ComboPoints);

		if (c != null)
			finalDamage *= c.Amount;

		SetHitDamage(finalDamage);
	}
}