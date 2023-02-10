using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock
{
	// 196412 - Eradication
	[SpellScript(196412)]
	public class spell_warlock_eradication : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			return eventInfo.GetSpellInfo().Id == WarlockSpells.CHAOS_BOLT;
		}
	}
}