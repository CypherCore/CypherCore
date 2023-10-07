// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Global;

namespace Scripts.Spells.Priest
{

    struct SpellIds
    {
        public const uint AbyssalReverie = 373054;
        public const uint AngelicFeatherAreatrigger = 158624;
        public const uint AngelicFeatherAura = 121557;
        public const uint AnsweredPrayers = 394289;
        public const uint Apotheosis = 200183;
        public const uint ArmorOfFaith = 28810;
        public const uint Atonement = 81749;
        public const uint AtonementEffect = 194384;
        public const uint AtonementHeal = 81751;
        public const uint Benediction = 193157;
        public const uint Benevolence = 415416;
        public const uint BlessedHealing = 70772;
        public const uint BlessedLight = 196813;
        public const uint BodyAndSoul = 64129;
        public const uint BodyAndSoulSpeed = 65081;
        public const uint CircleOfHealing = 204883;
        public const uint DarkReprimand = 400169;
        public const uint DarkReprimandChannelDamage = 373129;
        public const uint DarkReprimandChannelHealing = 400171;
        public const uint DarkReprimandDamage = 373130;
        public const uint DarkReprimandHealing = 400187;
        public const uint DazzlingLight = 196810;
        public const uint DivineBlessing = 40440;
        public const uint DivineHymnHeal = 64844;
        public const uint DivineImageSummon = 392990;
        public const uint DivineImageEmpower = 409387;
        public const uint DivineImageEmpowerStack = 405963;
        public const uint DivineService = 391233;
        public const uint DivineStarHoly = 110744;
        public const uint DivineStarShadow = 122121;
        public const uint DivineStarHolyDamage = 122128;
        public const uint DivineStarHolyHeal = 110745;
        public const uint DivineStarShadowDamage = 390845;
        public const uint DivineStarShadowHeal = 390981;
        public const uint DivineWrath = 40441;
        public const uint EmpoweredRenewHeal = 391359;
        public const uint Epiphany = 414553;
        public const uint EpiphanyHighlight = 414556;
        public const uint EssenceDevourer = 415479;
        public const uint EssenceDevourerShadowfiendHeal = 415673;
        public const uint EssenceDevourerMindbenderHeal = 415676;
        public const uint FlashHeal = 2061;
        public const uint GreaterHeal = 289666;
        public const uint FocusedMending = 372354;
        public const uint GuardianSpiritHeal = 48153;
        public const uint HaloHoly = 120517;
        public const uint HaloShadow = 120644;
        public const uint HaloHolyDamage = 120696;
        public const uint HaloHolyHeal = 120692;
        public const uint HaloShadowDamage = 390964;
        public const uint HaloShadowHeal = 390971;
        public const uint Heal = 2060;
        public const uint HealingLight = 196809;
        public const uint HolyFire = 14914;
        public const uint HolyMendingHeal = 391156;
        public const uint HolyNova = 132157;
        public const uint HolyWordChastise = 88625;
        public const uint HolyWordSalvation = 265202;
        public const uint HolyWordSanctify = 34861;
        public const uint HolyWordSerenity = 2050;
        public const uint Holy101ClassSet2PChooser = 411097;
        public const uint Holy101ClassSet4P = 405556;
        public const uint Holy101ClassSet4PEffect = 409479;
        public const uint ItemEfficiency = 37595;
        public const uint LeapOfFaithEffect = 92832;
        public const uint LevitateEffect = 111759;
        public const uint LightEruption = 196812;
        public const uint LightsWrathVisual = 215795;
        public const uint MasochismTalent = 193063;
        public const uint MasochismPeriodicHeal = 193065;
        public const uint MasteryGrace = 271534;
        public const uint MindbenderDisc = 123040;
        public const uint MindbenderShadow = 200174;
        public const uint Mindgames = 375901;
        public const uint MindgamesVenthyr = 323673;
        public const uint MindBombStun = 226943;
        public const uint OracularHeal = 26170;
        public const uint Penance = 47540;
        public const uint PenanceChannelDamage = 47758;
        public const uint PenanceChannelHealing = 47757;
        public const uint PenanceDamage = 47666;
        public const uint PenanceHealing = 47750;
        public const uint PowerLeechMindbenderMana = 123051;
        public const uint PowerLeechMindbenderInsanity = 200010;
        public const uint PowerLeechShadowfiendMana = 343727;
        public const uint PowerLeechShadowfiendInsanity = 262485;
        public const uint PowerOfTheDarkSide = 198069;
        public const uint PowerOfTheDarkSideTint = 225795;
        public const uint PowerWordLife = 373481;
        public const uint PowerWordRadiance = 194509;
        public const uint PowerWordShield = 17;
        public const uint PowerWordSolaceEnergize = 129253;
        public const uint PrayerOfHealing = 596;
        public const uint PrayerOfMending = 33076;
        public const uint PrayerOfMendingAura = 41635;
        public const uint PrayerOfMendingHeal = 33110;
        public const uint PrayerOfMendingJump = 155793;
        public const uint PurgeTheWicked = 204197;
        public const uint PurgeTheWickedDummy = 204215;
        public const uint PurgeTheWickedPeriodic = 204213;
        public const uint Rapture = 47536;
        public const uint Renew = 139;
        public const uint RenewedHope = 197469;
        public const uint RenewedHopeEffect = 197470;
        public const uint RevelInPurity = 373003;
        public const uint SayYourPrayers = 391186;
        public const uint SearingLight = 196811;
        public const uint ShadowMendDamage = 186439;
        public const uint ShadowWordDeath = 32379;
        public const uint ShadowMendPeriodicDummy = 187464;
        public const uint ShadowWordPain = 589;
        public const uint ShieldDiscipline = 197045;
        public const uint ShieldDisciplineEffect = 47755;
        public const uint SinsOfTheMany = 280398;
        public const uint Smite = 585;
        public const uint SpiritOfRedemption = 27827;
        public const uint StrengthOfSoul = 197535;
        public const uint StrengthOfSoulEffect = 197548;
        public const uint TranquilLight = 196816;
        public const uint ThePenitentAura = 200347;
        public const uint TrailOfLightHeal = 234946;
        public const uint Trinity = 214205;
        public const uint TrinityEffect = 214206;
        public const uint VapiricEmbraceHeal = 15290;
        public const uint VapiricTouchDispel = 64085;
        public const uint VoidShield = 199144;
        public const uint VoidShieldEffect = 199145;
        public const uint WeakenedSoul = 6788;

        public const uint PvpRulesEnabledHardcoded = 134735;
        public const uint VisualPriestPowerWordRadiance = 52872;
        public const uint VisualPriestPrayerOfMending = 38945;
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
            OnEffectHit.Add(new(HandleEffectDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // Angelic Feather areatrigger - created by SpellIds.AngelicFeatherAreatrigger
    class areatrigger_pri_angelic_feather : AreaTriggerAI
    {
        public areatrigger_pri_angelic_feather(AreaTrigger areatrigger) : base(areatrigger) { }

        // Called when the AreaTrigger has just been initialized, just before added to map
        public override void OnInitialize()
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                var areaTriggers = caster.GetAreaTriggers(SpellIds.AngelicFeatherAreatrigger);

                if (areaTriggers.Count >= 3)
                    areaTriggers.FirstOrDefault().SetDuration(0);
            }
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
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

    [Script] // 391387 - Answered Prayers
    class spell_pri_answered_prayers : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AnsweredPrayers, SpellIds.Apotheosis)
                && ValidateSpellEffect((spellInfo.Id, 1));
        }

        void HandleOnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            TimeSpan extraDuration = TimeSpan.FromMilliseconds(0);
            AuraEffect durationEffect = GetEffect(1);
            if (durationEffect != null)
                extraDuration = TimeSpan.FromSeconds(durationEffect.GetAmount());

            Unit target = eventInfo.GetActor();

            Aura answeredPrayers = target.GetAura(SpellIds.AnsweredPrayers);

            // Note: if caster has no aura, we must cast it first.
            if (answeredPrayers == null)
                target.CastSpell(target, SpellIds.AnsweredPrayers, TriggerCastFlags.IgnoreCastInProgress);
            else
            {
                // Note: there's no BaseValue dummy that we can use as reference, so we hardcode the increasing stack value.
                answeredPrayers.ModStackAmount(1);

                // Note: if current stacks match max. stacks, trigger Apotheosis.
                if (answeredPrayers.GetStackAmount() != aurEff.GetAmount())
                    return;

                answeredPrayers.Remove();

                Aura apotheosis = GetTarget().GetAura(SpellIds.Apotheosis);
                if (apotheosis != null)
                {
                    apotheosis.SetDuration((int)(apotheosis.GetDuration() + extraDuration.TotalMilliseconds));
                    apotheosis.SetMaxDuration((int)(apotheosis.GetMaxDuration() + extraDuration.TotalMilliseconds));
                }
                else
                    target.CastSpell(target, SpellIds.Apotheosis,
                        new CastSpellExtraArgs(TriggerCastFlags.FullMask & ~TriggerCastFlags.CastDirectly)
                        .AddSpellMod(SpellValueMod.Duration, (int)extraDuration.TotalMilliseconds));
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleOnProc, 0, AuraType.AddFlatModifierBySpellLabel));
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
            args.AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct((int)(healInfo.GetHeal()), 10));
            caster.CastSpell(caster, SpellIds.OracularHeal, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 81749 - Atonement
    class spell_pri_atonement : AuraScript
    {
        List<ObjectGuid> _appliedAtonements = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AtonementHeal, SpellIds.SinsOfTheMany)
                && ValidateSpellEffect((spellInfo.Id, 1), (SpellIds.SinsOfTheMany, 2));
        }

        static bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo() != null;
        }

        void HandleOnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            TriggerAtonementHealOnTargets(aurEff, eventInfo);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleOnProc, 0, AuraType.Dummy));
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

        public List<ObjectGuid> GetAtonementTargets()
        {
            return _appliedAtonements;
        }

        public class TriggerArgs
        {
            public SpellInfo TriggeredBy;
            public SpellSchoolMask DamageSchoolMask;
        }

        public void TriggerAtonementHealOnTargets(AuraEffect atonementEffect, ProcEventInfo eventInfo)
        {
            Unit priest = GetUnitOwner();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            CastSpellExtraArgs args = new(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);

            // Note: atonementEffect holds the correct amount Since we passed the effect in the AuraScript that calls this method.
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), atonementEffect.GetAmount()));

            args.SetCustomArg(new TriggerArgs() { TriggeredBy = eventInfo.GetSpellInfo(), DamageSchoolMask = eventInfo.GetDamageInfo().GetSchoolMask() });

            float distanceLimit = GetEffectInfo(1).CalcValue();

            _appliedAtonements.RemoveAll(targetGuid =>
            {
                Unit target = ObjAccessor.GetUnit(priest, targetGuid);
                if (target != null)
                {
                    if (target.IsInDist2d(priest, distanceLimit))
                        priest.CastSpell(target, SpellIds.AtonementHeal, args);

                    return false;
                }

                return true;
            });
        }

        void UpdateSinsOfTheManyValue()
        {
            // Note: the damage dimish starts at the 6th application as of 10.0.5.
            float[] damageByStack = { 40.0f, 40.0f, 40.0f, 40.0f, 40.0f, 35.0f, 30.0f, 25.0f, 20.0f, 15.0f, 11.0f, 8.0f, 5.0f, 4.0f, 3.0f, 2.5f, 2.0f, 1.5f, 1.25f, 1.0f };

            foreach (uint effectIndex in new[] { 0, 1, 2 })
            {
                AuraEffect sinOfTheMany = GetUnitOwner().GetAuraEffect(SpellIds.SinsOfTheMany, effectIndex);
                if (sinOfTheMany != null)
                    sinOfTheMany.ChangeAmount((int)damageByStack[Math.Min(_appliedAtonements.Count, damageByStack.Length - 1)]);
            }
        }
    }

    [Script] // 81751 - Atonement (Heal)
    class spell_pri_abyssal_reverie : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellIds.AbyssalReverie, 0));
        }

        void CalculateHealingBonus(Unit victim, ref int healing, ref int flatMod, ref float pctMod)
        {
            spell_pri_atonement.TriggerArgs args = (spell_pri_atonement.TriggerArgs)GetSpell().m_customArg;
            if (args == null || (args.DamageSchoolMask & SpellSchoolMask.Shadow) == 0)
                return;

            AuraEffect abyssalReverieEffect = GetCaster().GetAuraEffect(SpellIds.AbyssalReverie, 0);
            if (abyssalReverieEffect != null)
                MathFunctions.AddPct(ref pctMod, abyssalReverieEffect.GetAmount());
        }

        public override void Register()
        {
            CalcHealing.Add(new(CalculateHealingBonus));
        }
    }

    // 17 - Power Word: Shield
    // 139 - Renew
    // 2061 - Flash Heal
    [Script] // 194509 - Power Word: Radiance
    class spell_pri_atonement_effect : SpellScript
    {
        uint _effectSpellId = SpellIds.AtonementEffect;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Atonement, SpellIds.AtonementEffect, SpellIds.Trinity, SpellIds.TrinityEffect, SpellIds.PowerWordRadiance, SpellIds.PowerWordShield)
            && ValidateSpellEffect((SpellIds.PowerWordRadiance, 3));
        }

        public override bool Load()
        {
            Unit caster = GetCaster();
            if (!caster.HasAura(SpellIds.Atonement))
                return false;

            // only apply Trinity if the Priest has both Trinity and Atonement and the triggering spell is Power Word: Shield.
            if (caster.HasAura(SpellIds.Trinity))
            {
                if (GetSpellInfo().Id != SpellIds.PowerWordShield)
                    return false;

                _effectSpellId = SpellIds.TrinityEffect;
            }

            return true;
        }

        void HandleOnHitTarget()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.SetTriggeringSpell(GetSpell());

            // Power Word: Radiance applies Atonement at 60 % (without modifiers) of its total duration.
            if (GetSpellInfo().Id == SpellIds.PowerWordRadiance)
                args.AddSpellMod(SpellValueMod.DurationPct, GetSpellInfo().GetEffect(3).CalcValue(caster));

            caster.CastSpell(target, _effectSpellId, args);
        }

        public override void Register()
        {
            AfterHit.Add(new(HandleOnHitTarget));
        }
    }

    [Script] // 194384 - Atonement (Buff), 214206 - Atonement [Trinity] (Buff)
    class spell_pri_atonement_effect_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Atonement);
        }

        void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Aura atonement = caster.GetAura(SpellIds.Atonement);
                if (atonement != null)
                {
                    spell_pri_atonement script = atonement.GetScript<spell_pri_atonement>();
                    if (script != null)
                        script.AddAtonementTarget(GetTarget().GetGUID());
                }
            }
        }

        void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Aura atonement = caster.GetAura(SpellIds.Atonement);
                if (atonement != null)
                {
                    spell_pri_atonement script = atonement.GetScript<spell_pri_atonement>();
                    if (script != null)
                        script.RemoveAtonementTarget(GetTarget().GetGUID());
                }
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 195178 - Atonement (Passive)
    class spell_pri_atonement_passive : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellIds.Atonement, 0));
        }

        void HandleOnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();
            Unit summoner = target.GetOwner();
            if (summoner == null)
                return;

            AuraEffect atonementEffect = summoner.GetAuraEffect(SpellIds.Atonement, 0);
            if (atonementEffect != null)
            {
                spell_pri_atonement script = atonementEffect.GetBase().GetScript<spell_pri_atonement>();
                if (script != null)
                    script.TriggerAtonementHealOnTargets(atonementEffect, eventInfo);
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleOnProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 33110 - Prayer of Mending (Heal)
    class spell_pri_benediction : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Renew)
            && ValidateSpellEffect((SpellIds.Benediction, 0));
        }

        void HandleEffectHitTarget(uint effIndex)
        {
            AuraEffect benediction = GetCaster().GetAuraEffect(SpellIds.Benediction, 0);
            if (benediction != null)
                if (RandomHelper.randChance(benediction.GetAmount()))
                    GetCaster().CastSpell(GetHitUnit(), SpellIds.Renew, TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleEffectHitTarget, 0, SpellEffectName.Heal));
        }
    }

    [Script] // 204883 - Circle of Healing
    class spell_pri_circle_of_healing : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1));
        }

        void FilterTargets(List<WorldObject> targets)
        {
            // Note: we must Remove one Math.Since target is always chosen.
            uint maxTargets = (uint)GetSpellInfo().GetEffect(1).CalcValue(GetCaster()) - 1;

            SelectRandomInjuredTargets(targets, maxTargets, true);

            Unit explicitTarget = GetExplTargetUnit();
            if (explicitTarget != null)
                targets.Add(explicitTarget);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    struct DivineImageHelpers
    {
        const uint NpcPriestDivineImage = 198236;

        public static Unit GetSummon(Unit owner)
        {
            foreach (Unit summon in owner.m_Controlled)
                if (summon.GetEntry() == NpcPriestDivineImage)
                    return summon;

            return null;
        }

        public static uint? GetSpellToCast(uint spellId)
        {
            switch (spellId)
            {
                case SpellIds.Renew:
                    return SpellIds.TranquilLight;
                case SpellIds.PowerWordShield:
                case SpellIds.PowerWordLife:
                case SpellIds.FlashHeal:
                case SpellIds.Heal:
                case SpellIds.GreaterHeal:
                case SpellIds.HolyWordSerenity:
                    return SpellIds.HealingLight;
                case SpellIds.PrayerOfMending:
                case SpellIds.PrayerOfMendingHeal:
                    return SpellIds.BlessedLight;
                case SpellIds.PrayerOfHealing:
                case SpellIds.CircleOfHealing:
                case SpellIds.HaloHoly:
                case SpellIds.DivineStarHolyHeal:
                case SpellIds.DivineHymnHeal:
                case SpellIds.HolyWordSanctify:
                case SpellIds.HolyWordSalvation:
                    return SpellIds.DazzlingLight;
                case SpellIds.ShadowWordPain:
                case SpellIds.Smite:
                case SpellIds.HolyFire:
                case SpellIds.ShadowWordDeath:
                case SpellIds.HolyWordChastise:
                case SpellIds.Mindgames:
                case SpellIds.MindgamesVenthyr:
                    return SpellIds.SearingLight;
                case SpellIds.HolyNova:
                    return SpellIds.LightEruption;
                default:
                    break;
            }

            return null;
        }

        public static void Trigger(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetActor();
            if (target == null)
                return;

            Unit divineImage = GetSummon(target);
            if (divineImage == null)
                return;

            var spellId = GetSpellToCast(eventInfo.GetSpellInfo().Id);
            if (!spellId.HasValue)
                return;

            divineImage.CastSpell(eventInfo.GetProcSpell().m_targets, spellId.Value, aurEff);
        }
    }

    [Script] // 392988 - Divine Image
    class spell_pri_divine_image : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DivineImageSummon, SpellIds.DivineImageEmpower, SpellIds.DivineImageEmpowerStack);
        }

        static void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetActor();
            if (target == null)
                return;

            // Note: if target has an active Divine Image, we should empower it rather than summoning a new one.
            Unit divineImage = DivineImageHelpers.GetSummon(target);
            if (divineImage != null)
            {
                // Note: Divine Image now teleports near the target when they cast a Holy Word spell if the Divine Image is further than 15 yards away (Patch 10.1.0).
                if (target.GetDistance(divineImage) > 15.0f)
                    divineImage.NearTeleportTo(target.GetRandomNearPosition(3.0f));

                TempSummon tempSummon = divineImage.ToTempSummon();
                if (tempSummon != null)
                    tempSummon.RefreshTimer();

                divineImage.CastSpell(divineImage, SpellIds.DivineImageEmpower, eventInfo.GetProcSpell());
            }
            else
            {
                target.CastSpell(target, SpellIds.DivineImageSummon, new CastSpellExtraArgs()
                    .SetTriggeringAura(aurEff)
                    .SetTriggeringSpell(eventInfo.GetProcSpell())
                    .SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DisallowProcEvents | TriggerCastFlags.DontReportCastError));

                // Note: Divine Image triggers a cast immediately based on the Holy Word cast.
                DivineImageHelpers.Trigger(aurEff, eventInfo);
            }

            target.CastSpell(target, SpellIds.DivineImageEmpowerStack, new CastSpellExtraArgs()
                .SetTriggeringAura(aurEff)
                .SetTriggeringSpell(eventInfo.GetProcSpell())
                .SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DisallowProcEvents | TriggerCastFlags.DontReportCastError));
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 405216 - Divine Image (Spell Cast Check)
    class spell_pri_divine_image_spell_triggered : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Renew, SpellIds.PowerWordShield, SpellIds.PowerWordLife, SpellIds.FlashHeal, SpellIds.HolyWordSerenity, SpellIds.PrayerOfMending,
                SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfHealing, SpellIds.CircleOfHealing, SpellIds.HaloHoly, SpellIds.DivineStarHolyHeal, SpellIds.DivineHymnHeal, SpellIds.HolyWordSanctify,
                SpellIds.HolyWordSalvation, SpellIds.Smite, SpellIds.HolyFire, SpellIds.ShadowWordDeath, SpellIds.ShadowWordPain, SpellIds.Mindgames, SpellIds.MindgamesVenthyr, SpellIds.HolyWordChastise,
                SpellIds.HolyNova, SpellIds.TranquilLight, SpellIds.HealingLight, SpellIds.BlessedLight, SpellIds.DazzlingLight, SpellIds.SearingLight, SpellIds.LightEruption, SpellIds.DivineImageEmpowerStack);
        }

        static bool CheckProc(ProcEventInfo eventInfo)
        {
            return DivineImageHelpers.GetSummon(eventInfo.GetActor()) != null;
        }

        void HandleAfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.DivineImageEmpowerStack);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(DivineImageHelpers.Trigger, 0, AuraType.Dummy));
            AfterEffectRemove.Add(new(HandleAfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 405963 Divine Image
    [Script] // 409387 Divine Image
    class spell_pri_divine_image_stack_timer : AuraScript
    {
        void TrackStackApplicationTime(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            var spellId = GetId();
            var owner = GetUnitOwner();
            GetUnitOwner().m_Events.AddEventAtOffset(() => owner.RemoveAuraFromStack(spellId), TimeSpan.FromMilliseconds(GetMaxDuration()));
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(TrackStackApplicationTime, 0, AuraType.Any, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script] // 33110 - Prayer of Mending (Heal)
    class spell_pri_divine_service : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingAura)
            && ValidateSpellEffect((SpellIds.DivineService, 0));
        }

        void CalculateHealingBonus(Unit victim, ref int healing, ref int flatMod, ref float pctMod)
        {
            AuraEffect divineServiceEffect = GetCaster().GetAuraEffect(SpellIds.DivineService, 0);
            if (divineServiceEffect != null)
            {
                Aura prayerOfMending = victim.GetAura(SpellIds.PrayerOfMendingAura, GetCaster().GetGUID());
                if (prayerOfMending != null)
                    MathFunctions.AddPct(ref pctMod, (int)(divineServiceEffect.GetAmount() * prayerOfMending.GetStackAmount()));
            }
        }

        public override void Register()
        {
            CalcHealing.Add(new(CalculateHealingBonus));
        }
    }

    [Script] // 122121 - Divine Star (Shadow)
    class spell_pri_divine_star_shadow : SpellScript
    {
        void HandleHitTarget(uint effIndex)
        {
            Unit caster = GetCaster();

            if (caster.GetPowerType() != (PowerType)GetEffectInfo().MiscValue)
                PreventHitDefaultEffect(effIndex);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHitTarget, 2, SpellEffectName.Energize));
        }
    }

    // 110744 - Divine Star (Holy)
    [Script] // 122121 - Divine Star (Shadow)
    class areatrigger_pri_divine_star : AreaTriggerAI
    {
        TaskScheduler _scheduler = new();
        Position _casterCurrentPosition;
        List<ObjectGuid> _affectedUnits = new();
        float _maxTravelDistance;

        public areatrigger_pri_divine_star(AreaTrigger areatrigger) : base(areatrigger) { }

        public override void OnInitialize()
        {
            SpellInfo spellInfo = SpellMgr.GetSpellInfo(at.GetSpellId(), Difficulty.None);
            if (spellInfo == null)
                return;

            if (spellInfo.GetEffects().Count <= 1)
                return;

            Unit caster = at.GetCaster();
            if (caster == null)
                return;

            _casterCurrentPosition = caster.GetPosition();

            // Note: max. distance at which the Divine Star can travel to is 1's BasePoints yards.
            _maxTravelDistance = (float)(spellInfo.GetEffect(1).CalcValue(caster));

            Position destPos = _casterCurrentPosition;
            at.MovePositionToFirstCollision(destPos, _maxTravelDistance, 0.0f);

            PathGenerator firstPath = new(at);
            firstPath.CalculatePath(destPos.GetPositionX(), destPos.GetPositionY(), destPos.GetPositionZ(), false);

            Vector3 endPoint = firstPath.GetPath().Last();

            // Note: it takes TimeSpan.FromMilliseconds(1000) to reach 1's BasePoints yards, so it takes (1000 / 1's BasePoints)ms to run 1 yard.
            at.InitSplines(firstPath.GetPath(), (uint)(at.GetDistance(endPoint.X, endPoint.Y, endPoint.Z) * (float)(1000 / _maxTravelDistance)));
        }

        public override void OnUpdate(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void OnUnitEnter(Unit unit)
        {
            HandleUnitEnterExit(unit);
        }

        public override void OnUnitExit(Unit unit)
        {
            // Note: this ensures any unit receives a second hit if they happen to be inside the At when Divine Star starts its return path.
            HandleUnitEnterExit(unit);
        }

        void HandleUnitEnterExit(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster == null)
                return;

            if (_affectedUnits.Contains(unit.GetGUID()))
                return;

            TriggerCastFlags TriggerFlags = TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress;

            if (caster.IsValidAttackTarget(unit))
                caster.CastSpell(unit, at.GetSpellId() == SpellIds.DivineStarShadow ? SpellIds.DivineStarShadowDamage : SpellIds.DivineStarHolyDamage,
                    TriggerFlags);
            else if (caster.IsValidAssistTarget(unit))
                caster.CastSpell(unit, at.GetSpellId() == SpellIds.DivineStarShadow ? SpellIds.DivineStarShadowHeal : SpellIds.DivineStarHolyHeal,
                    TriggerFlags);

            _affectedUnits.Add(unit.GetGUID());
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
                if (caster == null)
                    return;

                _casterCurrentPosition = caster.GetPosition();

                Vector3[] returnSplinePoints = new Vector3[4];

                returnSplinePoints[0] = at.GetPosition();
                returnSplinePoints[1] = at.GetPosition();
                returnSplinePoints[2] = caster.GetPosition();
                returnSplinePoints[3] = caster.GetPosition();

                at.InitSplines(returnSplinePoints, (uint)(at.GetDistance(caster) / _maxTravelDistance * 1000));

                task.Repeat(TimeSpan.FromMilliseconds(250));
            });
        }
    }

    [Script] // 391339 - Empowered Renew
    class spell_pri_empowered_renew : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Renew, SpellIds.EmpoweredRenewHeal)
            && ValidateSpellEffect((SpellIds.Renew, 0))
            && SpellMgr.GetSpellInfo(SpellIds.Renew, Difficulty.None).GetEffect(0).IsAura(AuraType.PeriodicHeal);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            SpellInfo renewSpellInfo = SpellMgr.GetSpellInfo(SpellIds.Renew, GetCastDifficulty());
            SpellEffectInfo renewEffect = renewSpellInfo.GetEffect(0);
            int estimatedTotalHeal = (int)AuraEffect.CalculateEstimatedfTotalPeriodicAmount(caster, target, renewSpellInfo, renewEffect, renewEffect.CalcValue(caster), 1);
            int healAmount = MathFunctions.CalculatePct(estimatedTotalHeal, aurEff.GetAmount());

            caster.CastSpell(target, SpellIds.EmpoweredRenewHeal, new CastSpellExtraArgs(aurEff).AddSpellMod(SpellValueMod.BasePoint0, healAmount));
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 414553 - Epiphany
    class spell_pri_epiphany : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMending, SpellIds.EpiphanyHighlight);
        }

        static bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        void HandleOnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();

            target.GetSpellHistory().ResetCooldown(SpellIds.PrayerOfMending, true);

            target.CastSpell(target, SpellIds.EpiphanyHighlight, aurEff);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new(HandleOnProc, 0, AuraType.Dummy));
        }
    }

    // 415673 - Essence Devourer (Heal)
    [Script] // 415676 - Essence Devourer (Heal)
    class spell_pri_essence_devourer_heal : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            SelectRandomInjuredTargets(targets, 1, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    [Script] // 246287 - Evangelism
    class spell_pri_evangelism : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Trinity, SpellIds.AtonementEffect, SpellIds.TrinityEffect);
        }

        void HandleScriptEffect(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            Aura atonementAura = caster.HasAura(SpellIds.Trinity)
                ? target.GetAura(SpellIds.TrinityEffect, caster.GetGUID())
                : target.GetAura(SpellIds.AtonementEffect, caster.GetGUID());
            if (atonementAura == null)
                return;

            TimeSpan extraDuration = TimeSpan.FromSeconds(GetEffectValue());

            atonementAura.SetDuration((int)(atonementAura.GetDuration() + extraDuration.TotalMilliseconds));
            atonementAura.SetMaxDuration((int)(atonementAura.GetDuration() + extraDuration.TotalMilliseconds));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 33110 - Prayer of Mending (Heal)
    class spell_pri_focused_mending : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellIds.FocusedMending, 0));
        }

        void CalculateHealingBonus(Unit victim, ref int healing, ref int flatMod, ref float pctMod)
        {
            AuraEffect focusedMendingEffect = GetCaster().GetAuraEffect(SpellIds.FocusedMending, 0);
            if (focusedMendingEffect != null)
            {
                bool isEmpoweredByFocusedMending = (bool)GetSpell().m_customArg;

                if (isEmpoweredByFocusedMending && isEmpoweredByFocusedMending)
                    MathFunctions.AddPct(ref pctMod, focusedMendingEffect.GetAmount());
            }
        }

        public override void Register()
        {
            CalcHealing.Add(new(CalculateHealingBonus));
        }
    }

    [Script] // 47788 - Guardian Spirit
    class spell_pri_guardian_spirit : AuraScript
    {
        uint healPct;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GuardianSpiritHeal) && ValidateSpellEffect((spellInfo.Id, 1));
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
            DoEffectCalcAmount.Add(new(CalculateAmount, 1, AuraType.SchoolAbsorb));
            OnEffectAbsorb.Add(new(Absorb, 1));
        }
    }

    [Script] // 120644 - Halo (Shadow)
    class spell_pri_halo_shadow : SpellScript
    {
        void HandleHitTarget(uint effIndex)
        {
            Unit caster = GetCaster();

            if (caster.GetPowerType() != (PowerType)GetEffectInfo().MiscValue)
                PreventHitDefaultEffect(effIndex);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHitTarget, 1, SpellEffectName.Energize));
        }
    }

    // 120517 - Halo (Holy)
    [Script] // 120644 - Halo (Shadow)
    class areatrigger_pri_halo : AreaTriggerAI
    {
        public areatrigger_pri_halo(AreaTrigger areatrigger) : base(areatrigger) { }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                if (caster.IsValidAttackTarget(unit))
                    caster.CastSpell(unit, at.GetSpellId() == SpellIds.HaloShadow ? SpellIds.HaloShadowDamage : SpellIds.HaloHolyDamage,
                        TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress);
                else if (caster.IsValidAssistTarget(unit))
                    caster.CastSpell(unit, at.GetSpellId() == SpellIds.HaloShadow ? SpellIds.HaloShadowHeal : SpellIds.HaloHolyHeal,
                        TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress);
            }
        }
    }

    [Script] // 391154 - Holy Mending
    class spell_pri_holy_mending : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Renew, SpellIds.HolyMendingHeal);
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            return procInfo.GetProcTarget().HasAura(SpellIds.Renew, procInfo.GetActor().GetGUID());
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.HolyMendingHeal, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 63733 - Holy Words
    class spell_pri_holy_words : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Heal, SpellIds.FlashHeal, SpellIds.PrayerOfHealing, SpellIds.Renew, SpellIds.Smite, SpellIds.HolyWordChastise, SpellIds.HolyWordSanctify, SpellIds.HolyWordSerenity)
                && ValidateSpellEffect((SpellIds.HolyWordSerenity, 1), (SpellIds.HolyWordSanctify, 3), (SpellIds.HolyWordChastise, 1));
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
                    // cdReduction = SpellMgr.GetSpellInfo(SpellIds.HolyWordSerenity, GetCastDifficulty()).GetEffect(1).CalcValue(player);
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

            SpellInfo targetSpellInfo = SpellMgr.GetSpellInfo(targetSpellId, GetCastDifficulty());
            int cdReduction = targetSpellInfo.GetEffect(cdReductionEffIndex).CalcValue(GetTarget());
            GetTarget().GetSpellHistory().ModifyCooldown(targetSpellInfo, TimeSpan.FromSeconds(-cdReduction), true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 265202 - Holy Word: Salvation
    class spell_pri_holy_word_salvation : SpellScript
    {
        SpellInfo _spellInfoHeal;
        SpellEffectInfo _healEffectDummy;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingAura, SpellIds.Renew)
                && ValidateSpellEffect((SpellIds.PrayerOfMendingHeal, 0), (spellInfo.Id, 1))
                && spellInfo.GetEffect(1).TargetB.GetTarget() == Targets.UnitSrcAreaAlly;
        }

        public override bool Load()
        {
            _spellInfoHeal = SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None);
            _healEffectDummy = _spellInfoHeal.GetEffect(0);
            return true;
        }

        void HandleApplyBuffs(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);

            // amount of Prayer of Mending is SpellIds.HolyWordSalvation's 1.
            args.AddSpellMod(SpellValueMod.AuraStack, GetEffectValue());

            int basePoints = caster.SpellHealingBonusDone(target, _spellInfoHeal, _healEffectDummy.CalcValue(caster), DamageEffectType.Heal, _healEffectDummy);
            args.AddSpellMod(SpellValueMod.BasePoint0, basePoints);
            caster.CastSpell(target, SpellIds.PrayerOfMendingAura, args);

            // a full duration Renew is triggered.
            caster.CastSpell(target, SpellIds.Renew, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetTriggeringSpell(GetSpell()));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleApplyBuffs, 1, SpellEffectName.Dummy));
        }
    }

    // 2050 - Holy Word: Serenity
    [Script] // 34861 - Holy Word: Sanctify
    class spell_pri_holy_word_salvation_cooldown_reduction : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyWordSalvation)
            && ValidateSpellEffect((SpellIds.HolyWordSalvation, 2));
        }

        public override bool Load()
        {
            return GetCaster().HasSpell(SpellIds.HolyWordSalvation);
        }

        void ReduceCooldown()
        {
            // cooldown reduced by SpellIds.HolyWordSalvation's Seconds(2).
            int cooldownReduction = SpellMgr.GetSpellInfo(SpellIds.HolyWordSalvation, GetCastDifficulty()).GetEffect(2).CalcValue(GetCaster());

            GetCaster().GetSpellHistory().ModifyCooldown(SpellIds.HolyWordSalvation, TimeSpan.FromSeconds(-cooldownReduction), true);
        }

        public override void Register()
        {
            AfterCast.Add(new(ReduceCooldown));
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
            if ((eventInfo.GetSpellTypeMask() & ProcFlagsSpellType.Heal) != 0)
                caster.CastSpell(null, SpellIds.DivineBlessing, true);

            if ((eventInfo.GetSpellTypeMask() & ProcFlagsSpellType.Damage) != 0)
                caster.CastSpell(null, SpellIds.DivineWrath, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
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
            GetHitUnit().CastSpell(targets, (uint)GetEffectValue(), GetCastDifficulty());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleEffectDummy, 0, SpellEffectName.Dummy));
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
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 373178 - Light's Wrath
    class spell_pri_lights_wrath : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1));
        }

        public override void OnPrecast()
        {
            Aura atonement = GetCaster().GetAura(SpellIds.Atonement);
            if (atonement == null)
                return;

            spell_pri_atonement script = atonement.GetScript<spell_pri_atonement>();
            if (script == null)
                return;

            foreach (ObjectGuid atonementTarget in script.GetAtonementTargets())
            {
                Unit target = ObjAccessor.GetUnit(GetCaster(), atonementTarget);
                if (target != null)
                {
                    target.CancelSpellMissiles(SpellIds.LightsWrathVisual, false, false);
                    target.CastSpell(GetCaster(), SpellIds.LightsWrathVisual, TriggerCastFlags.IgnoreCastInProgress);
                }
            }
        }

        void CalculateDamageBonus(Unit victim, ref int damage, ref int flatMod, ref float pctMod)
        {
            Aura atonement = GetCaster().GetAura(SpellIds.Atonement);
            if (atonement == null)
                return;

            // Atonement size may have changed when missile hits, we need to take an updated count of Atonement applications.
            spell_pri_atonement script = atonement.GetScript<spell_pri_atonement>();
            if (script != null)
                MathFunctions.AddPct(ref pctMod, GetEffectInfo(1).CalcValue(GetCaster()) * script.GetAtonementTargets().Count);
        }

        public override void Register()
        {
            CalcDamage.Add(new(CalculateDamageBonus));
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
            {
                Unit caster = GetCaster();
                if (caster != null)
                    caster.CastSpell(GetTarget().GetPosition(), SpellIds.MindBombStun, true);
            }
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(RemoveEffect, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 390686 - Painful Punishment
    class spell_pri_painful_punishment : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShadowWordPain, SpellIds.PurgeTheWickedPeriodic);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetActionTarget();
            if (caster == null || target == null)
                return;

            int additionalDuration = aurEff.GetAmount();

            Aura shadowWordPain = target.GetOwnedAura(SpellIds.ShadowWordPain, caster.GetGUID());
            if (shadowWordPain != null)
                shadowWordPain.SetDuration(shadowWordPain.GetDuration() + additionalDuration);

            Aura purgeTheWicked = target.GetOwnedAura(SpellIds.PurgeTheWickedPeriodic, caster.GetGUID());
            if (purgeTheWicked != null)
                purgeTheWicked.SetDuration(purgeTheWicked.GetDuration() + additionalDuration);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script("spell_pri_penance", SpellIds.PenanceChannelDamage, SpellIds.PenanceChannelHealing)] // 47540 - Penance
    [Script("spell_pri_dark_reprimand", SpellIds.DarkReprimandChannelDamage, SpellIds.DarkReprimandChannelHealing)] // 400169 - Dark Reprimand
    class spell_pri_penance : SpellScript
    {
        uint _damageSpellId;
        uint _healingSpellId;

        public spell_pri_penance(uint damageSpellId, uint healingSpellId)
        {
            _damageSpellId = damageSpellId;
            _healingSpellId = healingSpellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_damageSpellId, _healingSpellId);
        }

        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();

            Unit target = GetExplTargetUnit();
            if (target != null)
            {
                if (!caster.IsFriendlyTo(target))
                {
                    if (!caster.IsValidAttackTarget(target))
                        return SpellCastResult.BadTargets;

                    if (!caster.IsInFront(target))
                        return SpellCastResult.UnitNotInfront;
                }
            }

            return SpellCastResult.SpellCastOk;
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();

            Unit target = GetHitUnit();
            if (target != null)
            {
                if (caster.IsFriendlyTo(target))
                    caster.CastSpell(target, _healingSpellId, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreSpellAndCategoryCD)
                        .SetTriggeringSpell(GetSpell()));
                else
                    caster.CastSpell(target, _damageSpellId, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreSpellAndCategoryCD)
                        .SetTriggeringSpell(GetSpell()));
            }
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 47758 - Penance (Channel Damage), 47757 - Penance (Channel Healing)
    [Script] // 373129 - Dark Reprimand (Channel Damage), 400171 - Dark Reprimand (Channel Healing)
    class spell_pri_penance_or_dark_reprimand_channeled : AuraScript
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
            OnEffectRemove.Add(new(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 262484 - Power Leech (Passive for Shadowfiend)
    [Script] // 284621 - Power Leech (Passive for Mindbender)
    class spell_pri_power_leech_passive : AuraScript
    {
        const uint NpcPriestMindbender = 62982;
        const uint NpcPriestShadowfiend = 19668;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerLeechShadowfiendInsanity, SpellIds.PowerLeechShadowfiendMana, SpellIds.PowerLeechMindbenderInsanity, SpellIds.PowerLeechMindbenderMana, SpellIds.EssenceDevourer, SpellIds.EssenceDevourerShadowfiendHeal, SpellIds.EssenceDevourerMindbenderHeal)
            && ValidateSpellEffect((SpellIds.PowerLeechShadowfiendInsanity, 0), (SpellIds.PowerLeechShadowfiendMana, 0), (SpellIds.PowerLeechMindbenderInsanity, 0), (SpellIds.PowerLeechMindbenderMana, 0));
        }

        static bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo() != null;
        }

        void HandleOnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();
            Player summoner = target.GetOwner()?.ToPlayer();
            if (summoner == null)
                return;

            SpellInfo spellInfo;
            int divisor = 1;

            if (summoner.GetPrimarySpecialization() != ChrSpecialization.PriestShadow)
            {
                if (target.GetEntry() == NpcPriestShadowfiend)
                {
                    // Note: divisor is 100 because effect value is 5 and its supposed to restore 0.5%
                    spellInfo = SpellMgr.GetSpellInfo(SpellIds.PowerLeechShadowfiendMana, GetCastDifficulty());
                    divisor = 10;
                }
                else
                {
                    // Note: divisor is 100 because effect value is 20 and its supposed to restore 0.2%
                    spellInfo = SpellMgr.GetSpellInfo(SpellIds.PowerLeechMindbenderMana, GetCastDifficulty());
                    divisor = 100;
                }
            }
            else
                spellInfo = SpellMgr.GetSpellInfo(target.GetEntry() == NpcPriestShadowfiend
                    ? SpellIds.PowerLeechShadowfiendInsanity
                    : SpellIds.PowerLeechMindbenderInsanity, GetCastDifficulty());

            target.CastSpell(summoner, spellInfo.Id, new CastSpellExtraArgs(aurEff)
                .AddSpellMod(SpellValueMod.BasePoint0, spellInfo.GetEffect(0).CalcValue() / divisor));

            // Note: Essence Devourer talent.
            if (summoner.HasAura(SpellIds.EssenceDevourer))
                summoner.CastSpell(null, target.GetEntry() == NpcPriestShadowfiend ? SpellIds.EssenceDevourerShadowfiendHeal : SpellIds.EssenceDevourerMindbenderHeal, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleOnProc, 0, AuraType.Dummy));
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
            OnEffectApply.Add(new(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 47666 - Penance (Damage)
    [Script] // 373130 - Dark Reprimand (Damage)
    class spell_pri_power_of_the_dark_side_damage_bonus : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerOfTheDarkSide);
        }

        void CalculateDamageBonus(Unit victim, ref int damage, ref int flatMod, ref float pctMod)
        {
            AuraEffect powerOfTheDarkSide = GetCaster().GetAuraEffect(SpellIds.PowerOfTheDarkSide, 0);
            if (powerOfTheDarkSide != null)
                MathFunctions.AddPct(ref pctMod, powerOfTheDarkSide.GetAmount());
        }

        public override void Register()
        {
            CalcDamage.Add(new(CalculateDamageBonus));
        }
    }

    // 47750 - Penance (Healing)
    [Script] // 400187 - Dark Reprimand (Healing)
    class spell_pri_power_of_the_dark_side_healing_bonus : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerOfTheDarkSide);
        }

        void CalculateHealingBonus(Unit victim, ref int healing, ref int flatMod, ref float pctMod)
        {
            AuraEffect powerOfTheDarkSide = GetCaster().GetAuraEffect(SpellIds.PowerOfTheDarkSide, 0);
            if (powerOfTheDarkSide != null)
                MathFunctions.AddPct(ref pctMod, powerOfTheDarkSide.GetAmount());
        }

        public override void Register()
        {
            CalcHealing.Add(new(CalculateHealingBonus));
        }
    }

    [Script] // 194509 - Power Word: Radiance
    class spell_pri_power_word_radiance : SpellScript
    {
        List<ObjectGuid> _visualTargets = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AtonementEffect);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            Unit explTarget = GetExplTargetUnit();

            // we must add one Math.Since explicit target is always chosen.
            uint maxTargets = (uint)GetEffectInfo(2).CalcValue(GetCaster()) + 1;

            if (targets.Count > maxTargets)
            {
                // priority is: a) no Atonement b) injured c) anything else (excluding explicit target which is always added).
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

            foreach (WorldObject target in targets)
            {
                if (target == explTarget)
                    continue;

                _visualTargets.Add(target.GetGUID());
            }
        }

        void HandleEffectHitTarget(uint effIndex)
        {
            foreach (ObjectGuid guid in _visualTargets)
            {
                Unit target = ObjAccessor.GetUnit(GetHitUnit(), guid);
                if (target != null)
                    GetHitUnit().SendPlaySpellVisual(target, SpellIds.VisualPriestPowerWordRadiance, 0, 0, 70.0f);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitDestAreaAlly));
            OnEffectHitTarget.Add(new(HandleEffectHitTarget, 0, SpellEffectName.Dummy));
        }

        (bool, bool) MakeSortTuple(WorldObject obj)
        {
            return (IsUnitWithNoAtonement(obj), IsUnitInjured(obj));
        }

        // Returns true if obj is a unit but has no atonement
        bool IsUnitWithNoAtonement(WorldObject obj)
        {
            Unit unit = obj.ToUnit();
            return unit != null && !unit.HasAura(SpellIds.AtonementEffect, GetCaster().GetGUID());
        }

        // Returns true if obj is a unit and is injured
        static bool IsUnitInjured(WorldObject obj)
        {
            Unit unit = obj.ToUnit();
            return unit != null && !unit.IsFullHealth();
        }
    }

    [Script] // 17 - Power Word: Shield
    class spell_pri_power_word_shield : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StrengthOfSoul, SpellIds.StrengthOfSoulEffect, SpellIds.AtonementEffect, SpellIds.TrinityEffect, SpellIds.ShieldDiscipline, SpellIds.ShieldDisciplineEffect, SpellIds.PvpRulesEnabledHardcoded)
                && ValidateSpellEffect((SpellIds.MasteryGrace, 0), (SpellIds.Rapture, 1), (SpellIds.Benevolence, 0));
        }

        void CalculateAmount(AuraEffect auraEffect, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;

            Unit caster = GetCaster();
            if (caster != null)
            {
                float modifiedAmount = caster.SpellBaseDamageBonusDone(GetSpellInfo().GetSchoolMask()) * 3.36f;

                Player player = caster.ToPlayer();
                if (player != null)
                {
                    MathFunctions.AddPct(ref modifiedAmount, player.GetRatingBonusValue(CombatRating.VersatilityDamageDone));

                    // Mastery: Grace (Tbd: move into DoEffectCalcDamageAndHealing hook with a new SpellScript and AuraScript).
                    AuraEffect masteryGraceEffect = caster.GetAuraEffect(SpellIds.MasteryGrace, 0);
                    if (masteryGraceEffect != null)
                        if (GetUnitOwner().HasAura(SpellIds.AtonementEffect) || GetUnitOwner().HasAura(SpellIds.TrinityEffect))
                            MathFunctions.AddPct(ref modifiedAmount, masteryGraceEffect.GetAmount());

                    if (player.GetPrimarySpecialization() != ChrSpecialization.PriestHoly)
                    {
                        modifiedAmount *= 1.25f;
                        if (caster.HasAura(SpellIds.PvpRulesEnabledHardcoded))
                            modifiedAmount *= 0.8f;
                    }
                }

                // Rapture talent (Tbd: move into DoEffectCalcDamageAndHealing hook).
                AuraEffect raptureEffect = caster.GetAuraEffect(SpellIds.Rapture, 1);
                if (raptureEffect != null)
                    MathFunctions.AddPct(ref modifiedAmount, raptureEffect.GetAmount());

                // Benevolence talent
                AuraEffect benevolenceEffect = caster.GetAuraEffect(SpellIds.Benevolence, 0);
                if (benevolenceEffect != null)
                    MathFunctions.AddPct(ref modifiedAmount, benevolenceEffect.GetAmount());

                amount = (int)modifiedAmount;
            }
        }

        void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster == null)
                return;

            // Note: Strength of Soul PvP talent.
            if (caster.HasAura(SpellIds.StrengthOfSoul))
                caster.CastSpell(GetTarget(), SpellIds.StrengthOfSoulEffect, aurEff);
        }

        void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.StrengthOfSoulEffect);

            // Note: Shield Discipline talent.
            Unit caster = GetCaster();
            if (caster != null)
                if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell && caster.HasAura(SpellIds.ShieldDiscipline))
                    caster.CastSpell(caster, SpellIds.ShieldDisciplineEffect, aurEff);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.SchoolAbsorb));
            AfterEffectApply.Add(new(HandleOnApply, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.RealOrReapplyMask));
            AfterEffectRemove.Add(new(HandleOnRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real));
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
            OnEffectLaunch.Add(new(RestoreMana, 1, SpellEffectName.Dummy));
        }
    }

    [Script] // 33076 - Prayer of Mending (Dummy)
    class spell_pri_prayer_of_mending_dummy : SpellScript
    {
        SpellInfo _spellInfoHeal;
        SpellEffectInfo _healEffectDummy;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfMendingAura, SpellIds.PrayerOfMendingAura, SpellIds.Epiphany, SpellIds.EpiphanyHighlight)
                && ValidateSpellEffect((SpellIds.PrayerOfMendingHeal, 0));
        }

        public override bool Load()
        {
            _spellInfoHeal = SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None);
            _healEffectDummy = _spellInfoHeal.GetEffect(0);
            return true;
        }

        void HandleEffectDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            // Note: we need to increase BasePoints by 1 Math.Since it's 4 as default. Also Hackfix, we shouldn't reduce it by 1 if the target has the aura already.
            byte stackAmount = (byte)(target.HasAura(SpellIds.PrayerOfMendingAura, caster.GetGUID()) ? GetEffectValue() : GetEffectValue() + 1);

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.AuraStack, stackAmount);

            // Note: this line's purpose is to show the correct amount in Points field in SmsgAuraUpdate.
            int basePoints = caster.SpellHealingBonusDone(target, _spellInfoHeal, _healEffectDummy.CalcValue(caster), DamageEffectType.Heal, _healEffectDummy);
            args.AddSpellMod(SpellValueMod.BasePoint0, basePoints);

            // Note: Focused Mending talent.
            args.SetCustomArg(true);

            caster.CastSpell(target, SpellIds.PrayerOfMendingAura, args);

            // Note: the visualSender is the priest if it is first cast or the aura holder when the aura triggers.
            caster.SendPlaySpellVisual(target, SpellIds.VisualPriestPrayerOfMending, 0, 0, 40.0f);

            // Note: Epiphany talent.
            if (caster.HasAura(SpellIds.Epiphany))
                caster.RemoveAurasDueToSpell(SpellIds.EpiphanyHighlight);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleEffectDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 41635 - Prayer of Mending (Aura)
    class spell_pri_prayer_of_mending_AuraScript : AuraScript
    {
        bool _isEmpoweredByFocusedMending;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfMendingJump)
            && ValidateSpellEffect((SpellIds.SayYourPrayers, 0));
        }

        void HandleHeal(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            // Note: caster is the priest who cast the spell and target is current holder of the aura.
            Unit target = GetTarget();

            Unit caster = GetCaster();
            if (caster != null)
            {
                CastSpellExtraArgs args = new(aurEff);
                args.SetCustomArg(_isEmpoweredByFocusedMending);

                caster.CastSpell(target, SpellIds.PrayerOfMendingHeal, args);

                // Note: jump is only executed if higher than 1 stack.
                int stackAmount = GetStackAmount();
                if (stackAmount > 1)
                {
                    args.OriginalCaster = caster.GetGUID();

                    int newStackAmount = stackAmount - 1;
                    AuraEffect sayYourPrayers = caster.GetAuraEffect(SpellIds.SayYourPrayers, 0);
                    if (sayYourPrayers != null)
                        if (RandomHelper.randChance(sayYourPrayers.GetAmount()))
                            ++newStackAmount;

                    args.AddSpellMod(SpellValueMod.BasePoint0, newStackAmount);

                    target.CastSpell(target, SpellIds.PrayerOfMendingJump, args);
                }

                Remove();
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleHeal, 0, AuraType.Dummy));
        }

        public void SetEmpoweredByFocusedMending(bool isEmpowered)
        {
            _isEmpoweredByFocusedMending = isEmpowered;
        }
    }

    [Script]
    class spell_pri_prayer_of_mending : SpellScript
    {
        void HandleEffectDummy(uint effIndex)
        {
            Aura aura = GetHitAura();
            if (aura == null)
                return;

            var script = aura.GetScript<spell_pri_prayer_of_mending_AuraScript>();
            if (script == null)
                return;

            bool isEmpoweredByFocusedMending = (bool)GetSpell().m_customArg;
            if (isEmpoweredByFocusedMending)
                script.SetEmpoweredByFocusedMending(isEmpoweredByFocusedMending);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleEffectDummy, 0, SpellEffectName.ApplyAura));
        }
    }

    [Script] // 155793 - Prayer of Mending (Jump)
    class spell_pri_prayer_of_mending_jump : SpellScript
    {
        SpellInfo _spellInfoHeal;
        SpellEffectInfo _healEffectDummy;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfMendingAura)
            && ValidateSpellEffect((SpellIds.PrayerOfMendingHeal, 0));
        }

        public override bool Load()
        {
            _spellInfoHeal = SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None);
            _healEffectDummy = _spellInfoHeal.GetEffect(0);
            return true;
        }

        void FilterTargets(List<WorldObject> targets)
        {
            // Note: priority list is a) players b) non-player units. Also, this spell became smartheal in WoD.
            SelectRandomInjuredTargets(targets, 1, true);
        }

        void HandleJump(uint effIndex)
        {
            Unit origCaster = GetOriginalCaster();
            if (origCaster != null)
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.AuraStack, (byte)GetEffectValue());

                // Note: this line's purpose is to show the correct amount in Points field in SmsgAuraUpdate.
                int basePoints = origCaster.SpellHealingBonusDone(GetHitUnit(), _spellInfoHeal, _healEffectDummy.CalcValue(origCaster), DamageEffectType.Heal, _healEffectDummy);
                args.AddSpellMod(SpellValueMod.BasePoint0, basePoints);

                // Note: Focused Mending talent.
                args.SetCustomArg(false);

                origCaster.CastSpell(GetHitUnit(), SpellIds.PrayerOfMendingAura, args);

                // Note: the visualSender is the priest if it is first cast or the aura holder when the aura triggers.
                GetCaster().SendPlaySpellVisual(GetHitUnit(), SpellIds.VisualPriestPrayerOfMending, 0, 0, 40.0f);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitSrcAreaAlly));
            OnEffectHitTarget.Add(new(HandleJump, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 405554 - Priest Holy 10.1 Class Set 2pc
    class spell_pri_holy_10_1_class_set_2pc : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Holy101ClassSet2PChooser)
            && ValidateSpellEffect((SpellIds.PrayerOfMending, 0));
        }

        static bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            CastSpellExtraArgs args = new(aurEff);
            args.SetTriggeringSpell(eventInfo.GetProcSpell());
            args.AddSpellMod(SpellValueMod.BasePoint0, SpellMgr.GetSpellInfo(SpellIds.PrayerOfMending, GetCastDifficulty()).GetEffect(0).CalcValue(GetCaster()));

            GetTarget().CastSpell(GetTarget(), SpellIds.Holy101ClassSet2PChooser, args);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 411097 - Priest Holy 10.1 Class Set 2pc (Chooser)
    class spell_pri_holy_10_1_class_set_2pc_chooser : SpellScript
    {
        SpellInfo _spellInfoHeal;
        SpellEffectInfo _healEffectDummy;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PrayerOfMendingAura, SpellIds.PrayerOfMendingHeal, SpellIds.PrayerOfMendingAura)
                && ValidateSpellEffect((SpellIds.PrayerOfMendingHeal, 0));
        }

        public override bool Load()
        {
            _spellInfoHeal = SpellMgr.GetSpellInfo(SpellIds.PrayerOfMendingHeal, Difficulty.None);
            _healEffectDummy = _spellInfoHeal.GetEffect(0);
            return true;
        }

        void FilterTargets(List<WorldObject> targets)
        {
            SelectRandomInjuredTargets(targets, 1, true);
        }

        void HandleEffectDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            // Note: we need to increase BasePoints by 1 Math.Since it's 4 as default. Also Hackfix, we shouldn't reduce it by 1 if the target has the aura already.
            byte stackAmount = (byte)(target.HasAura(SpellIds.PrayerOfMendingAura, caster.GetGUID()) ? GetEffectValue() : GetEffectValue() + 1);

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.AuraStack, stackAmount);

            // Note: this line's purpose is to show the correct amount in Points field in SmsgAuraUpdate.
            int basePoints = caster.SpellHealingBonusDone(target, _spellInfoHeal, _healEffectDummy.CalcValue(caster), DamageEffectType.Heal, _healEffectDummy);
            args.AddSpellMod(SpellValueMod.BasePoint0, basePoints);

            // Note: Focused Mending talent.
            args.SetCustomArg(true);

            caster.CastSpell(target, SpellIds.PrayerOfMendingAura, args);

            // Note: the visualSender is the priest if it is first cast or the aura holder when the aura triggers.
            caster.SendPlaySpellVisual(target, SpellIds.VisualPriestPrayerOfMending, 0, 0, 40.0f);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaEntry));
            OnEffectHitTarget.Add(new(HandleEffectDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 155793 - Prayer of Mending (Jump)
    class spell_pri_holy_10_1_class_set_4pc : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Holy101ClassSet4P, SpellIds.Holy101ClassSet4PEffect);
        }

        void HandleEffectDummy(uint effIndex)
        {
            if (GetOriginalCaster().HasAura(SpellIds.Holy101ClassSet4P))
                GetOriginalCaster().CastSpell(GetOriginalCaster(), SpellIds.Holy101ClassSet4PEffect, TriggerCastFlags.IgnoreGCD);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleEffectDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 41635 - Prayer of Mending (Aura)
    class spell_pri_holy_10_1_class_set_4pc_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Holy101ClassSet4P, SpellIds.Holy101ClassSet4PEffect);
        }

        void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            if (GetCaster().HasAura(SpellIds.Holy101ClassSet4P))
                GetCaster().CastSpell(GetCaster(), SpellIds.Holy101ClassSet4PEffect, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD).SetTriggeringAura(aurEff));
        }

        public override void Register()
        {
            OnEffectRemove.Add(new(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 204197 - Purge the Wicked
    [Script] // Called by Penance - 47540, Dark Reprimand - 400169
    class spell_pri_purge_the_wicked : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PurgeTheWickedPeriodic, SpellIds.PurgeTheWickedDummy);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            if (target.HasAura(SpellIds.PurgeTheWickedPeriodic, caster.GetGUID()))
                caster.CastSpell(target, SpellIds.PurgeTheWickedDummy, TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 204215 - Purge the Wicked
    class spell_pri_purge_the_wicked_dummy : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PurgeTheWickedPeriodic, SpellIds.RevelInPurity)
            && ValidateSpellEffect((SpellIds.RevelInPurity, 1));
        }

        void FilterTargets(List<WorldObject> targets)
        {
            Unit caster = GetCaster();
            Unit explTarget = GetExplTargetUnit();

            targets.RemoveAll(obj =>
            {
                // Note: we must Remove any non-unit target, the explicit target and any other target that may be under any crowd control aura.
                Unit target = obj.ToUnit();
                return target == null || target == explTarget || target.HasBreakableByDamageCrowdControlAura();
            });

            if (targets.Empty())
                return;

            // Note: there's no SpellEffectDummy with BasePoints 1 in any of the spells related to use as reference so we hardcode the value.
            uint spreadCount = 1;

            // Note: we must sort our list of targets whose priority is 1) aura, 2) distance, and 3) duration.
            targets.Sort((lhs, rhs) =>
            {
                Unit targetA = lhs.ToUnit();
                Unit targetB = rhs.ToUnit();

                Aura auraA = targetA.GetAura(SpellIds.PurgeTheWickedPeriodic, caster.GetGUID());
                Aura auraB = targetB.GetAura(SpellIds.PurgeTheWickedPeriodic, caster.GetGUID());

                if (auraA == null)
                {
                    if (auraB != null)
                        return 1;
                    return explTarget.GetExactDist(targetA).CompareTo(explTarget.GetExactDist(targetB));
                }
                if (auraB == null)
                    return -1;

                return auraA.GetDuration().CompareTo(auraB.GetDuration());
            });

            // Note: Revel in Purity talent.
            if (caster.HasAura(SpellIds.RevelInPurity))
                spreadCount += (uint)SpellMgr.GetSpellInfo(SpellIds.RevelInPurity, Difficulty.None).GetEffect(1).CalcValue(GetCaster());

            if (targets.Count > spreadCount)
                targets.Resize(spreadCount);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            caster.CastSpell(target, SpellIds.PurgeTheWickedPeriodic, TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitDestAreaEnemy));
            OnEffectHitTarget.Add(new(HandleDummy, 1, SpellEffectName.Dummy));
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

            Unit target = ObjAccessor.GetUnit(caster, _raptureTarget);
            if (target != null)
                caster.CastSpell(target, SpellIds.PowerWordShield,
                    new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnorePowerAndReagentCost | TriggerCastFlags.IgnoreCastInProgress)
                    .SetTriggeringSpell(GetSpell()));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleEffectDummy, 0, SpellEffectName.Dummy));
            AfterCast.Add(new(HandleAfterCast));
        }
    }

    [Script] // 280391 - Math.Sins of the Many
    class spell_pri_Sins_of_the_many : AuraScript
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
            OnEffectApply.Add(new(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
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
            target.CastSpell(target, SpellIds.SpiritOfRedemption, aurEff);
            target.SetFullHealth();
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new(HandleAbsorb, 0));
        }
    }

    [Script] // 314867 - Shadow Covenant
    class spell_pri_shadow_covenant : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 2));
        }

        void FilterTargets(List<WorldObject> targets)
        {
            // Remove explicit target (will be readded later)
            targets.Remove(GetExplTargetWorldObject());

            // we must Remove one Math.Since explicit target is always added.
            uint maxTargets = (uint)GetEffectInfo(2).CalcValue(GetCaster()) - 1;

            SelectRandomInjuredTargets(targets, maxTargets, true);

            Unit explicitTarget = GetExplTargetUnit();
            if (explicitTarget != null)
                targets.Add(explicitTarget);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitDestAreaAlly));
        }
    }

    [Script] // 186263 - Shadow Mend
    class spell_pri_shadow_mend : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Atonement, SpellIds.AtonementEffect, SpellIds.Trinity, SpellIds.MasochismTalent, SpellIds.MasochismPeriodicHeal, SpellIds.ShadowMendPeriodicDummy);
        }

        void HandleEffectHit()
        {
            Unit target = GetHitUnit();
            if (target != null)
            {
                Unit caster = GetCaster();

                int periodicAmount = GetHitHeal() / 20;
                int damageForAuraRemoveAmount = periodicAmount * 10;

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
            AfterHit.Add(new(HandleEffectHit));
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
            OnEffectPeriodic.Add(new(HandleDummyTick, 0, AuraType.PeriodicDummy));
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
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
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.ArmorOfFaith, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
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
                if (healTarget != null && healInfo.GetEffectiveHeal() != 0)
                    if (healTarget.GetHealth() >= healTarget.GetMaxHealth())
                        return true;
            }

            return false;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.ItemEfficiency, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
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

            SpellInfo spellInfo = SpellMgr.GetSpellInfo(SpellIds.BlessedHealing, GetCastDifficulty());
            int amount = MathFunctions.CalculatePct((int)(healInfo.GetHeal()), aurEff.GetAmount());

            Cypher.Assert(spellInfo.GetMaxTicks() > 0);
            amount /= (int)spellInfo.GetMaxTicks();

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.BlessedHealing, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 200128 - Trail of Light
    class spell_pri_trail_of_light : AuraScript
    {
        Queue<ObjectGuid> _healQueue = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TrailOfLightHeal);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (_healQueue.Count == 0 || _healQueue.Last() != eventInfo.GetActionTarget().GetGUID())
                _healQueue.Enqueue(eventInfo.GetActionTarget().GetGUID());

            if (_healQueue.Count > 2)
                _healQueue.Dequeue();

            if (_healQueue.Count == 2)
                return true;

            return false;
        }

        void HandleOnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit caster = GetTarget();
            Unit oldTarget = ObjAccessor.GetUnit(caster, _healQueue.First());
            if (oldTarget == null)
                return;

            // Note: old target may not be friendly anymore due to charm and faction change effects.
            if (!caster.IsValidAssistTarget(oldTarget))
                return;

            SpellInfo healSpellInfo = SpellMgr.GetSpellInfo(SpellIds.TrailOfLightHeal, Difficulty.None);
            if (healSpellInfo == null)
                return;

            // Note: distance may be greater than the heal's spell range.
            if (!caster.IsWithinDist(oldTarget, healSpellInfo.GetMaxRange(true, caster)))
                return;

            uint healAmount = MathFunctions.CalculatePct(eventInfo.GetHealInfo().GetHeal(), aurEff.GetAmount());

            caster.CastSpell(oldTarget, SpellIds.TrailOfLightHeal, new CastSpellExtraArgs(aurEff).AddSpellMod(SpellValueMod.BasePoint0, (int)healAmount));
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleOnProc, 0, AuraType.Dummy));
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
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 15286 - Vapiric Embrace
    class spell_pri_vapiric_embrace : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VapiricEmbraceHeal);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            // Not proc from Mind Sear
            return (eventInfo.GetDamageInfo().GetSpellInfo().SpellFamilyFlags[1] & 0x80000) == 0;
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
            GetTarget().CastSpell(null, SpellIds.VapiricEmbraceHeal, args);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 15290 - Vapiric Embrace (heal)
    class spell_pri_vapiric_embrace_target : SpellScript
    {
        void FilterTargets(List<WorldObject> unitList)
        {
            unitList.Remove(GetCaster());
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitCasterAreaParty));
        }
    }

    [Script] // 34914 - VaMathF.PIric Touch
    class spell_pri_vapiric_touch : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VapiricTouchDispel, SpellIds.GenReplenishment);
        }

        void HandleDispel(DispelInfo dispelInfo)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Unit target = GetUnitOwner();
                if (target != null)
                {
                    AuraEffect aurEff = GetEffect(1);
                    if (aurEff != null)
                    {
                        // backfire damage
                        int bp = aurEff.GetAmount();
                        bp = target.SpellDamageBonusTaken(caster, aurEff.GetSpellInfo(), bp, DamageEffectType.DOT);
                        bp *= 8;

                        CastSpellExtraArgs args = new(aurEff);
                        args.AddSpellMod(SpellValueMod.BasePoint0, bp);
                        caster.CastSpell(target, SpellIds.VapiricTouchDispel, args);
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
            eventInfo.GetProcTarget().CastSpell(null, SpellIds.GenReplenishment, aurEff);
        }

        public override void Register()
        {
            AfterDispel.Add(new(HandleDispel));
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleEffectProc, 2, AuraType.Dummy));
        }
    }
}