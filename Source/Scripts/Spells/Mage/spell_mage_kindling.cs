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

[SpellScript(155148)]
public class spell_mage_kindling : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIREBALL || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIRE_BLAST || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_PHOENIX_FLAMES;
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.GetSpellHistory().ModifyCooldown(MageSpells.SPELL_MAGE_COMBUSTION, TimeSpan.FromSeconds(aurEff.GetAmount() * -1));
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}