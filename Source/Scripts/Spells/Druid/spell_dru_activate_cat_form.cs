using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(new uint[]
             {
	             1850, 5215, 102280
             })]
public class spell_dru_activate_cat_form : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (!caster.HasAura(ShapeshiftFormSpells.SPELL_DRUID_CAT_FORM))
			caster.CastSpell(caster, ShapeshiftFormSpells.SPELL_DRUID_CAT_FORM, true);
	}
}