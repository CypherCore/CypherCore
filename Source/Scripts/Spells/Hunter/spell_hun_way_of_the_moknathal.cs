using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(201082)]
public class spell_hun_way_of_the_moknathal : AuraScript, IAuraCheckProc
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(HunterSpells.SPELL_HUNTER_RAPTOR_STRIKE, Difficulty.None) != null)
			return false;

		return true;
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id == HunterSpells.SPELL_HUNTER_RAPTOR_STRIKE)
			return true;

		return false;
	}
}