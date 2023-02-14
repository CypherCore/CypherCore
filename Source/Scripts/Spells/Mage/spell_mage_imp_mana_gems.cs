// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 61062 - Improved Mana Gems
internal class spell_mage_imp_mana_gems : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.ManaSurge);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		eventInfo.GetActor().CastSpell((Unit)null, MageSpells.ManaSurge, true);
	}
}