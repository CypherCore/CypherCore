// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 155580 - Lunar Inspiration
	internal class spell_dru_lunar_inspiration : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spell)
		{
			return ValidateSpellInfo(DruidSpellIds.LunarInspirationOverride);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
			AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		}

		private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			GetTarget().CastSpell(GetTarget(), DruidSpellIds.LunarInspirationOverride, true);
		}

		private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			GetTarget().RemoveAurasDueToSpell(DruidSpellIds.LunarInspirationOverride);
		}
	}
}