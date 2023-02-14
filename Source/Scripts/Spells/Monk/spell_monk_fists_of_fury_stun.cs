// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(120086)]
public class spell_monk_fists_of_fury_stun : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void RemoveInvalidTargets(List<WorldObject> targets)
	{
		targets.RemoveIf(new UnitAuraCheck<WorldObject>(true, GetSpellInfo().Id));
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 0, Targets.UnitConeEnemy24));
	}
}