// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(79684)]
public class spell_mage_clearcasting : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var eff0 = GetSpellInfo().GetEffect(0).CalcValue();

		if (eff0 != 0)
		{
			double reqManaToSpent = 0;
			var manaUsed       = 0;

			// For each ${$c*100/$s1} mana you spend, you have a 1% chance
			// Means: I cast a spell which costs 1000 Mana, for every 500 mana used I have 1% chance =  2% chance to proc
			foreach (var powerCost in GetSpellInfo().CalcPowerCost(GetCaster(), GetSpellInfo().GetSchoolMask()))
				if (powerCost.Power == PowerType.Mana)
					reqManaToSpent = powerCost.Amount * 100 / eff0;

			// Something changed in DBC, Clearcasting should cost 1% of base mana 8.0.1
			if (reqManaToSpent == 0)
				return false;

			foreach (var powerCost in eventInfo.GetSpellInfo().CalcPowerCost(GetCaster(), eventInfo.GetSpellInfo().GetSchoolMask()))
				if (powerCost.Power == PowerType.Mana)
					manaUsed = powerCost.Amount;

			var chance = Math.Floor(manaUsed / reqManaToSpent * (double)1);

			return RandomHelper.randChance(chance);
		}

		return false;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		var actor = eventInfo.GetActor();
		actor.CastSpell(actor, MageSpells.CLEARCASTING_BUFF, true);

		if (actor.HasAura(MageSpells.ARCANE_EMPOWERMENT))
			actor.CastSpell(actor, MageSpells.CLEARCASTING_PVP_STACK_EFFECT, true);
		else
			actor.CastSpell(actor, MageSpells.CLEARCASTING_EFFECT, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}