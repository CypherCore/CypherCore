// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 49576 - Death Grip Initial
internal class spell_dk_death_grip_initial : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.DeathGripDummy, DeathKnightSpells.DeathGripJump, DeathKnightSpells.Blood, DeathKnightSpells.DeathGripTaunt);
	}

	public SpellCastResult CheckCast()
	{
		var caster = GetCaster();

		// Death Grip should not be castable while jumping/falling
		if (caster.HasUnitState(UnitState.Jumping) ||
		    caster.HasUnitMovementFlag(MovementFlag.Falling))
			return SpellCastResult.Moving;

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(int effIndex)
	{
		GetCaster().CastSpell(GetHitUnit(), DeathKnightSpells.DeathGripDummy, true);
		GetHitUnit().CastSpell(GetCaster(), DeathKnightSpells.DeathGripJump, true);

		if (GetCaster().HasAura(DeathKnightSpells.Blood))
			GetCaster().CastSpell(GetHitUnit(), DeathKnightSpells.DeathGripTaunt, true);
	}
}