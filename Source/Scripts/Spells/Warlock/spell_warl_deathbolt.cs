using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 264106 - Deathbolt
	[SpellScript(264106)]
	public class spell_warl_deathbolt : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(int effIndex)
		{
			PreventHitDefaultEffect(effIndex);
			SetHitDamage(CalculateDamage());
		}

		private int CalculateDamage()
		{
			var damage = 0;
			var auras  = GetHitUnit().GetAppliedAuras();

			foreach (var aura in auras.KeyValueList)
			{
				var spell = aura.Value.GetBase().GetSpellInfo();

				if (spell.SpellFamilyName == SpellFamilyNames.Warlock && (spell.SpellFamilyFlags & new FlagArray128(502, 8110, 300000, 0))) // out of Mastery : Potent Afflictions
				{
					var effects = aura.Value.GetBase().GetAuraEffects();

					foreach (var iter in effects)
						if (iter.GetAuraType() == AuraType.PeriodicDamage)
						{
							var valToUse = 0;

							if (spell.Id == WarlockSpells.CORRUPTION_DOT)
								valToUse = iter.GetRemainingAmount(GetSpellInfo().GetEffect(2).BasePoints * 1000);

							damage += valToUse * GetSpellInfo().GetEffect(1).BasePoints / 100;
						}
				}
			}

			return damage;
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}
}