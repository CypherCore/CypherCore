// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	// Ravager - 152277
	// Ravager - 228920
	[SpellScript(new uint[]
	             {
		             152277, 228920
	             })]
	public class spell_warr_ravager : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleOnHit(uint UnnamedParameter)
		{
			var dest = GetExplTargetDest();

			if (dest != null)
				GetCaster().CastSpell(dest.GetPosition(), WarriorSpells.RAVAGER_SUMMON, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
		}
	}
}