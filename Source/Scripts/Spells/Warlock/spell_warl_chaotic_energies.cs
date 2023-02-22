// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[Script] // 77220 - Mastery: Chaotic Energies
	internal class spell_warl_chaotic_energies : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 2, false, AuraScriptHookType.EffectAbsorb));
		}

		private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref double absorbAmount)
		{
			var auraEffect = GetEffect(1);

			if (auraEffect == null ||
			    !GetTargetApplication().HasEffect(1))
			{
				PreventDefaultAction();

				return;
			}

			// You take ${$s2/3}% reduced Damage
			var damageReductionPct = (double)auraEffect.GetAmount() / 3;
			// plus a random amount of up to ${$s2/3}% additional reduced Damage
			damageReductionPct += RandomHelper.FRand(0.0f, damageReductionPct);

			absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), damageReductionPct);
		}
	}
}