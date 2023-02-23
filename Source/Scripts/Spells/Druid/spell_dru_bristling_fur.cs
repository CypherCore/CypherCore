// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 155835 - Bristling Fur
	internal class spell_dru_bristling_fur : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.BristlingFurGainRage);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			// BristlingFurRage = 100 * Damage / MaxHealth.
			var damageInfo = eventInfo.GetDamageInfo();

			if (damageInfo != null)
			{
				var target = GetTarget();
				var rage   = (uint)(target.GetMaxPower(PowerType.Rage) * (double)damageInfo.GetDamage() / (double)target.GetMaxHealth());

				if (rage > 0)
					target.CastSpell(target, DruidSpellIds.BristlingFurGainRage, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)rage));
			}
		}
	}
}