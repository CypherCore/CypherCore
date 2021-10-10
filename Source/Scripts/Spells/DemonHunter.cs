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

namespace Scripts.Spells.DemonHunter
{
    struct SpellIds
    {
        public const uint ChaosStrikeEnergize = 193840;
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
