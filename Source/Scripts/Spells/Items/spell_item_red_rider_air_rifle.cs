// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_red_rider_air_rifle : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(ItemSpellIds.AirRifleHoldVisual, ItemSpellIds.AirRifleShoot, ItemSpellIds.AirRifleShootSelf);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var caster = GetCaster();
		var target = GetHitUnit();

		if (target)
		{
			caster.CastSpell(caster, ItemSpellIds.AirRifleHoldVisual, true);
			// needed because this spell shares GCD with its triggered spells (which must not be cast with triggered flag)
			var player = caster.ToPlayer();

			if (player)
				player.GetSpellHistory().CancelGlobalCooldown(GetSpellInfo());

			if (RandomHelper.URand(0, 4) != 0)
				caster.CastSpell(target, ItemSpellIds.AirRifleShoot, false);
			else
				caster.CastSpell(caster, ItemSpellIds.AirRifleShootSelf, false);
		}
	}
}