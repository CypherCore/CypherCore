using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 198063 - Burning Determination
internal class spell_mage_burning_determination : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		SpellInfo spellInfo = eventInfo.GetSpellInfo();

		if (spellInfo != null)
			if (spellInfo.GetAllEffectsMechanicMask().HasAnyFlag(((1u << (int)Mechanics.Interrupt) | (1 << (int)Mechanics.Silence))))
				return true;

		return false;
	}
}