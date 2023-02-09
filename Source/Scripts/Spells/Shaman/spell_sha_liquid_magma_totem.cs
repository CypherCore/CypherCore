using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 192223 - Liquid Magma Totem (erupting hit spell)
internal class spell_sha_liquid_magma_totem : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.LiquidMagmaHit);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(HandleTargetSelect, 0, Targets.UnitDestAreaEnemy));
		SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleEffectHitTarget(uint effIndex)
	{
		var hitUnit = GetHitUnit();

		if (hitUnit != null)
			GetCaster().CastSpell(hitUnit, ShamanSpells.LiquidMagmaHit, true);
	}

	private void HandleTargetSelect(List<WorldObject> targets)
	{
		// choose one random Target from targets
		if (targets.Count > 1)
		{
			var selected = targets.SelectRandom();
			targets.Clear();
			targets.Add(selected);
		}
	}
}