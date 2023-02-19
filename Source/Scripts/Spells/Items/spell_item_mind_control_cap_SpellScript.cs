// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 13180 - Gnomish Mind Control Cap
internal class spell_item_mind_control_cap_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		if (!GetCastItem())
			return false;

		return true;
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.GnomishMindControlCap, ItemSpellIds.Dullard);
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
			if (RandomHelper.randChance(95))
				caster.CastSpell(target, RandomHelper.randChance(32) ? ItemSpellIds.Dullard : ItemSpellIds.GnomishMindControlCap, new CastSpellExtraArgs(GetCastItem()));
			else
				target.CastSpell(caster, ItemSpellIds.GnomishMindControlCap, true); // backfire - 5% chance
		}
	}
}