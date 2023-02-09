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
		return ValidateSpellInfo(PriestSpells.Heal, PriestSpells.FlashHeal, PriestSpells.PrayerOfHealing, PriestSpells.Renew, PriestSpells.Smite, PriestSpells.HolyWordChastise, PriestSpells.HolyWordSanctify, PriestSpells.HolyWordSerenity) && Global.SpellMgr.GetSpellInfo(PriestSpells.HolyWordSerenity, Difficulty.None).GetEffects().Count > 1 && Global.SpellMgr.GetSpellInfo(PriestSpells.HolyWordSanctify, Difficulty.None).GetEffects().Count > 3 && Global.SpellMgr.GetSpellInfo(PriestSpells.HolyWordChastise, Difficulty.None).GetEffects().Count > 1;
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
			case PriestSpells.Heal:
			case PriestSpells.FlashHeal: // reduce Holy Word: Serenity cd by 6 seconds
				targetSpellId       = PriestSpells.HolyWordSerenity;
				cdReductionEffIndex = 1;

				// cdReduction = sSpellMgr.GetSpellInfo(SPELL_PRIEST_HOLY_WORD_SERENITY, GetCastDifficulty()).GetEffect(EFFECT_1).CalcValue(player);
				break;
			case PriestSpells.PrayerOfHealing: // reduce Holy Word: Sanctify cd by 6 seconds
				targetSpellId       = PriestSpells.HolyWordSanctify;
				cdReductionEffIndex = 2;

				break;
			case PriestSpells.Renew: // reuce Holy Word: Sanctify cd by 2 seconds
				targetSpellId       = PriestSpells.HolyWordSanctify;
				cdReductionEffIndex = 3;

				break;
			case PriestSpells.Smite: // reduce Holy Word: Chastise cd by 4 seconds
				targetSpellId       = PriestSpells.HolyWordChastise;
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