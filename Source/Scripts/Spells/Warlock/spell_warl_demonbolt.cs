// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Demonbolt - 157695
	[SpellScript(157695)]
	public class spell_warl_demonbolt : SpellScript, IHasSpellEffects
	{
		private int _summons = 0;

		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleHit(uint UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			var damage = GetHitDamage();
			MathFunctions.AddPct(ref damage, _summons * 20);
			SetHitDamage(damage);
		}

		private void CountSummons(List<WorldObject> targets)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			foreach (var wo in targets)
			{
				if (!wo.ToCreature())
					continue;

				if (wo.ToCreature().GetOwner() != caster)
					continue;

				if (wo.ToCreature().GetCreatureType() != CreatureType.Demon)
					continue;

				_summons++;
			}

			targets.Clear();
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountSummons, 2, Targets.UnitCasterAndSummons));
		}
	}
}