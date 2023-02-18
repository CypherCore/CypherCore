// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid
{
	[Script] // 50286 - Starfall (Dummy)
	internal class spell_dru_starfall_dummy : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}

		private void FilterTargets(List<WorldObject> targets)
		{
			targets.Resize(2);
		}

		private void HandleDummy(uint effIndex)
		{
			var caster = GetCaster();

			// Shapeshifting into an animal form or mounting cancels the effect
			if (caster.GetCreatureType() == CreatureType.Beast ||
			    caster.IsMounted())
			{
				var spellInfo = GetTriggeringSpell();

				if (spellInfo != null)
					caster.RemoveAura(spellInfo.Id);

				return;
			}

			// Any effect which causes you to lose control of your character will supress the starfall effect.
			if (caster.HasUnitState(UnitState.Controlled))
				return;

			caster.CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
		}
	}
}