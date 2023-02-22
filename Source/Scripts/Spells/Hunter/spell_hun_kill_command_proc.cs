// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(83381)]
public class spell_hun_kill_command_proc : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDamage(int effIndex)
	{
		var caster = GetCaster();
		var owner  = caster.GetOwner();
		var target = GetExplTargetUnit();

		// (1.5 * (rap * 3) * bmMastery * lowNerf * (1 + versability))
		double dmg     = 4.5f * owner.m_unitData.RangedAttackPower;
		var lowNerf = Math.Min((int)owner.GetLevel(), 20) * 0.05f;

		var ownerPlayer = owner.ToPlayer();

		if (ownerPlayer != null)
			dmg = MathFunctions.AddPct(ref dmg, ownerPlayer.m_activePlayerData.Mastery);

		dmg *= lowNerf;

		dmg = caster.SpellDamageBonusDone(target, GetSpellInfo(), dmg, DamageEffectType.Direct, GetEffectInfo(0), 1, GetSpell());
		dmg = target.SpellDamageBonusTaken(caster, GetSpellInfo(), dmg, DamageEffectType.Direct);

		SetHitDamage(dmg);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}