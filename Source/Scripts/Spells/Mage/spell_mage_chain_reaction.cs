using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Mage;

[SpellScript(195419)]
public class spell_mage_chain_reaction : AuraScript, IAuraCheckProc
{


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FROSTBOLT || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FROSTBOLT_TRIGGER;
	}


}