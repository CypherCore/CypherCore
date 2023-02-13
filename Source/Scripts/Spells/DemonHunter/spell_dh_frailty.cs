﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(224509)]
public class spell_dh_frailty : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();
	uint _damage = 0;

	private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = GetCaster();

		if (caster == null || caster != eventInfo.GetActor() || eventInfo.GetDamageInfo() != null)
			return;

		_damage += MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
	}

	private void PeriodicTick(AuraEffect UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (_damage != 0)
		{
			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_FRAILTY_HEAL, (int)(_damage * .1), true);
			_damage = 0;
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDummy));
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
	}
}