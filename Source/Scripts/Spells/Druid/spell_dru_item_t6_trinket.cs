// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 40442 - Druid Tier 6 Trinket
	internal class spell_dru_item_t6_trinket : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.BlessingOfRemulos, DruidSpellIds.BlessingOfElune, DruidSpellIds.BlessingOfCenarius);
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

			// Starfire
			if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000004u))
			{
				spellId = DruidSpellIds.BlessingOfRemulos;
				chance  = 25;
			}
			// Rejuvenation
			else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000010u))
			{
				spellId = DruidSpellIds.BlessingOfElune;
				chance  = 25;
			}
			// Mangle (Bear) and Mangle (Cat)
			else if (spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x00000440u))
			{
				spellId = DruidSpellIds.BlessingOfCenarius;
				chance  = 40;
			}
			else
			{
				return;
			}

			if (RandomHelper.randChance(chance))
				eventInfo.GetActor().CastSpell((Unit)null, spellId, new CastSpellExtraArgs(aurEff));
		}
	}
}