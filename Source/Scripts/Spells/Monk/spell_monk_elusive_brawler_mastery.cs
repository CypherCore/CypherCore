using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Monk;

[SpellScript(117906)]
public class spell_monk_elusive_brawler_mastery : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetTypeMask().HasFlag(ProcFlags.TakenHitMask))
			return true;

		return eventInfo.GetProcSpell() && (eventInfo.GetProcSpell().GetSpellInfo().Id == MonkSpells.SPELL_MONK_BLACKOUT_STRIKE || eventInfo.GetProcSpell().GetSpellInfo().Id == MonkSpells.SPELL_MONK_BREATH_OF_FIRE);
	}
}