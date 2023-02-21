// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(70890)]
public class spell_dk_scourge_strike_trigger : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

    private void GetTargetUnit(List<WorldObject> targets)
    {
        if (!GetCaster().HasAura(DeathKnightSpells.DEATH_AND_DECAY_CLEAVE))
		{
            targets.RemoveIf((WorldObject target) => {
                return GetExplTargetUnit() != target;
            });
        }
    }

    public override void Register()
	{
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(GetTargetUnit, 1, Targets.UnitDestAreaEnemy));
    }
}