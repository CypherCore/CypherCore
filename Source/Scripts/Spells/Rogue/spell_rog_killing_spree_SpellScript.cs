using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[Script] // 51690 - Killing Spree
internal class spell_rog_killing_spree_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
		SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		if (targets.Empty() ||
		    GetCaster().GetVehicleBase())
			FinishCast(SpellCastResult.OutOfRange);
	}

	private void HandleDummy(uint effIndex)
	{
		var aura = GetCaster().GetAura(RogueSpells.KillingSpree);

		if (aura != null)
		{
			var script = aura.GetScript<spell_rog_killing_spree_AuraScript>();

			script?.AddTarget(GetHitUnit());
		}
	}
}