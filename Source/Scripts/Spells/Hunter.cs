// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

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
        public const uint Lonewolf = 155228;
        public const uint MastersCallTriggered = 62305;
        public const uint Misdirection = 34477;
        public const uint MisdirectionProc = 35079;
        public const uint PetLastStandTriggered = 53479;
        public const uint PetHeartOfThePhoenixTriggered = 54114;
        public const uint PetHeartOfThePhoenixDebuff = 55711;
        public const uint PosthasteIncreaseSpeed = 118922;
        public const uint PosthasteTalent = 109215;
        public const uint SteadyShotFocus = 77443;
        public const uint T94PGreatness = 68130;
        public const uint DraeneiGiftOfTheNaaru = 59543;
        public const uint RoarOfSacrificeTriggered = 67481;
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
            OnEffectRemove.Add(new EffectApplyHandler(RemoveEffect, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
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
            AfterEffectRemove.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.ModIncreaseSpeed, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 109304 - Exhilaration
    class spell_hun_exhilaration : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ExhilarationR2, SpellIds.Lonewolf);
        }

        void HandleOnHit()
        {
            if (GetCaster().HasAura(SpellIds.ExhilarationR2) && !GetCaster().HasAura(SpellIds.Lonewolf))
                GetCaster().CastSpell((Unit)null, SpellIds.ExhilarationPet, true);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleOnHit));
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
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 53478 - Last Stand Pet
    [Script]
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 53271 - Masters Call
    [Script]
    class spell_hun_masters_call : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return !spellInfo.GetEffects().Empty() && ValidateSpellInfo(SpellIds.MastersCallTriggered, (uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        SpellCastResult DoCheckCast()
        {
            Guardian pet = GetCaster().ToPlayer().GetGuardianPet();
            if (pet == null || !pet.IsPet() || !pet.IsAlive())
                return SpellCastResult.NoPet;

            // Do a mini Spell::CheckCasterAuras on the pet, no other way of doing this
            SpellCastResult result = SpellCastResult.SpellCastOk;
            UnitFlags unitflag = (UnitFlags)(uint)pet.m_unitData.Flags;
            if (!pet.GetCharmerGUID().IsEmpty())
                result = SpellCastResult.Charmed;
            else if (unitflag.HasAnyFlag(UnitFlags.Stunned))
                result = SpellCastResult.Stunned;
            else if (unitflag.HasAnyFlag(UnitFlags.Fleeing))
                result = SpellCastResult.Fleeing;
            else if (unitflag.HasAnyFlag(UnitFlags.Confused))
                result = SpellCastResult.Confused;

            if (result != SpellCastResult.SpellCastOk)
                return result;

            Unit target = GetExplTargetUnit();
            if (!target)
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
            GetHitUnit().CastSpell((Unit)null, SpellIds.MastersCallTriggered, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(DoCheckCast));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
        }
    }

    // 34477 - Misdirection
    [Script]
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
            GetTarget().CastSpell(GetTarget(), SpellIds.MisdirectionProc, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }

    // 35079 - Misdirection (Proc)
    [Script]
    class spell_hun_misdirection_proc : AuraScript
    {
        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().GetThreatManager().UnregisterRedirectThreat(SpellIds.Misdirection);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 55709 - Pet Heart of the Phoenix
    [Script]
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
            if (owner)
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            AfterCast.Add(new CastHandler(HandleAfterCast));
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
            if (damageInfo == null || !Convert.ToBoolean((int)damageInfo.GetSchoolMask() & aurEff.GetMiscValue()))
                return false;

            if (!GetCaster())
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
            DoCheckEffectProc.Add(new CheckEffectProcHandler(CheckProc, 1, AuraType.Dummy));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }

    // 37506 - Scatter Shot
    [Script]
    class spell_hun_scatter_shot : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            // break auto Shot and varhit
            caster.InterruptSpell(CurrentSpellTypes.AutoRepeat);
            caster.AttackStop();
            caster.SendAttackSwingCancelAttack();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 56641 - Steady Shot
    [Script]
    class spell_hun_steady_shot : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SteadyShotFocus);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleOnHit()
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.SteadyShotFocus, true);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleOnHit));
        }
    }

    // 1515 - Tame Beast
    [Script]
    class spell_hun_tame_beast : SpellScript
    {
        static uint[] CallPetSpellIds = { 883, 83242, 83243, 83244, 83245, };

        SpellCastResult CheckCast()
        {
            Player caster = GetCaster().ToPlayer();
            if (caster == null)
                return SpellCastResult.DontReport;

            if (!GetExplTargetUnit())
                return SpellCastResult.BadImplicitTargets;

            Creature target = GetExplTargetUnit().ToCreature();
            if (target)
            {
                if (target.GetLevel() > caster.GetLevel())
                    return SpellCastResult.Highlevel;

                // use SMSG_PET_TAME_FAILURE?
                if (!target.GetCreatureTemplate().IsTameable(caster.CanTameExoticPets()))
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
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
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
            if (eventInfo.GetActor().IsTypeId(TypeId.Player) && eventInfo.GetActor().ToPlayer().GetPet())
                return true;
            return false;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();

            caster.CastSpell(caster.ToPlayer().GetPet(), SpellIds.T94PGreatness, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }
}
