using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Druid;

[SpellScript(159286)]
public class spell_dru_primal_fury : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var _spellCanProc = (eventInfo.GetSpellInfo().Id == DruidSpells.SPELL_DRUID_SHRED || eventInfo.GetSpellInfo().Id == DruidSpells.SPELL_DRUID_RAKE || eventInfo.GetSpellInfo().Id == DruidSpells.SPELL_DRUID_SWIPE_CAT || eventInfo.GetSpellInfo().Id == DruidSpells.SPELL_DRUID_MOONFIRE_CAT);

		if ((eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0 && _spellCanProc)
			return true;

		return false;
	}
}