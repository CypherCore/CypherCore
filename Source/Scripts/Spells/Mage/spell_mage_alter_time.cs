// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(108978)]
public class spell_mage_alter_time : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(uint UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		// Check if the spell has been cast before
		var alterTime = target.GetAura(MageSpells.ALTER_TIME);

		if (alterTime != null)
		{
			// Check if the target has moved a long distance
			if (target.GetDistance(alterTime.GetCaster()) > 50.0f)
			{
				target.RemoveAura(MageSpells.ALTER_TIME);

				return;
			}

			// Check if the target has died
			if (target.IsDead())
			{
				target.RemoveAura(MageSpells.ALTER_TIME);

				return;
			}

			// Return the target to their location and health from when the spell was first cast
			target.SetHealth(alterTime.GetEffect(0).GetAmount());
			target.NearTeleportTo(alterTime.GetCaster().GetPositionX(), alterTime.GetCaster().GetPositionY(), alterTime.GetCaster().GetPositionZ(), alterTime.GetCaster().GetOrientation());
			target.RemoveAura(MageSpells.ALTER_TIME);
		}
		else
		{
			// Save the target's current location and health
			caster.AddAura(MageSpells.ALTER_TIME, target);
			target.SetAuraStack(MageSpells.ALTER_TIME, target, (uint)target.GetHealth());
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}