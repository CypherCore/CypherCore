using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(58180)]
public class spell_dru_infected_wounds : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleDummy(uint UnnamedParameter)
	{
		if (!GetCaster())
			return;

		if (GetCaster().HasAura(GetSpellInfo().Id))
			GetCaster().RemoveAurasDueToSpell(GetSpellInfo().Id);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
	}
}