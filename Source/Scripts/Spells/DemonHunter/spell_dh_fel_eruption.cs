using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(211881)]
public class spell_dh_fel_eruption : SpellScript, ISpellBeforeHit, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();
		var target = GetExplTargetUnit();

		if (caster == null || target == null)
			return;

		caster.CastSpell(target, DemonHunterSpells.SPELL_DH_FEL_ERUPTION_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, 1));
	}

	public void BeforeHit(SpellMissInfo missInfo)
	{
		var caster = GetCaster();
		var target = GetExplTargetUnit();

		if (caster == null || target == null)
			return;

		if (missInfo == SpellMissInfo.Immune || missInfo == SpellMissInfo.Immune2)
			caster.CastSpell(target, DemonHunterSpells.SPELL_DH_FEL_ERUPTION_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, 2));
	}
}