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
    [Script] // 45102 - Romantic Picnic
    class spell_love_is_in_the_air_romantic_picnic : AuraScript
    {
        const uint SpellBasketCheck = 45119; // Holiday - Valentine - Romantic Picnic Near Basket Check
        const uint SpellMealPeriodic = 45103; // Holiday - Valentine - Romantic Picnic Meal Periodic - effect dummy
        const uint SpellMealEatVisual = 45120; // Holiday - Valentine - Romantic Picnic Meal Eat Visual
        // const uint SpellMealParticle          = 45114; // Holiday - Valentine - Romantic Picnic Meal Particle - unused
        const uint SpellDrinkVisual = 45121; // Holiday - Valentine - Romantic Picnic Drink Visual
        const uint SpellRomanticPicnicAchiev = 45123; // Romantic Picnic periodic = 5000

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBasketCheck, SpellMealPeriodic, SpellMealEatVisual, SpellDrinkVisual, SpellRomanticPicnicAchiev);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetStandState(UnitStandStateType.Sit);
            target.CastSpell(target, SpellMealPeriodic);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            // Every 5 seconds
            Unit target = GetTarget();

            // If our player is no longer sit, Remove all auras
            if (target.GetStandState() != UnitStandStateType.Sit)
            {
                target.RemoveAurasDueToSpell(SpellRomanticPicnicAchiev);
                target.RemoveAura(GetAura());
                return;
            }

            target.CastSpell(target, SpellBasketCheck); // unknown use, it targets Romantic Basket
            target.CastSpell(target, RandomHelper.RAND(SpellMealEatVisual, SpellDrinkVisual));

            bool foundSomeone = false;
            // For nearby players, check if they have the same aura. If so, cast Romantic Picnic (45123)
            // required by achievement and "hearts" visual
            List<Unit> playerList = new();
            AnyPlayerInObjectRangeCheck checker = new(target, SharedConst.InteractionDistance * 2);
            PlayerListSearcher searcher = new(target, playerList, checker);
            Cell.VisitWorldObjects(target, searcher, SharedConst.InteractionDistance * 2);
            foreach (var playerFound in playerList)
            {
                if (playerFound != null)
                {
                    if (target != playerFound && playerFound.HasAura(GetId()))
                    {
                        playerFound.CastSpell(playerFound, SpellRomanticPicnicAchiev, true);
                        target.CastSpell(target, SpellRomanticPicnicAchiev, true);
                        foundSomeone = true;
                        break;
                    }
                }
            }

            if (!foundSomeone && target.HasAura(SpellRomanticPicnicAchiev))
                target.RemoveAurasDueToSpell(SpellRomanticPicnicAchiev);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
            OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 26678 - Create Heart Candy
    class spell_love_is_in_the_air_create_heart_candy : SpellScript
    {
        uint[] CreateHeartCandySpells = { 26668, 26670, 26671, 26672, 26673, 26674, 26675, 26676 };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(CreateHeartCandySpells);
        }

        void HandleScript(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), CreateHeartCandySpells.SelectRandom());
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 71508 - Recently Analyzed
    class spell_love_is_in_the_air_recently_analyzed : AuraScript
    {
        const uint SpellHeavilyPerfumed = 71507;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellHeavilyPerfumed);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
                GetTarget().CastSpell(GetTarget(), SpellHeavilyPerfumed);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
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
            OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 71450 - Crown Parcel Service Uniform
    class spell_love_is_in_the_air_service_uniform : AuraScript
    {
        const uint ModelGoblinMale = 31002;
        const uint ModelGoblinFemale = 31003;

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
                    target.SetDisplayId(ModelGoblinMale);
                else
                    target.SetDisplayId(ModelGoblinFemale);
            }
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell((uint)GetEffectInfo(0).CalcValue());
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(AfterApply, 0, AuraType.Transform, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real));
        }
    }

    // 71522 - Crown Chemical Co. Supplies
    [Script] // 71539 - Crown Chemical Co. Supplies
    class spell_love_is_in_the_air_cancel_service_uniform : SpellScript
    {
        const uint SpellServiceUniform = 71450;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellServiceUniform);
        }

        void HandleScript(uint effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell(SpellServiceUniform);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
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
            OnEffectHit.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
            OnEffectHit.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }
}