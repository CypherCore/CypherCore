// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(new uint[]
             {
	             178963, 203794, 228532
             })]
public class spell_dh_soul_fragment_heals : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHit(int effIndex)
	{
		if (!GetCaster().HasAura(DemonHunterSpells.DEMONIC_APPETITE))
			PreventHitDefaultEffect(effIndex);
	}

	private void HandleHeal(int UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var reductionTime = caster.GetAuraEffectAmount(DemonHunterSpells.FEAST_ON_THE_SOULS, 0);

		if (reductionTime != 0)
		{
			caster.GetSpellHistory().ModifyCooldown(DemonHunterSpells.CHAOS_NOVA, TimeSpan.FromSeconds(-reductionTime));
			caster.GetSpellHistory().ModifyCooldown(DemonHunterSpells.EYE_BEAM, TimeSpan.FromSeconds(-reductionTime));
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.LaunchTarget));
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.Launch));
	}
}