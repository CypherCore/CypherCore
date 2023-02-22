// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(63900)]
public class spell_hun_pet_thunderstomp : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void HandleDamage(int effIndex)
	{
		var caster = GetCaster();
		var owner  = GetCaster().GetOwner();
		var target = GetHitUnit();

		if (owner == null || target == null)
			return;

		double dmg = 1.5f * (owner.m_unitData.RangedAttackPower * 0.250f);

		dmg = caster.SpellDamageBonusDone(target, GetSpellInfo(), dmg, DamageEffectType.Direct, GetEffectInfo(0), 1, GetSpell());
		dmg = target.SpellDamageBonusTaken(caster, GetSpellInfo(), dmg, DamageEffectType.Direct);

		SetHitDamage(dmg);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}