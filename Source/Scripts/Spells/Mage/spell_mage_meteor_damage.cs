using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(153564)]
public class spell_mage_meteor_damage : SpellScript, IHasSpellEffects
{
	private int _targets;

	public List<ISpellEffect> SpellEffects => new();

	private void HandleHit(int UnnamedParameter)
	{
		var unit = GetHitUnit();

		if (unit == null)
			return;

		SetHitDamage(GetHitDamage() / _targets);
	}

	private void CountTargets(List<WorldObject> targets)
	{
		_targets = targets.Count;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitDestAreaEnemy));
	}
}