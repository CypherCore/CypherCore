// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Azerite
{
    [Script]
    class spell_azerite_gen_aura_calc_from_2nd_effect_triggered_spell : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1)) && ValidateSpellInfo(spellInfo.GetEffect(1).TriggerSpell);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                amount = 0;
                canBeRecalculated = false;
                foreach (var (_, aurApp) in caster.GetAppliedAuras().Where(pair => pair.Key == GetEffectInfo(1).TriggerSpell))
                    if (aurApp.HasEffect(0))
                        amount += aurApp.GetBase().GetEffect(0).GetAmount();
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.ModRating));
        }
    }

    [Script] // 270658 - Azerite Fortification
    class spell_item_azerite_fortification : AuraScript
    {
        bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();
            if (procSpell == null)
                return false;

            return procSpell.GetSpellInfo().HasAura(AuraType.ModStun)
                || procSpell.GetSpellInfo().HasAura(AuraType.ModRoot)
                || procSpell.GetSpellInfo().HasAura(AuraType.ModRoot2)
                || procSpell.GetSpellInfo().HasEffect(SpellEffectName.KnockBack);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 271548 - Strength in Numbers
    class spell_item_strength_in_numbers : SpellScript
    {
        const uint SpellStrengthInNumbersTrait = 271546;
        const uint SpellStrengthInNumbersBuff = 271550;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellStrengthInNumbersTrait, SpellStrengthInNumbersBuff);
        }

        void TriggerHealthBuff()
        {
            AuraEffect trait = GetCaster().GetAuraEffect(SpellStrengthInNumbersTrait, 0, GetCaster().GetGUID());
            if (trait != null)
            {
                long enemies = GetUnitTargetCountForEffect(0);
                if (enemies != 0)
                    GetCaster().CastSpell(GetCaster(), SpellStrengthInNumbersBuff, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                        .AddSpellMod(SpellValueMod.BasePoint0, trait.GetAmount())
                        .AddSpellMod(SpellValueMod.AuraStack, (int)enemies));
            }
        }

        public override void Register()
        {
            AfterHit.Add(new(TriggerHealthBuff));
        }
    }

    [Script] // 271843 - Blessed Portents
    class spell_item_blessed_portents : AuraScript
    {
        const uint SpellBlessedPortentsTrait = 267889;
        const uint SpellBlessedPortentsHeal = 280052;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBlessedPortentsTrait, SpellBlessedPortentsHeal);
        }

        void CheckProc(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            if (GetTarget().HealthBelowPctDamaged(50, dmgInfo.GetDamage()))
            {
                Unit caster = GetCaster();
                if (caster != null)
                {
                    AuraEffect trait = caster.GetAuraEffect(SpellBlessedPortentsTrait, 0, caster.GetGUID());
                    if (trait != null)
                        caster.CastSpell(GetTarget(), SpellBlessedPortentsHeal, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                .AddSpellMod(SpellValueMod.BasePoint0, trait.GetAmount()));
                }
            }
            else
                PreventDefaultAction();
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new(CheckProc, 0));
        }
    }

    [Script] // 272260 - Concentrated Mending
    class spell_item_concentrated_mending : AuraScript
    {
        const uint SpellConcentratedMendingTrait = 267882;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellConcentratedMendingTrait);
        }

        void RecalculateHealAmount(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                AuraEffect trait = caster.GetAuraEffect(SpellConcentratedMendingTrait, 0, caster.GetGUID());
                if (trait != null)
                    aurEff.ChangeAmount((int)(trait.GetAmount() * aurEff.GetTickNumber()));
            }
        }

        public override void Register()
        {
            OnEffectUpdatePeriodic.Add(new(RecalculateHealAmount, 0, AuraType.PeriodicHeal));
        }
    }

    [Script] // 272276 - Bracing Chill
    class spell_item_bracing_chill_proc : AuraScript
    {
        const uint SpellBracingChillTrait = 267884;
        const uint SpellBracingChillHeal = 272428;
        const uint SpellBracingChillSearchJumpTarget = 272436;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBracingChillTrait, SpellBracingChillHeal, SpellBracingChillSearchJumpTarget);
        }

        bool CheckHealCaster(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return GetCasterGUID() == eventInfo.GetActor().GetGUID();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit caster = procInfo.GetActor();
            if (caster == null)
                return;

            AuraEffect trait = caster.GetAuraEffect(SpellBracingChillTrait, 0, caster.GetGUID());
            if (trait != null)
                caster.CastSpell(procInfo.GetProcTarget(), SpellBracingChillHeal,
                    new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, trait.GetAmount()));

            if (GetStackAmount() > 1)
                caster.CastSpell(null, SpellBracingChillSearchJumpTarget,
                    new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, GetStackAmount() - 1));

            Remove();
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckHealCaster, 0, AuraType.Dummy));
            AfterEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 272436 - Bracing Chill
    class spell_item_bracing_chill_search_jump_target : SpellScript
    {
        const uint SpellBracingChill = 272276;

        void FilterTarget(List<WorldObject> targets)
        {
            if (targets.Empty())
                return;

            List<WorldObject> copy = new(targets);
            copy.RandomResize(target => target.IsUnit() && !target.ToUnit().HasAura(SpellBracingChill, GetCaster().GetGUID()), 1);

            if (!copy.Empty())
            {
                // found a preferred target, use that
                targets = copy;
                return;
            }

            WorldObject target = targets.SelectRandom();
            targets.Clear();
            targets.Add(target);
        }

        void MoveAura(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellBracingChill,
                new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, GetSpellValue().AuraStackAmount));
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTarget, 0, Targets.UnitDestAreaAlly));
            OnEffectHitTarget.Add(new(MoveAura, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 272837 - Trample the Weak
    class spell_item_trample_the_weak : AuraScript
    {
        bool CheckHealthPct(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetActor().GetHealthPct() > eventInfo.GetActionTarget().GetHealthPct();
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckHealthPct, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 272892 - Wracking Brilliance
    class spell_item_wracking_brilliance : AuraScript
    {
        const uint SpellAgonySoulShardGain = 210067;

        bool _canTrigger = true;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellAgonySoulShardGain);
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null)
                return false;

            if (spellInfo.Id != SpellAgonySoulShardGain)
                return false;

            _canTrigger = !_canTrigger; // every other soul shard gain
            return _canTrigger;
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 275514 - Orbital Precision
    class spell_item_orbital_precision : AuraScript
    {
        const uint SpellMageFrozenOrb = 84714;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellMageFrozenOrb);
        }

        bool CheckFrozenOrbActive(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetActor().GetAreaTrigger(SpellMageFrozenOrb) != null;
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckFrozenOrbActive, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 277966 - Blur of Talons
    class spell_item_blur_of_talons : AuraScript
    {
        const uint SpellHunterCoordinatedAssault = 266779;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellHunterCoordinatedAssault);
        }

        bool CheckCoordinatedAssaultActive(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetActor().HasAura(SpellHunterCoordinatedAssault, eventInfo.GetActor().GetGUID());
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckCoordinatedAssaultActive, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 278519 - Divine Right
    class spell_item_divine_right : AuraScript
    {
        bool CheckHealthPct(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget().HasAuraState(AuraStateType.Wounded20Percent, eventInfo.GetSpellInfo(), eventInfo.GetActor());
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckHealthPct, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 280409 - Blood Rite
    class spell_item_blood_rite : AuraScript
    {
        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            RefreshDuration();
        }

        public override void Register()
        {
            AfterEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
        }
    }

    [Script] // 281843 - Tradewinds
    class spell_item_tradewinds : AuraScript
    {
        const uint SpellTradewindsAllyBuff = 281844;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellTradewindsAllyBuff);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            AuraEffect trait = GetTarget().GetAuraEffect(GetEffectInfo(1).TriggerSpell, 1);
            if (trait != null)
                GetTarget().CastSpell(null, SpellTradewindsAllyBuff,
                    new CastSpellExtraArgs(aurEff).AddSpellMod(SpellValueMod.BasePoint0, trait.GetAmount()));
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.ModRating, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 287379 - Bastion of Might
    class spell_item_bastion_of_might : SpellScript
    {
        const uint SpellWarriorIgnorePain = 190456;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellWarriorIgnorePain);
        }

        void TriggerIgnorePain()
        {
            GetCaster().CastSpell(GetCaster(), SpellWarriorIgnorePain, GetSpell());
        }

        public override void Register()
        {
            AfterHit.Add(new(TriggerIgnorePain));
        }
    }

    [Script] // 287650 - Echoing Blades
    class spell_item_echoing_blades : AuraScript
    {
        ObjectGuid _lastFanOfKnives;

        void PrepareProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetProcSpell() != null)
            {
                if (eventInfo.GetProcSpell().m_castId != _lastFanOfKnives)
                    GetEffect(0).RecalculateAmount();

                _lastFanOfKnives = eventInfo.GetProcSpell().m_castId;
            }
        }

        bool CheckFanOfKnivesCounter(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return aurEff.GetAmount() > 0;
        }

        void ReduceCounter(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            aurEff.SetAmount(aurEff.GetAmount() - 1);
        }

        public override void Register()
        {
            DoPrepareProc.Add(new(PrepareProc));
            DoCheckEffectProc.Add(new(CheckFanOfKnivesCounter, 0, AuraType.ProcTriggerSpell));
            AfterEffectProc.Add(new(ReduceCounter, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 287653 - Echoing Blades
    class spell_item_echoing_blades_damage : SpellScript
    {
        const uint SpellEchoingBladesTrait = 287649;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellEchoingBladesTrait, 2));
        }

        void CalculateDamage(Unit victim, ref int damage, ref int flatMod, ref float pctMod)
        {
            AuraEffect trait = GetCaster().GetAuraEffect(SpellEchoingBladesTrait, 2);
            if (trait != null)
                damage = trait.GetAmount() * 2;
        }

        void ForceCritical(Unit victim, ref float critChance)
        {
            critChance = 100.0f;
        }

        public override void Register()
        {
            CalcDamage.Add(new(CalculateDamage));
            OnCalcCritChance.Add(new(ForceCritical));
        }
    }

    [Script] // 288882 - Hour of Reaping
    class spell_item_hour_of_reaping : AuraScript
    {
        const uint SpellDhSoulBarrier = 263648;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellDhSoulBarrier);
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return GetStackAmount() == GetAura().CalcMaxStackAmount();
        }

        void TriggerSoulBarrier(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            GetTarget().CastSpell(GetTarget(), SpellDhSoulBarrier, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
            AfterEffectProc.Add(new(TriggerSoulBarrier, 0, AuraType.Dummy));
        }
    }

    [Script] // 304086  - Azerite Fortification
    class spell_item_conflict_wearer_on_stun_proc : AuraScript
    {
        bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();
            if (procSpell == null)
                return false;

            return procSpell.GetSpellInfo().HasAura(AuraType.ModStun)
                || procSpell.GetSpellInfo().HasAura(AuraType.ModStunDisableGravity);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 305723 - Strife (Azerite Essence)
    class spell_item_conflict_rank3 : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetHitMask().HasAnyFlag(ProcFlagsHit.Interrupt | ProcFlagsHit.Dispel))
                return true;

            Spell procSpell = eventInfo.GetProcSpell();
            if (procSpell == null)
                return false;

            bool isCrowdControl = procSpell.GetSpellInfo().HasAura(AuraType.ModConfuse)
                || procSpell.GetSpellInfo().HasAura(AuraType.ModFear)
                || procSpell.GetSpellInfo().HasAura(AuraType.ModStun)
                || procSpell.GetSpellInfo().HasAura(AuraType.ModPacify)
                || procSpell.GetSpellInfo().HasAura(AuraType.ModRoot)
                || procSpell.GetSpellInfo().HasAura(AuraType.ModSilence)
                || procSpell.GetSpellInfo().HasAura(AuraType.ModPacifySilence)
                || procSpell.GetSpellInfo().HasAura(AuraType.ModRoot2);

            if (!isCrowdControl)
                return false;

            return eventInfo.GetActionTarget().HasAura(aura => aura.GetCastId() == procSpell.m_castId);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script] // 277253 - Heart of Azeroth
    class spell_item_heart_of_azeroth : AuraScript
    {
        void SetEquippedFlag(AuraEffect effect, AuraEffectHandleModes mode)
        {
            SetState(true);
        }

        void ClearEquippedFlag(AuraEffect effect, AuraEffectHandleModes mode)
        {
            SetState(false);
        }

        void SetState(bool equipped)
        {
            Player target = GetTarget().ToPlayer();
            if (target != null)
            {
                target.ApplyAllAzeriteEmpoweredItemMods(equipped);

                PlayerAzeriteItemEquippedStatusChanged statusChanged = new();
                statusChanged.IsHeartEquipped = equipped;
                target.SendPacket(statusChanged);
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new(SetEquippedFlag, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(ClearEquippedFlag, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 315176 - Grasping Tendrils
    class spell_item_corruption_grasping_tendrils : AuraScript
    {
        public override bool Load()
        {
            return GetUnitOwner().IsPlayer();
        }

        void CalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player player = GetUnitOwner().ToPlayer();
            amount = (int)MathFunctions.Clamp(10.0f + player.GetRatingBonusValue(CombatRating.Corruption) - player.GetRatingBonusValue(CombatRating.CorruptionResistance), 0.0f, 99.0f);
            canBeRecalculated = false;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalcAmount, 0, AuraType.ModDecreaseSpeed));
        }
    }
}