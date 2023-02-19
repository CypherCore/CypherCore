// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 6353 - Soul Fire
	[SpellScript(6353)]
	public class spell_warlock_soul_fire : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleHit(int effIndex)
		{
			if (GetCaster())
				GetCaster().ModifyPower(PowerType.SoulShards, +40);

			//TODO: Improve it later
			GetCaster().GetSpellHistory().ModifyCooldown(WarlockSpells.SOUL_FIRE, TimeSpan.FromSeconds(-2));
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
		}
	}
}