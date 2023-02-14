// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	[Script] // 107570 - Storm Bolt
	internal class spell_warr_storm_bolt : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.STORM_BOLT_STUN);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}

		private void HandleOnHit(uint effIndex)
		{
			GetCaster().CastSpell(GetHitUnit(), WarriorSpells.STORM_BOLT_STUN, true);
		}
	}
}