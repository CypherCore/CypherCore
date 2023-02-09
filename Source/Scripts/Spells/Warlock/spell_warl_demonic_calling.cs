using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Demonic Calling - 205145
	public class spell_warl_demonic_calling : AuraScript, IAuraCheckProc
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(WarlockSpells.DEMONIC_CALLING_TRIGGER, Difficulty.None) != null)
				return false;

			return true;
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			var caster = GetCaster();

			if (caster == null)
				return false;

			if (eventInfo.GetSpellInfo() != null && (eventInfo.GetSpellInfo().Id == WarlockSpells.DEMONBOLT || eventInfo.GetSpellInfo().Id == WarlockSpells.SHADOW_BOLT) && RandomHelper.randChance(20))
				caster.CastSpell(caster, WarlockSpells.DEMONIC_CALLING_TRIGGER, true);

			return false;
		}
	}
}