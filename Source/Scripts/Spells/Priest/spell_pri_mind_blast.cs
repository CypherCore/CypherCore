using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(8092)]
public class spell_pri_mind_blast : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleOnHit(int UnnamedParameter)
	{
		if (GetCaster().HasAura(PriestSpells.SPELL_PRIEST_SHADOWY_INSIGHTS))
			GetCaster().RemoveAurasDueToSpell(PriestSpells.SPELL_PRIEST_SHADOWY_INSIGHTS);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}