// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 80353 - Time Warp
internal class spell_mage_time_warp : SpellScript, ISpellAfterHit, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.TemporalDisplacement, MageSpells.HunterInsanity, MageSpells.ShamanExhaustion, MageSpells.ShamanSated, MageSpells.PetNetherwindsFatigued);
	}

	public void AfterHit()
	{
		var target = GetHitUnit();

		if (target)
			target.CastSpell(target, MageSpells.TemporalDisplacement, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, SpellConst.EffectAll, Targets.UnitCasterAreaRaid));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void RemoveInvalidTargets(List<WorldObject> targets)
	{
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.TemporalDisplacement));
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.HunterInsanity));
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.ShamanExhaustion));
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.ShamanSated));
	}
}