// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleHit(int effIndex)
		{
			PreventHitDefaultEffect(effIndex);
			SetHitDamage(CalculateDamage());
		}

		private double CalculateDamage()
		{
			double damage = 0;
			var auras  = GetHitUnit().GetAppliedAurasQuery();

			foreach (var aura in auras.HasSpellFamily(SpellFamilyNames.Warlock).GetResults())
			{
				var spell = aura.GetBase().GetSpellInfo();

				if (spell.SpellFamilyName == SpellFamilyNames.Warlock && (spell.SpellFamilyFlags & new FlagArray128(502, 8110, 300000, 0))) // out of Mastery : Potent Afflictions
				{
					var effects = aura.GetBase().GetAuraEffects();

					foreach (var iter in effects)
						if (iter.Value.GetAuraType() == AuraType.PeriodicDamage)
						{
							double valToUse = 0f;

							if (spell.Id == WarlockSpells.CORRUPTION_DOT)
								valToUse = iter.Value.GetRemainingAmount(GetSpellInfo().GetEffect(2).BasePoints * 1000);

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