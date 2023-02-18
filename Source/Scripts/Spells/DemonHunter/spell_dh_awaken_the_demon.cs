// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(205598)]
public class spell_dh_awaken_the_demon : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var caster = GetCaster();

		if (caster == null || eventInfo.GetDamageInfo() != null)
			return;

		if (!GetSpellInfo().GetEffect(1).IsEffect() || !GetSpellInfo().GetEffect(2).IsEffect())
			return;

		var threshold1 = caster.CountPctFromMaxHealth(aurEff.GetBaseAmount());
		var threshold2 = caster.CountPctFromMaxHealth(GetSpellInfo().GetEffect(1).BasePoints);
		var duration   = GetSpellInfo().GetEffect(2).BasePoints;

		if (caster.GetHealth() - eventInfo.GetDamageInfo().GetDamage() < threshold1)
		{
			if (caster.HasAura(DemonHunterSpells.SPELL_DH_AWAKEN_THE_DEMON_CD))
				return;

			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_AWAKEN_THE_DEMON_CD, true);
			var aur = caster.GetAura(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_HAVOC);

			if (aur != null)
			{
				aur.SetDuration(Math.Min(duration * Time.InMilliseconds + aur.GetDuration(), aur.GetMaxDuration()));

				return;
			}

			aur = caster.AddAura(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_HAVOC, caster);

			if (aur != null)
				aur.SetDuration(duration * Time.InMilliseconds);
		}

		// Check only if we are above the second threshold and we are falling under it just now
		if (caster.GetHealth() > threshold2 && caster.GetHealth() - eventInfo.GetDamageInfo().GetDamage() < threshold2)
		{
			var aur = caster.GetAura(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_HAVOC);

			if (aur != null)
			{
				aur.SetDuration(Math.Min(duration * Time.InMilliseconds + aur.GetDuration(), aur.GetMaxDuration()));

				return;
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}