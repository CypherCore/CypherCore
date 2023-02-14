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
	[Script] // 16864 - Omen of Clarity
	internal class spell_dru_omen_of_clarity : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.BalanceT10Bonus, DruidSpellIds.BalanceT10BonusProc);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			var target = GetTarget();

			if (target.HasAura(DruidSpellIds.BalanceT10Bonus))
				target.CastSpell(null, DruidSpellIds.BalanceT10BonusProc, true);
		}
	}
}