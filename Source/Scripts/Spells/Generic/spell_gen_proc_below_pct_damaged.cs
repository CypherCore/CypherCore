using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Generic;

[Script("spell_item_soul_harvesters_charm")]
[Script("spell_item_commendation_of_kaelthas")]
[Script("spell_item_corpse_tongue_coin")]
[Script("spell_item_corpse_tongue_coin_heroic")]
[Script("spell_item_petrified_twilight_scale")]
[Script("spell_item_petrified_twilight_scale_heroic")]
internal class spell_gen_proc_below_pct_damaged : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var damageInfo = eventInfo.GetDamageInfo();

		if (damageInfo == null ||
		    damageInfo.GetDamage() == 0)
			return false;

		var pct = GetSpellInfo().GetEffect(0).CalcValue();

		if (eventInfo.GetActionTarget().HealthBelowPctDamaged(pct, damageInfo.GetDamage()))
			return true;

		return false;
	}
}