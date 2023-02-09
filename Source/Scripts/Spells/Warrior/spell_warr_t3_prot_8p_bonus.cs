using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
	[Script] // 28845 - Cheat Death
	internal class spell_warr_t3_prot_8p_bonus : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			if (eventInfo.GetActionTarget().HealthBelowPct(20))
				return true;

			var damageInfo = eventInfo.GetDamageInfo();

			if (damageInfo != null &&
			    damageInfo.GetDamage() != 0)
				if (GetTarget().HealthBelowPctDamaged(20, damageInfo.GetDamage()))
					return true;

			return false;
		}
	}
}