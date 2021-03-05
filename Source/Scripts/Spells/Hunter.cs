/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.Spells.Hunter
{
    internal struct SpellIds
    {
        public const uint AspectCheetahSlow = 186258;
        public const uint Exhilaration = 109304;
        public const uint ExhilarationPet = 128594;
        public const uint ExhilarationR2 = 231546;
        public const uint Lonewolf = 155228;
        public const uint MastersCallTriggered = 62305;
        public const uint MisdirectionProc = 35079;
        public const uint PetLastStandTriggered = 53479;
        public const uint PetHeartOfThePhoenixTriggered = 54114;
        public const uint PetHeartOfThePhoenixDebuff = 55711;
        public const uint SteadyShotFocus = 77443;
        public const uint T94PGreatness = 68130;
        public const uint DraeneiGiftOfTheNaaru = 59543;
        public const uint RoarOfSacrificeTriggered = 67481;
    }

    [Script] // 186257 - Aspect of the Cheetah
    internal class spell_hun_aspect_cheetah : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AspectCheetahSlow);
        }

        private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
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
    internal class spell_hun_exhilaration : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ExhilarationR2, SpellIds.Lonewolf);
        }

        private void HandleOnHit()
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
    internal class spell_hun_hunting_party : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Exhilaration, SpellIds.ExhilarationPet);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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
    internal class spell_hun_last_stand_pet : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PetLastStandTriggered);
        }

        private void HandleDummy(uint effIndex)
        {
            var caster = GetCaster();
            var healthModSpellBasePoints0 = (int)caster.CountPctFromMaxHealth(30);
            caster.CastCustomSpell(caster, SpellIds.PetLastStandTriggered, healthModSpellBasePoints0, 0, 0, true, null);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 53271 - Masters Call
    [Script]
    internal class spell_hun_masters_call : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffect(0) != null && ValidateSpellInfo(SpellIds.MastersCallTriggered, (uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        private SpellCastResult DoCheckCast()
        {
            var pet = GetCaster().ToPlayer().GetGuardianPet();
            if (pet == null || !pet.IsPet() || !pet.IsAlive())
                return SpellCastResult.NoPet;

            // Do a mini Spell::CheckCasterAuras on the pet, no other way of doing this
            var result = SpellCastResult.SpellCastOk;
            var unitflag = (UnitFlags)(uint)pet.m_unitData.Flags;
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

            var target = GetExplTargetUnit();
            if (!target)
                return SpellCastResult.BadTargets;

            if (!pet.IsWithinLOSInMap(target))
                return SpellCastResult.LineOfSight;

            return SpellCastResult.SpellCastOk;
        }

        private void HandleDummy(uint effIndex)
        {
            GetCaster().ToPlayer().GetPet().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }

        private void HandleScriptEffect(uint effIndex)
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
    internal class spell_hun_misdirection : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MisdirectionProc);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Default || GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Interrupt)
                return;

            if (!GetTarget().HasAura(SpellIds.MisdirectionProc))
                GetTarget().ResetRedirectThreat();
        }

        private bool CheckProc(ProcEventInfo eventInfo)
        {
            return GetTarget().GetRedirectThreatTarget();
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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
    internal class spell_hun_misdirection_proc : AuraScript
    {
        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().ResetRedirectThreat();
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 55709 - Pet Heart of the Phoenix
    [Script]
    internal class spell_hun_pet_heart_of_the_phoenix : SpellScript
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

        private void HandleScript(uint effIndex)
        {
            var caster = GetCaster();
            var owner = caster.GetOwner();
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

    [Script] // 53480 - Roar of Sacrifice
    internal class spell_hun_roar_of_sacrifice : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RoarOfSacrificeTriggered);
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            var damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || !Convert.ToBoolean((int)damageInfo.GetSchoolMask() & aurEff.GetMiscValue()))
                return false;

            if (!GetCaster())
                return false;

            return true;
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            var damage = (int)MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
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
    internal class spell_hun_scatter_shot : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        private void HandleDummy(uint effIndex)
        {
            var caster = GetCaster().ToPlayer();
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
    internal class spell_hun_steady_shot : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SteadyShotFocus);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        private void HandleOnHit()
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
    internal class spell_hun_tame_beast : SpellScript
    {
        private SpellCastResult CheckCast()
        {
            var caster = GetCaster();
            if (!caster.IsTypeId(TypeId.Player))
                return SpellCastResult.DontReport;

            if (!GetExplTargetUnit())
                return SpellCastResult.BadImplicitTargets;

            var target = GetExplTargetUnit().ToCreature();
            if (target)
            {
                if (target.GetLevel() > caster.GetLevel())
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

    [Script] // 67151 - Item - Hunter T9 4P Bonus (Steady Shot)
    internal class spell_hun_t9_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.T94PGreatness);
        }

        private bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetActor().IsTypeId(TypeId.Player) && eventInfo.GetActor().ToPlayer().GetPet())
                return true;
            return false;
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            var caster = eventInfo.GetActor();

            caster.CastSpell(caster.ToPlayer().GetPet(), SpellIds.T94PGreatness, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }
}
