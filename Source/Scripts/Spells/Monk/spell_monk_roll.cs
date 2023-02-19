// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[Script] // 109132 - Roll
internal class spell_monk_roll : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MonkSpells.RollBackward, MonkSpells.RollForward, MonkSpells.NoFeatherFall);
	}

	public SpellCastResult CheckCast()
	{
		if (GetCaster().HasUnitState(UnitState.Root))
			return SpellCastResult.Rooted;

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(int effIndex)
	{
		GetCaster()
			.CastSpell(GetCaster(),
			           GetCaster().HasUnitMovementFlag(MovementFlag.Backward) ? MonkSpells.RollBackward : MonkSpells.RollForward,
			           new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));

		GetCaster().CastSpell(GetCaster(), MonkSpells.NoFeatherFall, true);
	}
}