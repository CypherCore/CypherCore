// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(206685)]
public class spell_hun_pet_cobra_spit : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void HandleDamage(int effIndex)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var owner = caster.GetOwner();

		if (owner == null)
			return;

		var target = GetExplTargetUnit();

		if (target == null)
			return;

		// (1 + AP * 0,2)
		double dmg = 1 + owner.m_unitData.RangedAttackPower * 0.2f;

		dmg = caster.SpellDamageBonusDone(target, GetSpellInfo(), dmg, DamageEffectType.Direct, GetEffectInfo(0), 1, GetSpell());
		dmg = target.SpellDamageBonusTaken(caster, GetSpellInfo(), dmg, DamageEffectType.Direct);

		SetHitDamage(dmg);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}