// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using static Global;

namespace Scripts.Spells.Hunter
{
    struct SpellIds
    {
        public const uint AMurderOfCrowsDamage = 131900;
        public const uint AMurderOfCrowsVisual1 = 131637;
        public const uint AMurderOfCrowsVisual2 = 131951;
        public const uint AMurderOfCrowsVisual3 = 131952;
        public const uint AspectCheetahSlow = 186258;
        public const uint Exhilaration = 109304;
        public const uint ExhilarationPet = 128594;
        public const uint ExhilarationR2 = 231546;
        public const uint ExplosiveShotDamage = 212680;
        public const uint LatentPoisonStack = 378015;
        public const uint LatentPoisonDamage = 378016;
        public const uint LatentPoisonInjectorsStack = 336903;
        public const uint LatentPoisonInjectorsDamage = 336904;
        public const uint LoneWolf = 155228;
        public const uint MastersCallTriggered = 62305;
        public const uint Misdirection = 34477;
        public const uint MisdirectionProc = 35079;
        public const uint MultiShotFocus = 213363;
        public const uint PetLastStandTriggered = 53479;
        public const uint PetHeartOfThePhoenixTriggered = 54114;
        public const uint PetHeartOfThePhoenixDebuff = 55711;
        public const uint PosthasteIncreaseSpeed = 118922;
        public const uint PosthasteTalent = 109215;
        public const uint RapidFireDamage = 257045;
        public const uint RapidFireEnergize = 263585;
        public const uint SteadyShotFocus = 77443;
        public const uint T94PGreatness = 68130;
        public const uint T292PMarksmanshipDamage = 394371;
        public const uint RoarOfSacrificeTriggered = 67481;
        public const uint DraeneiGiftOfTheNaaru = 59543;
    }

    [Script] // 131894 - A Murder of Crows
    class spell_hun_a_murder_of_crows : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AMurderOfCrowsDamage, SpellIds.AMurderOfCrowsVisual1, SpellIds.AMurderOfCrowsVisual2, SpellIds.AMurderOfCrowsVisual3);
        }

        void HandleDummyTick(AuraEffect aurEff)
        {
            Unit target = GetTarget();

            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(target, SpellIds.AMurderOfCrowsDamage, true);

            target.CastSpell(target, SpellIds.AMurderOfCrowsVisual1, true);
            target.CastSpell(target, SpellIds.AMurderOfCrowsVisual2, true);
            target.CastSpell(target, SpellIds.AMurderOfCrowsVisual3, true);
            target.CastSpell(target, SpellIds.AMurderOfCrowsVisual3, true); // not a mistake, it is intended to cast twice
        }

        void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Death)
            {
                Unit caster = GetCaster();
                if (caster != null)
                    caster.GetSpellHistory().ResetCooldown(GetId(), true);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleDummyTick, 0, AuraType.PeriodicDummy));
            OnEffectRemove.Add(new(RemoveEffect, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 186257 - Aspect of the Cheetah
    class spell_hun_aspect_cheetah : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AspectCheetahSlow);
        }

        void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
                GetTarget().CastSpell(GetTarget(), SpellIds.AspectCheetahSlow, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(HandleOnRemove, 0, AuraType.ModIncreaseSpeed, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 378750 - Cobra Sting
    class spell_hun_cobra_sting : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1));
        }

        bool RollProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            return RandomHelper.randChance(GetEffect(1).GetAmount());
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(RollProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 109304 - Exhilaration
    class spell_hun_exhilaration : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ExhilarationR2, SpellIds.LoneWolf);
        }

        void HandleOnHit()
        {
            if (GetCaster().HasAura(SpellIds.ExhilarationR2) && !GetCaster().HasAura(SpellIds.LoneWolf))
                GetCaster().CastSpell(null, SpellIds.ExhilarationPet, true);
        }

        public override void Register()
        {
            OnHit.Add(new(HandleOnHit));
        }
    }

    [Script] // 212431 - Explosive Shot
    class spell_hun_explosive_shot : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ExplosiveShotDamage);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(GetTarget(), SpellIds.ExplosiveShotDamage, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 212658 - Hunting Party
    class spell_hun_hunting_party : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Exhilaration, SpellIds.ExhilarationPet);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.Exhilaration, -TimeSpan.FromSeconds(aurEff.GetAmount()));
            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.ExhilarationPet, -TimeSpan.FromSeconds(aurEff.GetAmount()));
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 53478 - Last Stand Pet
    class spell_hun_last_stand_pet : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PetLastStandTriggered);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)caster.CountPctFromMaxHealth(30));
            caster.CastSpell(caster, SpellIds.PetLastStandTriggered, args);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 378016 - Latent Poison
    class spell_hun_latent_poison_damage : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LatentPoisonStack);
        }

        void CalculateDamage()
        {
            Aura stack = GetHitUnit().GetAura(SpellIds.LatentPoisonStack, GetCaster().GetGUID());
            if (stack != null)
            {
                SetHitDamage(GetHitDamage() * stack.GetStackAmount());
                stack.Remove();
            }
        }

        public override void Register()
        {
            OnHit.Add(new(CalculateDamage));
        }
    }

    // 19434 - Aimed Shot
    // 186270 - Raptor Strike
    // 217200 - Barbed Shot
    [Script] // 259387 - Mongoose Bite
    class spell_hun_latent_poison_trigger : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LatentPoisonStack, SpellIds.LatentPoisonDamage);
        }

        void TriggerDamage()
        {
            if (GetHitUnit().HasAura(SpellIds.LatentPoisonStack, GetCaster().GetGUID()))
                GetCaster().CastSpell(GetHitUnit(), SpellIds.LatentPoisonDamage, GetSpell());
        }

        public override void Register()
        {
            AfterHit.Add(new(TriggerDamage));
        }
    }

    [Script] // 336904 - Latent Poison Injectors
    class spell_hun_latent_poison_injectors_damage : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LatentPoisonInjectorsStack);
        }

        void CalculateDamage()
        {
            Aura stack = GetHitUnit().GetAura(SpellIds.LatentPoisonInjectorsStack, GetCaster().GetGUID());
            if (stack != null)
            {
                SetHitDamage(GetHitDamage() * stack.GetStackAmount());
                stack.Remove();
            }
        }

        public override void Register()
        {
            OnHit.Add(new(CalculateDamage));
        }
    }

    // 186270 - Raptor Strike
    [Script] // 259387 - Mongoose Bite
    class spell_hun_latent_poison_injectors_trigger : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LatentPoisonInjectorsStack, SpellIds.LatentPoisonInjectorsDamage);
        }

        void TriggerDamage()
        {
            if (GetHitUnit().HasAura(SpellIds.LatentPoisonInjectorsStack, GetCaster().GetGUID()))
                GetCaster().CastSpell(GetHitUnit(), SpellIds.LatentPoisonInjectorsDamage, GetSpell());
        }

        public override void Register()
        {
            AfterHit.Add(new(TriggerDamage));
        }
    }

    [Script] // 53271 - Masters Call
    class spell_hun_masters_call : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 0))
            && ValidateSpellInfo(SpellIds.MastersCallTriggered, (uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        SpellCastResult DoCheckCast()
        {
            Guardian pet = GetCaster().ToPlayer().GetGuardianPet();
            Cypher.Assert(pet != null); // checked in Spell.CheckCast

            if (!pet.IsPet() || !pet.IsAlive())
                return SpellCastResult.NoPet;

            // Do a mini Spell.CheckCasterAuras on the pet, no other way of doing this
            SpellCastResult result = SpellCastResult.SpellCastOk;
            UnitFlags unitflag = (UnitFlags)(uint)pet.m_unitData.Flags;
            if (!pet.GetCharmerGUID().IsEmpty())
                result = SpellCastResult.Charmed;
            else if (unitflag.HasFlag(UnitFlags.Stunned))
                result = SpellCastResult.Stunned;
            else if (unitflag.HasFlag(UnitFlags.Fleeing))
                result = SpellCastResult.Fleeing;
            else if (unitflag.HasFlag(UnitFlags.Confused))
                result = SpellCastResult.Confused;

            if (result != SpellCastResult.SpellCastOk)
                return result;

            Unit target = GetExplTargetUnit();
            if (target == null)
                return SpellCastResult.BadTargets;

            if (!pet.IsWithinLOSInMap(target))
                return SpellCastResult.LineOfSight;

            return SpellCastResult.SpellCastOk;
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().ToPlayer().GetPet().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }

        void HandleScriptEffect(uint effIndex)
        {
            GetHitUnit().CastSpell(null, SpellIds.MastersCallTriggered, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new(DoCheckCast));

            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
            OnEffectHitTarget.Add(new(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 34477 - Misdirection
    class spell_hun_misdirection : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MisdirectionProc);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Default || GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Interrupt)
                return;

            if (!GetTarget().HasAura(SpellIds.MisdirectionProc))
                GetTarget().GetThreatManager().UnregisterRedirectThreat(SpellIds.Misdirection);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.MisdirectionProc, aurEff);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
        }
    }

    [Script] // 35079 - Misdirection (Proc)
    class spell_hun_misdirection_proc : AuraScript
    {
        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().GetThreatManager().UnregisterRedirectThreat(SpellIds.Misdirection);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 2643 - Multi-Shot
    class spell_hun_multi_shot : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MultiShotFocus);
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleOnHit()
        {
            // We need to check hunter's spec because it doesn't generate focus on other specs than Mm
            if (GetCaster().ToPlayer().GetPrimarySpecialization() == ChrSpecialization.HunterMarksmanship)
                GetCaster().CastSpell(GetCaster(), SpellIds.MultiShotFocus, true);
        }

        public override void Register()
        {
            OnHit.Add(new(HandleOnHit));
        }
    }

    [Script] // 55709 - Pet Heart of the Phoenix
    class spell_hun_pet_heart_of_the_phoenix : SpellScript
    {
        public override bool Load()
        {
            if (!GetCaster().IsPet())
                return false;
            return true;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PetHeartOfThePhoenixTriggered, SpellIds.PetHeartOfThePhoenixDebuff);
        }

        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit owner = caster.GetOwner();
            if (owner != null)
            {
                if (!caster.HasAura(SpellIds.PetHeartOfThePhoenixDebuff))
                {
                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.AddSpellMod(SpellValueMod.BasePoint0, 100);
                    owner.CastSpell(caster, SpellIds.PetHeartOfThePhoenixTriggered, args);
                    caster.CastSpell(caster, SpellIds.PetHeartOfThePhoenixDebuff, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 781 - Disengage
    class spell_hun_posthaste : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PosthasteTalent, SpellIds.PosthasteIncreaseSpeed);
        }

        void HandleAfterCast()
        {
            if (GetCaster().HasAura(SpellIds.PosthasteTalent))
            {
                GetCaster().RemoveMovementImpairingAuras(true);
                GetCaster().CastSpell(GetCaster(), SpellIds.PosthasteIncreaseSpeed, GetSpell());
            }
        }

        public override void Register()
        {
            AfterCast.Add(new(HandleAfterCast));
        }
    }

    [Script] // 257044 - Rapid Fire
    class spell_hun_rapid_fire : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RapidFireDamage);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(GetTarget(), SpellIds.RapidFireDamage, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodic, 1, AuraType.PeriodicDummy));
        }
    }

    [Script] // 257045 - Rapid Fire Damage
    class spell_hun_rapid_fire_damage : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RapidFireEnergize);
        }

        void HandleHit(uint effIndex)
        {
            GetCaster().CastSpell(null, SpellIds.RapidFireEnergize, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 53480 - Roar of Sacrifice
    class spell_hun_roar_of_sacrifice : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RoarOfSacrificeTriggered);
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || (damageInfo.GetSchoolMask() & (SpellSchoolMask)aurEff.GetMiscValue()) == 0)
                return false;

            if (GetCaster() == null)
                return false;

            return true;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()));
            eventInfo.GetActor().CastSpell(GetCaster(), SpellIds.RoarOfSacrificeTriggered, args);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 1, AuraType.Dummy));
            OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
        }
    }

    [Script] // 37506 - Scatter Shot
    class spell_hun_scatter_shot : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            // break var Shot and autohit
            caster.InterruptSpell(CurrentSpellTypes.AutoRepeat);
            caster.AttackStop();
            caster.SendAttackSwingCancelAttack();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 56641 - Steady Shot
    class spell_hun_steady_shot : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SteadyShotFocus);
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleOnHit()
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.SteadyShotFocus, true);
        }

        public override void Register()
        {
            OnHit.Add(new(HandleOnHit));
        }
    }

    [Script] // 1515 - Tame Beast
    class spell_hun_tame_beast : SpellScript
    {
        uint[] CallPetSpellIds = { 883, 83242, 83243, 83244, 83245, };

        SpellCastResult CheckCast()
        {
            Player caster = GetCaster().ToPlayer();
            if (caster == null)
                return SpellCastResult.DontReport;

            if (GetExplTargetUnit() == null)
                return SpellCastResult.BadImplicitTargets;

            Creature target = GetExplTargetUnit().ToCreature();
            if (target != null)
            {
                if (target.GetLevelForTarget(caster) > caster.GetLevel())
                    return SpellCastResult.Highlevel;

                // use SmsgPetTameFailure?
                if (!target.GetCreatureTemplate().IsTameable(caster.CanTameExoticPets(), target.GetCreatureDifficulty()))
                    return SpellCastResult.BadTargets;

                PetStable petStable = caster.GetPetStable();
                if (petStable != null)
                {
                    if (petStable.CurrentPetIndex.HasValue)
                        return SpellCastResult.AlreadyHaveSummon;

                    var freeSlotIndex = Array.FindIndex(petStable.ActivePets, petInfo => petInfo == null);
                    if (freeSlotIndex == -1)
                    {
                        caster.SendTameFailure(PetTameResult.TooMany);
                        return SpellCastResult.DontReport;
                    }

                    // Check for known Call Pet X spells
                    if (!caster.HasSpell(CallPetSpellIds[freeSlotIndex]))
                    {
                        caster.SendTameFailure(PetTameResult.TooMany);
                        return SpellCastResult.DontReport;
                    }
                }

                if (!caster.GetCharmedGUID().IsEmpty())
                    return SpellCastResult.AlreadyHaveCharm;

                if (!target.GetOwnerGUID().IsEmpty())
                {
                    caster.SendTameFailure(PetTameResult.CreatureAlreadyOwned);
                    return SpellCastResult.DontReport;
                }
            }
            else
                return SpellCastResult.BadImplicitTargets;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
        }
    }

    [Script] // 67151 - Item - Hunter T9 4P Bonus (Steady Shot)
    class spell_hun_t9_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.T94PGreatness);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetActor().IsPlayer() && eventInfo.GetActor().ToPlayer().GetPet() != null)
                return true;
            return false;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();

            caster.CastSpell(caster.ToPlayer().GetPet(), SpellIds.T94PGreatness, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 394366 - Find The Mark
    class spell_hun_t29_2p_marksmanship_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellIds.T292PMarksmanshipDamage, 0))
                && SpellMgr.GetSpellInfo(SpellIds.T292PMarksmanshipDamage, Difficulty.None).GetMaxTicks() != 0;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit caster = eventInfo.GetActor();
            uint ticks = SpellMgr.GetSpellInfo(SpellIds.T292PMarksmanshipDamage, Difficulty.None).GetMaxTicks();
            uint damage = MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetOriginalDamage(), aurEff.GetAmount()) / ticks;

            caster.CastSpell(eventInfo.GetActionTarget(), SpellIds.T292PMarksmanshipDamage, new CastSpellExtraArgs(aurEff)
                .SetTriggeringSpell(eventInfo.GetProcSpell())
                .AddSpellMod(SpellValueMod.BasePoint0, (int)damage));
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }
}
