using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(19577)]
public class spell_hun_intimidation : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleDummy(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		Unit target = caster.ToPlayer().GetSelectedUnit();
		if (caster == null || target == null)
		{
			return;
		}

		caster.CastSpell(target, HunterSpells.SPELL_HUNTER_INTIMIDATION_STUN, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}