using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(197721)]
public class spell_dru_flourish : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleHit(uint UnnamedParameter)
	{
		if (!GetCaster() || !GetHitUnit())
		{
			return;
		}

		List<AuraEffect> auraEffects = GetHitUnit().GetAuraEffectsByType(AuraType.PeriodicHeal);

		foreach (AuraEffect auraEffect in auraEffects)
		{
			if (auraEffect.GetCasterGUID() == GetCaster().GetGUID())
			{
				Aura healAura = auraEffect.GetBase();
				if (healAura != null)
				{
					healAura.SetDuration(healAura.GetDuration() + GetEffectValue() * Time.InMilliseconds);
				}
			}
		}
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		List<WorldObject> tempTargets = new List<WorldObject>();
		foreach (WorldObject target in targets)
		{
			if (target.IsPlayer())
			{
				if (target.ToUnit().HasAuraTypeWithCaster(AuraType.PeriodicHeal, GetCaster().GetGUID()))
				{
					tempTargets.Add(target);
				}
			}
		}

		if (tempTargets.Count > 0)
		{
			targets.Clear();
			foreach (WorldObject target in tempTargets)
			{
				targets.Add(target);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
	}
}