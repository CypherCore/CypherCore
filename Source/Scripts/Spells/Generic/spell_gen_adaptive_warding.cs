// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 28764 - Adaptive Warding (Frostfire Regalia Set)
internal class spell_gen_adaptive_warding : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.GenAdaptiveWardingFire, GenericSpellIds.GenAdaptiveWardingNature, GenericSpellIds.GenAdaptiveWardingFrost, GenericSpellIds.GenAdaptiveWardingShadow, GenericSpellIds.GenAdaptiveWardingArcane);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo() == null)
			return false;

		// find Mage Armor
		if (GetTarget().GetAuraEffect(AuraType.ModManaRegenInterrupt, SpellFamilyNames.Mage, new FlagArray128(0x10000000, 0x0, 0x0)) == null)
			return false;

		switch (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()))
		{
			case SpellSchools.Normal:
			case SpellSchools.Holy:
				return false;
			default:
				break;
		}

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		uint spellId;

		switch (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()))
		{
			case SpellSchools.Fire:
				spellId = GenericSpellIds.GenAdaptiveWardingFire;

				break;
			case SpellSchools.Nature:
				spellId = GenericSpellIds.GenAdaptiveWardingNature;

				break;
			case SpellSchools.Frost:
				spellId = GenericSpellIds.GenAdaptiveWardingFrost;

				break;
			case SpellSchools.Shadow:
				spellId = GenericSpellIds.GenAdaptiveWardingShadow;

				break;
			case SpellSchools.Arcane:
				spellId = GenericSpellIds.GenAdaptiveWardingArcane;

				break;
			default:
				return;
		}

		GetTarget().CastSpell(GetTarget(), spellId, new CastSpellExtraArgs(aurEff));
	}
}