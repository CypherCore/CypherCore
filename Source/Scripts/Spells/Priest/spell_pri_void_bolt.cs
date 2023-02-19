// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(234746)]
public class spell_pri_void_bolt : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleEffectScriptEffect(int effIndex)
	{
		var voidBoltDurationBuffAura = GetCaster().GetAura(PriestSpells.VOID_BOLT_DURATION);

		if (voidBoltDurationBuffAura != null)
		{
			var unit = GetHitUnit();

			if (unit != null)
			{
				var durationIncreaseMs = voidBoltDurationBuffAura.GetEffect(0).GetBaseAmount();

				var pain = unit.GetAura(PriestSpells.SHADOW_WORD_PAIN, GetCaster().GetGUID());

				if (pain != null)
					pain.ModDuration(durationIncreaseMs);

				var vampiricTouch = unit.GetAura(PriestSpells.VAMPIRIC_TOUCH, GetCaster().GetGUID());

				if (vampiricTouch != null)
					vampiricTouch.ModDuration(durationIncreaseMs);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}
}