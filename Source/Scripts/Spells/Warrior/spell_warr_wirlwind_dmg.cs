using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	// 1680 Whirlwind
	[SpellScript(1680)]
	public class spell_warr_wirlwind_dmg : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public void HandleOnHitTarget(uint UnnamedParameter)
		{
			var caster = GetCaster().ToPlayer();

			if (caster != null)
				if (caster.HasAura(202316)) // Fervor of Battle
				{
					var target = caster.GetSelectedUnit();

					if (target != null)
						if (caster.IsValidAttackTarget(target))
							caster.CastSpell(target, WarriorSpells.SLAM_ARMS, true);
				}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.TriggerSpell, SpellScriptHookType.EffectHitTarget));
		}
	}
}