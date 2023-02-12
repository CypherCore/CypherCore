using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(119607)]
public class spell_monk_renewing_mist_jump : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.SPELL_MONK_RENEWING_MIST_HOT);
	}

	private void HandleTargets(List<WorldObject> targets)
	{
		var caster         = GetCaster();
		var previousTarget = GetExplTargetUnit();

		// Not remove full health targets now, dancing mists talent can jump on full health too


		targets.RemoveIf((WorldObject a) =>
		                 {
			                 var ally = a.ToUnit();

			                 if (ally == null || ally.HasAura(MonkSpells.SPELL_MONK_RENEWING_MIST_HOT, caster.GetGUID()) || ally == previousTarget)
				                 return true;

			                 return false;
		                 });

		targets.RemoveIf((WorldObject a) =>
		                 {
			                 var ally = a.ToUnit();

			                 if (ally == null || ally.IsFullHealth())
				                 return true;

			                 return false;
		                 });

		if (targets.Count > 1)
		{
			targets.Sort(new HealthPctOrderPred());
			targets.Resize(1);
		}

		_previousTargetGuid = previousTarget.GetGUID();
	}

	private void HandleHit(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var caster         = GetCaster();
		var previousTarget = ObjectAccessor.Instance.GetUnit(caster, _previousTargetGuid);

		if (previousTarget != null)
		{
			var oldAura = previousTarget.GetAura(MonkSpells.SPELL_MONK_RENEWING_MIST_HOT, GetCaster().GetGUID());

			if (oldAura != null)
			{
				var newAura = caster.AddAura(MonkSpells.SPELL_MONK_RENEWING_MIST_HOT, GetHitUnit());

				if (newAura != null)
				{
					newAura.SetDuration(oldAura.GetDuration());
					previousTarget.SendPlaySpellVisual(GetHitUnit().GetPosition(), previousTarget.GetOrientation(), MonkSpells.SPELL_MONK_VISUAL_RENEWING_MIST, 0, 0, 50.0f, false);
					oldAura.Remove();
				}
			}
		}
	}

	private ObjectGuid _previousTargetGuid = new();
	private ObjectGuid _additionalTargetGuid = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(HandleTargets, 1, Targets.UnitDestAreaAlly));
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}