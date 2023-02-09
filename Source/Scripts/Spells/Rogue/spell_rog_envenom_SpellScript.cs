using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 32645 - Envenom
internal class spell_rog_envenom_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(CalculateDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void CalculateDamage(uint effIndex)
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