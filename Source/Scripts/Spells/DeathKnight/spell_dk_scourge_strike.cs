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
				if (caster.HasAura(DeathKnightSpells.UNHOLY_FRENZY))
					caster.CastSpell(caster, DeathKnightSpells.UNHOLY_FRENZY_BUFF, true);

				var pestilentPustulesAura = caster.GetAura(DeathKnightSpells.PESTILENT_PUSTULES);
				if (pestilentPustulesAura != null)
					if (festeringWoundAura.GetStackAmount() >= pestilentPustulesAura.GetSpellInfo().GetEffect(0).BasePoints)
						caster.ModifyPower(PowerType.Runes, 1);

				double festeringWoundBurst = 1f;
				var castiragorAura      = caster.GetAura(DeathKnightSpells.CASTIGATOR);

				if (castiragorAura != null)
					festeringWoundBurst += castiragorAura.GetSpellInfo().GetEffect(1).BasePoints;

				festeringWoundBurst = Math.Min(festeringWoundBurst, festeringWoundAura.GetStackAmount());

				for (byte i = 0; i < festeringWoundBurst; ++i)
				{
					caster.CastSpell(target, DeathKnightSpells.FESTERING_WOUND_DAMAGE, true);
					festeringWoundAura.ModStackAmount(-1);
				}
			}
		}
	}

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
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(GetTargetUnit, 1, Targets.UnitDestAreaEnemy));
    }
}