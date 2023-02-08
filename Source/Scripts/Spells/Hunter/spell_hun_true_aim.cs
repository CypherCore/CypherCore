using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Hunter;

[SpellScript(199527)]
public class spell_hun_true_aim : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id == HunterSpells.SPELL_HUNTER_AIMED_SHOT || eventInfo.GetSpellInfo().Id == HunterSpells.SPELL_HUNTER_ARCANE_SHOT)
		{
			return true;
		}

		return false;
	}
}