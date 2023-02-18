// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(202090)]
public class spell_monk_teachings_of_the_monastery_buff : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.TEACHINGS_OF_THE_MONASTERY_PASSIVE, MonkSpells.BLACKOUT_KICK_TRIGGERED, MonkSpells.BLACKOUT_KICK);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (!GetTarget().HasAura(MonkSpells.TEACHINGS_OF_THE_MONASTERY_PASSIVE))
			return false;

		if (eventInfo.GetSpellInfo().Id != MonkSpells.BLACKOUT_KICK)
			return false;

		return true;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		var monasteryBuff = GetAura();

		if (monasteryBuff != null)
		{
			for (byte i = 0; i < monasteryBuff.GetStackAmount(); ++i)
				GetTarget().CastSpell(eventInfo.GetProcTarget(), MonkSpells.BLACKOUT_KICK_TRIGGERED);

			monasteryBuff.Remove();
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}