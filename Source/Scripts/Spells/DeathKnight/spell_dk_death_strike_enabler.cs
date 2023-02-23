// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 89832 - Death Strike Enabler - DEATH_STRIKE_ENABLER
internal class spell_dk_death_strike_enabler : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	// Amount of seconds we calculate Damage over
	private double[] _damagePerSecond = new double[5];

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetDamageInfo() != null;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(HandleCalcAmount, 0, AuraType.PeriodicDummy));
		AuraEffects.Add(new AuraEffectUpdatePeriodicHandler(Update, 0, AuraType.PeriodicDummy));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void Update(AuraEffect aurEff)
	{
		// Move backwards all datas by one from [23][0][0][0][0] -> [0][23][0][0][0]
		_damagePerSecond    = Enumerable.Range(1, _damagePerSecond.Length).Select(i => _damagePerSecond[i % _damagePerSecond.Length]).ToArray();
		_damagePerSecond[0] = 0;
	}

	private void HandleCalcAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
	{
		canBeRecalculated = true;
		amount            = Enumerable.Range(1, _damagePerSecond.Length).Sum();
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		_damagePerSecond[0] += eventInfo.GetDamageInfo().GetDamage();
	}
}