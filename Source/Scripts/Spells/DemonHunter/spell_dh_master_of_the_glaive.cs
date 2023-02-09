using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DemonHunter;

[SpellScript(203556)]
public class spell_dh_master_of_the_glaive : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id == DemonHunterSpells.SPELL_DH_THROW_GLAIVE)
			return true;

		return false;
	}
}