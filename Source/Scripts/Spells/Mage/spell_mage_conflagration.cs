using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Mage;

[SpellScript(205023)]
public class spell_mage_conflagration : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIREBALL;
	}
}