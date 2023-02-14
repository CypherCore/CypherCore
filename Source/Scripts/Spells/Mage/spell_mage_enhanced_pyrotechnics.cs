// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[SpellScript(157642)]
public class spell_mage_enhanced_pyrotechnics : AuraScript
{
	private bool HandleProc(ProcEventInfo eventInfo)
	{
		var caster = GetCaster();

		if (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIREBALL)
		{
			if ((eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0)
			{
				if (caster.HasAura(MageSpells.SPELL_MAGE_ENHANCED_PYROTECHNICS_AURA))
					caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_ENHANCED_PYROTECHNICS_AURA);

				return false;
			}

			return true;
		}

		return false;
	}
}