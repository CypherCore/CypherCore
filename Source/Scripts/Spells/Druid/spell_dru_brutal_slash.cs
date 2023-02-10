using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(202028)]
public class spell_dru_brutal_slash : SpellScript, ISpellOnHit
{
	private bool _awardComboPoint = true;

	public void OnHit()
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		// This prevent awarding multiple Combo Points when multiple targets hit with Brutal Slash AoE
		if (_awardComboPoint)
			// Awards the caster 1 Combo Point (get value from the spell data)
			caster.ModifyPower(PowerType.ComboPoints, Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_SWIPE_CAT, Difficulty.None).GetEffect(0).BasePoints);

		_awardComboPoint = false;
	}
}