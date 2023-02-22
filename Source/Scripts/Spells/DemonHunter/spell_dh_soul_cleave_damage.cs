// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(228478)]
public class spell_dh_soul_cleave_damage : SpellScript, IHasSpellEffects, ISpellOnHit
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private readonly int m_ExtraSpellCost = 0;

	public void OnHit()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		double dmg = GetHitDamage() * 2;
		dmg *= caster.VariableStorage.GetValue<double>("lastSoulCleaveMod", 0);
		SetHitDamage(dmg);
	}

	private void HandleDamage(int effIndex)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var dmg = GetHitDamage() * 2;
		dmg = (int)((double)dmg * (((double)m_ExtraSpellCost + 300.0f) / 600.0f));
		SetHitDamage(dmg);

		caster.SetPower(PowerType.Pain, caster.GetPower(PowerType.Pain) - m_ExtraSpellCost);
		caster.ToPlayer().SetPower(PowerType.Pain, caster.GetPower(PowerType.Pain) - m_ExtraSpellCost);

		if (caster.HasAura(DemonHunterSpells.GLUTTONY_BUFF))
			caster.RemoveAura(DemonHunterSpells.GLUTTONY_BUFF);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.WeaponPercentDamage, SpellScriptHookType.EffectHitTarget));
	}
}