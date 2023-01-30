// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells.Auras.EffectHandlers;

namespace Scripts.Spells.Hunter
{
    internal struct SpellIds
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
    internal class spell_hun_a_murder_of_crows : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AMurderOfCrowsDamage, SpellIds.AMurderOfCrowsVisual1, SpellIds.AMurderOfCrowsVisual2, SpellIds.AMurderOfCrowsVisual3);
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
            Effects.Add(new EffectApplyHandler(RemoveEffect, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void HandleDummyTick(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            Unit caster = GetCaster();

            caster?.CastSpell(target, SpellIds.AMurderOfCrowsDamage, true);

            target.CastSpell(target, SpellIds.AMurderOfCrowsVisual1, true);
            target.CastSpell(target, SpellIds.AMurderOfCrowsVisual2, true);
            target.CastSpell(target, SpellIds.AMurderOfCrowsVisual3, true);
            target.CastSpell(target, SpellIds.AMurderOfCrowsVisual3, true); // not a mistake, it is intended to cast twice
        }

        private void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Death)
            {
                Unit caster = GetCaster();

                caster?.GetSpellHistory().ResetCooldown(GetId(), true);
            }
        }
    }

    [Script] // 186257 - Aspect of the Cheetah
    internal class spell_hun_aspect_cheetah : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AspectCheetahSlow);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleOnRemove, 0, AuraType.ModIncreaseSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
                GetTarget().CastSpell(GetTarget(), SpellIds.AspectCheetahSlow, true);
        }
    }

    [Script] // 109304 - Exhilaration
    internal class spell_hun_exhilaration : SpellScript, IOnHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ExhilarationR2, SpellIds.Lonewolf);
        }

        public void OnHit()
        {
            if (GetCaster().HasAura(SpellIds.ExhilarationR2) &&
                !GetCaster().HasAura(SpellIds.Lonewolf))
                GetCaster().CastSpell((Unit)null, SpellIds.ExhilarationPet, true);
        }
    }

    [Script] // 212658 - Hunting Party
    internal class spell_hun_hunting_party : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Exhilaration, SpellIds.ExhilarationPet);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.Exhilaration, -TimeSpan.FromSeconds(aurEff.GetAmount()));
            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.ExhilarationPet, -TimeSpan.FromSeconds(aurEff.GetAmount()));
        }
    }

    // 53478 - Last Stand Pet
    [Script]
    internal class spell_hun_last_stand_pet : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PetLastStandTriggered);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)caster.CountPctFromMaxHealth(30));
            caster.CastSpell(caster, SpellIds.PetLastStandTriggered, args);
        }
    }

    // 53271 - Masters Call
    [Script]
    internal class spell_hun_masters_call : SpellScript, ICheckCastHander, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return !spellInfo.GetEffects().Empty() && ValidateSpellInfo(SpellIds.MastersCallTriggered, (uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        public SpellCastResult CheckCast()
        {
            Guardian pet = GetCaster().ToPlayer().GetGuardianPet();

            if (pet == null ||
                !pet.IsPet() ||
                !pet.IsAlive())
                return SpellCastResult.NoPet;

            // Do a mini Spell::CheckCasterAuras on the pet, no other way of doing this
            SpellCastResult result = SpellCastResult.SpellCastOk;
            UnitFlags unitflag = (UnitFlags)(uint)pet.UnitData.Flags;

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

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint effIndex)
        {
            GetCaster().ToPlayer().GetPet().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }

        private void HandleScriptEffect(uint effIndex)
        {
            GetHitUnit().CastSpell((Unit)null, SpellIds.MastersCallTriggered, true);
        }
    }

    // 34477 - Misdirection
    [Script]
    internal class spell_hun_misdirection : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MisdirectionProc);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
            Effects.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Default ||
                GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Interrupt)
                return;

            if (!GetTarget().HasAura(SpellIds.MisdirectionProc))
                GetTarget().GetThreatManager().UnregisterRedirectThreat(SpellIds.Misdirection);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.MisdirectionProc, new CastSpellExtraArgs(aurEff));
        }
    }

    // 35079 - Misdirection (Proc)
    [Script]
    internal class spell_hun_misdirection_proc : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().GetThreatManager().UnregisterRedirectThreat(SpellIds.Misdirection);
        }
    }

    // 55709 - Pet Heart of the Phoenix
    [Script]
    internal class spell_hun_pet_heart_of_the_phoenix : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

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

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit owner = caster.GetOwner();

            if (owner)
                if (!caster.HasAura(SpellIds.PetHeartOfThePhoenixDebuff))
                {
                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.AddSpellMod(SpellValueMod.BasePoint0, 100);
                    owner.CastSpell(caster, SpellIds.PetHeartOfThePhoenixTriggered, args);
                    caster.CastSpell(caster, SpellIds.PetHeartOfThePhoenixDebuff, true);
                }
        }
    }

    [Script] // 781 - Disengage
    internal class spell_hun_posthaste : SpellScript, IAfterCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PosthasteTalent, SpellIds.PosthasteIncreaseSpeed);
        }

        public void AfterCast()
        {
            if (GetCaster().HasAura(SpellIds.PosthasteTalent))
            {
                GetCaster().RemoveMovementImpairingAuras(true);
                GetCaster().CastSpell(GetCaster(), SpellIds.PosthasteIncreaseSpeed, GetSpell());
            }
        }
    }

    [Script] // 53480 - Roar of Sacrifice
    internal class spell_hun_roar_of_sacrifice : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RoarOfSacrificeTriggered);
        }

        public override void Register()
        {
            Effects.Add(new CheckEffectProcHandler(CheckProc, 1, AuraType.Dummy));
            Effects.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo == null ||
                !Convert.ToBoolean((int)damageInfo.GetSchoolMask() & aurEff.GetMiscValue()))
                return false;

            if (!GetCaster())
                return false;

            return true;
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()));
            eventInfo.GetActor().CastSpell(GetCaster(), SpellIds.RoarOfSacrificeTriggered, args);
        }
    }

    // 37506 - Scatter Shot
    [Script]
    internal class spell_hun_scatter_shot : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            // break auto Shot and varhit
            caster.InterruptSpell(CurrentSpellTypes.AutoRepeat);
            caster.AttackStop();
            caster.SendAttackSwingCancelAttack();
        }
    }

    // 56641 - Steady Shot
    [Script]
    internal class spell_hun_steady_shot : SpellScript, IOnHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SteadyShotFocus);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public void OnHit()
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.SteadyShotFocus, true);
        }
    }

    // 1515 - Tame Beast
    [Script]
    internal class spell_hun_tame_beast : SpellScript, ICheckCastHander
    {
        private static readonly uint[] CallPetSpellIds =
        {
            883, 83242, 83243, 83244, 83245
        };

        public SpellCastResult CheckCast()
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
            {
                return SpellCastResult.BadImplicitTargets;
            }

            return SpellCastResult.SpellCastOk;
        }
    }

    [Script] // 67151 - Item - Hunter T9 4P Bonus (Steady Shot)
    internal class spell_hun_t9_4p_bonus : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.T94PGreatness);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetActor().IsTypeId(TypeId.Player) &&
                eventInfo.GetActor().ToPlayer().GetPet())
                return true;

            return false;
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();

            caster.CastSpell(caster.ToPlayer().GetPet(), SpellIds.T94PGreatness, new CastSpellExtraArgs(aurEff));
        }
    }
}