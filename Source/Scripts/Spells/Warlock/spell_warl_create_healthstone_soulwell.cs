// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Create Healthstone (Soulwell) - 34130
	[SpellScript(34130)]
	public class spell_warl_create_healthstone_soulwell : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(WarlockSpells.SOULWELL_CREATE_HEALTHSTONE, Difficulty.None) != null)
				return false;

			return true;
		}

		private void HandleScriptEffect(int effIndex)
		{
			GetCaster().CastSpell(GetCaster(), 23517, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		}
	}
}