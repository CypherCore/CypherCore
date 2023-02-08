using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(214579)]
public class spell_hun_sidewinders : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleDummy(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			Unit target = GetHitUnit();
			if (target != null)
			{
				caster.CastSpell(target, 187131, true);
			}
		}
	}

	private void HandleDummy1(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			Unit target = GetHitUnit();
			if (target != null)
			{
				caster.CastSpell(target, 214581, true);
				caster.SendPlaySpellVisual(target, target.GetOrientation(), 56931, 0, 0, 18.0f, false);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleDummy1, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}