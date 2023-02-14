// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(185244)]
public class spell_demon_hunter_pain : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		var caster = GetCaster();

		if (caster == null || eventInfo.GetDamageInfo() != null)
			return;

		if (eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsPositive())
			return;

		var damageTaken = eventInfo.GetDamageInfo().GetDamage();

		if (damageTaken <= 0)
			return;

		var painAmount = (50.0f * (float)damageTaken) / (float)caster.GetMaxHealth();
		caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_REWARD_PAIN, (int)painAmount);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ModPowerDisplay, AuraScriptHookType.EffectProc));
	}
}