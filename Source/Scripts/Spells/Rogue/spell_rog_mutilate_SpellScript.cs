// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(1329)]
public class spell_rog_mutilate_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void HandleOnHit(int effIndex)
	{
		var caster = GetCaster().ToPlayer();
		var target = GetHitUnit();

		if (target == null || caster == null)
			return;

		if (caster.HasAura(5374) || caster.HasAura(27576))
			caster.ToPlayer().ModifyPower(PowerType.ComboPoints, 1);

		if (caster.HasAura(14190))
			caster.ToPlayer().ModifyPower(PowerType.ComboPoints, 2);

		caster.ModifyPower(PowerType.ComboPoints, -3);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 2, SpellEffectName.TriggerSpell, SpellScriptHookType.EffectHitTarget));
	}
}