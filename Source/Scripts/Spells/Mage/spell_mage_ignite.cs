// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 11119 - Ignite
internal class spell_mage_ignite : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.Ignite);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetProcTarget();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var igniteDot = Global.SpellMgr.GetSpellInfo(MageSpells.Ignite, GetCastDifficulty());
		var pct       = aurEff.GetAmount();

		var amount = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), pct) / igniteDot.GetMaxTicks());

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, amount);
		GetTarget().CastSpell(eventInfo.GetProcTarget(), MageSpells.Ignite, args);
	}
}