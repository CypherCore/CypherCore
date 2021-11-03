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
using Game.AI;
using System.Collections.Generic;

namespace Scripts.Spells.DemonHunter
{
    struct SpellIds
    {
        public const uint ChaosStrikeEnergize = 193840;
        public const uint FirstBlood = 206416;
        public const uint SigilOfChainsGrip = 208674;
        public const uint SigilOfChainsSlow = 204843;
        public const uint SigilOfChainsTargetSelect = 204834;
        public const uint SigilOfChainsVisual = 208673;
        public const uint SigilOfFlameAoe = 204598;
        public const uint SigilOfMiseryAoe = 207685;
        public const uint SigilOfSilenceAoe = 204490;
    }

    [Script] // 197125 - Chaos Strike
    class spell_dh_chaos_strike : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChaosStrikeEnergize);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount());
            args.SetTriggeringAura(aurEff);
            GetTarget().CastSpell(GetTarget(), SpellIds.ChaosStrikeEnergize, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 206416 - First Blood
    class spell_dh_first_blood : AuraScript
    {
        ObjectGuid _firstTargetGUID;

        public ObjectGuid GetFirstTarget() { return _firstTargetGUID; }
        public void SetFirstTarget(ObjectGuid targetGuid) { _firstTargetGUID = targetGuid; }

        public override void Register() { }
    }

    // 188499 - Blade Dance
    [Script] // 210152 - Death Sweep
    class spell_dh_blade_dance : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FirstBlood);
        }

        void DecideFirstTarget(List<WorldObject> targetList)
        {
            if (targetList.Empty())
                return;

            Aura aura = GetCaster().GetAura(SpellIds.FirstBlood);
            if (aura == null)
                return;

            ObjectGuid firstTargetGUID = ObjectGuid.Empty;
            ObjectGuid selectedTarget = GetCaster().GetTarget();

            // Prefer the selected target if he is one of the enemies
            if (targetList.Count > 1 && !selectedTarget.IsEmpty())
            {
                var foundObj = targetList.Find(obj => obj.GetGUID() == selectedTarget);
                if (foundObj != null)
                    firstTargetGUID = foundObj.GetGUID();
            }

            if (firstTargetGUID.IsEmpty())
                firstTargetGUID = targetList[0].GetGUID();

            spell_dh_first_blood script = aura.GetScript<spell_dh_first_blood>(nameof(spell_dh_first_blood));
            if (script != null)
                script.SetFirstTarget(firstTargetGUID);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(DecideFirstTarget, 0, Targets.UnitSrcAreaEnemy));
        }
    }

    // 199552 - Blade Dance
    // 200685 - Blade Dance
    // 210153 - Death Sweep
    [Script] // 210155 - Death Sweep
    class spell_dh_blade_dance_damage : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FirstBlood);
        }

        void HandleHitTarget()
        {
            int damage = GetHitDamage();

            AuraEffect aurEff = GetCaster().GetAuraEffect(SpellIds.FirstBlood, 0);
            if (aurEff != null)
            {
                spell_dh_first_blood script = aurEff.GetBase().GetScript<spell_dh_first_blood>(nameof(spell_dh_first_blood));
                if (script != null)
                    if (GetHitUnit().GetGUID() == script.GetFirstTarget())
                        MathFunctions.AddPct(ref damage, aurEff.GetAmount());
            }

            SetHitDamage(damage);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleHitTarget));
        }
    }

    // 204596 - Sigil of Flame
    // 207684 - Sigil of Misery
    // 202137 - Sigil of Silence
    [Script("areatrigger_dh_sigil_of_silence", SpellIds.SigilOfSilenceAoe)]
    [Script("areatrigger_dh_sigil_of_misery", SpellIds.SigilOfMiseryAoe)]
    [Script("areatrigger_dh_sigil_of_flame", SpellIds.SigilOfFlameAoe)]
    class areatrigger_dh_generic_sigil : AreaTriggerAI
    {
        uint _trigger;

        public areatrigger_dh_generic_sigil(AreaTrigger at, uint trigger) : base(at)
        {
            _trigger = trigger;
        }

        public override void OnRemove()
        {
            Unit caster = at.GetCaster();
            if (caster != null)
                caster.CastSpell(at.GetPosition(), _trigger, new CastSpellExtraArgs());
        }
    }

    [Script] // 208673 - Sigil of Chains
    class spell_dh_sigil_of_chains : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SigilOfChainsSlow, SpellIds.SigilOfChainsGrip);
        }

        void HandleEffectHitTarget(uint effIndex)
        {
            WorldLocation loc = GetExplTargetDest();
            if (loc != null)
            {
                GetCaster().CastSpell(GetHitUnit(), SpellIds.SigilOfChainsSlow, new CastSpellExtraArgs(true));
                GetHitUnit().CastSpell(loc.GetPosition(), SpellIds.SigilOfChainsGrip, new CastSpellExtraArgs(true));
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 202138 - Sigil of Chains
    class areatrigger_dh_sigil_of_chains : AreaTriggerAI
    {
        public areatrigger_dh_sigil_of_chains(AreaTrigger at) : base(at) { }

        public override void OnRemove()
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                caster.CastSpell(at.GetPosition(), SpellIds.SigilOfChainsVisual, new CastSpellExtraArgs());
                caster.CastSpell(at.GetPosition(), SpellIds.SigilOfChainsTargetSelect, new CastSpellExtraArgs());
            }
        }
    }
}
