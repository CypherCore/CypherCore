// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Events.HallowsEnd
{
    struct SpellIds
    {
        //CandySpells
        public const uint HallowsEndCandyOrangeGiant = 24924;
        public const uint HallowsEndCandySkeleton = 24925;
        public const uint HallowsEndCandyPirate = 24926;
        public const uint HallowsEndCandyGhost = 24927;
        public const uint HallowsEndCandyFemaleDefiasPirate = 44742;
        public const uint HallowsEndCandyMaleDefiasPirate = 44743;

        //TrickSpells
        public const uint PirateCostumeMale = 24708;
        public const uint PirateCostumeFemale = 24709;
        public const uint NinjaCostumeMale = 24710;
        public const uint NinjaCostumeFemale = 24711;
        public const uint LeperGnomeCostumeMale = 24712;
        public const uint LeperGnomeCostumeFemale = 24713;
        public const uint SkeletonCostume = 24723;
        public const uint GhostCostumeMale = 24735;
        public const uint GhostCostumeFemale = 24736;
        public const uint TrickBuff = 24753;

        //TrickOrTreatSpells
        public const uint Trick = 24714;
        public const uint Treat = 24715;
        public const uint TrickedOrTreated = 24755;
        public const uint TrickyTreatSpeed = 42919;
        public const uint TrickyTreatTrigger = 42965;
        public const uint UpsetTummy = 42966;

        //HallowendData
        public const uint HallowedWandPirate = 24717;
        public const uint HallowedWandNinja = 24718;
        public const uint HallowedWandLeperGnome = 24719;
        public const uint HallowedWandRandom = 24720;
        public const uint HallowedWandSkeleton = 24724;
        public const uint HallowedWandWisp = 24733;
        public const uint HallowedWandGhost = 24737;
        public const uint HallowedWandBat = 24741;
    }

    [Script] // 24930 - Hallow's End Candy
    class spell_hallow_end_candy : SpellScript
    {
        uint[] CandysSpells = { SpellIds.HallowsEndCandyOrangeGiant, SpellIds.HallowsEndCandySkeleton, SpellIds.HallowsEndCandyPirate, SpellIds.HallowsEndCandyGhost };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(CandysSpells);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), CandysSpells.SelectRandom(), true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 24926 - Hallow's End Candy
    class spell_hallow_end_candy_pirate : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HallowsEndCandyFemaleDefiasPirate, SpellIds.HallowsEndCandyMaleDefiasPirate);
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            uint spell = GetTarget().GetNativeGender() == Gender.Female ? SpellIds.HallowsEndCandyFemaleDefiasPirate : SpellIds.HallowsEndCandyMaleDefiasPirate;
            GetTarget().CastSpell(GetTarget(), spell, true);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            uint spell = GetTarget().GetNativeGender() == Gender.Female ? SpellIds.HallowsEndCandyFemaleDefiasPirate : SpellIds.HallowsEndCandyMaleDefiasPirate;
            GetTarget().RemoveAurasDueToSpell(spell);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(HandleApply, 0, AuraType.ModIncreaseSwimSpeed, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.ModIncreaseSwimSpeed, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 24750 - Trick
    class spell_hallow_end_trick : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.PirateCostumeMale, SpellIds.PirateCostumeFemale, SpellIds.NinjaCostumeMale, SpellIds.NinjaCostumeFemale, SpellIds.LeperGnomeCostumeMale,
                SpellIds.LeperGnomeCostumeFemale, SpellIds.SkeletonCostume, SpellIds.GhostCostumeMale, SpellIds.GhostCostumeFemale, SpellIds.TrickBuff);
        }

        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            Player target = GetHitPlayer();
            if (target != null)
            {
                Gender gender = target.GetNativeGender();
                uint spellId = RandomHelper.URand(0, 5) switch
                {
                    1 => gender == Gender.Female ? SpellIds.LeperGnomeCostumeFemale : SpellIds.LeperGnomeCostumeMale,
                    2 => gender == Gender.Female ? SpellIds.PirateCostumeFemale : SpellIds.PirateCostumeMale,
                    3 => gender == Gender.Female ? SpellIds.GhostCostumeFemale : SpellIds.GhostCostumeMale,
                    4 => gender == Gender.Female ? SpellIds.NinjaCostumeFemale : SpellIds.NinjaCostumeMale,
                    5 => SpellIds.SkeletonCostume,
                    _ => SpellIds.TrickBuff
                };

                caster.CastSpell(target, spellId, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 24751 - Trick or Treat
    class spell_hallow_end_trick_or_treat : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.Trick, SpellIds.Treat, SpellIds.TrickedOrTreated);
        }

        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            Player target = GetHitPlayer();
            if (target != null)
            {
                caster.CastSpell(target, RandomHelper.randChance(50) ? SpellIds.Trick : SpellIds.Treat, true);
                caster.CastSpell(target, SpellIds.TrickedOrTreated, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 44436 - Tricky Treat
    class spell_hallow_end_tricky_treat : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.TrickyTreatSpeed, SpellIds.TrickyTreatTrigger, SpellIds.UpsetTummy);
        }

        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster.HasAura(SpellIds.TrickyTreatTrigger) && caster.GetAuraCount(SpellIds.TrickyTreatSpeed) > 3 && RandomHelper.randChance(33))
                caster.CastSpell(caster, SpellIds.UpsetTummy, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 24717, 24718, 24719, 24720, 24724, 24733, 24737, 24741
    class spell_hallow_end_wand : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.PirateCostumeMale, SpellIds.PirateCostumeFemale, SpellIds.NinjaCostumeMale, SpellIds.NinjaCostumeFemale, SpellIds.LeperGnomeCostumeMale,
                SpellIds.LeperGnomeCostumeFemale, SpellIds.GhostCostumeMale, SpellIds.GhostCostumeFemale);
        }

        void HandleScriptEffect()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            Gender gender = target.GetNativeGender();

            uint spellId = GetSpellInfo().Id switch
            {
                SpellIds.HallowedWandLeperGnome => gender == Gender.Female ? SpellIds.LeperGnomeCostumeFemale : SpellIds.LeperGnomeCostumeMale,
                SpellIds.HallowedWandPirate => gender == Gender.Female ? SpellIds.PirateCostumeFemale : SpellIds.PirateCostumeMale,
                SpellIds.HallowedWandGhost => gender == Gender.Female ? SpellIds.GhostCostumeFemale : SpellIds.GhostCostumeMale,
                SpellIds.HallowedWandNinja => gender == Gender.Female ? SpellIds.NinjaCostumeFemale : SpellIds.NinjaCostumeMale,
                SpellIds.HallowedWandRandom => RandomHelper.RAND(SpellIds.HallowedWandPirate, SpellIds.HallowedWandNinja, SpellIds.HallowedWandLeperGnome, SpellIds.HallowedWandSkeleton, SpellIds.HallowedWandWisp, SpellIds.HallowedWandGhost, SpellIds.HallowedWandBat),
                _ => 0
            };

            if (spellId != 0)
                caster.CastSpell(target, spellId, true);
        }

        public override void Register()
        {
            AfterHit.Add(new(HandleScriptEffect));
        }
    }
}