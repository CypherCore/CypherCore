// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Soul Conduit - 215941
	[SpellScript(215941)]
	public class spell_warl_soul_conduit : AuraScript, IHasAuraEffects, IAuraCheckProc
	{
		private int _refund = 0;

		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			var caster = GetCaster();

			if (caster == null)
				return false;

			if (eventInfo.GetActor() && eventInfo.GetActor() != caster)
				return false;

			var spell = eventInfo.GetProcSpell();

			if (spell == null)
			{
				var costs = spell.GetPowerCost();

				var costData = costs.FirstOrDefault(cost => cost.Power == PowerType.Mana && cost.Amount > 0);

				if (costData == null)
					return false;

				_refund = costData.Amount;

				return true;
			}

			return false;
		}

		private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (RandomHelper.randChance(GetSpellInfo().GetEffect(0).BasePoints))
				caster.CastSpell(caster, WarlockSpells.SOUL_CONDUIT_REFUND, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)_refund));
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}
}