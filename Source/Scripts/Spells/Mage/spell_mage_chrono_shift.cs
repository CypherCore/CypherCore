using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Mage;

[SpellScript(235711)]
public class spell_mage_chrono_shift : AuraScript, IAuraCheckProc
{


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		bool _spellCanProc = (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_ARCANE_BARRAGE || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_ARCANE_BARRAGE_TRIGGERED);

		if (_spellCanProc)
		{
			return true;
		}
		return false;
	}


}