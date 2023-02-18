// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// 215864 Rainfall
	[SpellScript(215864)]
	public class aura_sha_rainfall_AuraScript : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private void HandleHealPeriodic(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				var rainfallTrigger = caster.GetSummonedCreatureByEntry(ShamanNpcs.NPC_RAINFALL);

				if (rainfallTrigger != null)
					caster.CastSpell(rainfallTrigger.GetPosition(), ShamanSpells.RAINFALL_HEAL, true);
			}
		}

		private void HandleAfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				var rainfallTrigger = caster.GetSummonedCreatureByEntry(ShamanNpcs.NPC_RAINFALL);

				if (rainfallTrigger != null)
					rainfallTrigger.DespawnOrUnsummon(TimeSpan.FromMilliseconds(100));
			}
		}

		private void HandleProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
		{
			SetDuration(GetDuration() + GetEffect(2).GetBaseAmount() * Time.InMilliseconds, (GetEffect(3).GetBaseAmount() * Time.InMilliseconds) > 0);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandleHealPeriodic, 1, AuraType.PeriodicDummy));
			AuraEffects.Add(new AuraEffectApplyHandler(HandleAfterRemove, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 2, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}
}