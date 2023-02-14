// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 264178 - Demonbolt
	[SpellScript(264178)]
	public class spell_warlock_demonbolt_new : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleHit(uint UnnamedParameter)
		{
			if (GetCaster())
			{
				GetCaster().CastSpell(GetCaster(), WarlockSpells.DEMONBOLT_ENERGIZE, true);
				GetCaster().CastSpell(GetCaster(), WarlockSpells.DEMONBOLT_ENERGIZE, true);
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
		}
	}
}