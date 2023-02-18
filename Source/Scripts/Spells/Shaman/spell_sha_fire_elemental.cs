// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// 198067
	[SpellScript(198067)]
	public class spell_sha_fire_elemental : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleSummon(uint UnnamedParameter)
		{
			GetCaster().CastSpell(GetHitUnit(), ShamanSpells.FIRE_ELEMENTAL_SUMMON, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleSummon, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}