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

[SpellScript(203283)]
public class spell_mage_firestarter_pvp : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIREBALL;
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.GetSpellHistory().ModifyCooldown(MageSpells.SPELL_MAGE_COMBUSTION, TimeSpan.FromSeconds((aurEff.GetAmount() * -1) - 5000));
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}