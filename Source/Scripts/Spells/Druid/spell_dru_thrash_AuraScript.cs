﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 192090 - Thrash (Aura) - SPELL_DRUID_THRASH_BEAR_AURA
	internal class spell_dru_thrash_AuraScript : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.BloodFrenzyAura, DruidSpellIds.BloodFrenzyRageGain);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
		}

		private void HandlePeriodic(AuraEffect aurEff)
		{
			var caster = GetCaster();

			if (caster != null)
				if (caster.HasAura(DruidSpellIds.BloodFrenzyAura))
					caster.CastSpell(caster, DruidSpellIds.BloodFrenzyRageGain, true);
		}
	}
}