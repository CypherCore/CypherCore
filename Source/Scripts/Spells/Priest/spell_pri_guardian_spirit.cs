// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 47788 - Guardian Spirit
internal class spell_pri_guardian_spirit : AuraScript, IHasAuraEffects
{
	private uint healPct;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.GUARDIAN_SPIRIT_HEAL) && spellInfo.GetEffects().Count > 1;
	}

	public override bool Load()
	{
		healPct = (uint)GetEffectInfo(1).CalcValue();

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 1, false, AuraScriptHookType.EffectAbsorb));
	}

	private void CalculateAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
	{
		// Set absorbtion amount to unlimited
		amount = -1;
	}

	private void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref double absorbAmount)
	{
		var target = GetTarget();

		if (dmgInfo.GetDamage() < target.GetHealth())
			return;

		var healAmount = (int)target.CountPctFromMaxHealth((int)healPct);
		// Remove the aura now, we don't want 40% healing bonus
		Remove(AuraRemoveMode.EnemySpell);
		CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
		args.AddSpellMod(SpellValueMod.BasePoint0, healAmount);
		target.CastSpell(target, PriestSpells.GUARDIAN_SPIRIT_HEAL, args);
		absorbAmount = dmgInfo.GetDamage();
	}
}