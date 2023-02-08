using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Hunter;

[SpellScript(194595)]
public class spell_hun_lock_and_load : AuraScript, IAuraCheckProc
{

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id == HunterSpells.SPELL_HUNTER_AUTO_SHOT)
		{
			return true;
		}

		return false;
	}
}