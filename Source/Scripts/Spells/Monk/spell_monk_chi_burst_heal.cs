// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(130654)]
public class spell_monk_chi_burst_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHeal(int effIndex)
	{
		var caster = GetCaster();
		var unit   = GetHitUnit();

		if (caster == null || unit == null)
			return;

		var spellInfo = Global.SpellMgr.GetSpellInfo(MonkSpells.CHI_BURST_HEAL, Difficulty.None);

		if (spellInfo == null)
			return;

		var effectInfo = spellInfo.GetEffect(0);

		if (!effectInfo.IsEffect())
			return;

		var damage = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 4.125f;
		damage = caster.SpellDamageBonusDone(unit, spellInfo, damage, DamageEffectType.Heal, effectInfo, 1, GetSpell());
		damage = unit.SpellDamageBonusTaken(caster, spellInfo, damage, DamageEffectType.Heal);

		SetHitHeal(damage);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}
}