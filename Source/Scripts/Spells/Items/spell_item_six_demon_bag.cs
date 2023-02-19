// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 14537 Six Demon Bag
internal class spell_item_six_demon_bag : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.Frostbolt, ItemSpellIds.Polymorph, ItemSpellIds.SummonFelhoundMinion, ItemSpellIds.Fireball, ItemSpellIds.ChainLightning, ItemSpellIds.EnvelopingWinds);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (target)
		{
			uint spellId;
			var  rand = RandomHelper.URand(0, 99);

			if (rand < 25) // Fireball (25% chance)
			{
				spellId = ItemSpellIds.Fireball;
			}
			else if (rand < 50) // Frostball (25% chance)
			{
				spellId = ItemSpellIds.Frostbolt;
			}
			else if (rand < 70) // Chain Lighting (20% chance)
			{
				spellId = ItemSpellIds.ChainLightning;
			}
			else if (rand < 80) // Polymorph (10% chance)
			{
				spellId = ItemSpellIds.Polymorph;

				if (RandomHelper.URand(0, 100) <= 30) // 30% chance to self-cast
					target = caster;
			}
			else if (rand < 95) // Enveloping Winds (15% chance)
			{
				spellId = ItemSpellIds.EnvelopingWinds;
			}
			else // Summon Felhund minion (5% chance)
			{
				spellId = ItemSpellIds.SummonFelhoundMinion;
				target  = caster;
			}

			caster.CastSpell(target, spellId, new CastSpellExtraArgs(GetCastItem()));
		}
	}
}