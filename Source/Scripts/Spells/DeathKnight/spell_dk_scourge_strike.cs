// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Scripts.Spells.DemonHunter;

namespace Scripts.Spells.DeathKnight;

[SpellScript(55090)]
public class spell_dk_scourge_strike : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleOnHit(int effIndex)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (target != null)
		{
			var festeringWoundAura = target.GetAura(DeathKnightSpells.FESTERING_WOUND, GetCaster().GetGUID());

			if (festeringWoundAura != null)
			{
				festeringWoundAura.ModStackAmount(-1);
                caster.CastSpell(target, DeathKnightSpells.FESTERING_WOUND_DAMAGE, true);
                if (caster.HasAura(DeathKnightSpells.BURSTING_SORES))
					caster.CastSpell(target, DeathKnightSpells.BURSTING_SORES_DAMAGE, true);

			}
		}
	}

    private void GetTargetUnit(List<WorldObject> targets) {
        if (!GetCaster().HasAura(DeathKnightSpells.DEATH_AND_DECAY_CLEAVE)) {
            targets.RemoveIf((WorldObject target) => {
                return GetExplTargetUnit() != target;
            });
        }
    }

    public override void Register() {
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(GetTargetUnit, 1, Targets.UnitDestAreaEnemy));
    }
}