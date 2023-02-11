﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 203953 - Brambles - SPELL_DRUID_BRAMBLES_PASSIVE
	internal class spell_dru_brambles : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.BramblesRelect, DruidSpellIds.BramblesDamageAura);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0, false, AuraScriptHookType.EffectAbsorb));
			AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAfterAbsorb, 0, false, AuraScriptHookType.EffectAfterAbsorb));
		}

		private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
		{
			// Prevent Removal
			PreventDefaultAction();
		}

		private void HandleAfterAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
		{
			// reflect back Damage to the Attacker
			var target   = GetTarget();
			var attacker = dmgInfo.GetAttacker();

			if (attacker != null)
				target.CastSpell(attacker, DruidSpellIds.BramblesRelect, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorbAmount));
		}
	}
}