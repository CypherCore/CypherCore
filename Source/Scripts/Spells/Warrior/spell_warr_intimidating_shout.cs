// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	// 5246 - Intimidating Shout
	[Script]
	internal class spell_warr_intimidating_shout : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitSrcAreaEnemy));
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 2, Targets.UnitSrcAreaEnemy));
		}

		private void FilterTargets(List<WorldObject> unitList)
		{
			unitList.Remove(GetExplTargetWorldObject());
		}
	}
}