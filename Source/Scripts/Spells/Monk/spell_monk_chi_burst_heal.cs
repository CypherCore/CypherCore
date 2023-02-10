using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(130654)]
public class spell_monk_chi_burst_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleHeal(uint UnnamedParameter)
	{
		var caster = GetCaster();
		var unit   = GetHitUnit();

		if (caster == null || unit == null)
			return;

		var spellInfo = Global.SpellMgr.GetSpellInfo(MonkSpells.SPELL_MONK_CHI_BURST_HEAL, Difficulty.None);

		if (spellInfo == null)
			return;

		var effectInfo = spellInfo.GetEffect(0);

		if (!effectInfo.IsEffect())
			return;

		var damage = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 4.125f;
		damage = caster.SpellDamageBonusDone(unit, spellInfo, damage, DamageEffectType.Heal, effectInfo);
		damage = unit.SpellDamageBonusTaken(caster, spellInfo, damage, DamageEffectType.Heal);

		SetHitHeal(damage);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}
}