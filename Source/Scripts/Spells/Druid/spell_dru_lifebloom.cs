// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 33763 - Lifebloom
	internal class spell_dru_lifebloom : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spell)
		{
			return ValidateSpellInfo(DruidSpellIds.LifebloomFinalHeal);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		}

		private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			// Final heal only on duration end
			if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire ||
			    GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell)
				GetCaster().CastSpell(GetUnitOwner(), DruidSpellIds.LifebloomFinalHeal, true);
		}
	}
}