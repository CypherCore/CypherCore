using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Thal'kiel's Consumption - 211714
	[SpellScript(211714)]
	public class spell_warlock_artifact_thalkiels_consumption : SpellScript, IHasSpellEffects
	{
		private uint _damage = 0;

		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleHit(uint UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (target == null || caster == null)
				return;

			caster.CastSpell(target, WarlockSpells.THALKIELS_CONSUMPTION_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)_damage));
		}

		private void SaveDamage(List<WorldObject> targets)
		{
			targets.RemoveIf((WorldObject target) =>
			                 {
				                 if (!target.ToUnit() || target.ToPlayer())
					                 return true;

				                 if (target.ToCreature().GetCreatureType() != CreatureType.Demon)
					                 return true;

				                 return false;
			                 });

			var basePoints = GetSpellInfo().GetEffect(1).BasePoints;

			foreach (var pet in targets)
				_damage += (uint)pet.ToUnit().CountPctFromMaxHealth(basePoints);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(SaveDamage, 1, Targets.UnitCasterAndSummons));
		}
	}
}