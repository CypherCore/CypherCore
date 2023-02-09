using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(195630)]
public class spell_monk_elusive_brawler_stacks : AuraScript
{


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetHitMask().HasFlag(ProcFlagsHit.Dodge))
		{
			return false;
		}

		Aura elusiveBrawler = GetCaster().GetAura(MonkSpells.SPELL_MONK_ELUSIVE_BRAWLER, GetCaster().GetGUID());
		if (elusiveBrawler != null)
		{
			elusiveBrawler.SetDuration(0);
		}

		return true;
	}


}