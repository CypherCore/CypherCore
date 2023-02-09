using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(115203)]
public class spell_monk_fortifying_brew : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleDummy(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster != null && caster.GetTypeId() == TypeId.Player)
		{
			caster.CastSpell(caster, MonkSpells.SPELL_MONK_FORTIFYING_BREW, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}