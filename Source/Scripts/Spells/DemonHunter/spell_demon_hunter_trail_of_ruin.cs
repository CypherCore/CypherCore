using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DemonHunter;

[SpellScript(258881)]
public class spell_demon_hunter_trail_of_ruin : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo().Id == Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_BLADE_DANCE, Difficulty.None).GetEffect(0).TriggerSpell;
	}
}