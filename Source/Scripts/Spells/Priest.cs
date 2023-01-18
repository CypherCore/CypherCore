// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Movement;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Scripts.Spells.Priest
{
    struct SpellIds
    {
        public const uint AngelicFeatherAreatrigger = 158624;
        public const uint AngelicFeatherAura = 121557;
        public const uint ArmorOfFaith = 28810;
        public const uint Atonement = 81749;
        public const uint AtonementHeal = 81751;
        public const uint AtonementTriggered = 194384;
        public const uint AtonementTriggeredPowerTrinity = 214206;
        public const uint BlessedHealing = 70772;
        public const uint BodyAndSoul = 64129;
        public const uint BodyAndSoulSpeed = 65081;
        public const uint DivineBlessing = 40440;
        public const uint DivineStarDamage = 122128;
        public const uint DivineStarHeal = 110745;
        public const uint DivineWrath = 40441;
        public const uint FlashHeal = 2061;
        public const uint GuardianSpiritHeal = 48153;
        public const uint HaloDamage = 120696;
        public const uint HaloHeal = 120692;
        public const uint Heal = 2060;
        public const uint HolyWordChastise = 88625;
        public const uint HolyWordSanctify = 34861;
        public const uint HolyWordSerenity = 2050;
        public const uint ItemEfficiency = 37595;
        public const uint LeapOfFaithEffect = 92832;
        public const uint LevitateEffect = 111759;
        public const uint MasochismTalent = 193063;
        public const uint MasochismPeriodicHeal = 193065;
        public const uint MasteryGrace = 271534;
        public const uint MindBombStun = 226943;
        public const uint OracularHeal = 26170;
        public const uint Penance = 47540;
        public const uint PenanceChannelDamage = 47758;
        public const uint PenanceChannelHealing = 47757;
        public const uint PenanceDamage = 47666;
        public const uint SPELL_PRIEST_PENANCE_HEALING = 47750;
        public const uint PowerOfTheDarkSide = 198069;
        public const uint PowerOfTheDarkSideTint = 225795;
        public const uint PowerWordShield = 17;
        public const uint PowerWordSolaceEnergize = 129253;
        public const uint PrayerOfMendingAura = 41635;
        public const uint PrayerOfMendingHeal = 33110;
        public const uint PrayerOfMendingJump = 155793;
        public const uint PrayerOfHealing = 596;
        public const uint Rapture = 47536;
        public const uint Renew = 139;
        public const uint RenewedHope = 197469;
        public const uint RenewedHopeEffect = 197470;
        public const uint ShadowMendDamage = 186439;
        public const uint ShadowMendPeriodicDummy = 187464;
        public const uint ShieldDisciplineEnergize = 47755;
        public const uint ShieldDisciplinePassive = 197045;
        public const uint SinsOfTheMany = 280398;
        public const uint Smite = 585;
        public const uint SpiritOfRedemption = 27827;
        public const uint StrengthOfSoul = 197535;
        public const uint StrengthOfSoulEffect = 197548;
        public const uint ThePenitentAura = 200347;
        public const uint Trinity = 214205;
        public const uint VampiricEmbraceHeal = 15290;
        public const uint VampiricTouchDispel = 64085;
        public const uint VoidShield = 199144;
        public const uint VoidShieldEffect = 199145;
        public const uint WeakenedSoul = 6788;

        public const uint GenReplenishment = 57669;
    }

    [Script] // 121536 - Angelic Feather talent
    class spell_pri_angelic_feather_trigger : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AngelicFeatherAreatrigger);
        }

        void HandleEffectDummy(uint effIndex)
        {
            Position destPos = GetHitDest().GetPosition();
            float radius = GetEffectInfo().CalcRadius();

            // Caster is prioritary
            if (GetCaster().IsWithinDist2d(destPos, radius))
            {
                GetCaster().CastSpell(GetCaster(), SpellIds.AngelicFeatherAura, true);
            }
            else
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.CastDifficulty = GetCastDifficulty();
                GetCaster().CastSpell(destPos, SpellIds.AngelicFeatherAreatrigger, args);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // Angelic Feather areatrigger - created by SPELL_PRIEST_ANGELIC_FEATHER_AREATRIGGER
    class areatrigger_pri_angelic_feather : AreaTriggerAI
    {
        public areatrigger_pri_angelic_feather(AreaTrigger areatrigger) : base(areatrigger) { }

        // Called when the AreaTrigger has just been initialized, just before added to map
        public override void OnInitialize()
        {
            Unit caster = at.GetCaster();
            if (caster)
            {
                List<AreaTrigger> areaTriggers = caster.GetAreaTriggers(SpellIds.AngelicFeatherAreatrigger);

                if (areaTriggers.Count >= 3)
                    areaTriggers.First().SetDuration(0);
            }
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster)
            {
                if (caster.IsFriendlyTo(unit))
                {
                    // If target already has aura, increase duration to max 130% of initial duration
                    caster.CastSpell(unit, SpellIds.AngelicFeatherAura, true);
                    at.SetDuration(0);
                }
            }
        }
    }

    [Script] // 26169 - Oracle Healing Bonus
    class spell_pri_aq_3p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.OracularHeal);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();
            if (caster == eventInfo.GetProcTarget())
                return;

            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return;

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(healInfo.GetHeal(), 10));
            caster.CastSpell(caster, SpellIds.OracularHeal, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 81749 - Atonement
    public class spell_pri_atonement : AuraScript
    {
        List<ObjectGuid> _appliedAtonements = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AtonementHeal, SpellIds.SinsOfTheMany)
            && spellInfo.GetEffects().Count > 1
            && Global.SpellMgr.GetSpellInfo(SpellIds.SinsOfTheMany, Difficulty.None).GetEffects().Count > 2;
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo() != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount()));
            _appliedAtonements.RemoveAll(targetGuid =>
            {
                Unit target = Global.ObjAccessor.GetUnit(GetTarget(), targetGuid);
                if (target)
                {
                    if (target.GetExactDist(GetTarget()) < GetEffectInfo(1).CalcValue())
                        GetTarget().CastSpell(target, SpellIds.AtonementHeal, args);

                    return false;
                }
                return true;
            });
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }

        public void AddAtonementTarget(ObjectGuid target)
        {
            _appliedAtonements.Add(target);

            UpdateSinsOfTheManyValue();
        }

        public void RemoveAtonementTarget(ObjectGuid target)
        {
            _appliedAtonements.Remove(target);

            UpdateSinsOfTheManyValue();
        }

        void UpdateSinsOfTheManyValue()
        {
            float[] damageByStack = { 12.0f, 12.0f, 10.0f, 8.0f, 7.0f, 6.0f, 5.0f, 5.0f, 4.0f, 4.0f, 3.0f };

            foreach (uint effectIndex in new[] { 0, 1, 2 })
            {
                AuraEffect sinOfTheMany = GetUnitOwner().GetAuraEffect(SpellIds.SinsOfTheMany, effectIndex);
                if (sinOfTheMany != null)
                    sinOfTheMany.ChangeAmount((int)damageByStack[Math.Min(_appliedAtonements.Count, damageByStack.Length - 1)]);
            }
        }
    }

    [Script] // 194384, 214206 - Atonement
    class spell_pri_atonement_triggered : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Atonement);
        }

        void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                Aura atonement = caster.GetAura(SpellIds.Atonement);
                if (atonement != null)
                {
                    var script = atonement.GetScript<spell_pri_atonement>();
                    if (script != null)
                        script.AddAtonementTarget(GetTarget().GetGUID());
                }
            }
        }

        void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                Aura atonement = caster.GetAura(SpellIds.Atonement);
                if (atonement != null)
                {
                    var script = atonement.GetScript<spell_pri_atonement>();
                    if (script != null)
                        script.RemoveAtonementTarget(GetTarget().GetGUID());
                }
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 64844 - Divine Hymn
    class spell_pri_divine_hymn : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
            {
                Unit target = obj.ToUnit();
                if (target)
                    return !GetCaster().IsInRaidWith(target);

                return true;
            });

            uint maxTargets = 3;

            if (targets.Count > maxTargets)
            {
                targets.Sort(new HealthPctOrderPred());
                targets.Resize(maxTargets);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, SpellConst.EffectAll, Targets.UnitSrcAreaAlly));
        }
    }

    [Script] // 110744 - Divine Star
    class areatrigger_pri_divine_star : AreaTriggerAI
    {
        TaskScheduler _scheduler = new();
        Position _casterCurrentPosition = new();
        List<ObjectGuid> _affectedUnits = new();

        public areatrigger_pri_divine_star(AreaTrigger areatrigger) : base(areatrigger) { }

        public override void OnInitialize()
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                _casterCurrentPosition = caster.GetPosition();

                // Note: max. distance at which the Divine Star can travel to is 24 yards.
                float divineStarXOffSet = 24.0f;

                Position destPos = _casterCurrentPosition;
                at.MovePositionToFirstCollision(destPos, divineStarXOffSet, 0.0f);

                PathGenerator firstPath = new(at);
                firstPath.CalculatePath(destPos.GetPositionX(), destPos.GetPositionY(), destPos.GetPositionZ(), false);

                Vector3 endPoint = firstPath.GetPath().Last();

                // Note: it takes 1000ms to reach 24 yards, so it takes 41.67ms to run 1 yard.
                at.InitSplines(firstPath.GetPath().ToList(), (uint)(at.GetDistance(endPoint.X, endPoint.Y, endPoint.Z) * 41.67f));
            }
        }

        public override void OnUpdate(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                if (!_affectedUnits.Contains(unit.GetGUID()))
                {
                    if (caster.IsValidAttackTarget(unit))
                        caster.CastSpell(unit, SpellIds.DivineStarDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
                    else if (caster.IsValidAssistTarget(unit))
                        caster.CastSpell(unit, SpellIds.DivineStarHeal, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));

                    _affectedUnits.Add(unit.GetGUID());
                }
            }
        }

        public override void OnUnitExit(Unit unit)
        {
            // Note: this ensures any unit receives a second hit if they happen to be inside the AT when Divine Star starts its return path.
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                if (!_affectedUnits.Contains(unit.GetGUID()))
                {
                    if (caster.IsValidAttackTarget(unit))
                        caster.CastSpell(unit, SpellIds.DivineStarDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
                    else if (caster.IsValidAssistTarget(unit))
                        caster.CastSpell(unit, SpellIds.DivineStarHeal, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));

                    _affectedUnits.Add(unit.GetGUID());
                }
            }
        }

        public override void OnDestinationReached()
        {
            Unit caster = at.GetCaster();
            if (caster == null)
                return;

            if (at.GetDistance(_casterCurrentPosition) > 0.05f)
            {
                _affectedUnits.Clear();

                ReturnToCaster();
            }
            else
                at.Remove();
        }

        void ReturnToCaster()
        {
            _scheduler.Schedule(TimeSpan.FromMilliseconds(0), task =>
                {
                    Unit caster = at.GetCaster();
                    if (caster != null)
                    {
                        _casterCurrentPosition = caster.GetPosition();

                        List<Vector3> returnSplinePoints = new();

                        returnSplinePoints.Add(at.GetPosition());
                        returnSplinePoints.Add(at.GetPosition());
                        returnSplinePoints.Add(caster.GetPosition());
                        returnSplinePoints.Add(caster.GetPosition());

                        at.InitSplines(returnSplinePoints, (uint)at.GetDistance(caster) / 24 * 1000);

                        task.Repeat(TimeSpan.FromMilliseconds(250));
                    }
                });
        }
    }
    
    [Script] // 47788 - Guardian Spirit
    class spell_pri_guardian_spirit : AuraScript
    {
        uint healPct;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GuardianSpiritHeal) && spellInfo.GetEffects().Count > 1;
        }

        public override bool Load()
        {
            healPct = (uint)GetEffectInfo(1).CalcValue();
            return true;
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            // Set absorbtion amount to unlimited
            amount = -1;
        }

        void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            Unit target = GetTarget();
            if (dmgInfo.GetDamage() < target.GetHealth())
                return;

            int healAmount = (int)target.CountPctFromMaxHealth((int)healPct);
            // Remove the aura now, we don't want 40% healing bonus
            Remove(AuraRemoveMode.EnemySpell);
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, healAmount);
            target.CastSpell(target, SpellIds.GuardianSpiritHeal, args);
            absorbAmount = dmgInfo.GetDamage();
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
            OnEffectAbsorb.Add(new EffectAbsorbHandler(Absorb, 1));
        }
    }

    [Script] // 120517 - Halo
    class areatrigger_pri_halo : AreaTriggerAI
    {
        public areatrigger_pri_halo(AreaTrigger areatrigger) : base(areatrigger) { }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                if (caster.IsValidAttackTarget(unit))
                    caster.CastSpell(unit, SpellIds.HaloDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
                else if (caster.IsValidAssistTarget(unit))
                    caster.CastSpell(unit, SpellIds.HaloHeal, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
            }
        }
    }
    
    [Script] // 63733 - Holy Words
    class spell_pri_holy_words : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Heal, SpellIds.FlashHeal, SpellIds.PrayerOfHealing, SpellIds.Renew, SpellIds.Smite, SpellIds.HolyWordChastise, SpellIds.HolyWordSanctify, SpellIds.HolyWordSerenity)
                && Global.SpellMgr.GetSpellInfo(SpellIds.HolyWordSerenity, Difficulty.None).GetEffects().Count > 1
                && Global.SpellMgr.GetSpellInfo(SpellIds.HolyWordSanctify, Difficulty.None).GetEffects().Count > 3
                && Global.SpellMgr.GetSpellInfo(SpellIds.HolyWordChastise, Difficulty.None).GetEffects().Count > 1;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null)
                return;

            uint targetSpellId;
            uint cdReductionEffIndex;
            switch (spellInfo.Id)
            {
                case SpellIds.Heal:
                case SpellIds.FlashHeal: // reduce Holy Word: Serenity cd by 6 seconds
                    targetSpellId = SpellIds.HolyWordSerenity;
                    cdReductionEffIndex = 1;
                    // cdReduction = sSpellMgr.GetSpellInfo(SPELL_PRIEST_HOLY_WORD_SERENITY, GetCastDifficulty()).GetEffect(EFFECT_1).CalcValue(player);
                    break;
                case SpellIds.PrayerOfHealing: // reduce Holy Word: Sanctify cd by 6 seconds
                    targetSpellId = SpellIds.HolyWordSanctify;
                    cdReductionEffIndex = 2;
                    break;
                case SpellIds.Renew: // reuce Holy Word: Sanctify cd by 2 seconds
                    targetSpellId = SpellIds.HolyWordSanctify;
                    cdReductionEffIndex = 3;
                    break;
                case SpellIds.Smite: // reduce Holy Word: Chastise cd by 4 seconds
                    targetSpellId = SpellIds.HolyWordChastise;
                    cdReductionEffIndex = 1;
                    break;
                default:
                    Log.outWarn(LogFilter.Spells, $"HolyWords aura has been proced by an unknown spell: {GetSpellInfo().Id}");
                    return;
            }

            SpellInfo targetSpellInfo = Global.SpellMgr.GetSpellInfo(targetSpellId, GetCastDifficulty());
            int cdReduction = targetSpellInfo.GetEffect(cdReductionEffIndex).CalcValue(GetTarget());
            GetTarget().GetSpellHistory().ModifyCooldown(targetSpellInfo, TimeSpan.FromSeconds(-cdReduction), true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 40438 - Priest Tier 6 Trinket
    class spell_pri_item_t6_trinket : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DivineBlessing, SpellIds.DivineWrath);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();
            if (eventInfo.GetSpellTypeMask().HasAnyFlag(ProcFlagsSpellType.Heal))
                caster.CastSpell((Unit)null, SpellIds.DivineBlessing, true);

            if (eventInfo.GetSpellTypeMask().HasAnyFlag(ProcFlagsSpellType.Damage))
                caster.CastSpell((Unit)null, SpellIds.DivineWrath, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 92833 - Leap of Faith
    class spell_pri_leap_of_faith_effect_trigger : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LeapOfFaithEffect);
        }

        void HandleEffectDummy(uint effIndex)
        {
            Position destPos = GetHitDest().GetPosition();

            SpellCastTargets targets = new();
            targets.SetDst(destPos);
            targets.SetUnitTarget(GetCaster());
            GetHitUnit().CastSpell(targets, (uint)GetEffectValue(), new CastSpellExtraArgs(GetCastDifficulty()));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 1706 - Levitate
    class spell_pri_levitate : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LevitateEffect);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.LevitateEffect, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 205369 - Mind Bomb
    class spell_pri_mind_bomb : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MindBombStun);
        }

        void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Death || GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
                GetCaster()?.CastSpell(GetTarget().GetPosition(), SpellIds.MindBombStun, new CastSpellExtraArgs(true));
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(RemoveEffect, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 47540 - Penance
    class spell_pri_penance : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PenanceChannelDamage, SpellIds.PenanceChannelHealing);
        }

        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();
            if (target)
            {
                if (!caster.IsFriendlyTo(target))
                {
                    if (!caster.IsValidAttackTarget(target))
                        return SpellCastResult.BadTargets;

                    if (!caster.IsInFront(target))
                        return SpellCastResult.NotInfront;
                }
            }
            return SpellCastResult.SpellCastOk;
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target)
            {
                if (caster.IsFriendlyTo(target))
                    caster.CastSpell(target, SpellIds.PenanceChannelHealing, new CastSpellExtraArgs().SetTriggeringSpell(GetSpell()));
                else
                    caster.CastSpell(target, SpellIds.PenanceChannelDamage, new CastSpellExtraArgs().SetTriggeringSpell(GetSpell()));
            }
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 47758 - Penance (Channel Damage), 47757 - Penance (Channel Healing)
    class spell_pri_penance_channeled : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerOfTheDarkSide);
        }

        void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.RemoveAura(SpellIds.PowerOfTheDarkSide);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 198069 - Power of the Dark Side
    class spell_pri_power_of_the_dark_side : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerOfTheDarkSideTint);
        }

        void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(caster, SpellIds.PowerOfTheDarkSideTint, true);
        }

        void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.RemoveAura(SpellIds.PowerOfTheDarkSideTint);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 47666 - Penance (Damage)
    class spell_pri_power_of_the_dark_side_damage_bonus : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerOfTheDarkSide);
        }

        void HandleLaunchTarget(uint effIndex)
        {
            AuraEffect powerOfTheDarkSide = GetCaster().GetAuraEffect(SpellIds.PowerOfTheDarkSide, 0);
            if (powerOfTheDarkSide != null)
            {
                PreventHitDefaultEffect(effIndex);

                float damageBonus = GetCaster().SpellDamageBonusDone(GetHitUnit(), GetSpellInfo(), (uint)GetEffectValue(), DamageEffectType.SpellDirect, GetEffectInfo());
                float value = damageBonus + damageBonus * GetEffectVariance();
                value *= 1.0f + (powerOfTheDarkSide.GetAmount() / 100.0f);
                value = GetHitUnit().SpellDamageBonusTaken(GetCaster(), GetSpellInfo(), (uint)value, DamageEffectType.SpellDirect);
                SetHitDamage((int)value);
            }
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 47750 - Penance (Healing)
    class spell_pri_power_of_the_dark_side_healing_bonus : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerOfTheDarkSide);
        }

        void HandleLaunchTarget(uint effIndex)
        {
            AuraEffect powerOfTheDarkSide = GetCaster().GetAuraEffect(SpellIds.PowerOfTheDarkSide, 0);
            if (powerOfTheDarkSide != null)
            {
                PreventHitDefaultEffect(effIndex);

                float healingBonus = GetCaster().SpellHealingBonusDone(GetHitUnit(), GetSpellInfo(), (uint)GetEffectValue(), DamageEffectType.Heal, GetEffectInfo());
                float value = healingBonus + healingBonus * GetEffectVariance();
                value *= 1.0f + (powerOfTheDarkSide.GetAmount() / 100.0f);
                value = GetHitUnit().SpellHealingBonusTaken(GetCaster(), GetSpellInfo(), (uint)value, DamageEffectType.Heal);
                SetHitHeal((int)value);
            }
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.Heal));
        }
    }
    
    [Script] // 194509 - Power Word: Radiance
    class spell_pri_power_word_radiance : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Atonement, SpellIds.AtonementTriggered, SpellIds.Trinity) && spellInfo.GetEffects().Count > 3;
        }

        void OnTargetSelect(List<WorldObject> targets)
        {
            uint maxTargets = (uint)(GetEffectInfo(2).CalcValue(GetCaster()) + 1); // adding 1 for explicit target unit
            if (targets.Count > maxTargets)
            {
                Unit explTarget = GetExplTargetUnit();

                // Sort targets so units with no atonement are first, then units who are injured, then oher units
                // Make sure explicit target unit is first
                targets.Sort((lhs, rhs) =>
                {
                    if (lhs == explTarget) // explTarget > anything: always true
                        return 1;
                    if (rhs == explTarget) // anything > explTarget: always false
                        return -1;

                    return MakeSortTuple(lhs).Equals(MakeSortTuple(rhs)) ? 1 : -1;
                });

                targets.Resize(maxTargets);
            }
        }

        void HandleEffectHitTarget(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster.HasAura(SpellIds.Trinity))
                return;

            int durationPct = GetEffectInfo(3).CalcValue(caster);
            if (caster.HasAura(SpellIds.Atonement))
                caster.CastSpell(GetHitUnit(), SpellIds.AtonementTriggered, new CastSpellExtraArgs(SpellValueMod.DurationPct, durationPct).SetTriggerFlags(TriggerCastFlags.FullMask));
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(OnTargetSelect, 1, Targets.UnitDestAreaAlly));
            OnEffectHitTarget.Add(new EffectHandler(HandleEffectHitTarget, 1, SpellEffectName.Heal));
        }

        Tuple<bool, bool> MakeSortTuple(WorldObject obj)
        {
            return Tuple.Create(IsUnitWithNoAtonement(obj), IsUnitInjured(obj));
        }

        // Returns true if obj is a unit but has no atonement
        bool IsUnitWithNoAtonement(WorldObject obj)
        {
            Unit unit = obj.ToUnit();
            return unit != null && !unit.HasAura(SpellIds.AtonementTriggered, GetCaster().GetGUID());
        }

        // Returns true if obj is a unit and is injured
        static bool IsUnitInjured(WorldObject obj)
        {
            Unit unit = obj.ToUnit();
            return unit != null && unit.IsFullHealth();
        }
    }

    [Script] // 17 - Power Word: Shield
    class spell_pri_power_word_shield : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.WeakenedSoul);
        }

        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();
            if (target != null)
                if (!caster.HasAura(SpellIds.Rapture))
                    if (target.HasAura(SpellIds.WeakenedSoul, caster.GetGUID()))
                        return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        void HandleEffectHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target != null)
                if (!caster.HasAura(SpellIds.Rapture))
                    caster.CastSpell(target, SpellIds.WeakenedSoul, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
            AfterHit.Add(new HitHandler(HandleEffectHit));
        }
    }

    [Script] // 17 - Power Word: Shield Aura
    class spell_pri_power_word_shield_aura : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BodyAndSoul, SpellIds.BodyAndSoulSpeed, SpellIds.StrengthOfSoul, SpellIds.StrengthOfSoulEffect, SpellIds.RenewedHope, SpellIds.RenewedHopeEffect,
                SpellIds.VoidShield, SpellIds.VoidShieldEffect, SpellIds.Atonement, SpellIds.Trinity, SpellIds.AtonementTriggered, SpellIds.AtonementTriggeredPowerTrinity, SpellIds.ShieldDisciplinePassive,
                SpellIds.ShieldDisciplineEnergize, SpellIds.Rapture, SpellIds.MasteryGrace);
        }

        void CalculateAmount(AuraEffect auraEffect, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;

            Unit caster = GetCaster();
            if (caster != null)
            {
                float amountF = caster.SpellBaseDamageBonusDone(GetSpellInfo().GetSchoolMask()) * 1.65f;

                Player player = caster.ToPlayer();
                if (player != null)
                {
                    MathFunctions.AddPct(ref amountF, player.GetRatingBonusValue(CombatRating.VersatilityDamageDone));

                    AuraEffect mastery = caster.GetAuraEffect(SpellIds.MasteryGrace, 0);
                    if (mastery != null)
                        if (GetUnitOwner().HasAura(SpellIds.AtonementTriggered) || GetUnitOwner().HasAura(SpellIds.AtonementTriggeredPowerTrinity))
                            MathFunctions.AddPct(ref amountF, mastery.GetAmount());
                }

                AuraEffect rapture = caster.GetAuraEffect(SpellIds.Rapture, 1);
                if (rapture != null)
                    MathFunctions.AddPct(ref amountF, rapture.GetAmount());

                amount = (int)amountF;
            }
        }

        void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            Unit target = GetTarget();
            if (!caster)
                return;

            if (caster.HasAura(SpellIds.BodyAndSoul))
                caster.CastSpell(target, SpellIds.BodyAndSoulSpeed, true);
            if (caster.HasAura(SpellIds.StrengthOfSoul))
                caster.CastSpell(target, SpellIds.StrengthOfSoulEffect, true);
            if (caster.HasAura(SpellIds.RenewedHope))
                caster.CastSpell(target, SpellIds.RenewedHopeEffect, true);
            if (caster.HasAura(SpellIds.VoidShield) && caster == target)
                caster.CastSpell(target, SpellIds.VoidShieldEffect, true);
            if (caster.HasAura(SpellIds.Atonement))
                caster.CastSpell(target, caster.HasAura(SpellIds.Trinity) ? SpellIds.AtonementTriggeredPowerTrinity : SpellIds.AtonementTriggered, true);
        }

        void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.StrengthOfSoulEffect);
            Unit caster = GetCaster();
            if (caster)
                if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell && caster.HasAura(SpellIds.ShieldDisciplinePassive))
                    caster.CastSpell(caster, SpellIds.ShieldDisciplineEnergize, true);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            AfterEffectApply.Add(new EffectApplyHandler(HandleOnApply, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.RealOrReapplyMask));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 129250 - Power Word: Solace
    class spell_pri_power_word_solace : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerWordSolaceEnergize);
        }

        void RestoreMana(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.PowerWordSolaceEnergize,
                new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell())
                    .AddSpellMod(SpellValueMod.BasePoint0, GetEffectValue() / 100));
        }

        public override void Register()
        {
            OnEffectLaunch.Add(new EffectHandler(RestoreMana, 1, SpellEffectName.Dummy));
        }
    }
    
    [Script] // 33076 - Prayer of Mending
    class spell_pri_prayer_of_mending : SpellScript
    {
        SpellInfo _spellInfoHeal;
        SpellEffectInfo _healEffectDummy;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfMendingAura)
                && !Global.SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None).GetEffects().Empty();
        }

        public override bool Load()
        {
            _spellInfoHeal = Global.SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None);
            _healEffectDummy = _spellInfoHeal.GetEffect(0);
            return true;
        }

        void HandleEffectDummy(uint effIndex)
        {
            uint basePoints = GetCaster().SpellHealingBonusDone(GetHitUnit(), _spellInfoHeal, (uint)_healEffectDummy.CalcValue(GetCaster()), DamageEffectType.Heal, _healEffectDummy);
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.AuraStack, GetEffectValue());
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)basePoints);
            GetCaster().CastSpell(GetHitUnit(), SpellIds.PrayerOfMendingAura, args);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 41635 - Prayer of Mending (Aura) - SPELL_PRIEST_PRAYER_OF_MENDING_AURA
    class spell_pri_prayer_of_mending_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfMendingJump);
        }

        void HandleHeal(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            // Caster: player (priest) that cast the Prayer of Mending
            // Target: player that currently has Prayer of Mending aura on him
            Unit target = GetTarget();
            Unit caster = GetCaster();
            if (caster != null)
            {
                // Cast the spell to heal the owner
                caster.CastSpell(target, SpellIds.PrayerOfMendingHeal, new CastSpellExtraArgs(aurEff));

                // Only cast jump if stack is higher than 0
                int stackAmount = GetStackAmount();
                if (stackAmount > 1)
                {
                    CastSpellExtraArgs args = new(aurEff);
                    args.OriginalCaster = caster.GetGUID();
                    args.AddSpellMod(SpellValueMod.BasePoint0, stackAmount - 1);
                    target.CastSpell(target, SpellIds.PrayerOfMendingJump, args);
                }

                Remove();
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleHeal, 0, AuraType.Dummy));
        }
    }

    [Script] // 155793 - prayer of mending (Jump) - SPELL_PRIEST_PRAYER_OF_MENDING_JUMP
    class spell_pri_prayer_of_mending_jump : SpellScript
    {
        SpellInfo _spellInfoHeal;
        SpellEffectInfo _healEffectDummy;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfMendingAura)
                && Global.SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None).GetEffect(0) != null;
        }

        public override bool Load()
        {
            _spellInfoHeal = Global.SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None);
            _healEffectDummy = _spellInfoHeal.GetEffect(0);
            return true;
        }

        void OnTargetSelect(List<WorldObject> targets)
        {
            // Find the best target - prefer players over pets
            bool foundPlayer = false;
            foreach (WorldObject worldObject in targets)
            {
                if (worldObject.IsPlayer())
                {
                    foundPlayer = true;
                    break;
                }
            }

            if (foundPlayer)
                targets.RemoveAll(new ObjectTypeIdCheck(TypeId.Player, false));

            // choose one random target from targets
            if (targets.Count > 1)
            {
                WorldObject selected = targets.SelectRandom();
                targets.Clear();
                targets.Add(selected);
            }
        }

        void HandleJump(uint effIndex)
        {
            Unit origCaster = GetOriginalCaster(); // the one that started the prayer of mending chain
            Unit target = GetHitUnit(); // the target we decided the aura should jump to

            if (origCaster)
            {
                uint basePoints = origCaster.SpellHealingBonusDone(target, _spellInfoHeal, (uint)_healEffectDummy.CalcValue(origCaster), DamageEffectType.Heal, _healEffectDummy);
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.AuraStack, GetEffectValue());
                args.AddSpellMod(SpellValueMod.BasePoint0, (int)basePoints);
                origCaster.CastSpell(target, SpellIds.PrayerOfMendingAura, args);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(OnTargetSelect, 0, Targets.UnitSrcAreaAlly));
            OnEffectHitTarget.Add(new EffectHandler(HandleJump, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 47536 - Rapture
    class spell_pri_rapture : SpellScript
    {
        ObjectGuid _raptureTarget;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerWordShield);
        }

        void HandleEffectDummy(uint effIndex)
        {
            _raptureTarget = GetHitUnit().GetGUID();
        }

        void HandleAfterCast()
        {
            Unit caster = GetCaster();
            Unit target = Global.ObjAccessor.GetUnit(caster, _raptureTarget);
            if (target != null)
                caster.CastSpell(target, SpellIds.PowerWordShield, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnorePowerAndReagentCost | TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell()));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy));
            AfterCast.Add(new CastHandler(HandleAfterCast));
        }
    }

    [Script] // 280391 - Sins of the Many
    class spell_pri_sins_of_the_many : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SinsOfTheMany);
        }

        void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.SinsOfTheMany, true);
        }

        void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.SinsOfTheMany);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }
    
    [Script] // 20711 - Spirit of Redemption
    class spell_pri_spirit_of_redemption : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SpiritOfRedemption);
        }

        void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.SpiritOfRedemption, new CastSpellExtraArgs(aurEff));
            target.SetFullHealth();

            absorbAmount = dmgInfo.GetDamage();
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new EffectAbsorbHandler(HandleAbsorb, 0, true));
        }
    }

    [Script] // 186263 - Shadow Mend
    class spell_pri_shadow_mend : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Atonement, SpellIds.AtonementTriggered, SpellIds.Trinity, SpellIds.MasochismTalent, SpellIds.MasochismPeriodicHeal, SpellIds.ShadowMendPeriodicDummy);
        }

        void HandleEffectHit()
        {
            Unit target = GetHitUnit();
            if (target != null)
            {
                Unit caster = GetCaster();

                int periodicAmount = GetHitHeal() / 20;
                int damageForAuraRemoveAmount = periodicAmount * 10;
                if (caster.HasAura(SpellIds.Atonement) && !caster.HasAura(SpellIds.Trinity))
                    caster.CastSpell(target, SpellIds.AtonementTriggered, new CastSpellExtraArgs(GetSpell()));

                // Handle Masochism talent
                if (caster.HasAura(SpellIds.MasochismTalent) && caster.GetGUID() == target.GetGUID())
                    caster.CastSpell(caster, SpellIds.MasochismPeriodicHeal, new CastSpellExtraArgs(GetSpell()).AddSpellMod(SpellValueMod.BasePoint0, periodicAmount));
                else if (target.IsInCombat() && periodicAmount != 0)
                {
                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.SetTriggeringSpell(GetSpell());
                    args.AddSpellMod(SpellValueMod.BasePoint0, periodicAmount);
                    args.AddSpellMod(SpellValueMod.BasePoint1, damageForAuraRemoveAmount);
                    caster.CastSpell(target, SpellIds.ShadowMendPeriodicDummy, args);
                }
            }
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleEffectHit));
        }
    }

    [Script] // 187464 - Shadow Mend (Damage)
    class spell_pri_shadow_mend_periodic_damage : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShadowMendDamage);
        }

        void HandleDummyTick(AuraEffect aurEff)
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.SetOriginalCaster(GetCasterGUID());
            args.SetTriggeringAura(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount());
            GetTarget().CastSpell(GetTarget(), SpellIds.ShadowMendDamage, args);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo() != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            int newAmount = (int)(aurEff.GetAmount() - eventInfo.GetDamageInfo().GetDamage());

            aurEff.ChangeAmount(newAmount);
            if (newAmount < 0)
                Remove();
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }
    
    [Script] // 28809 - Greater Heal
    class spell_pri_t3_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ArmorOfFaith);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.ArmorOfFaith, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 37594 - Greater Heal Refund
    class spell_pri_t5_heal_2p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemEfficiency);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo != null)
            {
                Unit healTarget = healInfo.GetTarget();
                if (healTarget)
                    // @todo: fix me later if (healInfo.GetEffectiveHeal())
                    if (healTarget.GetHealth() >= healTarget.GetMaxHealth())
                        return true;
            }

            return false;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.ItemEfficiency, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 70770 - Item - Priest T10 Healer 2P Bonus
    class spell_pri_t10_heal_2p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessedHealing);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.BlessedHealing, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.BlessedHealing, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 109142 - Twist of Fate (Shadow)
    [Script] // 265259 - Twist of Fate (Discipline)
    class spell_pri_twist_of_fate : AuraScript
    {
        bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget().GetHealthPct() < aurEff.GetAmount();
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new CheckEffectProcHandler(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }
    
    [Script] // 15286 - Vampiric Embrace
    class spell_pri_vampiric_embrace : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VampiricEmbraceHeal);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            // Not proc from Mind Sear
            return !eventInfo.GetDamageInfo().GetSpellInfo().SpellFamilyFlags[1].HasAnyFlag(0x80000u);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            int selfHeal = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            int teamHeal = selfHeal / 2;

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, teamHeal);
            args.AddSpellMod(SpellValueMod.BasePoint1, selfHeal);
            GetTarget().CastSpell((Unit)null, SpellIds.VampiricEmbraceHeal, args);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 15290 - Vampiric Embrace (heal)
    class spell_pri_vampiric_embrace_target : SpellScript
    {
        void FilterTargets(List<WorldObject> unitList)
        {
            unitList.Remove(GetCaster());
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitCasterAreaParty));
        }
    }

    [Script] // 34914 - Vampiric Touch
    class spell_pri_vampiric_touch : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VampiricTouchDispel, SpellIds.GenReplenishment);
        }

        void HandleDispel(DispelInfo dispelInfo)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                Unit target = GetUnitOwner();
                if (target)
                {
                    AuraEffect aurEff = GetEffect(1);
                    if (aurEff != null)
                    {
                        // backfire damage
                        CastSpellExtraArgs args = new(aurEff);
                        args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount() * 8);
                        caster.CastSpell(target, SpellIds.VampiricTouchDispel, args);
                    }
                }
            }
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget() == GetCaster();
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetProcTarget().CastSpell((Unit)null, SpellIds.GenReplenishment, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            AfterDispel.Add(new AuraDispelHandler(HandleDispel));
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 2, AuraType.Dummy));
        }
    }
}