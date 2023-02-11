﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[Script] // 124273, 124274, 124275 - Light/Moderate/Heavy Stagger - SPELL_MONK_STAGGER_LIGHT / SPELL_MONK_STAGGER_MODERATE / SPELL_MONK_STAGGER_HEAVY
internal class spell_monk_stagger_debuff_aura : AuraScript, IHasAuraEffects
{
	private float _period;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MonkSpells.StaggerDamageAura) && !Global.SpellMgr.GetSpellInfo(MonkSpells.StaggerDamageAura, Difficulty.None).GetEffects().Empty();
	}

	public override bool Load()
	{
		_period = (float)Global.SpellMgr.GetSpellInfo(MonkSpells.StaggerDamageAura, GetCastDifficulty()).GetEffect(0).ApplyAuraPeriod;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnReapply, 1, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void OnReapply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		// Calculate Damage per tick
		float total   = aurEff.GetAmount();
		var   perTick = total * _period / (float)GetDuration(); // should be same as GetMaxDuration() TODO: verify

		// Set amount on effect for tooltip
		var effInfo = GetAura().GetEffect(0);

		effInfo?.ChangeAmount((int)perTick);

		// Set amount on Damage aura (or cast it if needed)
		CastOrChangeTickDamage(perTick);
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (mode != AuraEffectHandleModes.Real)
			return;

		// Remove Damage aura
		GetTarget().RemoveAura(MonkSpells.StaggerDamageAura);
	}

	private void CastOrChangeTickDamage(float tickDamage)
	{
		var unit       = GetTarget();
		var auraDamage = unit.GetAura(MonkSpells.StaggerDamageAura);

		if (auraDamage == null)
		{
			unit.CastSpell(unit, MonkSpells.StaggerDamageAura, true);
			auraDamage = unit.GetAura(MonkSpells.StaggerDamageAura);
		}

		if (auraDamage != null)
		{
			var eff = auraDamage.GetEffect(0);

			eff?.ChangeAmount((int)tickDamage);
		}
	}
}