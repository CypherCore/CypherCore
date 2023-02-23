// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 235313 - Blazing Barrier
internal class spell_mage_blazing_barrier : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.BlazingBarrierTrigger);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void CalculateAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
	{
		canBeRecalculated = false;
		var caster = GetCaster();

		if (caster)
			amount = (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 7.0f);
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = eventInfo.GetDamageInfo().GetVictim();
		var target = eventInfo.GetDamageInfo().GetAttacker();

		if (caster && target)
			caster.CastSpell(target, MageSpells.BlazingBarrierTrigger, true);
	}
}