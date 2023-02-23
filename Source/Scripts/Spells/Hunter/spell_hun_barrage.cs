// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(120361)]
public class spell_hun_barrage : SpellScript, IHasSpellEffects, ISpellOnHit
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void CheckLOS(List<WorldObject> targets)
	{
		if (targets.Count == 0)
			return;

		var caster = GetCaster();

		if (caster == null)
			return;


		targets.RemoveIf((WorldObject objects) =>
		                 {
			                 if (objects == null)
				                 return true;

			                 if (!objects.IsWithinLOSInMap(caster))
				                 return true;

			                 if (objects.ToUnit() && !caster.IsValidAttackTarget(objects.ToUnit()))
				                 return true;

			                 return false;
		                 });
	}

	public void OnHit()
	{
		var player = GetCaster().ToPlayer();
		var target = GetHitUnit();

		if (player == null || target == null)
			return;

		double minDamage = 0.0f;
		double maxDamage = 0.0f;

		player.CalculateMinMaxDamage(WeaponAttackType.RangedAttack, true, true, out minDamage, out maxDamage);

        double dmg = (minDamage + maxDamage) / 2 * 0.8f;

		if (!target.HasAura(HunterSpells.BARRAGE, player.GetGUID()))
			dmg /= 2;

		dmg = player.SpellDamageBonusDone(target, GetSpellInfo(), dmg, DamageEffectType.Direct, GetEffectInfo(0), 1, GetSpell());
		dmg = target.SpellDamageBonusTaken(player, GetSpellInfo(), dmg, DamageEffectType.Direct);

		// Barrage now deals only 80% of normal damage against player-controlled targets.
		if (target.GetSpellModOwner())
			dmg = MathFunctions.CalculatePct(dmg, 80);

		SetHitDamage(dmg);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CheckLOS, 0, Targets.UnitConeEnemy24));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CheckLOS, 1, Targets.UnitConeEnemy24));
	}
}