using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DemonHunter;

[SpellScript(203753)]
public class spell_dh_blade_turning : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if ((eventInfo.GetHitMask() & ProcFlagsHit.Parry) != 0)
			return true;

		return false;
	}
}