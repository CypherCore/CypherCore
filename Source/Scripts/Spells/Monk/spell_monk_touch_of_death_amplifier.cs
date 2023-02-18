// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(271233)]
public class spell_monk_touch_of_death_amplifier : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.TOUCH_OF_DEATH, MonkSpells.TOUCH_OF_DEATH_AMPLIFIER);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetDamageInfo() != null && eventInfo.GetDamageInfo().GetDamage() > 0;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		var aurEff = GetTarget().GetAuraEffect(MonkSpells.TOUCH_OF_DEATH, 0);

		if (aurEff != null)
		{
			var aurEffAmplifier = eventInfo.GetActor().GetAuraEffect(MonkSpells.TOUCH_OF_DEATH_AMPLIFIER, 0);

			if (aurEffAmplifier != null)
			{
				var damage = aurEff.GetAmount() + MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEffAmplifier.GetAmount());
				aurEff.SetAmount(damage);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}