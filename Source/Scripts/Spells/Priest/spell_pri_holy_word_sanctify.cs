// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(34861)]
public class spell_pri_holy_word_sanctify : SpellScript, IHasSpellEffects, ISpellOnCast
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.RemoveIf(new RaidCheck(GetCaster()));
		targets.Sort(new HealthPctOrderPred());
	}

	public void OnCast()
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
			player.GetSpellHistory().ModifyCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORLD_SALVATION, TimeSpan.FromSeconds(-30000));
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
	}
}