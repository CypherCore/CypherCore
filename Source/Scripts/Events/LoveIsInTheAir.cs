// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Events.LoveIsInTheAir
{
    struct SpellIds
    {
        //Romantic Picnic
        public const uint BasketCheck = 45119; // Holiday - Valentine - Romantic Picnic Near Basket Check
        public const uint MealPeriodic = 45103; // Holiday - Valentine - Romantic Picnic Meal Periodic - Effect Dummy
        public const uint MealEatVisual = 45120; // Holiday - Valentine - Romantic Picnic Meal Eat Visual
        //public const uint MealParticle = 45114; // Holiday - Valentine - Romantic Picnic Meal Particle - Unused
        public const uint DrinkVisual = 45121; // Holiday - Valentine - Romantic Picnic Drink Visual
        public const uint RomanticPicnicAchiev = 45123; // Romantic Picnic Periodic = 5000

        //CreateHeartCandy
        public const uint CreateHeartCandy1 = 26668;
        public const uint CreateHeartCandy2 = 26670;
        public const uint CreateHeartCandy3 = 26671;
        public const uint CreateHeartCandy4 = 26672;
        public const uint CreateHeartCandy5 = 26673;
        public const uint CreateHeartCandy6 = 26674;
        public const uint CreateHeartCandy7 = 26675;
        public const uint CreateHeartCandy8 = 26676;

        //SomethingStinks
        public const uint HeavilyPerfumed = 71507;

        //PilferingPerfume
        public const uint ServiceUniform = 71450;
    }

    struct ModelIds
    {
        //PilferingPerfume
        public const uint GoblinMale = 31002;
        public const uint GoblinFemale = 31003;
    }

    [Script] // 45102 Romantic Picnic
    class spell_love_is_in_the_air_romantic_picnic : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BasketCheck, SpellIds.MealPeriodic, SpellIds.MealEatVisual, SpellIds.DrinkVisual, SpellIds.RomanticPicnicAchiev);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetStandState(UnitStandStateType.Sit);
            target.CastSpell(target, SpellIds.MealPeriodic);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            // Every 5 seconds
            Unit target = GetTarget();

            // If our player is no longer sit, Remove all auras
            if (target.GetStandState() != UnitStandStateType.Sit)
            {
                target.RemoveAura(SpellIds.RomanticPicnicAchiev);
                target.RemoveAura(GetAura());
                return;
            }

            target.CastSpell(target, SpellIds.BasketCheck); // unknown use, it targets Romantic Basket
            target.CastSpell(target, RandomHelper.RAND(SpellIds.MealEatVisual, SpellIds.DrinkVisual));

            bool foundSomeone = false;
            // For nearby players, check if they have the same aura. If so, cast Romantic Picnic (45123)
            // required by achievement and "hearts" visual
            List<Unit> playerList = new();
            AnyPlayerInObjectRangeCheck checker = new(target, SharedConst.InteractionDistance * 2);
            var searcher = new PlayerListSearcher(target, playerList, checker);
            Cell.VisitWorldObjects(target, searcher, SharedConst.InteractionDistance * 2);
            foreach (Player playerFound in playerList)
            {
                if (target != playerFound && playerFound.HasAura(GetId()))
                {
                    playerFound.CastSpell(playerFound, SpellIds.RomanticPicnicAchiev, true);
                    target.CastSpell(target, SpellIds.RomanticPicnicAchiev, true);
                    foundSomeone = true;
                    break;
                }
            }

            if (!foundSomeone && target.HasAura(SpellIds.RomanticPicnicAchiev))
                target.RemoveAura(SpellIds.RomanticPicnicAchiev);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 26678 - Create Heart Candy
    class spell_love_is_in_the_air_create_heart_candy : SpellScript
    {
        uint[] CreateHeartCandySpells =
        {
            SpellIds.CreateHeartCandy1, SpellIds.CreateHeartCandy2, SpellIds.CreateHeartCandy3, SpellIds.CreateHeartCandy4,
            SpellIds.CreateHeartCandy5, SpellIds.CreateHeartCandy6, SpellIds.CreateHeartCandy7, SpellIds.CreateHeartCandy8
        };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(CreateHeartCandySpells);
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Player target = GetHitPlayer();
            if (target != null)
                target.CastSpell(target, CreateHeartCandySpells.SelectRandom(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 70192 - Fragrant Air Analysis
    class spell_love_is_in_the_air_fragrant_air_analysis : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        void HandleScript(uint effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell((uint)GetEffectValue());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 71507 - Heavily Perfumed
    class spell_love_is_in_the_air_heavily_perfumed : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), (uint)GetEffectInfo(0).CalcValue());
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 71508 - Recently Analyzed
    class spell_love_is_in_the_air_recently_analyzed : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HeavilyPerfumed);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.HeavilyPerfumed);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 69438 - Sample Satisfaction
    class spell_love_is_in_the_air_sample_satisfaction : AuraScript
    {
        void OnPeriodic(AuraEffect aurEff)
        {
            if (RandomHelper.randChance(30))
                Remove();
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 71450 - Crown Parcel Service Uniform
    class spell_love_is_in_the_air_service_uniform : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            if (target.IsPlayer())
            {
                if (target.GetNativeGender() == Gender.Male)
                    target.SetDisplayId(ModelIds.GoblinMale);
                else
                    target.SetDisplayId(ModelIds.GoblinFemale);
            }
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell((uint)GetEffectInfo(0).CalcValue());
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(AfterApply, 0, AuraType.Transform, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real));
        }
    }

    // 71522 - Crown Chemical Co. Supplies
    [Script] // 71539 - Crown Chemical Co. Supplies
    class spell_love_is_in_the_air_cancel_service_uniform : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ServiceUniform);
        }

        void HandleScript(uint effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell(SpellIds.ServiceUniform);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }

    // 68529 - Perfume Immune
    [Script] // 68530 - Cologne Immune
    class spell_love_is_in_the_air_perfume_cologne_immune : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue(), (uint)spellInfo.GetEffect(1).CalcValue());
        }

        void HandleScript(uint effIndex)
        {
            GetCaster().RemoveAurasDueToSpell((uint)GetEffectValue());
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            OnEffectHit.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }
}
