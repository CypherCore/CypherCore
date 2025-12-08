// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the Gnu General Public License. See License file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Linq;

namespace Scripts.Spells.Hunter;

struct SpellIds
{
    public const uint AMurderOfCrowsDamage = 131900;
    public const uint AMurderOfCrowsVisual1 = 131637;
    public const uint AMurderOfCrowsVisual2 = 131951;
    public const uint AMurderOfCrowsVisual3 = 131952;
    public const uint AimedShot = 19434;
    public const uint AspectCheetahSlow = 186258;
    public const uint AspectOfTheFox = 1219162;
    public const uint AspectOfTheTurtlePacifyAura = 205769;
    public const uint BindingShot = 109248;
    public const uint BindingShotImmune = 117553;
    public const uint BindingShotMarker = 117405;
    public const uint BindingShotStun = 117526;
    public const uint BindingShotVisual = 117614;
    public const uint BindingShotVisualArrow = 118306;
    public const uint ConcussiveShot = 5116;
    public const uint EmergencySalveTalent = 459517;
    public const uint EmergencySalveDispel = 459521;
    public const uint EntrapmentTalent = 393344;
    public const uint EntrapmentRoot = 393456;
    public const uint Exhilaration = 109304;
    public const uint ExhilarationPet = 128594;
    public const uint ExhilarationR2 = 231546;
    public const uint ExplosiveShotDamage = 212680;
    public const uint GreviousInjury = 1217789;
    public const uint HighExplosiveTrap = 236775;
    public const uint HighExplosiveTrapDamage = 236777;
    public const uint ImplosiveTrap = 462032;
    public const uint ImplosiveTrapDamage = 462033;
    public const uint Intimidation = 19577;
    public const uint IntimidationMarksmanship = 474421;
    public const uint LatentPoisonStack = 378015;
    public const uint LatentPoisonDamage = 378016;
    public const uint LatentPoisonInjectorsStack = 336903;
    public const uint LatentPoisonInjectorsDamage = 336904;
    public const uint LockAndLoad = 194594;
    public const uint LoneWolf = 155228;
    public const uint MarksmanshipHunterAura = 137016;
    public const uint MasterMarksman = 269576;
    public const uint MastersCallTriggered = 62305;
    public const uint Misdirection = 34477;
    public const uint MisdirectionProc = 35079;
    public const uint MultiShotFocus = 213363;
    public const uint PetLastStandTriggered = 53479;
    public const uint PetHeartOfThePhoenixTriggered = 54114;
    public const uint PetHeartOfThePhoenixDebuff = 55711;
    public const uint PosthasteIncreaseSpeed = 118922;
    public const uint PosthasteTalent = 109215;
    public const uint PreciseShots = 260242;
    public const uint RapidFire = 257044;
    public const uint RapidFireDamage = 257045;
    public const uint RapidFireEnergize = 263585;
    public const uint RejuvenatingWindHeal = 385540;
    public const uint ScoutsInstincts = 459455;
    public const uint ShrapnelShotTalent = 473520;
    public const uint ShrapnelShotDebuff = 474310;
    public const uint SteadyShot = 56641;
    public const uint SteadyShotFocus = 77443;
    public const uint StreamlineTalent = 260367;
    public const uint StreamlineBuff = 342076;
    public const uint T9_4PGreatness = 68130;
    public const uint T29_2PMarksmanshipDamage = 394371;
    public const uint TarTrap = 187699;
    public const uint TarTrapAreatrigger = 187700;
    public const uint TarTrapSlow = 135299;
    public const uint WildernessMedicineTalent = 343242;
    public const uint WildernessMedicineDispel = 384784;
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

[Script] // 1219162 - Aspect of the Fox (attached to 186257 - Aspect of the Cheetah)
class spell_hun_aspect_of_the_fox : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AspectOfTheFox)
            && ValidateSpellEffect((spellInfo.Id, 2))
            && spellInfo.GetEffect(2).IsAura(AuraType.CastWhileWalking);
    }

    public override bool Load()
    {
        return !GetCaster().HasAura(SpellIds.AspectOfTheFox);
    }

    void HandleCastWhileWalking(ref WorldObject target)
    {
        target = null;
    }

    public override void Register()
    {
        OnObjectTargetSelect.Add(new(HandleCastWhileWalking, 2, Targets.UnitCaster));
    }
}

[Script] // 186265 - Aspect of the Turtle
class spell_hun_aspect_of_the_turtle : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AspectOfTheTurtlePacifyAura);
    }

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().CastSpell(GetTarget(), SpellIds.AspectOfTheTurtlePacifyAura, true);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.AspectOfTheTurtlePacifyAura);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(OnApply, 0, AuraType.ModAttackerMeleeHitChance, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.ModAttackerMeleeHitChance, AuraEffectHandleModes.Real));
    }
}

[Script] // 109248 - Binding Shot
class spell_hun_binding_shot : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BindingShotVisualArrow);
    }

    void HandleCast()
    {
        GetCaster().CastSpell(GetExplTargetDest().GetPosition(), SpellIds.BindingShotVisualArrow, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
    }

    public override void Register()
    {
        OnCast.Add(new(HandleCast));
    }
}

// 109248 - Binding Shot
[Script] // Id - 1524
class at_hun_binding_shot(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    TaskScheduler _scheduler = new();

    public override void OnInitialize()
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            foreach (AreaTrigger other in caster.GetAreaTriggers(SpellIds.BindingShot))
                other.SetDuration(0);
        }
    }

    public override void OnCreate(Spell creatingSpell)
    {
        _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
        {
            foreach (ObjectGuid guid in at.GetInsideUnits())
            {
                Unit unit = Global.ObjAccessor.GetUnit(at, guid);
                if (!unit.HasAura(SpellIds.BindingShotMarker))
                    continue;

                unit.CastSpell(at.GetPosition(), SpellIds.BindingShotVisual, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
            }

            task.Repeat();
        });
    }


    public override void OnUnitEnter(Unit unit)
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            if (caster.IsValidAttackTarget(unit) && !unit.HasAura(SpellIds.BindingShotImmune, caster.GetGUID()))
            {
                caster.CastSpell(unit, SpellIds.BindingShotMarker, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
                unit.CastSpell(at.GetPosition(), SpellIds.BindingShotVisual, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
            }
        }
    }

    public override void OnUnitExit(Unit unit, AreaTriggerExitReason reason)
    {
        unit.RemoveAurasDueToSpell(SpellIds.BindingShotMarker, at.GetCasterGUID());

        if (at.IsRemoved())
            return;

        Unit caster = at.GetCaster();
        if (caster != null)
        {
            if (caster.IsValidAttackTarget(unit) && !unit.HasAura(SpellIds.BindingShotImmune, caster.GetGUID()))
            {
                caster.CastSpell(unit, SpellIds.BindingShotStun, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
                caster.CastSpell(unit, SpellIds.BindingShotImmune, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
            }
        }
    }

    public override void OnUpdate(uint diff)
    {
        _scheduler.Update(diff);
    }
}

[Script] // 204089 - Bullseye
class spell_hun_bullseye : AuraScript
{
    bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetActionTarget().HealthBelowPct(aurEff.GetAmount());
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckEffectProc, 0, AuraType.ProcTriggerSpell));
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

[Script] // 5116 - Concussive Shot (attached to 193455 - Cobra Shot and 56641 - Steady Shot)
class spell_hun_concussive_shot : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ConcussiveShot)
            && ValidateSpellEffect((SpellIds.SteadyShot, 2));
    }

    void HandleDuration(uint effIndex)
    {
        Unit caster = GetCaster();

        Aura concussiveShot = GetHitUnit().GetAura(SpellIds.ConcussiveShot, caster.GetGUID());
        if (concussiveShot != null)
        {
            SpellInfo steadyShot = Global.SpellMgr.GetSpellInfo(SpellIds.SteadyShot, GetCastDifficulty());
            TimeSpan extraDuration = TimeSpan.FromSeconds(steadyShot.GetEffect(2).CalcValue(caster) / 10);
            TimeSpan newDuration = TimeSpan.FromSeconds(concussiveShot.GetDuration()) + extraDuration;
            concussiveShot.SetDuration((int)newDuration.TotalMicroseconds);
            concussiveShot.SetMaxDuration((int)newDuration.TotalMicroseconds);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDuration, SpellConst.EffectFirstFound, SpellEffectName.SchoolDamage));
    }
}

[Script] // 459517 - Concussive Shot (attached to 186265 - Aspect of the Turtle and 5384 - Feign Death)
class spell_hun_emergency_salve : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EmergencySalveTalent, SpellIds.EmergencySalveDispel);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.EmergencySalveTalent);
    }

    void HandleAfterCast()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.EmergencySalveDispel, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleAfterCast));
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

// 236775 - High Explosive Trap
[Script] // 9810 - AreatriggerId
class areatrigger_hun_high_explosive_trap(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    public override void OnInitialize()
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            foreach (AreaTrigger other in caster.GetAreaTriggers(SpellIds.HighExplosiveTrap))
                other.SetDuration(0);
        }
    }

    public override void OnUnitEnter(Unit unit)
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            if (caster.IsValidAttackTarget(unit))
            {
                caster.CastSpell(at.GetPosition(), SpellIds.HighExplosiveTrapDamage, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
                at.Remove();
            }
        }
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

// 462032 - Implosive Trap
[Script] // 34378 - AreatriggerId
class areatrigger_hun_implosive_trap(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    public override void OnInitialize()
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            foreach (AreaTrigger other in caster.GetAreaTriggers(SpellIds.ImplosiveTrap))
                other.SetDuration(0);
        }
    }

    public override void OnUnitEnter(Unit unit)
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            if (caster.IsValidAttackTarget(unit))
            {
                caster.CastSpell(at.GetPosition(), SpellIds.ImplosiveTrapDamage, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
                at.Remove();
            }
        }
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

[Script] // 194595 - Lock and Load
class spell_hun_lock_and_load : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LockAndLoad);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return RandomHelper.randChance(aurEff.GetAmount());
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Unit caster = eventInfo.GetActor();
        caster.CastSpell(caster, SpellIds.LockAndLoad, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError
        });
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 1217788 - Manhunter
class spell_hun_manhunter : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GreviousInjury);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.GetProcTarget().IsPlayer();
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        eventInfo.GetActor().CastSpell(eventInfo.GetActionTarget(), SpellIds.GreviousInjury, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 260309 - Master Marksman
class spell_hun_master_marksman : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MasterMarksman);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        uint ticks = Global.SpellMgr.GetSpellInfo(SpellIds.MasterMarksman, Difficulty.None).GetMaxTicks();
        if (ticks == 0)
            return;

        int damage = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()) / ticks);

        eventInfo.GetActor().CastSpell(eventInfo.GetActionTarget(), SpellIds.MasterMarksman, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, damage) }
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
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
        Cypher.Assert(pet != null); // checked in Spell::CheckCast

        if (!pet.IsPet() || !pet.IsAlive())
            return SpellCastResult.NoPet;

        // Do a mini Spell::CheckCasterAuras on the pet, no other way of doing this
        SpellCastResult result = SpellCastResult.SpellCastOk;
        UnitFlags unitflag = (UnitFlags)pet.m_unitData.Flags._value;
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

[Script] // 459783 - Penetrating Shots
class spell_hun_penetrating_shots : AuraScript
{
    void CalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        AuraEffect amountHolder = GetEffect(1);
        if (amountHolder != null)
        {
            float critChanceDone = GetUnitOwner().GetUnitCriticalChanceDone(WeaponAttackType.BaseAttack);
            amount = (int)MathFunctions.CalculatePct(critChanceDone, amountHolder.GetAmount());
        }
    }

    void UpdatePeriodic(AuraEffect aurEff)
    {
        AuraEffect bonus = GetEffect(0);
        if (bonus != null)
            bonus.RecalculateAmount(aurEff);
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalcAmount, 0, AuraType.ModCritDamageBonus));
        OnEffectPeriodic.Add(new(UpdatePeriodic, 1, AuraType.PeriodicDummy));
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

[Script] // 260240 - Precise Shots
class spell_hun_precise_shots : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PreciseShots);
    }

    void HandleProc(ProcEventInfo eventInfo)
    {
        eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.PreciseShots, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = eventInfo.GetProcSpell()
        });
    }

    public override void Register()
    {
        OnProc.Add(new(HandleProc));
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

[Script] // 385539 - Rejuvenating Wind
class spell_hun_rejuvenating_wind : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RejuvenatingWindHeal)
            && Global.SpellMgr.GetSpellInfo(SpellIds.RejuvenatingWindHeal, Difficulty.None).GetMaxTicks() > 0;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procEvent)
    {
        PreventDefaultAction();

        Unit caster = GetTarget();

        uint ticks = Global.SpellMgr.GetSpellInfo(SpellIds.RejuvenatingWindHeal, Difficulty.None).GetMaxTicks();
        int heal = (int)(MathFunctions.CalculatePct(caster.GetMaxHealth(), aurEff.GetAmount()) / ticks);

        caster.CastSpell(caster, SpellIds.RejuvenatingWindHeal, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, heal) }
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
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
        if (damageInfo == null || ((int)damageInfo.GetSchoolMask() & aurEff.GetMiscValue()) == 0)
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
        // break Auto Shot and autohit
        caster.InterruptSpell(CurrentSpellTypes.AutoRepeat);
        caster.AttackStop();
        caster.SendAttackSwingCancelAttack();
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 459455 - Scout's Instincts (attached to 186257 - Aspect of the Cheetah)
class spell_hun_scouts_instincts : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ScoutsInstincts)
            && ValidateSpellEffect((spellInfo.Id, 1))
            && spellInfo.GetEffect(1).IsAura(AuraType.ModMinimumSpeed);
    }

    public override bool Load()
    {
        return !GetCaster().HasAura(SpellIds.ScoutsInstincts);
    }

    void HandleMinSpeed(ref WorldObject target)
    {
        target = null;
    }

    public override void Register()
    {
        OnObjectTargetSelect.Add(new(HandleMinSpeed, 1, Targets.UnitCaster));
    }
}

[Script] // 459533 - Scrappy
class spell_hun_scrappy : AuraScript
{
    static uint[] AffectedSpellIds = [SpellIds.BindingShot, SpellIds.Intimidation, SpellIds.IntimidationMarksmanship];

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(AffectedSpellIds);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        foreach (uint spellId in AffectedSpellIds)
            GetTarget().GetSpellHistory().ModifyCooldown(spellId, -TimeSpan.FromSeconds(aurEff.GetAmount()));
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 473520 - Shrapnel Shot
class spell_hun_shrapnel_shot : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LockAndLoad);
    }

    void HandleProc(ProcEventInfo eventInfo)
    {
        if (!RandomHelper.randChance(GetEffect(0).GetAmount()))
            return;

        GetCaster().CastSpell(GetCaster(), SpellIds.LockAndLoad, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError
        });
    }

    public override void Register()
    {
        OnProc.Add(new(HandleProc));
    }
}

[Script] // 56641 - Steady Shot
class spell_hun_steady_shot : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SteadyShotFocus, SpellIds.AimedShot, SpellIds.MarksmanshipHunterAura);
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleOnHit()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.SteadyShotFocus, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });

        if (GetCaster().HasAura(SpellIds.MarksmanshipHunterAura))
            GetCaster().GetSpellHistory().ModifyCooldown(SpellIds.AimedShot, TimeSpan.FromSeconds(-GetEffectInfo(1).CalcValue()));
    }

    public override void Register()
    {
        OnHit.Add(new(HandleOnHit));
    }
}

[Script] // 260367 - Streamline (attached to 257044 - Rapid Fire)
class spell_hun_streamline : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.StreamlineTalent, SpellIds.StreamlineBuff);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.StreamlineTalent);
    }

    void HandleAfterCast()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.StreamlineBuff, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleAfterCast));
    }
}

[Script] // 391559 - Surging Shots
class spell_hun_surging_shots : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RapidFire);
    }

    void HandleProc(ProcEventInfo eventInfo)
    {
        GetTarget().GetSpellHistory().ResetCooldown(SpellIds.RapidFire, true);
    }

    public override void Register()
    {
        OnProc.Add(new(HandleProc));
    }
}

[Script] // 1515 - Tame Beast
class spell_hun_tame_beast : SpellScript
{
    static uint[] CallPetSpellIds =
    [
        883,
        83242,
        83243,
        83244,
        83245,
    ];

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

                var freeSlotIndex = petStable.ActivePets.ToList().FindIndex(petInfo => petInfo == null);
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

// 187700 - Tar Trap
[Script] // 4436 - AreatriggerId
class areatrigger_hun_tar_trap(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    public override void OnCreate(Spell creatingSpell)
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            if (caster.HasAura(SpellIds.EntrapmentTalent))
                caster.CastSpell(at.GetPosition(), SpellIds.EntrapmentRoot, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
        }
    }

    public override void OnUnitEnter(Unit unit)
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            if (caster.IsValidAttackTarget(unit))
                caster.CastSpell(unit, SpellIds.TarTrapSlow, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
        }
    }

    public override void OnUnitExit(Unit unit, AreaTriggerExitReason reason)
    {
        unit.RemoveAurasDueToSpell(SpellIds.TarTrapSlow, at.GetCasterGUID());
    }
}

// 187699 - Tar Trap
[Script] // 4435 - AreatriggerId
class areatrigger_hun_tar_trap_activate(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    public override void OnInitialize()
    {
        Unit caster = at.GetCaster();
        if (caster != null)
            foreach (AreaTrigger other in caster.GetAreaTriggers(SpellIds.TarTrap))
                other.SetDuration(0);
    }

    public override void OnUnitEnter(Unit unit)
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            if (caster.IsValidAttackTarget(unit))
            {
                caster.CastSpell(at.GetPosition(), SpellIds.TarTrapAreatrigger, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
                at.Remove();
            }
        }
    }
}

[Script] // 67151 - Item - Hunter T9 4P Bonus (Steady Shot)
class spell_hun_t9_4p_bonus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.T9_4PGreatness);
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

        caster.CastSpell(caster.ToPlayer().GetPet(), SpellIds.T9_4PGreatness, aurEff);
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
        return ValidateSpellEffect((SpellIds.T29_2PMarksmanshipDamage, 0))
            && Global.SpellMgr.GetSpellInfo(SpellIds.T29_2PMarksmanshipDamage, Difficulty.None).GetMaxTicks() != 0;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit caster = eventInfo.GetActor();
        uint ticks = Global.SpellMgr.GetSpellInfo(SpellIds.T29_2PMarksmanshipDamage, Difficulty.None).GetMaxTicks();
        uint damage = MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetOriginalDamage(), aurEff.GetAmount()) / ticks;

        caster.CastSpell(eventInfo.GetActionTarget(), SpellIds.T29_2PMarksmanshipDamage, new CastSpellExtraArgs(aurEff)
            .SetTriggeringSpell(eventInfo.GetProcSpell())
            .AddSpellMod(SpellValueMod.BasePoint0, (int)damage));
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // Called by 136 - Mend Pet
class spell_hun_wilderness_medicine : AuraScript
{
    int _dispelChance = 0;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.WildernessMedicineTalent, SpellIds.WildernessMedicineDispel);
    }

    public override bool Load()
    {
        Unit caster = GetCaster();
        if (caster == null)
            return false;

        AuraEffect wildernessMedicine = GetCaster().GetAuraEffect(SpellIds.WildernessMedicineTalent, 1);
        if (wildernessMedicine == null)
            return false;

        _dispelChance = wildernessMedicine.GetAmount();
        return true;
    }

    void OnPeriodic(AuraEffect aurEff)
    {
        Unit caster = GetCaster();
        if (caster != null)
        {
            if (RandomHelper.randChance(_dispelChance))
            {
                caster.CastSpell(GetTarget(), SpellIds.WildernessMedicineDispel, new CastSpellExtraArgs()
                {
                    TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                    TriggeringAura = aurEff
                });
            }
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.ObsModHealth));
    }
}