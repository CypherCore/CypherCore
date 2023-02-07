using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(6807)]
public class spell_dru_maul_bear : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void OnHit(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();
		if (caster == null || target == null)
		{
			return;
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(OnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(OnHit, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}