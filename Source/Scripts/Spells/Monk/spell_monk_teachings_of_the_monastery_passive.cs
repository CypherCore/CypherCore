// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(116645)]
public class spell_monk_teachings_of_the_monastery_passive : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.SPELL_MONK_TEACHINGS_OF_THE_MONASTERY, MonkSpells.SPELL_MONK_TIGER_PALM, MonkSpells.SPELL_MONK_BLACKOUT_KICK);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id != MonkSpells.SPELL_MONK_TIGER_PALM && eventInfo.GetSpellInfo().Id != MonkSpells.SPELL_MONK_BLACKOUT_KICK && eventInfo.GetSpellInfo().Id != MonkSpells.SPELL_MONK_BLACKOUT_KICK_TRIGGERED)
			return false;

		return true;
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id == MonkSpells.SPELL_MONK_TIGER_PALM)
		{
			GetTarget().CastSpell(GetTarget(), MonkSpells.SPELL_MONK_TEACHINGS_OF_THE_MONASTERY, true);
		}
		else if (RandomHelper.randChance(aurEff.GetAmount()))
		{
			var spellInfo = Global.SpellMgr.GetSpellInfo(MonkSpells.SPELL_MONK_RISING_SUN_KICK, Difficulty.None);

			if (spellInfo != null)
				GetTarget().GetSpellHistory().RestoreCharge(spellInfo.ChargeCategoryId);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}