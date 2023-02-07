using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(202360)]
public class spell_dru_blessing_of_the_ancients : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


	private void HandleDummy(uint UnnamedParameter)
	{
		uint removeAura = GetCaster().HasAura(DruidSpells.SPELL_DRUID_BLESSING_OF_ELUNE) ? (uint)DruidSpells.SPELL_DRUID_BLESSING_OF_ELUNE : (uint)DruidSpells.SPELL_DRUID_BLESSING_OF_ANSHE;
		uint addAura    = GetCaster().HasAura(DruidSpells.SPELL_DRUID_BLESSING_OF_ELUNE) ? (uint)DruidSpells.SPELL_DRUID_BLESSING_OF_ANSHE : (uint)DruidSpells.SPELL_DRUID_BLESSING_OF_ELUNE;

		GetCaster().RemoveAurasDueToSpell(removeAura);
		GetCaster().CastSpell(null, addAura, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}