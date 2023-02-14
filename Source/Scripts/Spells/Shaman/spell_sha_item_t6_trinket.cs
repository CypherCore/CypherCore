// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 40463 - Shaman Tier 6 Trinket
[SpellScript(40463)]
internal class spell_sha_item_t6_trinket : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.EnergySurge, ShamanSpells.PowerSurge);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var spellInfo = eventInfo.GetSpellInfo();

		if (spellInfo == null)
			return;

		uint spellId;
		int  chance;

		// Lesser Healing Wave
		if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000080u))
		{
			spellId = ShamanSpells.EnergySurge;
			chance  = 10;
		}
		// Lightning Bolt
		else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000001u))
		{
			spellId = ShamanSpells.EnergySurge;
			chance  = 15;
		}
		// Stormstrike
		else if (spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x00000010u))
		{
			spellId = ShamanSpells.PowerSurge;
			chance  = 50;
		}
		else
		{
			return;
		}

		if (RandomHelper.randChance(chance))
			eventInfo.GetActor().CastSpell((Unit)null, spellId, true);
	}
}