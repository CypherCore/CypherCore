// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// 73921 - Healing Rain
[SpellScript(73921)]
internal class spell_sha_healing_rain_target_limit : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(SelectTargets, 0, Targets.UnitDestAreaAlly));
	}

	private void SelectTargets(List<WorldObject> targets)
	{
		SelectRandomInjuredTargets(targets, 6, true);
	}
}