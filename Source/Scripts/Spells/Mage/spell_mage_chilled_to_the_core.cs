using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Mage;

[SpellScript(195448)]
public class spell_mage_chilled_to_the_core : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_ICY_VEINS;
	}
}