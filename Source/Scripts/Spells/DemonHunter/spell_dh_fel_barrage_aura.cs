// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(222703)]
public class spell_dh_fel_barrage_aura : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects => new();

	//Blade Dance    //Chaos Strike   //Fel Barrage
	readonly List<uint> _removeSpellIds = new()
	                                      {
		                                      199552,
		                                      210153,
		                                      222031,
		                                      227518,
		                                      211052
	                                      };

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		// Blade Dance, Chaos Strike and Annihilation have many damagers,
		// so we accept only 1 of those, and we remove the others
		// Also we remove fel barrage itself too.
		if (eventInfo.GetSpellInfo() != null)
			return false;

		return !_removeSpellIds.Contains(eventInfo.GetSpellInfo().Id);
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		PreventDefaultAction();

		var caster = GetCaster();

		if (caster == null)
			return;

		var chargeCatId = Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_FEL_BARRAGE, Difficulty.None).ChargeCategoryId;

		if (chargeCatId != 0)
			caster.GetSpellHistory().RestoreCharge(chargeCatId);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}