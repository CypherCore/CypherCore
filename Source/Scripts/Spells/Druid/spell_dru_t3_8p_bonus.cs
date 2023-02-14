// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 28719 - Healing Touch
	internal class spell_dru_t3_8p_bonus : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.Exhilarate);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var spell = eventInfo.GetProcSpell();

			if (spell == null)
				return;

			var caster             = eventInfo.GetActor();
			var spellPowerCostList = spell.GetPowerCost();
			var spellPowerCost     = spellPowerCostList.First(cost => cost.Power == PowerType.Mana);

			if (spellPowerCost == null)
				return;

			var                amount = MathFunctions.CalculatePct(spellPowerCost.Amount, aurEff.GetAmount());
			CastSpellExtraArgs args   = new(aurEff);
			args.AddSpellMod(SpellValueMod.BasePoint0, amount);
			caster.CastSpell((Unit)null, DruidSpellIds.Exhilarate, args);
		}
	}
}