// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	internal struct SpellIds
	{
		public const uint BladestormPeriodicWhirlwind = 50622;
		public const uint BloodthirstHeal = 117313;
		public const uint Charge = 34846;
		public const uint ChargeEffect = 218104;
		public const uint ChargeEffectBlazingTrail = 198337;
		public const uint ChargePauseRageDecay = 109128;
		public const uint ChargeRootEffect = 105771;
		public const uint ChargeSlowEffect = 236027;
		public const uint ColossusSmash = 167105;
		public const uint ColossusSmashEffect = 208086;
		public const uint Execute = 20647;
		public const uint GlyphOfTheBlazingTrail = 123779;
		public const uint GlyphOfHeroicLeap = 159708;
		public const uint GlyphOfHeroicLeapBuff = 133278;
		public const uint HeroicLeapJump = 178368;
		public const uint ImpendingVictory = 202168;
		public const uint ImpendingVictoryHeal = 202166;
		public const uint ImprovedHeroicLeap = 157449;
		public const uint MortalStrike = 12294;
		public const uint MortalWounds = 213667;
		public const uint RallyingCry = 97463;
		public const uint Shockwave = 46968;
		public const uint ShockwaveStun = 132168;
		public const uint Stoicism = 70845;
		public const uint StormBoltStun = 132169;
		public const uint SweepingStrikesExtraAttack1 = 12723;
		public const uint SweepingStrikesExtraAttack2 = 26654;
		public const uint Taunt = 355;
		public const uint TraumaEffect = 215537;
		public const uint Victorious = 32216;
		public const uint VictoriousRushHeal = 118779;
	}

	internal struct Misc
	{
		public const uint SpellVisualBlazingCharge = 26423;
	}

	[Script] // 23881 - Bloodthirst
	internal class spell_warr_bloodthirst : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.BloodthirstHeal);
		}

		private void HandleDummy(uint effIndex)
		{
			GetCaster().CastSpell(GetCaster(), SpellIds.BloodthirstHeal, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 3, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
		}
	}

	[Script] // 100 - Charge
	internal class spell_warr_charge : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.ChargeEffect, SpellIds.ChargeEffectBlazingTrail);
		}

		private void HandleDummy(uint effIndex)
		{
			uint spellId = SpellIds.ChargeEffect;

			if (GetCaster().HasAura(SpellIds.GlyphOfTheBlazingTrail))
				spellId = SpellIds.ChargeEffectBlazingTrail;

			GetCaster().CastSpell(GetHitUnit(), spellId, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}

	[Script] // 126661 - Warrior Charge Drop Fire Periodic
	internal class spell_warr_charge_drop_fire_periodic : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> Effects { get; } = new();

		private void DropFireVisual(AuraEffect aurEff)
		{
			PreventDefaultAction();

			if (GetTarget().IsSplineEnabled())
				for (uint i = 0; i < 5; ++i)
				{
					int     timeOffset = (int)(6 * i * aurEff.GetPeriod() / 25);
					Vector4 loc        = GetTarget().MoveSpline.ComputePosition(timeOffset);
					GetTarget().SendPlaySpellVisual(new Position(loc.X, loc.Y, loc.Z), 0.0f, Misc.SpellVisualBlazingCharge, 0, 0, 1.0f, true);
				}
		}

		public override void Register()
		{
			Effects.Add(new EffectPeriodicHandler(DropFireVisual, 0, AuraType.PeriodicTriggerSpell));
		}
	}

	// 198337 - Charge Effect (dropping Blazing Trail)
	[Script] // 218104 - Charge Effect
	internal class spell_warr_charge_effect : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.ChargePauseRageDecay, SpellIds.ChargeRootEffect, SpellIds.ChargeSlowEffect);
		}

		private void HandleCharge(uint effIndex)
		{
			Unit caster = GetCaster();
			Unit target = GetHitUnit();
			caster.CastSpell(caster, SpellIds.ChargePauseRageDecay, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 0));
			caster.CastSpell(target, SpellIds.ChargeRootEffect, true);
			caster.CastSpell(target, SpellIds.ChargeSlowEffect, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleCharge, 0, SpellEffectName.Charge, SpellScriptHookType.LaunchTarget));
		}
	}

	[Script] // 167105 - Colossus Smash 7.1.5
	internal class spell_warr_colossus_smash_SpellScript : SpellScript, IOnHit
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.ColossusSmashEffect);
		}

		public void OnHit()
		{
			Unit target = GetHitUnit();

			if (target)
				GetCaster().CastSpell(target, SpellIds.ColossusSmashEffect, true);
		}
	}

	[Script] // 6544 Heroic leap
	internal class spell_warr_heroic_leap : SpellScript, ICheckCastHander, IHasSpellEffects
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.HeroicLeapJump);
		}

		public SpellCastResult CheckCast()
		{
			WorldLocation dest = GetExplTargetDest();

			if (dest != null)
			{
				if (GetCaster().HasUnitMovementFlag(MovementFlag.Root))
					return SpellCastResult.Rooted;

				if (GetCaster().GetMap().Instanceable())
				{
					float range = GetSpellInfo().GetMaxRange(true, GetCaster()) * 1.5f;

					PathGenerator generatedPath = new(GetCaster());
					generatedPath.SetPathLengthLimit(range);

					bool result = generatedPath.CalculatePath(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ(), false);

					if (generatedPath.GetPathType().HasAnyFlag(PathType.Short))
						return SpellCastResult.OutOfRange;
					else if (!result ||
					         generatedPath.GetPathType().HasAnyFlag(PathType.NoPath))
						return SpellCastResult.NoPath;
				}
				else if (dest.GetPositionZ() > GetCaster().GetPositionZ() + 4.0f)
				{
					return SpellCastResult.NoPath;
				}

				return SpellCastResult.SpellCastOk;
			}

			return SpellCastResult.NoValidTargets;
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
		}

		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleDummy(uint effIndex)
		{
			WorldLocation dest = GetHitDest();

			if (dest != null)
				GetCaster().CastSpell(dest.GetPosition(), SpellIds.HeroicLeapJump, new CastSpellExtraArgs(true));
		}
	}

	[Script] // Heroic Leap (triggered by Heroic Leap (6544)) - 178368
	internal class spell_warr_heroic_leap_jump : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.GlyphOfHeroicLeap,
			                         SpellIds.GlyphOfHeroicLeapBuff,
			                         SpellIds.ImprovedHeroicLeap,
			                         SpellIds.Taunt);
		}

		private void AfterJump(uint effIndex)
		{
			if (GetCaster().HasAura(SpellIds.GlyphOfHeroicLeap))
				GetCaster().CastSpell(GetCaster(), SpellIds.GlyphOfHeroicLeapBuff, true);

			if (GetCaster().HasAura(SpellIds.ImprovedHeroicLeap))
				GetCaster().GetSpellHistory().ResetCooldown(SpellIds.Taunt, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(AfterJump, 1, SpellEffectName.JumpDest, SpellScriptHookType.EffectHit));
		}
	}

	[Script] // 202168 - Impending Victory
	internal class spell_warr_impending_victory : SpellScript, IAfterCast
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.ImpendingVictoryHeal);
		}

		public void AfterCast()
		{
			Unit caster = GetCaster();
			caster.CastSpell(caster, SpellIds.ImpendingVictoryHeal, true);
			caster.RemoveAurasDueToSpell(SpellIds.Victorious);
		}
	}

	// 5246 - Intimidating Shout
	[Script]
	internal class spell_warr_intimidating_shout : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void FilterTargets(List<WorldObject> unitList)
		{
			unitList.Remove(GetExplTargetWorldObject());
		}

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitSrcAreaEnemy));
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 2, Targets.UnitSrcAreaEnemy));
		}
	}

	// 70844 - Item - Warrior T10 Protection 4P Bonus
	[Script] // 7.1.5
	internal class spell_warr_item_t10_prot_4p_bonus : AuraScript, IAuraOnProc
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.Stoicism) && spellInfo.GetEffects().Count > 1;
		}

		public void OnProc(ProcEventInfo eventInfo)
		{
			PreventDefaultAction();

			Unit               target = eventInfo.GetActionTarget();
			int                bp0    = (int)MathFunctions.CalculatePct(target.GetMaxHealth(), GetEffectInfo(1).CalcValue());
			CastSpellExtraArgs args   = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
			target.CastSpell((Unit)null, SpellIds.Stoicism, args);
		}
	}

	[Script] // 12294 - Mortal Strike 7.1.5
	internal class spell_warr_mortal_strike : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.MortalWounds);
		}

		private void HandleDummy(uint effIndex)
		{
			Unit target = GetHitUnit();

			if (target)
				GetCaster().CastSpell(target, SpellIds.MortalWounds, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}

	[Script] // 97462 - Rallying Cry
	internal class spell_warr_rallying_cry : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.RallyingCry);
		}

		public override bool Load()
		{
			return GetCaster().IsTypeId(TypeId.Player);
		}

		private void HandleScript(uint effIndex)
		{
			CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)GetHitUnit().CountPctFromMaxHealth(GetEffectValue()));

			GetCaster().CastSpell(GetHitUnit(), SpellIds.RallyingCry, args);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}

	[Script] // 46968 - Shockwave
	internal class spell_warr_shockwave : SpellScript, IAfterCast, IHasSpellEffects
	{
		private uint _targetCount;

		public override bool Validate(SpellInfo spellInfo)
		{
			if (!ValidateSpellInfo(SpellIds.Shockwave, SpellIds.ShockwaveStun))
				return false;

			return spellInfo.GetEffects().Count > 3;
		}

		public override bool Load()
		{
			return GetCaster().IsTypeId(TypeId.Player);
		}

		// Cooldown reduced by 20 sec if it strikes at least 3 targets.
		public void AfterCast()
		{
			if (_targetCount >= (uint)GetEffectInfo(0).CalcValue())
				GetCaster().ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(-GetEffectInfo(3).CalcValue()));
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleStun, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}

		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleStun(uint effIndex)
		{
			GetCaster().CastSpell(GetHitUnit(), SpellIds.ShockwaveStun, true);
			++_targetCount;
		}
	}

	[Script] // 107570 - Storm Bolt
	internal class spell_warr_storm_bolt : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.StormBoltStun);
		}

		private void HandleOnHit(uint effIndex)
		{
			GetCaster().CastSpell(GetHitUnit(), SpellIds.StormBoltStun, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}

	// 52437 - Sudden Death
	[Script]
	internal class spell_warr_sudden_death : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> Effects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.ColossusSmash);
		}

		private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			// Remove cooldown on Colossus Smash
			Player player = GetTarget().ToPlayer();

			if (player)
				player.GetSpellHistory().ResetCooldown(SpellIds.ColossusSmash, true);
		}

		public override void Register()
		{
			Effects.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply)); // correct?
		}
	}

	// 12328, 18765, 35429 - Sweeping Strikes
	[Script]
	internal class spell_warr_sweeping_strikes : AuraScript, IAuraCheckProc, IHasAuraEffects
	{
		private Unit _procTarget;

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.SweepingStrikesExtraAttack1, SpellIds.SweepingStrikesExtraAttack2);
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			_procTarget = eventInfo.GetActor().SelectNearbyTarget(eventInfo.GetProcTarget());

			return _procTarget;
		}

		public override void Register()
		{
			Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}

		public List<IAuraEffectHandler> Effects { get; } = new();

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			DamageInfo damageInfo = eventInfo.GetDamageInfo();

			if (damageInfo != null)
			{
				SpellInfo spellInfo = damageInfo.GetSpellInfo();

				if (spellInfo != null &&
				    (spellInfo.Id == SpellIds.BladestormPeriodicWhirlwind || (spellInfo.Id == SpellIds.Execute && !_procTarget.HasAuraState(AuraStateType.Wounded20Percent))))
				{
					// If triggered by Execute (while target is not under 20% hp) or Bladestorm deals normalized weapon damage
					GetTarget().CastSpell(_procTarget, SpellIds.SweepingStrikesExtraAttack2, new CastSpellExtraArgs(aurEff));
				}
				else
				{
					CastSpellExtraArgs args = new(aurEff);
					args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage());
					GetTarget().CastSpell(_procTarget, SpellIds.SweepingStrikesExtraAttack1, args);
				}
			}
		}
	}

	[Script] // 215538 - Trauma
	internal class spell_warr_trauma : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> Effects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.TraumaEffect);
		}

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			Unit target = eventInfo.GetActionTarget();
			//Get 25% of damage from the spell casted (Slam & Whirlwind) plus Remaining Damage from Aura
			int                damage = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()) / Global.SpellMgr.GetSpellInfo(SpellIds.TraumaEffect, GetCastDifficulty()).GetMaxTicks());
			CastSpellExtraArgs args   = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.BasePoint0, damage);
			GetCaster().CastSpell(target, SpellIds.TraumaEffect, args);
		}

		public override void Register()
		{
			Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}

	[Script] // 28845 - Cheat Death
	internal class spell_warr_t3_prot_8p_bonus : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			if (eventInfo.GetActionTarget().HealthBelowPct(20))
				return true;

			DamageInfo damageInfo = eventInfo.GetDamageInfo();

			if (damageInfo != null &&
			    damageInfo.GetDamage() != 0)
				if (GetTarget().HealthBelowPctDamaged(20, damageInfo.GetDamage()))
					return true;

			return false;
		}
	}

	[Script] // 32215 - Victorious State
	internal class spell_warr_victorious_state : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> Effects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.ImpendingVictory);
		}

		private void HandleOnProc(AuraEffect aurEff, ProcEventInfo procInfo)
		{
			if (procInfo.GetActor().GetTypeId() == TypeId.Player &&
			    procInfo.GetActor().ToPlayer().GetPrimarySpecialization() == (uint)TalentSpecialization.WarriorFury)
				PreventDefaultAction();

			procInfo.GetActor().GetSpellHistory().ResetCooldown(SpellIds.ImpendingVictory, true);
		}

		public override void Register()
		{
			Effects.Add(new EffectProcHandler(HandleOnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}
	}

	[Script] // 34428 - Victory Rush
	internal class spell_warr_victory_rush : SpellScript, IAfterCast
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(SpellIds.Victorious, SpellIds.VictoriousRushHeal);
		}

		public void AfterCast()
		{
			Unit caster = GetCaster();

			caster.CastSpell(caster, SpellIds.VictoriousRushHeal, true);
			caster.RemoveAurasDueToSpell(SpellIds.Victorious);
		}
	}
}