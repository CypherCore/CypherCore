using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 40971 - Bonus Healing (Crystal Spire of Karabor)
internal class spell_item_crystal_spire_of_karabor : AuraScript, IAuraCheckProc
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return !spellInfo.GetEffects().Empty();
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		int      pct      = GetSpellInfo().GetEffect(0).CalcValue();
		HealInfo healInfo = eventInfo.GetHealInfo();

		if (healInfo != null)
		{
			Unit healTarget = healInfo.GetTarget();

			if (healTarget)
				if (healTarget.GetHealth() - healInfo.GetEffectiveHeal() <= healTarget.CountPctFromMaxHealth(pct))
					return true;
		}

		return false;
	}
}