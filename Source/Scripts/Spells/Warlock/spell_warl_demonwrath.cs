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
	// Demonwrath damage - 193439
	[SpellScript(193439)]
	public class spell_warl_demonwrath : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void SelectTargets(List<WorldObject> targets)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var pets = new List<Creature>();
			caster.GetCreatureListInGrid(pets, 100.0f);

			pets.RemoveIf((Creature creature) =>
			              {
				              if (creature == caster)
					              return true;

				              if (!creature.HasAura(WarlockSpells.DEMONWRATH_AURA))
					              return true;

				              if (creature.GetCreatureType() != CreatureType.Demon)
					              return true;

				              return false;
			              });


			targets.RemoveIf((WorldObject obj) =>
			                 {
				                 if (!obj.ToUnit())
					                 return true;

				                 if (!caster.IsValidAttackTarget(obj.ToUnit()))
					                 return true;

				                 var inRange = false;

				                 foreach (Unit pet in pets)
					                 if (pet.GetExactDist(obj) <= 10.0f)
						                 inRange = true;

				                 return !inRange;
			                 });
		}

		private void HandleHit(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var aur = caster.GetAura(WarlockSpells.DEMONIC_CALLING);

			if (aur != null)
			{
				var aurEff = aur.GetEffect(1);

				if (aurEff != null)
					if (RandomHelper.randChance(aurEff.GetBaseAmount()))
						caster.CastSpell(caster, WarlockSpells.DEMONIC_CALLING_TRIGGER, true);
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(SelectTargets, 0, Targets.UnitSrcAreaEnemy));
		}
	}
}