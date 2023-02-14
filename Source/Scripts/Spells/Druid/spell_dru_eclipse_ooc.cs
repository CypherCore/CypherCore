// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 329910 - Eclipse out of combat - SPELL_DRUID_ECLIPSE_OOC
	internal class spell_dru_eclipse_ooc : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.EclipseDummy, DruidSpellIds.EclipseSolarSpellCnt, DruidSpellIds.EclipseLunarSpellCnt);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(Tick, 0, AuraType.PeriodicDummy));
		}

		private void Tick(AuraEffect aurEff)
		{
			var owner        = GetTarget();
			var auraEffDummy = owner.GetAuraEffect(DruidSpellIds.EclipseDummy, 0);

			if (auraEffDummy == null)
				return;

			if (!owner.IsInCombat() &&
			    (!owner.HasAura(DruidSpellIds.EclipseSolarSpellCnt) || !owner.HasAura(DruidSpellIds.EclipseLunarSpellCnt)))
			{
				// Restore 2 stacks to each spell when out of combat
				spell_dru_eclipse_common.SetSpellCount(owner, DruidSpellIds.EclipseSolarSpellCnt, (uint)auraEffDummy.GetAmount());
				spell_dru_eclipse_common.SetSpellCount(owner, DruidSpellIds.EclipseLunarSpellCnt, (uint)auraEffDummy.GetAmount());
			}
		}
	}
}