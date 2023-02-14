// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 64411 - Blessing of Ancient Kings (Val'anyr, Hammer of Ancient Kings)
internal class spell_item_blessing_of_ancient_kings : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.ProtectionOfAncientKings);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetProcTarget() != null;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var healInfo = eventInfo.GetHealInfo();

		if (healInfo == null ||
		    healInfo.GetHeal() == 0)
			return;

		var absorb  = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), 15.0f);
		var protEff = eventInfo.GetProcTarget().GetAuraEffect(ItemSpellIds.ProtectionOfAncientKings, 0, eventInfo.GetActor().GetGUID());

		if (protEff != null)
		{
			// The shield can grow to a maximum size of 20,000 Damage absorbtion
			protEff.SetAmount(Math.Min(protEff.GetAmount() + absorb, 20000));

			// Refresh and return to prevent replacing the aura
			protEff.GetBase().RefreshDuration();
		}
		else
		{
			CastSpellExtraArgs args = new(aurEff);
			args.AddSpellMod(SpellValueMod.BasePoint0, absorb);
			GetTarget().CastSpell(eventInfo.GetProcTarget(), ItemSpellIds.ProtectionOfAncientKings, args);
		}
	}
}