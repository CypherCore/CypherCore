using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Summon Darkglare - 205180
	[SpellScript(205180)]
	public class spell_warlock_summon_darkglare : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleOnHitTarget(int UnnamedParameter)
		{
			var target = GetHitUnit();

			if (target != null)
			{
				var effectList = target.GetAuraEffectsByType(AuraType.PeriodicDamage);

				foreach (var effect in effectList)
				{
					var aura = effect.GetBase();

					if (aura != null)
						aura.ModDuration(8 * Time.InMilliseconds);
				}
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		}
	}
}