// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 45438 - Ice Block
internal class spell_mage_ice_block : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.EverwarmSocks);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectTargetSelectHandler(PreventStunWithEverwarmSocks, 0, Targets.UnitCaster));
		SpellEffects.Add(new ObjectTargetSelectHandler(PreventEverwarmSocks, 5, Targets.UnitCaster));
		SpellEffects.Add(new ObjectTargetSelectHandler(PreventEverwarmSocks, 6, Targets.UnitCaster));
	}

	private void PreventStunWithEverwarmSocks(ref WorldObject target)
	{
		if (GetCaster().HasAura(MageSpells.EverwarmSocks))
			target = null;
	}

	private void PreventEverwarmSocks(ref WorldObject target)
	{
		if (!GetCaster().HasAura(MageSpells.EverwarmSocks))
			target = null;
	}
}