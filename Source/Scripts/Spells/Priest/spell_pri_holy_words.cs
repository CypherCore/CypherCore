// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 63733 - Holy Words
internal class spell_pri_holy_words : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.HEAL, PriestSpells.FLASH_HEAL, PriestSpells.PRAYER_OF_HEALING, PriestSpells.RENEW, PriestSpells.SMITE, PriestSpells.HOLY_WORD_CHASTISE, PriestSpells.HOLY_WORD_SANCTIFY, PriestSpells.HOLY_WORD_SERENITY) && Global.SpellMgr.GetSpellInfo(PriestSpells.HOLY_WORD_SERENITY, Difficulty.None).GetEffects().Count > 1 && Global.SpellMgr.GetSpellInfo(PriestSpells.HOLY_WORD_SANCTIFY, Difficulty.None).GetEffects().Count > 3 && Global.SpellMgr.GetSpellInfo(PriestSpells.HOLY_WORD_CHASTISE, Difficulty.None).GetEffects().Count > 1;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var spellInfo = eventInfo.GetSpellInfo();

		if (spellInfo == null)
			return;

		uint targetSpellId;
		int cdReductionEffIndex;

		switch (spellInfo.Id)
		{
			case PriestSpells.HEAL:
			case PriestSpells.FLASH_HEAL: // reduce Holy Word: Serenity cd by 6 seconds
				targetSpellId       = PriestSpells.HOLY_WORD_SERENITY;
				cdReductionEffIndex = 1;

				// cdReduction = sSpellMgr.GetSpellInfo(HOLY_WORD_SERENITY, GetCastDifficulty()).GetEffect(EFFECT_1).CalcValue(player);
				break;
			case PriestSpells.PRAYER_OF_HEALING: // reduce Holy Word: Sanctify cd by 6 seconds
				targetSpellId       = PriestSpells.HOLY_WORD_SANCTIFY;
				cdReductionEffIndex = 2;

				break;
			case PriestSpells.RENEW: // reuce Holy Word: Sanctify cd by 2 seconds
				targetSpellId       = PriestSpells.HOLY_WORD_SANCTIFY;
				cdReductionEffIndex = 3;

				break;
			case PriestSpells.SMITE: // reduce Holy Word: Chastise cd by 4 seconds
				targetSpellId       = PriestSpells.HOLY_WORD_CHASTISE;
				cdReductionEffIndex = 1;

				break;
			default:
				Log.outWarn(LogFilter.Spells, $"HolyWords aura has been proced by an unknown spell: {GetSpellInfo().Id}");

				return;
		}

		var targetSpellInfo = Global.SpellMgr.GetSpellInfo(targetSpellId, GetCastDifficulty());
		var cdReduction     = targetSpellInfo.GetEffect(cdReductionEffIndex).CalcValue(GetTarget());
		GetTarget().GetSpellHistory().ModifyCooldown(targetSpellInfo, TimeSpan.FromSeconds(-cdReduction), true);
	}
}