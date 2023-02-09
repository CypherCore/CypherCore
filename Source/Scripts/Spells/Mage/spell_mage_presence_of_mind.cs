using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Mage;

[SpellScript(205025)]
public class spell_mage_presence_of_mind : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_ARCANE_BLAST)
			return true;

		return false;
	}
}