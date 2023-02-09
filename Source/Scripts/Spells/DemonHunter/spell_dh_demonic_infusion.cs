using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(236189)]
public class spell_dh_demonic_infusion : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.GetSpellHistory().ResetCharges(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_DEMON_SPIKES, Difficulty.None).ChargeCategoryId);
		caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_DEMON_SPIKES, true);
		caster.GetSpellHistory().ResetCharges(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_DEMON_SPIKES, Difficulty.None).ChargeCategoryId);
	}
}