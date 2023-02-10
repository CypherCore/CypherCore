using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Shaman
{
	// Crash Lightning aura - 187878
	[SpellScript(187878)]
	public class spell_sha_crash_lightning_aura : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			if (eventInfo.GetSpellInfo().Id == ShamanSpells.SPELL_SHAMAN_STORMSTRIKE_MAIN || eventInfo.GetSpellInfo().Id == ShamanSpells.SPELL_SHAMAN_LAVA_LASH)
				return true;

			return false;
		}
	}
}