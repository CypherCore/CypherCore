// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Maps;

using Game.Movement;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells;

namespace Scripts.Spells.Priest
{
    internal struct SpellIds
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
    internal class spell_pri_angelic_feather_trigger : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AngelicFeatherAreatrigger);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        private void HandleEffectDummy(uint effIndex)
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
    }

    [Script] // Angelic Feather areatrigger - created by SPELL_PRIEST_ANGELIC_FEATHER_AREATRIGGER
    internal class areatrigger_pri_angelic_feather : AreaTriggerAI
    {
        public areatrigger_pri_angelic_feather(AreaTrigger areatrigger) : base(areatrigger)
        {
        }

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
                if (caster.IsFriendlyTo(unit))
                {
                    // If Target already has aura, increase duration to max 130% of initial duration
                    caster.CastSpell(unit, SpellIds.AngelicFeatherAura, true);
                    at.SetDuration(0);
                }
        }
    }

    [Script] // 26169 - Oracle Healing Bonus
    internal class spell_pri_aq_3p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.OracularHeal);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();

            if (caster == eventInfo.GetProcTarget())
                return;

            HealInfo healInfo = eventInfo.GetHealInfo();

            if (healInfo == null ||
                healInfo.GetHeal() == 0)
                return;

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(healInfo.GetHeal(), 10));
            caster.CastSpell(caster, SpellIds.OracularHeal, args);
        }
    }

    [Script] // 81749 - Atonement
    public class spell_pri_atonement : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        private readonly List<ObjectGuid> _appliedAtonements = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AtonementHeal, SpellIds.SinsOfTheMany) && spellInfo.GetEffects().Count > 1 && Global.SpellMgr.GetSpellInfo(SpellIds.SinsOfTheMany, Difficulty.None).GetEffects().Count > 2;
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo() != null;
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

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

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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

        private void UpdateSinsOfTheManyValue()
        {
            float[] damageByStack =
            {
                12.0f, 12.0f, 10.0f, 8.0f, 7.0f, 6.0f, 5.0f, 5.0f, 4.0f, 4.0f, 3.0f
            };

            foreach (uint effectIndex in new[]
                                         {
                                             0, 1, 2
                                         })
            {
                AuraEffect sinOfTheMany = GetUnitOwner().GetAuraEffect(SpellIds.SinsOfTheMany, effectIndex);

                sinOfTheMany?.ChangeAmount((int)damageByStack[Math.Min(_appliedAtonements.Count, damageByStack.Length - 1)]);
            }
        }
    }

    [Script] // 194384, 214206 - Atonement
    internal class spell_pri_atonement_triggered : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Atonement);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            AuraEffects.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            if (caster)
            {
                Aura atonement = caster.GetAura(SpellIds.Atonement);

                if (atonement != null)
                {
                    var script = atonement.GetScript<spell_pri_atonement>();

                    script?.AddAtonementTarget(GetTarget().GetGUID());
                }
            }
        }

        private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            if (caster)
            {
                Aura atonement = caster.GetAura(SpellIds.Atonement);

                if (atonement != null)
                {
                    var script = atonement.GetScript<spell_pri_atonement>();

                    script?.RemoveAtonementTarget(GetTarget().GetGUID());
                }
            }
        }
    }

    [Script] // 64844 - Divine Hymn
    internal class spell_pri_divine_hymn : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, SpellConst.EffectAll, Targets.UnitSrcAreaAlly));
        }

        private void FilterTargets(List<WorldObject> targets)
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
    }

    [Script] // 110744 - Divine Star
    internal class areatrigger_pri_divine_star : AreaTriggerAI
    {
        private readonly List<ObjectGuid> _affectedUnits = new();
        private readonly TaskScheduler _scheduler = new();
        private Position _casterCurrentPosition = new();

        public areatrigger_pri_divine_star(AreaTrigger areatrigger) : base(areatrigger)
        {
        }

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
                if (!_affectedUnits.Contains(unit.GetGUID()))
                {
                    if (caster.IsValidAttackTarget(unit))
                        caster.CastSpell(unit, SpellIds.DivineStarDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
                    else if (caster.IsValidAssistTarget(unit))
                        caster.CastSpell(unit, SpellIds.DivineStarHeal, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));

                    _affectedUnits.Add(unit.GetGUID());
                }
        }

        public override void OnUnitExit(Unit unit)
        {
            // Note: this ensures any unit receives a second hit if they happen to be inside the AT when Divine Star starts its return path.
            Unit caster = at.GetCaster();

            if (caster != null)
                if (!_affectedUnits.Contains(unit.GetGUID()))
                {
                    if (caster.IsValidAttackTarget(unit))
                        caster.CastSpell(unit, SpellIds.DivineStarDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
                    else if (caster.IsValidAssistTarget(unit))
                        caster.CastSpell(unit, SpellIds.DivineStarHeal, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));

                    _affectedUnits.Add(unit.GetGUID());
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
            {
                at.Remove();
            }
        }

        private void ReturnToCaster()
        {
            _scheduler.Schedule(TimeSpan.FromMilliseconds(0),
                                task =>
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
    internal class spell_pri_guardian_spirit : AuraScript, IHasAuraEffects
    {
        private uint healPct;
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GuardianSpiritHeal) && spellInfo.GetEffects().Count > 1;
        }

        public override bool Load()
        {
            healPct = (uint)GetEffectInfo(1).CalcValue();

            return true;
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
            AuraEffects.Add(new EffectAbsorbHandler(Absorb, 1, false, AuraScriptHookType.EffectAbsorb));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            // Set absorbtion amount to unlimited
            amount = -1;
        }

        private void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
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
    }

    [Script] // 120517 - Halo
    internal class areatrigger_pri_halo : AreaTriggerAI
    {
        public areatrigger_pri_halo(AreaTrigger areatrigger) : base(areatrigger)
        {
        }

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
    internal class spell_pri_holy_words : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Heal, SpellIds.FlashHeal, SpellIds.PrayerOfHealing, SpellIds.Renew, SpellIds.Smite, SpellIds.HolyWordChastise, SpellIds.HolyWordSanctify, SpellIds.HolyWordSerenity) && Global.SpellMgr.GetSpellInfo(SpellIds.HolyWordSerenity, Difficulty.None).GetEffects().Count > 1 && Global.SpellMgr.GetSpellInfo(SpellIds.HolyWordSanctify, Difficulty.None).GetEffects().Count > 3 && Global.SpellMgr.GetSpellInfo(SpellIds.HolyWordChastise, Difficulty.None).GetEffects().Count > 1;
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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
    }

    [Script] // 40438 - Priest Tier 6 Trinket
    internal class spell_pri_item_t6_trinket : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DivineBlessing, SpellIds.DivineWrath);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();

            if (eventInfo.GetSpellTypeMask().HasAnyFlag(ProcFlagsSpellType.Heal))
                caster.CastSpell((Unit)null, SpellIds.DivineBlessing, true);

            if (eventInfo.GetSpellTypeMask().HasAnyFlag(ProcFlagsSpellType.Damage))
                caster.CastSpell((Unit)null, SpellIds.DivineWrath, true);
        }
    }

    [Script] // 92833 - Leap of Faith
    internal class spell_pri_leap_of_faith_effect_trigger : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LeapOfFaithEffect);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleEffectDummy(uint effIndex)
        {
            Position destPos = GetHitDest().GetPosition();

            SpellCastTargets targets = new();
            targets.SetDst(destPos);
            targets.SetUnitTarget(GetCaster());
            GetHitUnit().CastSpell(targets, (uint)GetEffectValue(), new CastSpellExtraArgs(GetCastDifficulty()));
        }
    }

    [Script] // 1706 - Levitate
    internal class spell_pri_levitate : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LevitateEffect);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.LevitateEffect, true);
        }
    }

    [Script] // 205369 - Mind Bomb
    internal class spell_pri_mind_bomb : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MindBombStun);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(RemoveEffect, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Death ||
                GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
                GetCaster()?.CastSpell(GetTarget().GetPosition(), SpellIds.MindBombStun, new CastSpellExtraArgs(true));
        }
    }

    [Script] // 47540 - Penance
    internal class spell_pri_penance : SpellScript, ISpellCheckCastHander, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PenanceChannelDamage, SpellIds.PenanceChannelHealing);
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();

            if (target)
                if (!caster.IsFriendlyTo(target))
                {
                    if (!caster.IsValidAttackTarget(target))
                        return SpellCastResult.BadTargets;

                    if (!caster.IsInFront(target))
                        return SpellCastResult.NotInfront;
                }

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint effIndex)
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
    }

    [Script] // 47758 - Penance (Channel Damage), 47757 - Penance (Channel Healing)
    internal class spell_pri_penance_channeled : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerOfTheDarkSide);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            caster?.RemoveAura(SpellIds.PowerOfTheDarkSide);
        }
    }

    [Script] // 198069 - Power of the Dark Side
    internal class spell_pri_power_of_the_dark_side : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerOfTheDarkSideTint);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            AuraEffects.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            caster?.CastSpell(caster, SpellIds.PowerOfTheDarkSideTint, true);
        }

        private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            caster?.RemoveAura(SpellIds.PowerOfTheDarkSideTint);
        }
    }

    [Script] // 47666 - Penance (Damage)
    internal class spell_pri_power_of_the_dark_side_damage_bonus : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerOfTheDarkSide);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.LaunchTarget));
        }

        private void HandleLaunchTarget(uint effIndex)
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
    }

    [Script] // 47750 - Penance (Healing)
    internal class spell_pri_power_of_the_dark_side_healing_bonus : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerOfTheDarkSide);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.Heal, SpellScriptHookType.LaunchTarget));
        }

        private void HandleLaunchTarget(uint effIndex)
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
    }

    [Script] // 194509 - Power Word: Radiance
    internal class spell_pri_power_word_radiance : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Atonement, SpellIds.AtonementTriggered, SpellIds.Trinity) && spellInfo.GetEffects().Count > 3;
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(OnTargetSelect, 1, Targets.UnitDestAreaAlly));
            SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 1, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
        }

        private void OnTargetSelect(List<WorldObject> targets)
        {
            uint maxTargets = (uint)(GetEffectInfo(2).CalcValue(GetCaster()) + 1); // adding 1 for explicit Target unit

            if (targets.Count > maxTargets)
            {
                Unit explTarget = GetExplTargetUnit();

                // Sort targets so units with no atonement are first, then units who are injured, then oher units
                // Make sure explicit Target unit is first
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

        private void HandleEffectHitTarget(uint effIndex)
        {
            Unit caster = GetCaster();

            if (caster.HasAura(SpellIds.Trinity))
                return;

            int durationPct = GetEffectInfo(3).CalcValue(caster);

            if (caster.HasAura(SpellIds.Atonement))
                caster.CastSpell(GetHitUnit(), SpellIds.AtonementTriggered, new CastSpellExtraArgs(SpellValueMod.DurationPct, durationPct).SetTriggerFlags(TriggerCastFlags.FullMask));
        }

        private Tuple<bool, bool> MakeSortTuple(WorldObject obj)
        {
            return Tuple.Create(IsUnitWithNoAtonement(obj), IsUnitInjured(obj));
        }

        // Returns true if obj is a unit but has no atonement
        private bool IsUnitWithNoAtonement(WorldObject obj)
        {
            Unit unit = obj.ToUnit();

            return unit != null && !unit.HasAura(SpellIds.AtonementTriggered, GetCaster().GetGUID());
        }

        // Returns true if obj is a unit and is injured
        private static bool IsUnitInjured(WorldObject obj)
        {
            Unit unit = obj.ToUnit();

            return unit != null && unit.IsFullHealth();
        }
    }

    [Script] // 17 - Power Word: Shield
    internal class spell_pri_power_word_shield : SpellScript, ISpellCheckCastHander, ISpellAfterHit
    {
        public void AfterHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            if (target != null)
                if (!caster.HasAura(SpellIds.Rapture))
                    caster.CastSpell(target, SpellIds.WeakenedSoul, true);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.WeakenedSoul);
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();

            if (target != null)
                if (!caster.HasAura(SpellIds.Rapture))
                    if (target.HasAura(SpellIds.WeakenedSoul, caster.GetGUID()))
                        return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }
    }

    [Script] // 17 - Power Word: Shield Aura
    internal class spell_pri_power_word_shield_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BodyAndSoul,
                                     SpellIds.BodyAndSoulSpeed,
                                     SpellIds.StrengthOfSoul,
                                     SpellIds.StrengthOfSoulEffect,
                                     SpellIds.RenewedHope,
                                     SpellIds.RenewedHopeEffect,
                                     SpellIds.VoidShield,
                                     SpellIds.VoidShieldEffect,
                                     SpellIds.Atonement,
                                     SpellIds.Trinity,
                                     SpellIds.AtonementTriggered,
                                     SpellIds.AtonementTriggeredPowerTrinity,
                                     SpellIds.ShieldDisciplinePassive,
                                     SpellIds.ShieldDisciplineEnergize,
                                     SpellIds.Rapture,
                                     SpellIds.MasteryGrace);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            AuraEffects.Add(new EffectApplyHandler(HandleOnApply, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
            AuraEffects.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void CalculateAmount(AuraEffect auraEffect, ref int amount, ref bool canBeRecalculated)
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
                        if (GetUnitOwner().HasAura(SpellIds.AtonementTriggered) ||
                            GetUnitOwner().HasAura(SpellIds.AtonementTriggeredPowerTrinity))
                            MathFunctions.AddPct(ref amountF, mastery.GetAmount());
                }

                AuraEffect rapture = caster.GetAuraEffect(SpellIds.Rapture, 1);

                if (rapture != null)
                    MathFunctions.AddPct(ref amountF, rapture.GetAmount());

                amount = (int)amountF;
            }
        }

        private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
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

            if (caster.HasAura(SpellIds.VoidShield) &&
                caster == target)
                caster.CastSpell(target, SpellIds.VoidShieldEffect, true);

            if (caster.HasAura(SpellIds.Atonement))
                caster.CastSpell(target, caster.HasAura(SpellIds.Trinity) ? SpellIds.AtonementTriggeredPowerTrinity : SpellIds.AtonementTriggered, true);
        }

        private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.StrengthOfSoulEffect);
            Unit caster = GetCaster();

            if (caster)
                if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell &&
                    caster.HasAura(SpellIds.ShieldDisciplinePassive))
                    caster.CastSpell(caster, SpellIds.ShieldDisciplineEnergize, true);
        }
    }

    [Script] // 129250 - Power Word: Solace
    internal class spell_pri_power_word_solace : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerWordSolaceEnergize);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(RestoreMana, 1, SpellEffectName.Dummy, SpellScriptHookType.Launch));
        }

        private void RestoreMana(uint effIndex)
        {
            GetCaster()
                .CastSpell(GetCaster(),
                           SpellIds.PowerWordSolaceEnergize,
                           new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell())
                                                                                        .AddSpellMod(SpellValueMod.BasePoint0, GetEffectValue() / 100));
        }
    }

    [Script] // 33076 - Prayer of Mending
    internal class spell_pri_prayer_of_mending : SpellScript, IHasSpellEffects
    {
        private SpellEffectInfo _healEffectDummy;
        private SpellInfo _spellInfoHeal;
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfMendingAura) && !Global.SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None).GetEffects().Empty();
        }

        public override bool Load()
        {
            _spellInfoHeal = Global.SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None);
            _healEffectDummy = _spellInfoHeal.GetEffect(0);

            return true;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleEffectDummy(uint effIndex)
        {
            uint basePoints = GetCaster().SpellHealingBonusDone(GetHitUnit(), _spellInfoHeal, (uint)_healEffectDummy.CalcValue(GetCaster()), DamageEffectType.Heal, _healEffectDummy);
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.AuraStack, GetEffectValue());
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)basePoints);
            GetCaster().CastSpell(GetHitUnit(), SpellIds.PrayerOfMendingAura, args);
        }
    }

    [Script] // 41635 - Prayer of Mending (Aura) - SPELL_PRIEST_PRAYER_OF_MENDING_AURA
    internal class spell_pri_prayer_of_mending_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfMendingJump);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleHeal, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleHeal(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            // Caster: player (priest) that cast the Prayer of Mending
            // Target: player that currently has Prayer of Mending aura on him
            Unit target = GetTarget();
            Unit caster = GetCaster();

            if (caster != null)
            {
                // Cast the spell to heal the owner
                caster.CastSpell(target, SpellIds.PrayerOfMendingHeal, new CastSpellExtraArgs(aurEff));

                // Only cast Jump if stack is higher than 0
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
    }

    [Script] // 155793 - prayer of mending (Jump) - SPELL_PRIEST_PRAYER_OF_MENDING_JUMP
    internal class spell_pri_prayer_of_mending_jump : SpellScript, IHasSpellEffects
    {
        private SpellEffectInfo _healEffectDummy;
        private SpellInfo _spellInfoHeal;
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfMendingAura) && Global.SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None).GetEffect(0) != null;
        }

        public override bool Load()
        {
            _spellInfoHeal = Global.SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None);
            _healEffectDummy = _spellInfoHeal.GetEffect(0);

            return true;
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(OnTargetSelect, 0, Targets.UnitSrcAreaAlly));
            SpellEffects.Add(new EffectHandler(HandleJump, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void OnTargetSelect(List<WorldObject> targets)
        {
            // Find the best Target - prefer players over pets
            bool foundPlayer = false;

            foreach (WorldObject worldObject in targets)
                if (worldObject.IsPlayer())
                {
                    foundPlayer = true;

                    break;
                }

            if (foundPlayer)
                targets.RemoveAll(new ObjectTypeIdCheck(TypeId.Player, false));

            // choose one random Target from targets
            if (targets.Count > 1)
            {
                WorldObject selected = targets.SelectRandom();
                targets.Clear();
                targets.Add(selected);
            }
        }

        private void HandleJump(uint effIndex)
        {
            Unit origCaster = GetOriginalCaster(); // the one that started the prayer of mending chain
            Unit target = GetHitUnit();        // the Target we decided the aura should Jump to

            if (origCaster)
            {
                uint basePoints = origCaster.SpellHealingBonusDone(target, _spellInfoHeal, (uint)_healEffectDummy.CalcValue(origCaster), DamageEffectType.Heal, _healEffectDummy);
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.AuraStack, GetEffectValue());
                args.AddSpellMod(SpellValueMod.BasePoint0, (int)basePoints);
                origCaster.CastSpell(target, SpellIds.PrayerOfMendingAura, args);
            }
        }
    }

    [Script] // 47536 - Rapture
    internal class spell_pri_rapture : SpellScript, ISpellAfterCast, IHasSpellEffects
    {
        private ObjectGuid _raptureTarget;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerWordShield);
        }

        public void AfterCast()
        {
            Unit caster = GetCaster();
            Unit target = Global.ObjAccessor.GetUnit(caster, _raptureTarget);

            if (target != null)
                caster.CastSpell(target, SpellIds.PowerWordShield, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnorePowerAndReagentCost | TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell()));
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleEffectDummy(uint effIndex)
        {
            _raptureTarget = GetHitUnit().GetGUID();
        }
    }

    [Script] // 280391 - Sins of the Many
    internal class spell_pri_sins_of_the_many : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SinsOfTheMany);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            AuraEffects.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.SinsOfTheMany, true);
        }

        private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.SinsOfTheMany);
        }
    }

    [Script] // 20711 - Spirit of Redemption
    internal class spell_pri_spirit_of_redemption : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SpiritOfRedemption);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectAbsorbHandler(HandleAbsorb, 0, true, AuraScriptHookType.EffectAbsorb));
        }

        private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.SpiritOfRedemption, new CastSpellExtraArgs(aurEff));
            target.SetFullHealth();

            absorbAmount = dmgInfo.GetDamage();
        }
    }

    [Script] // 186263 - Shadow Mend
    internal class spell_pri_shadow_mend : SpellScript, ISpellAfterHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Atonement, SpellIds.AtonementTriggered, SpellIds.Trinity, SpellIds.MasochismTalent, SpellIds.MasochismPeriodicHeal, SpellIds.ShadowMendPeriodicDummy);
        }

        public void AfterHit()
        {
            Unit target = GetHitUnit();

            if (target != null)
            {
                Unit caster = GetCaster();

                int periodicAmount = GetHitHeal() / 20;
                int damageForAuraRemoveAmount = periodicAmount * 10;

                if (caster.HasAura(SpellIds.Atonement) &&
                    !caster.HasAura(SpellIds.Trinity))
                    caster.CastSpell(target, SpellIds.AtonementTriggered, new CastSpellExtraArgs(GetSpell()));

                // Handle Masochism talent
                if (caster.HasAura(SpellIds.MasochismTalent) &&
                    caster.GetGUID() == target.GetGUID())
                {
                    caster.CastSpell(caster, SpellIds.MasochismPeriodicHeal, new CastSpellExtraArgs(GetSpell()).AddSpellMod(SpellValueMod.BasePoint0, periodicAmount));
                }
                else if (target.IsInCombat() &&
                         periodicAmount != 0)
                {
                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.SetTriggeringSpell(GetSpell());
                    args.AddSpellMod(SpellValueMod.BasePoint0, periodicAmount);
                    args.AddSpellMod(SpellValueMod.BasePoint1, damageForAuraRemoveAmount);
                    caster.CastSpell(target, SpellIds.ShadowMendPeriodicDummy, args);
                }
            }
        }
    }

    [Script] // 187464 - Shadow Mend (Damage)
    internal class spell_pri_shadow_mend_periodic_damage : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShadowMendDamage);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo() != null;
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
            AuraEffects.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleDummyTick(AuraEffect aurEff)
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.SetOriginalCaster(GetCasterGUID());
            args.SetTriggeringAura(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount());
            GetTarget().CastSpell(GetTarget(), SpellIds.ShadowMendDamage, args);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            int newAmount = (int)(aurEff.GetAmount() - eventInfo.GetDamageInfo().GetDamage());

            aurEff.ChangeAmount(newAmount);

            if (newAmount < 0)
                Remove();
        }
    }

    [Script] // 28809 - Greater Heal
    internal class spell_pri_t3_4p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ArmorOfFaith);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.ArmorOfFaith, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 37594 - Greater Heal Refund
    internal class spell_pri_t5_heal_2p_bonus : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemEfficiency);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
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

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.ItemEfficiency, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 70770 - Item - Priest T10 Healer 2P Bonus
    internal class spell_pri_t10_heal_2p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessedHealing);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();

            if (healInfo == null ||
                healInfo.GetHeal() == 0)
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
    }

    // 109142 - Twist of Fate (Shadow)
    [Script] // 265259 - Twist of Fate (Discipline)
    internal class spell_pri_twist_of_fate : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new CheckEffectProcHandler(CheckProc, 0, AuraType.ProcTriggerSpell));
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget().GetHealthPct() < aurEff.GetAmount();
        }
    }

    [Script] // 15286 - Vampiric Embrace
    internal class spell_pri_vampiric_embrace : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VampiricEmbraceHeal);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            // Not proc from Mind Sear
            return !eventInfo.GetDamageInfo().GetSpellInfo().SpellFamilyFlags[1].HasAnyFlag(0x80000u);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo == null ||
                damageInfo.GetDamage() == 0)
                return;

            int selfHeal = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            int teamHeal = selfHeal / 2;

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, teamHeal);
            args.AddSpellMod(SpellValueMod.BasePoint1, selfHeal);
            GetTarget().CastSpell((Unit)null, SpellIds.VampiricEmbraceHeal, args);
        }
    }

    [Script] // 15290 - Vampiric Embrace (heal)
    internal class spell_pri_vampiric_embrace_target : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitCasterAreaParty));
        }

        private void FilterTargets(List<WorldObject> unitList)
        {
            unitList.Remove(GetCaster());
        }
    }

    [Script] // 34914 - Vampiric Touch
    internal class spell_pri_vampiric_touch : AuraScript, IAfterAuraDispel, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VampiricTouchDispel, SpellIds.GenReplenishment);
        }

        public void HandleDispel(DispelInfo dispelInfo)
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
                        // backfire Damage
                        CastSpellExtraArgs args = new(aurEff);
                        args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount() * 8);
                        caster.CastSpell(target, SpellIds.VampiricTouchDispel, args);
                    }
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleEffectProc, 2, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget() == GetCaster();
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetProcTarget().CastSpell((Unit)null, SpellIds.GenReplenishment, new CastSpellExtraArgs(aurEff));
        }
    }
}