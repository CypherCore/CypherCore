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
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.Spells.Monk
{
    struct SpellIds
    {
        public const uint CracklingJadeLightningChannel = 117952;
        public const uint CracklingJadeLightningChiProc = 123333;
        public const uint CracklingJadeLightningKnockback = 117962;
        public const uint CracklingJadeLightningKnockbackCd = 117953;
        public const uint ProvokeSingleTarget = 116189;
        public const uint ProvokeAoe = 118635;
        public const uint SoothingMist = 115175;
        public const uint StanceOfTheSpiritedCrane = 154436;
        public const uint SurgingMistHeal = 116995;
    }

    [Script] // 117952 - Crackling Jade Lightning
    class spell_monk_crackling_jade_lightning : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StanceOfTheSpiritedCrane, SpellIds.CracklingJadeLightningChiProc);
        }

        void OnTick(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster)
                if (caster.HasAura(SpellIds.StanceOfTheSpiritedCrane))
                    caster.CastSpell(caster, SpellIds.CracklingJadeLightningChiProc, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDamage));
        }
    }

    [Script] // 117959 - Crackling Jade Lightning
    class spell_monk_crackling_jade_lightning_knockback_proc_aura : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CracklingJadeLightningKnockback, SpellIds.CracklingJadeLightningKnockbackCd);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (GetTarget().HasAura(SpellIds.CracklingJadeLightningKnockbackCd))
                return false;

            if (eventInfo.GetActor().HasAura(SpellIds.CracklingJadeLightningChannel, GetTarget().GetGUID()))
                return false;

            Spell currentChanneledSpell = GetTarget().GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (!currentChanneledSpell || currentChanneledSpell.GetSpellInfo().Id != SpellIds.CracklingJadeLightningChannel)
                return false;

            return true;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().CastSpell(eventInfo.GetActor(), SpellIds.CracklingJadeLightningKnockback, TriggerCastFlags.FullMask);
            GetTarget().CastSpell(GetTarget(), SpellIds.CracklingJadeLightningKnockbackCd, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 115546 - Provoke
    class spell_monk_provoke : SpellScript
    {
        const uint BlackOxStatusEntry = 61146;

        public override bool Validate(SpellInfo spellInfo)
        {
            if (!spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitMask)) // ensure GetExplTargetUnit() will return something meaningful during CheckCast
                return false;
            return ValidateSpellInfo(SpellIds.ProvokeSingleTarget, SpellIds.ProvokeAoe);
        }

        SpellCastResult CheckExplicitTarget()
        {
            if (GetExplTargetUnit().GetEntry() != BlackOxStatusEntry)
            {
                SpellInfo singleTarget = Global.SpellMgr.GetSpellInfo(SpellIds.ProvokeSingleTarget);
                SpellCastResult singleTargetExplicitResult = singleTarget.CheckExplicitTarget(GetCaster(), GetExplTargetUnit());
                if (singleTargetExplicitResult != SpellCastResult.SpellCastOk)
                    return singleTargetExplicitResult;
            }
            else if (GetExplTargetUnit().GetOwnerGUID() != GetCaster().GetGUID())
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            if (GetHitUnit().GetEntry() != BlackOxStatusEntry)
                GetCaster().CastSpell(GetHitUnit(), SpellIds.ProvokeSingleTarget, true);
            else
                GetCaster().CastSpell(GetHitUnit(), SpellIds.ProvokeAoe, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckExplicitTarget));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }
}
