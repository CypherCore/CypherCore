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
        //HallowEndCandysSpells
        public const uint CandyOrangeGiant = 24924; // Effect 1: Apply Aura: Mod Size, Value: 30%
        public const uint CandySkeleton = 24925; // Effect 1: Apply Aura: Change Model (Skeleton). Effect 2: Apply Aura: Underwater Breathing
        public const uint CandyPirate = 24926; // Effect 1: Apply Aura: Increase Swim Speed, Value: 50%
        public const uint CandyGhost = 24927; // Effect 1: Apply Aura: Levitate / Hover. Effect 2: Apply Aura: Slow Fall, Effect 3: Apply Aura: Water Walking
        public const uint CandyFemaleDefiasPirate = 44742; // Effect 1: Apply Aura: Change Model (Defias Pirate, Female). Effect 2: Increase Swim Speed, Value: 50%
        public const uint CandyMaleDefiasPirate = 44743;  // Effect 1: Apply Aura: Change Model (Defias Pirate, Male).   Effect 2: Increase Swim Speed, Value: 50%

        //Trickspells
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

        //Trickortreatspells
        public const uint Trick = 24714;
        public const uint Treat = 24715;
        public const uint TrickedOrTreated = 24755;
        public const uint TrickyTreatSpeed = 42919;
        public const uint TrickyTreatTrigger = 42965;
        public const uint UpsetTummy = 42966;

        //Wand Spells
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
    class spell_hallow_end_candy_SpellScript : SpellScript
    {
        uint[] spells =
        {
            SpellIds.CandyOrangeGiant,
            SpellIds.CandySkeleton,
            SpellIds.CandyPirate,
            SpellIds.CandyGhost
        };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(spells);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), spells.SelectRandom(), true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 24926 - Hallow's End Candy
    class spell_hallow_end_candy_pirate_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CandyFemaleDefiasPirate, SpellIds.CandyMaleDefiasPirate);
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            uint spell = GetTarget().GetNativeGender() == Gender.Female ? SpellIds.CandyFemaleDefiasPirate : SpellIds.CandyMaleDefiasPirate;
            GetTarget().CastSpell(GetTarget(), spell, true);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            uint spell = GetTarget().GetNativeGender() == Gender.Female ? SpellIds.CandyFemaleDefiasPirate : SpellIds.CandyMaleDefiasPirate;
            GetTarget().RemoveAurasDueToSpell(spell);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.ModIncreaseSwimSpeed, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.ModIncreaseSwimSpeed, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 24750 Trick
    class spell_hallow_end_trick : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.PirateCostumeMale, SpellIds.PirateCostumeFemale, SpellIds.NinjaCostumeMale, SpellIds.NinjaCostumeFemale,
                SpellIds.LeperGnomeCostumeMale, SpellIds.LeperGnomeCostumeFemale, SpellIds.SkeletonCostume, SpellIds.GhostCostumeMale, SpellIds.GhostCostumeFemale, SpellIds.TrickBuff);
        }

        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            Player target = GetHitPlayer();
            if (target)
            {
                Gender gender = target.GetNativeGender();
                uint spellId = SpellIds.TrickBuff;
                switch (RandomHelper.URand(0, 5))
                {
                    case 1:
                        spellId = gender == Gender.Female ? SpellIds.LeperGnomeCostumeFemale : SpellIds.LeperGnomeCostumeMale;
                        break;
                    case 2:
                        spellId = gender == Gender.Female ? SpellIds.PirateCostumeFemale : SpellIds.PirateCostumeMale;
                        break;
                    case 3:
                        spellId = gender == Gender.Female ? SpellIds.GhostCostumeFemale : SpellIds.GhostCostumeMale;
                        break;
                    case 4:
                        spellId = gender == Gender.Female ? SpellIds.NinjaCostumeFemale : SpellIds.NinjaCostumeMale;
                        break;
                    case 5:
                        spellId = SpellIds.SkeletonCostume;
                        break;
                    default:
                        break;
                }

                caster.CastSpell(target, spellId, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 24751 Trick or Treat
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
            if (target)
            {
                caster.CastSpell(target, RandomHelper.randChance(50) ? SpellIds.Trick : SpellIds.Treat, true);
                caster.CastSpell(target, SpellIds.TrickedOrTreated, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 24717, 24718, 24719, 24720, 24724, 24733, 24737, 24741
    class spell_hallow_end_wand : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.PirateCostumeMale, SpellIds.PirateCostumeFemale, SpellIds.NinjaCostumeMale, SpellIds.NinjaCostumeFemale,
                SpellIds.LeperGnomeCostumeMale, SpellIds.LeperGnomeCostumeFemale, SpellIds.GhostCostumeMale, SpellIds.GhostCostumeFemale);
        }

        void HandleScriptEffect()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            uint spellId;
            bool female = target.GetNativeGender() == Gender.Female;

            switch (GetSpellInfo().Id)
            {
                case SpellIds.HallowedWandLeperGnome:
                    spellId = female ? SpellIds.LeperGnomeCostumeFemale : SpellIds.LeperGnomeCostumeMale;
                    break;
                case SpellIds.HallowedWandPirate:
                    spellId = female ? SpellIds.PirateCostumeFemale : SpellIds.PirateCostumeMale;
                    break;
                case SpellIds.HallowedWandGhost:
                    spellId = female ? SpellIds.GhostCostumeFemale : SpellIds.GhostCostumeMale;
                    break;
                case SpellIds.HallowedWandNinja:
                    spellId = female ? SpellIds.NinjaCostumeFemale : SpellIds.NinjaCostumeMale;
                    break;
                case SpellIds.HallowedWandRandom:
                    spellId = RandomHelper.RAND(SpellIds.HallowedWandPirate, SpellIds.HallowedWandNinja, SpellIds.HallowedWandLeperGnome, SpellIds.HallowedWandSkeleton, SpellIds.HallowedWandWisp, SpellIds.HallowedWandGhost, SpellIds.HallowedWandBat);
                    break;
                default:
                    return;
            }
            caster.CastSpell(target, spellId, true);
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleScriptEffect));
        }
    }
}
