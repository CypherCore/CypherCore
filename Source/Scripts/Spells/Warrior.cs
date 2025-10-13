// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the Gnu General Public License. See License file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Warrior
{
    struct SpellIds
    {
        public const uint Avatar = 107574;
        public const uint Bladestorm = 227847;
        public const uint BladestormPeriodicWhirlwind = 50622;
        public const uint BloodthirstHeal = 117313;
        public const uint Charge = 34846;
        public const uint ChargeDropFirePeriodic = 126661;
        public const uint ChargeEffect = 198337;
        public const uint ChargeRootEffect = 105771;
        public const uint ColdSteelHotBloodTalent = 383959;
        public const uint ColossusSmash = 167105;
        public const uint ColossusSmashAura = 208086;
        public const uint CriticalThinkingEnergize = 392776;
        public const uint DeftExperience = 383295;
        public const uint Execute = 20647;
        public const uint Enrage = 184362;
        public const uint FrenziedEnrage = 383848;
        public const uint FrenzyTalent = 335077;
        public const uint FrenzyBuff = 335082;
        public const uint FreshMeatDebuff = 316044;
        public const uint FreshMeatTalent = 215568;
        public const uint FueledByViolenceHeal = 383104;
        public const uint GlyphOfTheBlazingTrail = 123779;
        public const uint GlyphOfHeroicLeap = 159708;
        public const uint GlyphOfHeroicLeapBuff = 133278;
        public const uint GushingWound = 385042;
        public const uint HeroicLeapJump = 178368;
        public const uint IgnorePain = 190456;
        public const uint ImprovedRagingBlow = 383854;
        public const uint ImprovedWhirlwind = 12950;
        public const uint IntimidatingShoutMenaceAoe = 316595;
        public const uint InvigoratingFury = 385174;
        public const uint InvigoratingFuryTalent = 383468;
        public const uint InForTheKill = 248621;
        public const uint InForTheKillHaste = 248622;
        public const uint ImpendingVictory = 202168;
        public const uint ImpendingVictoryHeal = 202166;
        public const uint ImprovedHeroicLeap = 157449;
        public const uint MortalStrike = 12294;
        public const uint MortalWounds = 115804;
        public const uint PowerfulEnrage = 440277;
        public const uint RallyingCry = 97463;
        public const uint Ravager = 228920;
        public const uint Recklessness = 1719;
        public const uint RumblingEarth = 275339;
        public const uint ShieldBlockAura = 132404;
        public const uint ShieldChargeEffect = 385953;
        public const uint ShieldSlam = 23922;
        public const uint ShieldSlamMarker = 224324;
        public const uint ShieldWall = 871;
        public const uint Shockwave = 46968;
        public const uint ShockwaveStun = 132168;
        public const uint SlaughteringStrikes = 388004;
        public const uint SlaughteringStrikesBuff = 393931;
        public const uint Stoicism = 70845;
        public const uint StormBoltStun = 132169;
        public const uint StormBolts = 436162;
        public const uint Strategist = 384041;
        public const uint SuddenDeath = 280721;
        public const uint SuddenDeathBuff = 280776;
        public const uint SweepingStrikesExtraAttack1 = 12723;
        public const uint SweepingStrikesExtraAttack2 = 26654;
        public const uint Taunt = 355;
        public const uint TitanicRage = 394329;
        public const uint TraumaEffect = 215537;
        public const uint ViciousContempt = 383885;
        public const uint Victorious = 32216;
        public const uint VictoryRushHeal = 118779;
        public const uint Warbreaker = 262161;
        public const uint WhirlwindCleaveAura = 85739;
        public const uint WhirlwindEnergize = 280715;
        public const uint WrathAndFury = 392936;

        public const uint VisualBlazingCharge = 26423;
    }

    struct WarriorMisc
    {
        public static void ApplyWhirlwindCleaveAura(Player caster, Difficulty difficulty, Spell triggeringSpell)
        {
            SpellInfo whirlwindCleaveAuraInfo = Global.SpellMgr.GetSpellInfo(SpellIds.WhirlwindCleaveAura, difficulty);
            int stackAmount = (int)(whirlwindCleaveAuraInfo.StackAmount);
            caster.ApplySpellMod(whirlwindCleaveAuraInfo, SpellModOp.MaxAuraStacks, ref stackAmount);

            caster.CastSpell(null, SpellIds.WhirlwindCleaveAura, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = triggeringSpell,
                SpellValueOverrides = { new(SpellValueMod.AuraStack, stackAmount) }
            });
        }
    }

    [Script] // 152278 - Anger Management
    class spell_warr_anger_management_proc : AuraScript
    {
        static TimeSpan CooldownReduction = TimeSpan.FromSeconds(1);
        static uint[] ArmsSpellIds = [SpellIds.ColossusSmash, SpellIds.Warbreaker, SpellIds.Bladestorm, SpellIds.Ravager];
        static uint[] FurySpellIds = [SpellIds.Recklessness, SpellIds.Bladestorm, SpellIds.Ravager];
        static uint[] ProtectionSpellIds = [SpellIds.Avatar, SpellIds.ShieldWall];

        static bool ValidateProc(AuraEffect aurEff, ProcEventInfo eventInfo, ChrSpecialization spec)
        {
            if (aurEff.GetAmount() == 0)
                return false;

            Player player = eventInfo.GetActor().ToPlayer();
            if (player == null)
                return false;

            Spell procSpell = eventInfo.GetProcSpell();
            if (procSpell == null)
                return false;

            if (procSpell.GetPowerTypeCostAmount(PowerType.Rage) <= 0)
                return false;

            return player.GetPrimarySpecialization() == spec;
        }

        static bool CheckArmsProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            if (!ValidateProc(aurEff, eventInfo, ChrSpecialization.WarriorArms))
                return false;

            // exclude non-attacks such as Ignore Pain
            if (!eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Warrior, new FlagArray128(0x100, 0x0, 0x0, 0x0)))
                return false;

            return true;
        }

        static bool CheckFuryProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return ValidateProc(aurEff, eventInfo, ChrSpecialization.WarriorFury);
        }

        static bool CheckProtectionProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return ValidateProc(aurEff, eventInfo, ChrSpecialization.WarriorProtection);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ColossusSmash, SpellIds.Bladestorm, SpellIds.Ravager, SpellIds.Warbreaker, SpellIds.Recklessness, SpellIds.Avatar, SpellIds.ShieldWall);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo, uint[] spellIds)
        {
            int rageCost = (int)eventInfo.GetProcSpell().GetPowerTypeCostAmount(PowerType.Rage) / 10; // db values are 10x the actual rage cost
            float multiplier = (float)(rageCost) / (float)(aurEff.GetAmount());
            TimeSpan cooldownMod = -(multiplier * CooldownReduction);

            foreach (uint spellId in spellIds)
                GetTarget().GetSpellHistory().ModifyCooldown(spellId, cooldownMod);
        }

        void OnProcArms(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            HandleProc(aurEff, eventInfo, ArmsSpellIds);
        }

        void OnProcFury(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            HandleProc(aurEff, eventInfo, FurySpellIds);
        }

        void OnProcProtection(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            HandleProc(aurEff, eventInfo, ProtectionSpellIds);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckArmsProc, 0, AuraType.Dummy));
            DoCheckEffectProc.Add(new(CheckProtectionProc, 1, AuraType.Dummy));
            DoCheckEffectProc.Add(new(CheckFuryProc, 2, AuraType.Dummy));

            OnEffectProc.Add(new(OnProcArms, 0, AuraType.Dummy));
            OnEffectProc.Add(new(OnProcProtection, 1, AuraType.Dummy));
            OnEffectProc.Add(new(OnProcFury, 2, AuraType.Dummy));
        }
    }

    [Script] // 392536 - Ashen Juggernaut
    class spell_warr_ashen_juggernaut : AuraScript
    {
        static bool CheckProc(ProcEventInfo eventInfo)
        {
            // should only proc on primary target
            return eventInfo.GetActionTarget() == eventInfo.GetProcSpell().m_targets.GetUnitTarget();
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script] // 107574 - Avatar
    class spell_warr_avatar : SpellScript
    {
        void HandleRemoveImpairingAuras(uint effIndex)
        {
            GetCaster().RemoveMovementImpairingAuras(true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleRemoveImpairingAuras, 5, SpellEffectName.ScriptEffect));
        }
    }

    // 23881 - Bloodthirst
    [Script] // 335096 - Bloodbath
    class spell_warr_bloodthirst : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BloodthirstHeal);
        }

        void CastHeal(uint effIndex)
        {
            if (GetHitUnit() != GetExplTargetUnit())
                return;

            GetCaster().CastSpell(GetCaster(), SpellIds.BloodthirstHeal, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(CastHeal, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 384036 - Brutal Vitality
    class spell_warr_brutal_vitality : AuraScript
    {
        uint _damageAmount;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IgnorePain);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            _damageAmount += MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
        }

        void HandleDummyTick(AuraEffect aurEff)
        {
            if (_damageAmount == 0)
                return;

            AuraEffect ignorePainAura = GetTarget().GetAuraEffect(SpellIds.IgnorePain, 0);
            if (ignorePainAura != null)
                ignorePainAura.ChangeAmount((int)(ignorePainAura.GetAmount() + _damageAmount));

            _damageAmount = 0;
        }

        public override void Register()
        {
            AfterEffectProc.Add(new(HandleProc, 0, AuraType.PeriodicDummy));
            OnEffectPeriodic.Add(new(HandleDummyTick, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 100 - Charge
    class spell_warr_charge : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChargeEffect);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.ChargeEffect, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 126661 - Warrior Charge Drop Fire Periodic
    class spell_warr_charge_drop_fire_periodic : AuraScript
    {
        void DropFireVisual(AuraEffect aurEff)
        {
            PreventDefaultAction();

            Unit target = GetTarget();
            if (target.IsSplineEnabled())
            {
                var from = target.MoveSpline.ComputePosition();
                var to = target.MoveSpline.ComputePosition(aurEff.GetPeriod());

                int fireCount = (int)Math.Round((to - from).Length());

                for (int i = 0; i < fireCount; ++i)
                {
                    int timeOffset = i * aurEff.GetPeriod() / fireCount;
                    var loc = target.MoveSpline.ComputePosition(timeOffset);
                    target.SendPlaySpellVisual(new Position(loc.X, loc.Y, loc.Z), SpellIds.VisualBlazingCharge, 0, 0, 1.0f, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(DropFireVisual, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 198337 - Charge Effect
    class spell_warr_charge_effect : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChargeRootEffect, SpellIds.ChargeDropFirePeriodic);
        }

        void HandleCharge(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            CastSpellExtraArgs args = new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.FullMask & ~TriggerCastFlags.CastDirectly,
                TriggeringSpell = GetSpell()
            };

            if (caster.HasAura(SpellIds.GlyphOfTheBlazingTrail))
                caster.CastSpell(target, SpellIds.ChargeDropFirePeriodic, args);

            caster.CastSpell(target, SpellIds.ChargeRootEffect, args);
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new(HandleCharge, 0, SpellEffectName.Charge));
        }
    }

    // 23881 - Bloodthirst
    [Script] // 335096 - Bloodbath
    class spell_warr_cold_steel_hot_blood_bloodthirst : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GushingWound, SpellIds.ColdSteelHotBloodTalent);
        }

        public override bool Load()
        {
            return GetCaster().HasAura(SpellIds.ColdSteelHotBloodTalent);
        }

        void CastGushingWound(uint effIndex)
        {
            if (!IsHitCrit())
                return;

            GetCaster().CastSpell(GetHitUnit(), SpellIds.GushingWound, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError
            });
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(CastGushingWound, 0, SpellEffectName.SchoolDamage));
        }
    }

    // 167105 - Colossus Smash
    [Script] // 262161 - Warbreaker
    class spell_warr_colossus_smash : SpellScript
    {
        bool _bonusHaste;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ColossusSmashAura, SpellIds.InForTheKill, SpellIds.InForTheKillHaste)
                && ValidateSpellEffect((SpellIds.InForTheKill, 2));
        }

        void HandleHit()
        {
            Unit target = GetHitUnit();
            Unit caster = GetCaster();

            GetCaster().CastSpell(GetHitUnit(), SpellIds.ColossusSmashAura, true);

            if (caster.HasAura(SpellIds.InForTheKill))
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.InForTheKill, Difficulty.None);
                if (spellInfo != null)
                {
                    if (target.HealthBelowPct(spellInfo.GetEffect(2).CalcValue(caster)))
                        _bonusHaste = true;
                }
            }
        }

        void HandleAfterCast()
        {
            Unit caster = GetCaster();
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.InForTheKill, Difficulty.None);
            if (spellInfo == null)
                return;

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, spellInfo.GetEffect(0).CalcValue(caster));
            if (_bonusHaste)
                args.AddSpellMod(SpellValueMod.BasePoint0, spellInfo.GetEffect(1).CalcValue(caster));
            caster.CastSpell(caster, SpellIds.InForTheKillHaste, args);
        }

        public override void Register()
        {
            OnHit.Add(new(HandleHit));
            AfterCast.Add(new(HandleAfterCast));
        }
    }

    [Script] // 389306 - Critical Thinking
    class spell_warr_critical_thinking : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CriticalThinkingEnergize);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            var rageCost = eventInfo.GetProcSpell().GetPowerTypeCostAmount(PowerType.Rage);
            if (rageCost.HasValue)
                GetTarget().CastSpell(null, SpellIds.CriticalThinkingEnergize, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                    .AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(rageCost.Value, aurEff.GetAmount())));
        }

        public override void Register()
        {
            AfterEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
        }
    }

    // 383295 - Deft Experience (attached to 23881 - Bloodthirst)
    [Script] // 383295 - Deft Experience (attached to 335096 - Bloodbath)
    class spell_warr_deft_experience : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellIds.DeftExperience, 1));
        }

        public override bool Load()
        {
            return GetCaster().HasAura(SpellIds.DeftExperience);
        }

        void HandleDeftExperience(uint effIndex)
        {
            if (GetHitUnit() != GetExplTargetUnit())
                return;

            Unit caster = GetCaster();
            Aura enrageAura = caster.GetAura(SpellIds.Enrage);
            if (enrageAura != null)
            {
                AuraEffect aurEff = caster.GetAuraEffect(SpellIds.DeftExperience, 1);
                if (aurEff != null)
                    enrageAura.SetDuration(enrageAura.GetDuration() + aurEff.GetAmount());
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDeftExperience, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 236279 - Devastator
    class spell_warr_devastator : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1)) && ValidateSpellInfo(SpellIds.ShieldSlam, SpellIds.ShieldSlamMarker);
        }

        void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            if (GetTarget().GetSpellHistory().HasCooldown(SpellIds.ShieldSlam))
            {
                if (RandomHelper.randChance(GetEffectInfo(1).CalcValue()))
                {
                    GetTarget().GetSpellHistory().ResetCooldown(SpellIds.ShieldSlam, true);
                    GetTarget().CastSpell(GetTarget(), SpellIds.ShieldSlamMarker, TriggerCastFlags.IgnoreCastInProgress);
                }
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new(OnProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 184361 - Enrage
    class spell_warr_enrage_proc : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FreshMeatTalent, SpellIds.FreshMeatDebuff);
        }

        static bool CheckRampageProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null || !spellInfo.IsAffected(SpellFamilyNames.Warrior, new FlagArray128(0x0, 0x0, 0x0, 0x8000000)))  // Rampage
                return false;

            return true;
        }

        static bool IsBloodthirst(SpellInfo spellInfo)
        {
            // Bloodthirst/Bloodbath
            return spellInfo.IsAffected(SpellFamilyNames.Warrior, new FlagArray128(0x0, 0x400));
        }

        static bool CheckBloodthirstProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null || !IsBloodthirst(spellInfo))
                return false;

            // Fresh Meat talent handling
            Unit actor = eventInfo.GetActor();
            if (actor != null)
            {
                if (actor.HasAura(SpellIds.FreshMeatTalent))
                {
                    Spell procSpell = eventInfo.GetProcSpell();
                    if (procSpell == null)
                        return false;

                    Unit target = procSpell.m_targets.GetUnitTarget();
                    if (target == null)
                        return false;

                    if (!target.HasAura(SpellIds.FreshMeatDebuff, actor.GetGUID()))
                        return true;
                }
            }

            return RandomHelper.randChance(aurEff.GetAmount());
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit auraTarget = GetTarget();

            auraTarget.CastSpell(null, SpellIds.Enrage, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = eventInfo.GetProcSpell()
            });

            // Fresh Meat talent handling
            if (auraTarget.HasAura(SpellIds.FreshMeatTalent))
            {
                Spell procSpell = eventInfo.GetProcSpell();
                if (procSpell == null)
                    return;

                if (!IsBloodthirst(procSpell.GetSpellInfo()))
                    return;

                Unit bloodthirstTarget = procSpell.m_targets.GetUnitTarget();
                if (bloodthirstTarget != null)
                    if (!bloodthirstTarget.HasAura(SpellIds.FreshMeatDebuff, auraTarget.GetGUID()))
                        auraTarget.CastSpell(bloodthirstTarget, SpellIds.FreshMeatDebuff, new CastSpellExtraArgs()
                        {
                            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError
                        });
            }
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckRampageProc, 0, AuraType.Dummy));
            DoCheckEffectProc.Add(new(CheckBloodthirstProc, 1, AuraType.Dummy));
            OnProc.Add(new(HandleProc));
        }
    }

    [Script] // 260798 - Execute (Arms, Protection)
    class spell_warr_execute_damage : SpellScript
    {
        static void CalculateExecuteDamage(SpellEffectInfo spellEffectInfo, Unit victim, ref int damageOrHealing, ref int flatMod, ref float pctMod)
        {
            // tooltip has 2 multiplier hardcoded in it $damage=${2.0*$260798s1}
            pctMod *= 2.0f;
        }

        public override void Register()
        {
            CalcDamage.Add(new(CalculateExecuteDamage));
        }
    }

    [Script] // 383848 - Frenzied Enrage (attached to 184362 - Enrage)
    class spell_warr_frenzied_enrage : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FrenziedEnrage)
                && ValidateSpellEffect((spellInfo.Id, 1))
                && spellInfo.GetEffect(0).IsAura(AuraType.MeleeSlow)
                && spellInfo.GetEffect(1).IsAura(AuraType.ModIncreaseSpeed);
        }

        public override bool Load()
        {
            return !GetCaster().HasAura(SpellIds.FrenziedEnrage);
        }

        void HandleFrenziedEnrage(ref WorldObject target)
        {
            target = null;
        }

        public override void Register()
        {
            OnObjectTargetSelect.Add(new(HandleFrenziedEnrage, 0, Targets.UnitCaster));
            OnObjectTargetSelect.Add(new(HandleFrenziedEnrage, 1, Targets.UnitCaster));
        }
    }

    [Script] // 335082 - frenzy
    class spell_warr_frenzy : AuraScript
    {
        ObjectGuid _targetGuid;

        public void SetTargetGuid(ObjectGuid guid) { _targetGuid = guid; }

        public ObjectGuid GetTarGetGUID() { return _targetGuid; }

        public override void Register() { }
    }

    [Script] // 335077 - Frenzy (attached to 184367 - Rampage)
    class spell_warr_frenzy_rampage : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FrenzyBuff, SpellIds.FrenzyTalent);
        }

        public override bool Load()
        {
            return GetCaster().HasAura(SpellIds.FrenzyTalent);
        }

        void HandleAfterCast(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit hitUnit = GetHitUnit();

            if (hitUnit != GetExplTargetUnit())
                return;

            caster.CastSpell(null, SpellIds.FrenzyBuff, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });

            Aura frenzyAura = caster.GetAura(SpellIds.FrenzyBuff);
            if (frenzyAura != null)
            {
                spell_warr_frenzy script = frenzyAura.GetScript<spell_warr_frenzy>();
                if (script != null)
                {
                    if (!script.GetTarGetGUID().IsEmpty() && script.GetTarGetGUID() != hitUnit.GetGUID())
                        frenzyAura.SetStackAmount(1);

                    script.SetTargetGuid(hitUnit.GetGUID());
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleAfterCast, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 440277 - Powerful Enrage (attached to 184362 - Enrage)
    class spell_warr_powerful_enrage : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerfulEnrage)
                && ValidateSpellEffect((spellInfo.Id, 4))
                && spellInfo.GetEffect(3).IsAura(AuraType.AddPctModifier) && spellInfo.GetEffect(3).MiscValue == (int)SpellModOp.HealingAndDamage
                && spellInfo.GetEffect(4).IsAura(AuraType.AddPctModifier) && spellInfo.GetEffect(4).MiscValue == (int)SpellModOp.PeriodicHealingAndDamage;
        }

        public override bool Load()
        {
            return !GetCaster().HasAura(SpellIds.PowerfulEnrage);
        }

        void HandlePowerfulEnrage(ref WorldObject target)
        {
            target = null;
        }

        public override void Register()
        {
            OnObjectTargetSelect.Add(new(HandlePowerfulEnrage, 3, Targets.UnitCaster));
            OnObjectTargetSelect.Add(new(HandlePowerfulEnrage, 4, Targets.UnitCaster));
        }
    }

    [Script] // 383103  - Fueled by Violence
    class spell_warr_fueled_by_violence : AuraScript
    {
        uint _nextHealAmount;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FueledByViolenceHeal);
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            _nextHealAmount += MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), GetEffectInfo(0).CalcValue(GetTarget()));
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            if (_nextHealAmount == 0)
                return;

            Unit target = GetTarget();
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)_nextHealAmount);

            target.CastSpell(target, SpellIds.FueledByViolenceHeal, args);
            _nextHealAmount = 0;
        }

        public override void Register()
        {
            OnProc.Add(new(HandleProc));
            OnEffectPeriodic.Add(new(HandlePeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 6544 - Heroic leap
    class spell_warr_heroic_leap : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HeroicLeapJump);
        }

        SpellCastResult CheckElevation()
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
                    else if (!result || generatedPath.GetPathType().HasAnyFlag(PathType.NoPath))
                        return SpellCastResult.NoPath;
                }
                else if (dest.GetPositionZ() > GetCaster().GetPositionZ() + 4.0f)
                    return SpellCastResult.NoPath;

                return SpellCastResult.SpellCastOk;
            }

            return SpellCastResult.NoValidTargets;
        }

        void HandleDummy(uint effIndex)
        {
            WorldLocation dest = GetHitDest();
            if (dest != null)
                GetCaster().CastSpell(dest, SpellIds.HeroicLeapJump, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckElevation));
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // Heroic Leap (triggered by Heroic Leap (6544)) - 178368
    class spell_warr_heroic_leap_jump : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfHeroicLeap, SpellIds.GlyphOfHeroicLeapBuff, SpellIds.ImprovedHeroicLeap, SpellIds.Taunt);
        }

        void AfterJump(uint effIndex)
        {
            if (GetCaster().HasAura(SpellIds.GlyphOfHeroicLeap))
                GetCaster().CastSpell(GetCaster(), SpellIds.GlyphOfHeroicLeapBuff, true);
            if (GetCaster().HasAura(SpellIds.ImprovedHeroicLeap))
                GetCaster().GetSpellHistory().ResetCooldown(SpellIds.Taunt, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(AfterJump, 1, SpellEffectName.JumpDest));
        }
    }

    [Script] // 202168 - Impending Victory
    class spell_warr_impending_victory : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ImpendingVictoryHeal);
        }

        void HandleAfterCast()
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, SpellIds.ImpendingVictoryHeal, true);
            caster.RemoveAurasDueToSpell(SpellIds.Victorious);
        }

        public override void Register()
        {
            AfterCast.Add(new(HandleAfterCast));
        }
    }

    [Script] // 12950 - Improved Whirlwind (attached to 190411 - Whirlwind)
    class spell_improved_whirlwind : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ImprovedWhirlwind, SpellIds.WhirlwindCleaveAura)
                && ValidateSpellEffect((spellInfo.Id, 2), (SpellIds.WhirlwindEnergize, 0));
        }

        public override bool Load()
        {
            return GetCaster().HasAura(SpellIds.ImprovedWhirlwind);
        }

        void HandleHit(uint effIndex)
        {
            long targetsHit = GetUnitTargetCountForEffect(0);
            if (targetsHit == 0)
                return;

            Player caster = GetCaster().ToPlayer();
            if (caster == null)
                return;

            int ragePerTarget = GetEffectValue();
            int baseRage = GetEffectInfo(0).CalcValue();
            int maxRage = baseRage + (ragePerTarget * GetEffectInfo(2).CalcValue());
            int rageGained = (int)Math.Min(baseRage + (targetsHit * ragePerTarget), maxRage);

            caster.CastSpell(null, SpellIds.WhirlwindEnergize, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell(),
                SpellValueOverrides = { new(SpellValueMod.BasePoint0, rageGained * 10) }
            });

            WarriorMisc.ApplyWhirlwindCleaveAura(caster, GetCastDifficulty(), GetSpell());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHit, 1, SpellEffectName.Dummy));
        }
    }

    [Script] // 5246 - Intimidating Shout
    class spell_warr_intimidating_shout : SpellScript
    {
        void FilterTargets(List<WorldObject> unitList)
        {
            unitList.Remove(GetExplTargetWorldObject());
        }

        void ClearTargets(List<WorldObject> unitList)
        {
            // This is used in effect 3, which is an Aoe Root effect.
            // This doesn't seem to be a thing anymore, so we clear the targets list here.
            unitList.Clear();
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 2, Targets.UnitSrcAreaEnemy));
            OnObjectAreaTargetSelect.Add(new(ClearTargets, 3, Targets.UnitSrcAreaEnemy));
        }
    }

    [Script] // 316594 - Intimidating Shout (Menace Talent, knock back . root)
    class spell_warr_intimidating_shout_menace_knock_back : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IntimidatingShoutMenaceAoe);
        }

        void FilterTargets(List<WorldObject> unitList)
        {
            unitList.Remove(GetExplTargetWorldObject());
        }

        void HandleRoot(uint effIndex)
        {
            CastSpellExtraArgs args = new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            };

            var caster = GetCaster();
            var targetsGuid = GetHitUnit().GetGUID();
            GetCaster().m_Events.AddEventAtOffset(() =>
            {
                Unit targetUnit = Global.ObjAccessor.GetUnit(caster, targetsGuid);
                if (targetUnit != null)
                    caster.CastSpell(targetUnit, SpellIds.IntimidatingShoutMenaceAoe, args);
            }, TimeSpan.FromSeconds(500));
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            OnEffectHitTarget.Add(new(HandleRoot, 0, SpellEffectName.KnockBack));
        }
    }

    [Script] // 385174 - Invigorating Fury (attached to 184364 - Enraged Regeneration)
    class spell_warr_invigorating_fury : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.InvigoratingFury, SpellIds.InvigoratingFuryTalent);
        }

        public override bool Load()
        {
            return GetCaster().HasAura(SpellIds.InvigoratingFuryTalent);
        }

        void CastHeal()
        {
            GetCaster().CastSpell(null, SpellIds.InvigoratingFury, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });
        }

        public override void Register()
        {
            AfterCast.Add(new(CastHeal));
        }
    }

    [Script] // 70844 - Item - Warrior T10 Protection 4P Bonus
    class spell_warr_item_t10_prot_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Stoicism)
                && ValidateSpellEffect((spellInfo.Id, 1));
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit target = eventInfo.GetActionTarget();
            int bp0 = (int)MathFunctions.CalculatePct(target.GetMaxHealth(), GetEffectInfo(1).CalcValue());
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
            target.CastSpell(null, SpellIds.Stoicism, args);
        }

        public override void Register()
        {
            OnProc.Add(new(HandleProc));
        }
    }

    [Script] // 12294 - Mortal Strike
    class spell_warr_mortal_strike : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MortalWounds);
        }

        void HandleMortalWounds(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.MortalWounds, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleMortalWounds, 0, SpellEffectName.SchoolDamage));
        }
    }

    // 383854 - Improved Raging Blow (attached to 85288 - Raging Blow, 335097 - Crushing Blow)
    [Script] // 392936 - Wrath and Fury (attached to 85288 - Raging Blow, 335097 - Crushing Blow)
    class spell_warr_raging_blow_cooldown_reset : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ImprovedRagingBlow)
                && ValidateSpellEffect((SpellIds.WrathAndFury, 0));
        }

        public override bool Load()
        {
            Unit caster = GetCaster();
            return caster.HasAura(SpellIds.ImprovedRagingBlow) || caster.HasAuraEffect(SpellIds.WrathAndFury, 0);
        }

        void HandleResetCooldown(uint effIndex)
        {
            // it is currently impossible to have Wrath and Fury without having Improved Raging Blow, but we will check it anyway
            Unit caster = GetCaster();
            int value = 0;
            if (caster.HasAura(SpellIds.ImprovedRagingBlow))
                value = GetEffectValue();

            AuraEffect auraEffect = caster.GetAuraEffect(SpellIds.WrathAndFury, 0);
            if (auraEffect != null)
                value += auraEffect.GetAmount();

            if (RandomHelper.randChance(value))
                caster.GetSpellHistory().RestoreCharge(GetSpellInfo().ChargeCategoryId);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleResetCooldown, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 97462 - Rallying Cry
    class spell_warr_rallying_cry : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RallyingCry);
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleScript(uint effIndex)
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)GetHitUnit().CountPctFromMaxHealth(GetEffectValue()));

            GetCaster().CastSpell(GetHitUnit(), SpellIds.RallyingCry, args);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 275339 - (attached to 46968 - Shockwave)
    class spell_warr_rumbling_earth : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellIds.RumblingEarth, 1));
        }

        public override bool Load()
        {
            return GetCaster().HasAura(SpellIds.RumblingEarth);
        }

        void HandleCooldownReduction(uint effIndex)
        {
            Unit caster = GetCaster();
            Aura rumblingEarth = caster.GetAura(SpellIds.RumblingEarth);
            if (rumblingEarth == null)
                return;

            AuraEffect minTargetCount = rumblingEarth.GetEffect(0);
            AuraEffect cooldownReduction = rumblingEarth.GetEffect(1);
            if (minTargetCount == null || cooldownReduction == null)
                return;

            if (GetUnitTargetCountForEffect(0) >= minTargetCount.GetAmount())
                GetCaster().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(-cooldownReduction.GetAmount()));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleCooldownReduction, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 2565 - Shield Block
    class spell_warr_shield_block : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShieldBlockAura);
        }

        void HandleHitTarget(uint effIndex)
        {
            GetCaster().CastSpell(null, SpellIds.ShieldBlockAura, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHitTarget, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 385952 - Shield Charge
    class spell_warr_shield_charge : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShieldChargeEffect);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.ShieldChargeEffect, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 46968 - Shockwave
    class spell_warr_shockwave : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShockwaveStun);
        }

        void HandleStun(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.ShockwaveStun, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleStun, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 107570 - Storm Bolt
    class spell_warr_storm_bolt : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StormBoltStun);
        }

        void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.StormBoltStun, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleOnHit, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 107570 - Storm Bolt
    class spell_warr_storm_bolts : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StormBolts);
        }

        public override bool Load()
        {
            return !GetCaster().HasAura(SpellIds.StormBolts);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.Clear();

            Unit target = GetExplTargetUnit();
            if (target != null)
                targets.Add(target);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaEnemy));
        }
    }

    [Script] // 384041 - Strategist
    class spell_warr_strategist : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShieldSlam, SpellIds.ShieldSlamMarker)
                && ValidateSpellEffect((SpellIds.Strategist, 0));
        }

        static bool CheckProc(AuraEffect aurEff, ProcEventInfo procEvent)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        void HandleCooldown(AuraEffect aurEff, ProcEventInfo procEvent)
        {
            Unit caster = GetTarget();
            caster.GetSpellHistory().ResetCooldown(SpellIds.ShieldSlam, true);
            caster.CastSpell(caster, SpellIds.ShieldSlamMarker, TriggerCastFlags.IgnoreCastInProgress);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new(HandleCooldown, 0, AuraType.Dummy));
        }
    }

    [Script] // 280776 - Sudden Death
    class spell_warr_sudden_death : AuraScript
    {
        static bool CheckProc(ProcEventInfo eventInfo)
        {
            // should only proc on primary target
            return eventInfo.GetActionTarget() == eventInfo.GetProcSpell().m_targets.GetUnitTarget();
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script] // 280721 - Sudden Death
    class spell_warr_sudden_death_proc : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SuddenDeathBuff);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().CastSpell(null, SpellIds.SuddenDeathBuff, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError
            });
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 12328, 18765, 35429 - Sweeping Strikes
    class spell_warr_sweeping_strikes : AuraScript
    {
        Unit _procTarget;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SweepingStrikesExtraAttack1, SpellIds.SweepingStrikesExtraAttack2);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            _procTarget = eventInfo.GetActor().SelectNearbyTarget(eventInfo.GetProcTarget());
            return _procTarget != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null)
            {
                SpellInfo spellInfo = damageInfo.GetSpellInfo();
                if (spellInfo != null && (spellInfo.Id == SpellIds.BladestormPeriodicWhirlwind || (spellInfo.Id == SpellIds.Execute && !_procTarget.HasAuraState(AuraStateType.Wounded20Percent))))
                {
                    // If triggered by Execute (while target is not under 20% hp) or Bladestorm deals normalized weapon damage
                    GetTarget().CastSpell(_procTarget, SpellIds.SweepingStrikesExtraAttack2, aurEff);
                }
                else
                {
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage());
                    GetTarget().CastSpell(_procTarget, SpellIds.SweepingStrikesExtraAttack1, args);
                }
            }
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 388933 - Tenderize
    class spell_warr_tenderize : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Enrage, SpellIds.SlaughteringStrikesBuff, SpellIds.SlaughteringStrikes);
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            GetTarget().CastSpell(null, SpellIds.Enrage, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = eventInfo.GetProcSpell()
            });
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();
            if (!target.HasAura(SpellIds.SlaughteringStrikes))
                return;

            target.CastSpell(null, SpellIds.SlaughteringStrikesBuff, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = eventInfo.GetProcSpell(),
                SpellValueOverrides = { new(SpellValueMod.AuraStack, aurEff.GetAmount()) }
            });
        }

        public override void Register()
        {
            OnProc.Add(new(HandleProc));
            OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 394329 - Titanic Rage
    class spell_warr_titanic_rage : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.WhirlwindCleaveAura);
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            Player target = GetTarget().ToPlayer();
            if (target == null)
                return;

            target.CastSpell(null, SpellIds.Enrage, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = eventInfo.GetProcSpell()
            });

            WarriorMisc.ApplyWhirlwindCleaveAura(target, GetCastDifficulty(), null);
        }

        public override void Register()
        {
            OnProc.Add(new(HandleProc));
        }
    }

    [Script] // 215538 - Trauma
    class spell_warr_trauma : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TraumaEffect);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetActionTarget();
            //Get 25% of damage from the spell casted (Slam & Whirlwind) plus Remaining Damage from Aura
            int damage = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()) / Global.SpellMgr.GetSpellInfo(SpellIds.TraumaEffect, GetCastDifficulty()).GetMaxTicks());
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, damage);
            GetCaster().CastSpell(target, SpellIds.TraumaEffect, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 28845 - Cheat Death
    class spell_warr_t3_prot_8p_bonus : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetActionTarget().HealthBelowPct(20))
                return true;

            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null && damageInfo.GetDamage() != 0)
                if (GetTarget().HealthBelowPctDamaged(20, damageInfo.GetDamage()))
                    return true;

            return false;
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script] // 389603 - Unbridled Ferocity
    class spell_warr_unbridled_ferocity : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Recklessness)
                && ValidateSpellEffect((spellInfo.Id, 1))
                && spellInfo.GetEffect(0).IsAura(AuraType.Dummy)
                && spellInfo.GetEffect(1).IsAura(AuraType.Dummy);
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            int durationMs = GetEffect(1).GetAmount();

            GetTarget().CastSpell(null, SpellIds.Recklessness, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError | TriggerCastFlags.IgnoreSpellAndCategoryCD,
                SpellValueOverrides = { new(SpellValueMod.Duration, durationMs) }
            });
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(GetEffect(0).GetAmount());
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnProc.Add(new(HandleProc));
        }
    }

    // 383885 - Vicious Contempt (attached to 23881 - Bloodthirst)
    [Script] // 383885 - Vicious Contempt (attached to 335096 - Bloodbath)
    class spell_warr_vicious_contempt : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellIds.ViciousContempt, 0));
        }

        public override bool Load()
        {
            return GetCaster().HasAura(SpellIds.ViciousContempt);
        }

        void CalculateDamage(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
        {
            if (!victim.HasAuraState(AuraStateType.Wounded35Percent))
                return;

            AuraEffect aurEff = GetCaster().GetAuraEffect(SpellIds.ViciousContempt, 0);
            if (aurEff != null)
                MathFunctions.AddPct(ref pctMod, aurEff.GetAmount());
        }

        public override void Register()
        {
            CalcDamage.Add(new(CalculateDamage));
        }
    }

    [Script] // 32215 - Victorious State
    class spell_warr_victorious_state : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ImpendingVictory);
        }

        void HandleOnProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            if (procInfo.GetActor().IsPlayer() && procInfo.GetActor().ToPlayer().GetPrimarySpecialization() == ChrSpecialization.WarriorFury)
                PreventDefaultAction();

            procInfo.GetActor().GetSpellHistory().ResetCooldown(SpellIds.ImpendingVictory, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleOnProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 34428 - Victory Rush
    class spell_warr_victory_rush : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Victorious, SpellIds.VictoryRushHeal);
        }

        void HandleHeal()
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, SpellIds.VictoryRushHeal, true);
            caster.RemoveAurasDueToSpell(SpellIds.Victorious);
        }

        public override void Register()
        {
            AfterCast.Add(new(HandleHeal));
        }
    }
}