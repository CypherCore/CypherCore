using Framework.Constants;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Rogue;

[SpellScript(108216)]
public class spell_rog_dirty_tricks_AuraScript : AuraScript
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var spellInfo = eventInfo.GetDamageInfo().GetSpellInfo();

		if (spellInfo == null)
			return true;

		if (eventInfo.GetActor().GetGUID() != GetCasterGUID())
			return true;

		if (spellInfo.Mechanic == Mechanics.Bleed || (spellInfo.GetAllEffectsMechanicMask() & (ulong)Mechanics.Bleed) != 0 || spellInfo.Dispel == DispelType.Poison)
			if (eventInfo.GetActor().HasAura(108216))
				return false;

		return true;
	}
}