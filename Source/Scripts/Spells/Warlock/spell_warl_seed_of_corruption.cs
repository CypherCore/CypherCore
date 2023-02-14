// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	[Script] // 27285 - Seed of Corruption
	internal class spell_warl_seed_of_corruption : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
		}

		private void FilterTargets(List<WorldObject> targets)
		{
			if (GetExplTargetUnit())
				targets.Remove(GetExplTargetUnit());
		}
	}
}