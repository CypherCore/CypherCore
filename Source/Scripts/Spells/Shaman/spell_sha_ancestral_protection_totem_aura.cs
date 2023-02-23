// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	//207498 ancestral protection
	[SpellScript(207498)]
	public class spell_sha_ancestral_protection_totem_aura : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private void CalculateAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
		{
			amount = -1;
		}

		private void HandleAfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
				return;

			var totem = GetCaster();

			if (totem == null)
				return;

			totem.CastSpell(GetTargetApplication().GetTarget(), TotemSpells.TOTEM_TOTEMIC_REVIVAL, true);
			totem.KillSelf();
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
			AuraEffects.Add(new AuraEffectApplyHandler(HandleAfterRemove, 1, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		}
	}
}