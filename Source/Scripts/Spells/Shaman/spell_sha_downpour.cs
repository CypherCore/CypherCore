using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 207778 - Downpour
[SpellScript(207778)]
internal class spell_sha_downpour : SpellScript, ISpellAfterCast, ISpellAfterHit, IHasSpellEffects
{
	private int _healedTargets = 0;

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 1;
	}

	public void AfterCast()
	{
		var cooldown = TimeSpan.FromMilliseconds(GetSpellInfo().RecoveryTime) + TimeSpan.FromSeconds(GetEffectInfo(1).CalcValue() * _healedTargets);
		GetCaster().GetSpellHistory().StartCooldown(GetSpellInfo(), 0, GetSpell(), false, cooldown);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
	}

	public void AfterHit()
	{
		// Cooldown increased for each Target effectively healed
		if (GetHitHeal() != 0)
			++_healedTargets;
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void FilterTargets(List<WorldObject> targets)
	{
		SelectRandomInjuredTargets(targets, 6, true);
	}
}