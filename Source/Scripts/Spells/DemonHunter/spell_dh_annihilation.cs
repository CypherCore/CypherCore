// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201427)]
public class spell_dh_annihilation : SpellScript, ISpellBeforeHit
{
	public void BeforeHit(SpellMissInfo missInfo)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var target = caster.GetVictim();

			if (target == null)
				return;

			var   attackPower = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) + 28.7f;
			double damage      = GetHitDamage();

			SetHitDamage(damage + attackPower);

			if (RandomHelper.randChance(20))
				caster.ModifyPower(PowerType.Fury, +20);
		}
	}
}