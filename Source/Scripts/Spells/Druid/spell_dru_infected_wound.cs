using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Druid;

[SpellScript(48484)]
public class spell_dru_infected_wound : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id == DruidSpells.SPELL_DRUID_RAKE)
		{
			return true;
		}

		return false;
	}
}