// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(194913)]
public class spell_dk_glacial_advance : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHit(uint UnnamedParameter)
	{
		var caster = GetCaster();

		var castPosition = caster.GetPosition();
		var collisonPos  = caster.GetFirstCollisionPosition(GetEffectInfo().MaxRadiusEntry.RadiusMax, 0);
		var maxDistance  = caster.GetDistance(collisonPos);

		for (var dist = 0.0f; dist <= maxDistance; dist += 1.5f)
			caster.m_Events.AddEventAtOffset(() =>
			                                 {
				                                 var targetPosition = new Position(castPosition);
				                                 caster.MovePosition(targetPosition, dist, 0.0f);
				                                 caster.CastSpell(targetPosition, DeathKnightSpells.SPELL_DK_GLACIAL_ADVANCE_DAMAGE, true);
			                                 },
			                                 TimeSpan.FromMilliseconds(dist / 1.5f * 50.0f));
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}
}