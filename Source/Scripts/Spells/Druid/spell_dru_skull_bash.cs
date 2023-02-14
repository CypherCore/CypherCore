// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 106839 - Skull Bash
	internal class spell_dru_skull_bash : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.SkullBashCharge, DruidSpellIds.SkullBashInterrupt);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}

		private void HandleDummy(uint effIndex)
		{
			GetCaster().CastSpell(GetHitUnit(), DruidSpellIds.SkullBashCharge, true);
			GetCaster().CastSpell(GetHitUnit(), DruidSpellIds.SkullBashInterrupt, true);
		}
	}
}