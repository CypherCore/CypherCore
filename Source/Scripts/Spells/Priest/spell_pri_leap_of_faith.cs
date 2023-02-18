// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(73325)]
public class spell_pri_leap_of_faith : SpellScript, IHasSpellEffects, ISpellOnHit
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return Global.SpellMgr.GetSpellInfo(PriestSpells.LEAP_OF_FAITH_GLYPH, Difficulty.None) != null && Global.SpellMgr.GetSpellInfo(PriestSpells.LEAP_OF_FAITH_EFFECT, Difficulty.None) != null;
	}

	private void HandleScript(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (caster.HasAura(PriestSpells.LEAP_OF_FAITH_GLYPH))
			GetHitUnit().RemoveMovementImpairingAuras(false);

		GetHitUnit().CastSpell(caster, PriestSpells.LEAP_OF_FAITH_EFFECT, true);
	}

	public void OnHit()
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
		{
			var target = GetHitUnit();

			if (target != null)
			{
				target.CastSpell(_player, PriestSpells.LEAP_OF_FAITH_JUMP, true);

				if (_player.HasAura(PriestSpells.BODY_AND_SOUL_AURA))
					_player.CastSpell(target, PriestSpells.BODY_AND_SOUL_SPEED, true);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}