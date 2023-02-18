// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(new uint[]
             {
	             MonkSpells.CHI_WAVE_DAMAGE, MonkSpells.CHI_WAVE_HEAL
             })]
public class spell_monk_chi_wave_target_selector : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		m_shouldHeal = true; // just for initializing

		return true;
	}

	private void SelectTarget(List<WorldObject> targets)
	{
		if (targets.Count == 0)
			return;

		var spellInfo = GetTriggeringSpell();

		if (spellInfo.Id == 132467) // Triggered by damage, so we need heal selector
		{
			targets.RemoveIf(new HealUnitCheck(GetCaster()));
			targets.Sort(new HealthPctOrderPred(false)); // Reverse order due to target is selected via std::list back
			m_shouldHeal = true;
		}
		else if (spellInfo.Id == 132464) // Triggered by heal, so we need damage selector
		{
			targets.RemoveIf(new DamageUnitCheck(GetCaster(), 25.0f));
			m_shouldHeal = false;
		}

		if (targets.Count == 0)
			return;

		var target = targets.LastOrDefault();

		if (target == null)
			return;

		targets.Clear();
		targets.Add(target);
	}

	private void HandleDummy(uint UnnamedParameter)
	{
		if (GetEffectValue() != 0) // Ran out of bounces
			return;

		if (!GetExplTargetUnit() || !GetOriginalCaster())
			return;

		var target = GetHitUnit();

		if (m_shouldHeal)
			GetExplTargetUnit().CastSpell(target, 132464, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, GetEffectValue()).SetOriginalCaster(GetOriginalCaster().GetGUID()));
		else
			GetExplTargetUnit().CastSpell(target, 132467, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, GetEffectValue()).SetOriginalCaster(GetOriginalCaster().GetGUID()));
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(SelectTarget, 1, Targets.UnitDestAreaEntry));
		SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private bool m_shouldHeal;
}