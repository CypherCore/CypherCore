using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(197721)]
public class spell_dru_flourish : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHit(uint UnnamedParameter)
	{
		if (!GetCaster() || !GetHitUnit())
			return;

		var auraEffects = GetHitUnit().GetAuraEffectsByType(AuraType.PeriodicHeal);

		foreach (var auraEffect in auraEffects)
			if (auraEffect.GetCasterGUID() == GetCaster().GetGUID())
			{
				var healAura = auraEffect.GetBase();

				if (healAura != null)
					healAura.SetDuration(healAura.GetDuration() + GetEffectValue() * Time.InMilliseconds);
			}
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		var tempTargets = new List<WorldObject>();

		foreach (var target in targets)
			if (target.IsPlayer())
				if (target.ToUnit().HasAuraTypeWithCaster(AuraType.PeriodicHeal, GetCaster().GetGUID()))
					tempTargets.Add(target);

		if (tempTargets.Count > 0)
		{
			targets.Clear();

			foreach (var target in tempTargets)
				targets.Add(target);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
	}
}