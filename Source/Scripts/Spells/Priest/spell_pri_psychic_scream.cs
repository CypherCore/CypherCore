using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(8122)]
public class spell_pri_psychic_scream : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		Unit target = eventInfo.GetActionTarget();
		if (target == null)
		{
			return false;
		}
		var  dmg  = eventInfo.GetDamageInfo();
		Aura fear = GetAura();
		if (fear != null && dmg != null && dmg.GetDamage() > 0)
		{
			fear.SetDuration(0);
		}
		return true;
	}
}