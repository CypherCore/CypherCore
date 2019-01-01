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
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Hunter
{
    struct SpellIds
    {
        public const uint AspectCheetahSlow = 186258;
        public const uint BestialWrath = 19574;
        public const uint ChimeraShotHeal = 53353;
        public const uint Exhilaration = 109304;
        public const uint ExhilarationPet = 128594;
        public const uint ExhilarationR2 = 231546;
        public const uint Fire = 82926;
        public const uint GenericEnergizeFocus = 91954;
        public const uint ImprovedMendPet = 24406;
        public const uint LockAndLoad = 56453;
        public const uint Lonewolf = 155228;
        public const uint MastersCallTriggered = 62305;
        public const uint MisdirectionProc = 35079;
        public const uint PetLastStandTriggered = 53479;
        public const uint PetHeartOfThePhoenix = 55709;
        public const uint PetHeartOfThePhoenixTriggered = 54114;
        public const uint PetHeartOfThePhoenixDebuff = 55711;
        public const uint PetCarrionFeederTriggered = 54045;
        public const uint Readiness = 23989;
        public const uint SerpentSting = 1978;
        public const uint SniperTrainingR1 = 53302;
        public const uint SniperTrainingBuffR1 = 64418;
        public const uint SteadyShotFocus = 77443;
        public const uint T94PGreatness = 68130;
        public const uint DraeneiGiftOfTheNaaru = 59543;
        public const uint RoarOfSacrificeTriggered = 67481;
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

    // 53209 - Chimera Shot
    [Script]
    class spell_hun_chimera_shot : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChimeraShotHeal, SpellIds.SerpentSting);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleScriptEffect(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.ChimeraShotHeal, true);
            Aura aur = GetHitUnit().GetAura(SpellIds.SerpentSting, GetCaster().GetGUID());
            if (aur != null)
                aur.SetDuration(aur.GetSpellInfo().GetMaxDuration(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 77767 - Cobra Shot
    [Script]
    class spell_hun_cobra_shot : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GenericEnergizeFocus, SpellIds.SerpentSting);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleScriptEffect(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.GenericEnergizeFocus, true);
            Aura aur = GetHitUnit().GetAura(SpellIds.SerpentSting, GetCaster().GetGUID());
            if (aur != null)
            {
                int newDuration = aur.GetDuration() + GetEffectValue() * Time.InMilliseconds;
                aur.SetDuration(Math.Min(newDuration, aur.GetMaxDuration()), true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
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

    // -19572 - Improved Mend Pet
    [Script]
    class spell_hun_improved_mend_pet : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ImprovedMendPet);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(GetEffect(0).GetAmount());
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.ImprovedMendPet, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
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
            int healthModSpellBasePoints0 = (int)caster.CountPctFromMaxHealth(30);
            caster.CastCustomSpell(caster, SpellIds.PetLastStandTriggered, healthModSpellBasePoints0, 0, 0, true, null);
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
            return spellInfo.GetEffect(0) != null && ValidateSpellInfo(SpellIds.MastersCallTriggered, (uint)spellInfo.GetEffect(0).CalcValue());
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
            UnitFlags unitflag = (UnitFlags)pet.GetUInt32Value(UnitFields.Flags);
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
                GetTarget().ResetRedirectThreat();
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return GetTarget().GetRedirectThreatTarget();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.MisdirectionProc, true, null, aurEff);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }

    // 35079 - Misdirection (Proc)
    [Script]
    class spell_hun_misdirection_proc : AuraScript
    {
        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().ResetRedirectThreat();
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 54044 - Pet Carrion Feeder
    [Script]
    class spell_hun_pet_carrion_feeder : SpellScript
    {
        public override bool Load()
        {
            if (!GetCaster().IsPet())
                return false;
            return true;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PetCarrionFeederTriggered);
        }

        SpellCastResult CheckIfCorpseNear()
        {
            Unit caster = GetCaster();
            float max_range = GetSpellInfo().GetMaxRange(false);

            // search for nearby enemy corpse in range
            var check = new AnyDeadUnitSpellTargetInRangeCheck<WorldObject>(caster, max_range, GetSpellInfo(), SpellTargetCheckTypes.Enemy);
            var searcher = new WorldObjectSearcher(caster, check);
            Cell.VisitWorldObjects(caster, searcher, max_range);
            if (!searcher.GetTarget())
                Cell.VisitGridObjects(caster, searcher, max_range);
            if (!searcher.GetTarget())
                return SpellCastResult.NoEdibleCorpses;
            return SpellCastResult.SpellCastOk;
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, SpellIds.PetCarrionFeederTriggered, false);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            OnCheckCast.Add(new CheckCastHandler(CheckIfCorpseNear));
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
                    owner.CastCustomSpell(SpellIds.PetHeartOfThePhoenixTriggered, SpellValueMod.BasePoint0, 100, caster, true);
                    caster.CastSpell(caster, SpellIds.PetHeartOfThePhoenixDebuff, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 23989 - Readiness
    [Script]
    class spell_hun_readiness : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleDummy(uint effIndex)
        {
            // immediately finishes the cooldown on your other Hunter abilities except Bestial Wrath
            GetCaster().GetSpellHistory().ResetCooldowns(p =>
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(p.Key);

                //! If spellId in cooldown map isn't valid, the above will return a null pointer.
                if (spellInfo.SpellFamilyName == SpellFamilyNames.Hunter &&
                spellInfo.Id != SpellIds.Readiness &&
                spellInfo.Id != SpellIds.BestialWrath &&
                spellInfo.Id != SpellIds.DraeneiGiftOfTheNaaru &&
                spellInfo.GetRecoveryTime() > 0)
                    return true;
                return false;
            }, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 82925 - Ready, Set, Aim...
    [Script]
    class spell_hun_ready_set_aim : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Fire);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetStackAmount() == 5)
            {
                GetTarget().CastSpell(GetTarget(), SpellIds.Fire, true, null, aurEff);
                GetTarget().RemoveAura(GetId());
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
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

            int damage = (int)MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
            eventInfo.GetActor().CastCustomSpell(SpellIds.RoarOfSacrificeTriggered, SpellValueMod.BasePoint0, damage, GetCaster(), TriggerCastFlags.FullMask, null, aurEff);
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
            // break Auto Shot and autohit
            caster.InterruptSpell(CurrentSpellTypes.AutoRepeat);
            caster.AttackStop();
            caster.SendAttackSwingCancelAttack();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // -53302 - Sniper Training
    [Script]
    class spell_hun_sniper_training : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SniperTrainingR1, SpellIds.SniperTrainingBuffR1);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            PreventDefaultAction();
            if (aurEff.GetAmount() <= 0)
            {
                uint spellId = SpellIds.SniperTrainingBuffR1 + GetId() - SpellIds.SniperTrainingR1;
                Unit target = GetTarget();
                target.CastSpell(target, spellId, true, null, aurEff);
                Player playerTarget = GetUnitOwner().ToPlayer();
                if (playerTarget)
                {
                    int baseAmount = aurEff.GetBaseAmount();
                    int amount = playerTarget.CalculateSpellDamage(playerTarget, GetSpellInfo(), aurEff.GetEffIndex(), baseAmount);
                    GetEffect(0).SetAmount(amount);
                }
            }
        }

        void HandleUpdatePeriodic(AuraEffect aurEff)
        {
            Player playerTarget = GetUnitOwner().ToPlayer();
            if (playerTarget)
            {
                int baseAmount = aurEff.GetBaseAmount();
                int amount = playerTarget.isMoving() ?
                playerTarget.CalculateSpellDamage(playerTarget, GetSpellInfo(), aurEff.GetEffIndex(), baseAmount) :
                aurEff.GetAmount() - 1;
                aurEff.SetAmount(amount);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicTriggerSpell));
            OnEffectUpdatePeriodic.Add(new EffectUpdatePeriodicHandler(HandleUpdatePeriodic, 0, AuraType.PeriodicTriggerSpell));
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
        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            if (!caster.IsTypeId(TypeId.Player))
                return SpellCastResult.DontReport;

            if (!GetExplTargetUnit())
                return SpellCastResult.BadImplicitTargets;

            Creature target = GetExplTargetUnit().ToCreature();
            if (target)
            {
                if (target.getLevel() > caster.getLevel())
                    return SpellCastResult.Highlevel;

                // use SMSG_PET_TAME_FAILURE?
                if (!target.GetCreatureTemplate().IsTameable(caster.ToPlayer().CanTameExoticPets()))
                    return SpellCastResult.BadTargets;

                if (!caster.GetPetGUID().IsEmpty())
                    return SpellCastResult.AlreadyHaveSummon;

                if (!caster.GetCharmGUID().IsEmpty())
                    return SpellCastResult.AlreadyHaveCharm;
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

    //  53434 - Call of the Wild
    [Script]
    class spell_hun_target_only_pet_and_owner : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.Clear();
            targets.Add(GetCaster());
            Unit owner = GetCaster().GetOwner();
            if (owner)
                targets.Add(owner);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitCasterAreaParty));
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitCasterAreaParty));
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

            caster.CastSpell(caster.ToPlayer().GetPet(), SpellIds.T94PGreatness, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    // -56333 - T.N.T.
    [Script]
    class spell_hun_tnt : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LockAndLoad);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(GetEffect(0).GetAmount());
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.LockAndLoad, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }
}
