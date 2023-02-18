// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Bgs.Protocol.Notification.V1;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Immolate Dot - 157736
	[SpellScript(157736)]
	public class spell_warlock_immolate_dot : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandlePeriodic(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var aur = caster.GetAura(WarlockSpells.CHANNEL_DEMONFIRE_ACTIVATOR);

			if (aur != null)
				aur.RefreshDuration();

            if (GetAura() != null && caster.HasAura(WarlockSpells.ROARING_BLAZE))
			{
                var dmgEff = Global.SpellMgr.GetSpellInfo(WarlockSpells.ROARING_BLASE_DMG_PCT, Difficulty.None)?.GetEffect(0);

				if (dmgEff != null)
				{
					var damage = GetEffect(0).GetAmount();
					MathFunctions.AddPct(ref damage, dmgEff.BasePoints);

					GetEffect(0).SetAmount(damage);
					GetAura().SetNeedClientUpdateForTargets();
				}
			}
		}

		private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.CastSpell(caster, WarlockSpells.CHANNEL_DEMONFIRE_ACTIVATOR, true);
		}

		private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

            var checker = new UnitAuraCheck<Unit>(false, WarlockSpells.IMMOLATE_DOT, caster.GetGUID());
            var enemies  = new List<Unit>();
			var check    = new AnyUnfriendlyUnitInObjectRangeCheck(caster, caster, 100.0f, checker.Invoke);
			var searcher = new UnitListSearcher(caster, enemies, check, GridType.All);
			Cell.VisitGrid(caster, searcher, 100.0f);

			if (enemies.Count == 0)
			{
				var aur = caster.GetAura(WarlockSpells.CHANNEL_DEMONFIRE_ACTIVATOR);

				if (aur != null)
					aur.SetDuration(0);
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.RealOrReapplyMask));
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterRemove));
		}
	}
}