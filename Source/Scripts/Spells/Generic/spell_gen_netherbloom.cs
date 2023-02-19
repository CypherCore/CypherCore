// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 28702 - Netherbloom
internal class spell_gen_netherbloom : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		for (byte i = 0; i < 5; ++i)
			if (!ValidateSpellInfo(GenericSpellIds.NetherBloomPollen1 + i))
				return false;

		return true;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var target = GetHitUnit();

		if (target)
		{
			// 25% chance of casting a random buff
			if (RandomHelper.randChance(75))
				return;

			// triggered spells are 28703 to 28707
			// Note: some sources say, that there was the possibility of
			//       receiving a debuff. However, this seems to be Removed by a patch.

			// don't overwrite an existing aura
			for (byte i = 0; i < 5; ++i)
				if (target.HasAura(GenericSpellIds.NetherBloomPollen1 + i))
					return;

			target.CastSpell(target, GenericSpellIds.NetherBloomPollen1 + RandomHelper.URand(0, 4), true);
		}
	}
}