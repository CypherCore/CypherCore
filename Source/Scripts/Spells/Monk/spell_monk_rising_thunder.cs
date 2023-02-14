// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(MonkSpells.SPELL_MONK_RISING_THUNDER)]
public class spell_monk_rising_thunder : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(MonkSpells.SPELL_MONK_RISING_THUNDER, Difficulty.None) != null)
			return false;

		return true;
	}

	private void HandleEffectProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		var caster = GetCaster();

		caster.ToPlayer().GetSpellHistory().ResetCooldown(MonkSpells.SPELL_MONK_THUNDER_FOCUS_TEA, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}