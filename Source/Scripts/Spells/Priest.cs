/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Priest
{
    struct SpellIds
    {
        public const uint Absolution = 33167;
        public const uint AngelicFeatherAreatrigger = 158624;
        public const uint AngelicFeatherAura = 121557;
        public const uint AngelicFeatherTrigger = 121536;
        public const uint ArmorOfFaith = 28810;
        public const uint Atonement = 81749;
        public const uint AtonementHeal = 81751;
        public const uint AtonementTriggered = 194384;
        public const uint BlessedHealing = 70772;
        public const uint BodyAndSoul = 64129;
        public const uint BodyAndSoulDispel = 64136;
        public const uint BodyAndSoulSpeed = 65081;
        public const uint CureDisease = 528;
        public const uint DispelMagicFriendly = 97690;
        public const uint DispelMagicHostile = 97691;
        public const uint DivineAegis = 47753;
        public const uint DivineBlessing = 40440;
        public const uint DivineWrath = 40441;
        public const uint GlyphOfCircleOfHealing = 55675;
        public const uint GlyphOfDispelMagic = 55677;
        public const uint GlyphOfDispelMagicHeal = 56131;
        public const uint GlyphOfLightwell = 55673;
        public const uint GlyphOfPrayerOfHealingHeal = 56161;
        public const uint GlyphOfShadow = 107906;
        public const uint GuardianSpiritHeal = 48153;
        public const uint ItemEfficiency = 37595;
        public const uint LeapOfFaith = 73325;
        public const uint LeapOfFaithEffect = 92832;
        public const uint LeapOfFaithEffectTrigger = 92833;
        public const uint LeapOfFaithTriggered = 92572;
        public const uint LevitateEffect = 111759;
        public const uint ManaLeechProc = 34650;
        public const uint OracularHeal = 26170;
        public const uint PenanceR1 = 47540;
        public const uint PenanceR1Damage = 47758;
        public const uint PenanceR1Heal = 47757;
        public const uint RenewedHope = 197469;
        public const uint RenewedHopeEffect = 197470;
        public const uint ShadowformVisualWithGlyph = 107904;
        public const uint ShadowformVisualWithoutGlyph = 107903;
        public const uint ShieldDisciplineEnergize = 47755;
        public const uint ShieldDisciplinePassive = 197045;
        public const uint SpiritOfRedemption = 27827;
        public const uint StrengthOfSoul = 197535;
        public const uint StrengthOfSoulEffect = 197548;
        public const uint T9Healing2p = 67201;
        public const uint ThePenitentAura = 200347;
        public const uint VampiricEmbraceHeal = 15290;
        public const uint VampiricTouchDispel = 64085;
        public const uint VoidShield = 199144;
        public const uint VoidShieldEffect = 199145;

        public const uint GenReplenishment = 57669;
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

            int amount = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), 10);
            caster.CastCustomSpell(SpellIds.OracularHeal, SpellValueMod.BasePoint0, amount, caster, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 81749 - Atonement
    public class spell_pri_atonement : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AtonementHeal);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo() != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            int heal = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            _appliedAtonements.RemoveAll(targetGuid =>
            {
                Unit target = Global.ObjAccessor.GetUnit(GetTarget(), targetGuid);
                if (target)
                {
                    if (target.GetExactDist(GetTarget()) < GetSpellInfo().GetEffect(1).CalcValue())
                        GetTarget().CastCustomSpell(SpellIds.AtonementHeal, SpellValueMod.BasePoint0, heal, target, true);

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

        List<ObjectGuid> _appliedAtonements = new List<ObjectGuid>();

        public void AddAtonementTarget(ObjectGuid target)
        {
            _appliedAtonements.Add(target);
        }

        public void RemoveAtonementTarget(ObjectGuid target)
        {
            _appliedAtonements.Remove(target);
        }
    }

    [Script] // 194384 - Atonement
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
                    var script = atonement.GetScript<spell_pri_atonement>("spell_pri_atonement");
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
                    var script = atonement.GetScript<spell_pri_atonement>("spell_pri_atonement");
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

    [Script] // 64129 - Body and Soul
    class spell_pri_body_and_soul : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CureDisease, SpellIds.BodyAndSoulDispel);
        }

        void HandleEffectSpeedProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            // Proc only with Power Word: Shield or Leap of Faith
            if (!(eventInfo.GetDamageInfo().GetSpellInfo().SpellFamilyFlags[0].HasAnyFlag(0x1u) || eventInfo.GetDamageInfo().GetSpellInfo().SpellFamilyFlags[2].HasAnyFlag(0x80000u)))
                return;

            GetTarget().CastCustomSpell(SpellIds.BodyAndSoulSpeed, SpellValueMod.BasePoint0, aurEff.GetAmount(), eventInfo.GetProcTarget(), true, null, aurEff);
        }

        void HandleEffectDispelProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            // Proc only with Cure Disease
            if (eventInfo.GetDamageInfo().GetSpellInfo().Id != SpellIds.CureDisease || eventInfo.GetProcTarget() != GetTarget())
                return;

            if (RandomHelper.randChance(aurEff.GetAmount()))
                GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.BodyAndSoulDispel, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectSpeedProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectDispelProc, 1, AuraType.Dummy));
        }
    }

    // 34861 - Circle of Healing
    [Script]
    class spell_pri_circle_of_healing : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfCircleOfHealing);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
            {
                Unit target = obj.ToUnit();
                if (target)
                    return !GetCaster().IsInRaidWith(target);

                return true;
            });

            uint maxTargets = (uint)(GetCaster().HasAura(SpellIds.GlyphOfCircleOfHealing) ? 6 : 5); // Glyph of Circle of Healing

            if (targets.Count > maxTargets)
            {
                targets.Sort(new HealthPctOrderPred());
                targets.Resize(maxTargets);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    // 527 - Dispel magic
    [Script]
    class spell_pri_dispel_magic : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Absolution, SpellIds.GlyphOfDispelMagicHeal, SpellIds.GlyphOfDispelMagic);
        }

        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();

            if (!target || (!caster.HasAura(SpellIds.Absolution) && caster != target && target.IsFriendlyTo(caster)))
                return SpellCastResult.BadTargets;
            return SpellCastResult.SpellCastOk;
        }

        void AfterEffectHit(uint effIndex)
        {
            if (GetHitUnit().IsFriendlyTo(GetCaster()))
            {
                GetCaster().CastSpell(GetHitUnit(), SpellIds.DispelMagicFriendly, true);
                AuraEffect aurEff = GetHitUnit().GetAuraEffect(SpellIds.GlyphOfDispelMagic, 0);
                if (aurEff != null)
                {
                    int heal = (int)GetHitUnit().CountPctFromMaxHealth(aurEff.GetAmount());
                    GetCaster().CastCustomSpell(SpellIds.GlyphOfDispelMagicHeal, SpellValueMod.BasePoint0, heal, GetHitUnit());
                }
            }
            else
                GetCaster().CastSpell(GetHitUnit(), SpellIds.DispelMagicHostile, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
            OnEffectHitTarget.Add(new EffectHandler(AfterEffectHit, 0, SpellEffectName.Dummy));
        }
    }

    // -47509 - Divine Aegis
    [Script]
    class spell_pri_divine_aegis : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DivineAegis);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return;

            int absorb = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());

            // Multiple effects stack, so let's try to find this aura.
            AuraEffect aegis = eventInfo.GetProcTarget().GetAuraEffect(SpellIds.DivineAegis, 0);
            if (aegis != null)
                absorb += aegis.GetAmount();

            absorb = (int)Math.Min(absorb, eventInfo.GetProcTarget().getLevel() * 125);

            GetTarget().CastCustomSpell(SpellIds.DivineAegis, SpellValueMod.BasePoint0, absorb, eventInfo.GetProcTarget(), true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 64844 - Divine Hymn
    [Script]
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

    // 55680 - Glyph of Prayer of Healing
    [Script]
    class spell_pri_glyph_of_prayer_of_healing : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfPrayerOfHealingHeal);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return;

            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.GlyphOfPrayerOfHealingHeal);
            int heal = (int)(MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount()) / triggeredSpellInfo.GetMaxTicks(Difficulty.None));
            GetTarget().CastCustomSpell(SpellIds.GlyphOfPrayerOfHealingHeal, SpellValueMod.BasePoint0, heal, eventInfo.GetProcTarget(), true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 24191 - Improved Power Word Shield
    class spell_pri_improved_power_word_shield : AuraScript
    {
        void HandleEffectCalcSpellMod(AuraEffect aurEff, ref SpellModifier spellMod)
        {
            if (spellMod == null)
            {
                spellMod = new SpellModifier(GetAura());
                spellMod.op = (SpellModOp)aurEff.GetMiscValue();
                spellMod.type = SpellModType.Pct;
                spellMod.spellId = GetId();
                spellMod.mask = GetSpellInfo().GetEffect(aurEff.GetEffIndex()).SpellClassMask;
            }

            spellMod.value = aurEff.GetAmount();
        }

        public override void Register()
        {
            DoEffectCalcSpellMod.Add(new EffectCalcSpellModHandler(HandleEffectCalcSpellMod, 0, AuraType.Dummy));
        }
    }

    // 47788 - Guardian Spirit
    [Script]
    class spell_pri_guardian_spirit : AuraScript
    {
        uint healPct;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GuardianSpiritHeal);
        }

        public override bool Load()
        {
            healPct = (uint)GetSpellInfo().GetEffect(1).CalcValue();
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
            // remove the aura now, we don't want 40% healing bonus
            Remove(AuraRemoveMode.EnemySpell);
            target.CastCustomSpell(target, SpellIds.GuardianSpiritHeal, healAmount, 0, 0, true);
            absorbAmount = dmgInfo.GetDamage();
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
            OnEffectAbsorb.Add(new EffectAbsorbHandler(Absorb, 1));
        }
    }

    // 64904 - Hymn of Hope
    [Script]
    class spell_pri_hymn_of_hope : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
            {
                Unit target = obj.ToUnit();
                if (target)
                    return target.GetPowerType() != PowerType.Mana;

                return true;
            });

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
                targets.Sort(new PowerPctOrderPred(PowerType.Mana));
                targets.Resize(maxTargets);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, SpellConst.EffectAll, Targets.UnitSrcAreaAlly));
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

    // 92833 - Leap of Faith
    [Script]
    class spell_pri_leap_of_faith_effect_trigger : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LeapOfFaithEffect);
        }

        void HandleEffectDummy(uint effIndex)
        {
            Position destPos = GetHitDest().GetPosition();

            SpellCastTargets targets = new SpellCastTargets();
            targets.SetDst(destPos);
            targets.SetUnitTarget(GetCaster());
            GetHitUnit().CastSpell(targets, Global.SpellMgr.GetSpellInfo((uint)GetEffectValue()), null);
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

    // 7001 - Lightwell Renew
    [Script]
    class spell_pri_lightwell_renew : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                // Bonus from Glyph of Lightwell
                AuraEffect modHealing = caster.GetAuraEffect(SpellIds.GlyphOfLightwell, 0);
                if (modHealing != null)
                    MathFunctions.AddPct(ref amount, modHealing.GetAmount());
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicHeal));
        }
    }

    // 8129 - Mana Burn
    [Script]
    class spell_pri_mana_burn : SpellScript
    {
        void HandleAfterHit()
        {
            Unit unitTarget = GetHitUnit();
            if (unitTarget)
                unitTarget.RemoveAurasWithMechanic((1 << (int)Mechanics.Fear) | (1 << (int)Mechanics.Polymorph));
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleAfterHit));
        }
    }

    // 28305 - Mana Leech (Passive) (Priest Pet Aura)
    [Script]
    class spell_pri_mana_leech : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ManaLeechProc);
        }

        public override bool Load()
        {
            _procTarget = null;
            return true;
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            _procTarget = GetTarget().GetOwner();
            return _procTarget;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(_procTarget, SpellIds.ManaLeechProc, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }

        Unit _procTarget;
    }

    // 47948 - Pain and Suffering (Proc)
    [Script]
    class spell_pri_pain_and_suffering_proc : SpellScript
    {
        void HandleEffectScriptEffect(uint effIndex)
        {
            // Refresh Shadow Word: Pain on target
            Unit unitTarget = GetHitUnit();
            if (unitTarget)
            {
                AuraEffect aur = unitTarget.GetAuraEffect(AuraType.PeriodicDamage, SpellFamilyNames.Priest, new FlagArray128(0x8000, 0, 0), GetCaster().GetGUID());
                if (aur != null)
                    aur.GetBase().RefreshDuration();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleEffectScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 47540 - Penance
    [Script]
    class spell_pri_penance : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            SpellInfo firstRankSpellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.PenanceR1);
            if (firstRankSpellInfo == null)
                return false;

            // can't use other spell than this penance due to spell_ranks dependency
            if (!spellInfo.IsRankOf(firstRankSpellInfo))
                return false;

            byte rank = spellInfo.GetRank();
            if (Global.SpellMgr.GetSpellWithRank(SpellIds.PenanceR1Damage, rank, true) == 0)
                return false;
            if (Global.SpellMgr.GetSpellWithRank(SpellIds.PenanceR1Heal, rank, true) == 0)
                return false;

            return true;
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target)
            {
                if (!target.IsAlive())
                    return;

                byte rank = GetSpellInfo().GetRank();

                if (caster.IsFriendlyTo(target))
                    caster.CastSpell(target, Global.SpellMgr.GetSpellWithRank(SpellIds.PenanceR1Heal, rank), false);
                else
                    caster.CastSpell(target, Global.SpellMgr.GetSpellWithRank(SpellIds.PenanceR1Damage, rank), false);
            }
        }

        SpellCastResult CheckCast()
        {
            Player caster = GetCaster().ToPlayer();
            Unit target = GetExplTargetUnit();
            if (target)
            {
                if (!caster.IsFriendlyTo(target))
                {
                    if (!caster.IsValidAttackTarget(target))
                        return SpellCastResult.BadTargets;

                    if (!caster.isInFront(target))
                        return SpellCastResult.NotInfront;
                }
                else
                {
                    //Support for modifications of this spell in Legion with The Penitent talent (7.1.5)
                    if (!caster.HasAura(SpellIds.ThePenitentAura))
                        return SpellCastResult.BadTargets;

                    if (!caster.isInFront(target))
                        return SpellCastResult.UnitNotInfront;
                }
            }
            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
        }
    }

    // -47569 - Phantasm
    [Script]
    class spell_pri_phantasm : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(GetEffect(0).GetAmount());
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().RemoveMovementImpairingAuras();
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 17 - Power Word: Shield
    class spell_pri_power_word_shield : AuraScript
    {
        void CalculateAmount(AuraEffect auraEffect, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;

            Player player = GetCaster().ToPlayer();
            if (player)
            {
                int playerMastery = (int)player.GetRatingBonusValue(CombatRating.Mastery);
                int playerSpellPower = player.SpellBaseDamageBonusDone(SpellSchoolMask.Holy);
                int playerVersatileDamage = (int)player.GetRatingBonusValue(CombatRating.VersatilityDamageDone);

                //Formula taken from SpellWork
                amount = (int)((playerSpellPower * 5.5f) + playerMastery) * (1 + playerVersatileDamage);
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
                caster.CastSpell(target, SpellIds.AtonementTriggered, true);
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

    [Script] // 33110 - Prayer of Mending Heal
    class spell_pri_prayer_of_mending_heal : SpellScript
    {
        void HandleHeal(uint effIndex)
        {
            Unit caster = GetOriginalCaster();
            if (caster)
            {
                AuraEffect aurEff = caster.GetAuraEffect(SpellIds.T9Healing2p, 0);
                if (aurEff != null)
                {
                    int heal = GetHitHeal();
                    MathFunctions.AddPct(ref heal, aurEff.GetAmount());
                    SetHitHeal(heal);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal));
        }
    }

    // 15473 - Shadowform
    [Script]
    class spell_pri_shadowform : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShadowformVisualWithoutGlyph, SpellIds.ShadowformVisualWithGlyph);
        }

        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), GetTarget().HasAura(SpellIds.GlyphOfShadow) ? SpellIds.ShadowformVisualWithGlyph : SpellIds.ShadowformVisualWithoutGlyph, true);
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(GetTarget().HasAura(SpellIds.GlyphOfShadow) ? SpellIds.ShadowformVisualWithGlyph : SpellIds.ShadowformVisualWithoutGlyph);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.ModShapeshift, AuraEffectHandleModes.RealOrReapplyMask));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script] // 20711 - Spirit of Redemption
    class spell_priest_spirit_of_redemption : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SpiritOfRedemption);
        }

        void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            Unit target = GetTarget();
            if (dmgInfo.GetDamage() >= target.GetHealth())
            {
                target.CastSpell(target, SpellIds.SpiritOfRedemption, TriggerCastFlags.FullMask, null, aurEff);
                target.SetFullHealth();
                return;
            }

            PreventDefaultAction();
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new EffectAbsorbHandler(HandleAbsorb, 0));
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
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.ArmorOfFaith, true, null, aurEff);
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
            GetTarget().CastSpell(GetTarget(), SpellIds.ItemEfficiency, true, null, aurEff);
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

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.BlessedHealing);
            int amount = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks(Difficulty.None);

            // Add remaining ticks to healing done
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();
            amount += (int)target.GetRemainingPeriodicAmount(caster.GetGUID(), SpellIds.BlessedHealing, AuraType.PeriodicHeal);

            caster.CastCustomSpell(SpellIds.BlessedHealing, SpellValueMod.BasePoint0, amount, target, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 15286 - Vampiric Embrace
    [Script]
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

            GetTarget().CastCustomSpell(null, SpellIds.VampiricEmbraceHeal, teamHeal, selfHeal, 0, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    // 15290 - Vampiric Embrace (heal)
    [Script]
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

    // 34914 - Vampiric Touch
    [Script]
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
                        int damage = aurEff.GetAmount() * 8;
                        // backfire damage
                        caster.CastCustomSpell(target, SpellIds.VampiricTouchDispel, damage, 0, 0, true, null, aurEff);
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
            eventInfo.GetProcTarget().CastSpell((Unit)null, SpellIds.GenReplenishment, true, null, aurEff);
        }

        public override void Register()
        {
            AfterDispel.Add(new AuraDispelHandler(HandleDispel));
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 2, AuraType.Dummy));
        }
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
                SpellCastTargets targets = new SpellCastTargets();
                targets.SetDst(destPos);
                GetCaster().CastSpell(targets, Global.SpellMgr.GetSpellInfo(SpellIds.AngelicFeatherAreatrigger), null);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // Angelic Feather areatrigger - created by SPELL_PRIEST_ANGELIC_FEATHER_AREATRIGGER
    class areatrigger_pri_angelic_feather : AreaTriggerEntityScript
    {
        public areatrigger_pri_angelic_feather() : base("areatrigger_pri_angelic_feather") { }

        class areatrigger_pri_angelic_featherAI : AreaTriggerAI
        {
            public areatrigger_pri_angelic_featherAI(AreaTrigger areatrigger) : base(areatrigger) { }

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

        public override AreaTriggerAI GetAI(AreaTrigger areatrigger)
        {
            return new areatrigger_pri_angelic_featherAI(areatrigger);
        }
    }
}