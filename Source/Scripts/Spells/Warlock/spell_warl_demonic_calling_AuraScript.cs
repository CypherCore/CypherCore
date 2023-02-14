// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Demonic Calling - 205145
	[SpellScript(205145)]
	public class spell_warl_demonic_calling_AuraScript : AuraScript, IAuraCheckProc
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return SpellManager.Instance.GetSpellInfo(WarlockSpells.DEMONIC_CALLING_TRIGGER, Difficulty.None) != null;
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			var caster = GetCaster();

			if (caster == null)
				return false;

			if (eventInfo.GetSpellInfo() != null &&
			    (eventInfo.GetSpellInfo().Id == WarlockSpells.DEMONBOLT || eventInfo.GetSpellInfo().Id == WarlockSpells.SHADOW_BOLT) &&
			    RandomHelper.randChance(20))
				caster.CastSpell(caster, WarlockSpells.DEMONIC_CALLING_TRIGGER, true);

			return false;
		}
	}
}