// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[Script] // 52751 - Death Gate
internal class spell_dk_death_gate : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public SpellCastResult CheckCast()
	{
		if (GetCaster().GetClass() != Class.Deathknight)
		{
			SetCustomCastResultMessage(SpellCustomErrors.MustBeDeathKnight);

			return SpellCastResult.CustomError;
		}

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleScript(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var target = GetHitUnit();

		if (target)
			target.CastSpell(target, (uint)GetEffectValue(), false);
	}
}