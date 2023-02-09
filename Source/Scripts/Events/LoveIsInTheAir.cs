// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;


using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells;

namespace Scripts.m_Events.LoveIsInTheAir
{
    internal struct SpellIds
    {
        //Romantic Picnic
        public const uint BasketCheck = 45119;  // Holiday - Valentine - Romantic Picnic Near Basket Check
        public const uint MealPeriodic = 45103; // Holiday - Valentine - Romantic Picnic Meal Periodic - Effect Dummy

        public const uint MealEatVisual = 45120; // Holiday - Valentine - Romantic Picnic Meal Eat Visual

        //public const uint MealParticle = 45114; // Holiday - Valentine - Romantic Picnic Meal Particle - Unused
        public const uint DrinkVisual = 45121;          // Holiday - Valentine - Romantic Picnic Drink Visual
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

    internal struct ModelIds
    {
        //PilferingPerfume
        public const uint GoblinMale = 31002;
        public const uint GoblinFemale = 31003;
    }

    [Script] // 45102 Romantic Picnic
    internal class spell_love_is_in_the_air_romantic_picnic : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BasketCheck, SpellIds.MealPeriodic, SpellIds.MealEatVisual, SpellIds.DrinkVisual, SpellIds.RomanticPicnicAchiev);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetStandState(UnitStandStateType.Sit);
            target.CastSpell(target, SpellIds.MealPeriodic);
        }

        private void OnPeriodic(AuraEffect aurEff)
        {
            // Every 5 seconds
            Unit target = GetTarget();

            // If our player is no longer sit, Remove all Auras
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
            // required by Achievement and "hearts" visual
            List<Unit> playerList = new();
            AnyPlayerInObjectRangeCheck checker = new(target, SharedConst.InteractionDistance * 2);
            var searcher = new PlayerListSearcher(target, playerList, checker);
            Cell.VisitWorldObjects(target, searcher, SharedConst.InteractionDistance * 2);

            foreach (Player playerFound in playerList)
                if (target != playerFound &&
                    playerFound.HasAura(GetId()))
                {
                    playerFound.CastSpell(playerFound, SpellIds.RomanticPicnicAchiev, true);
                    target.CastSpell(target, SpellIds.RomanticPicnicAchiev, true);
                    foundSomeone = true;

                    break;
                }

            if (!foundSomeone &&
                target.HasAura(SpellIds.RomanticPicnicAchiev))
                target.RemoveAura(SpellIds.RomanticPicnicAchiev);
        }
    }

    [Script] // 26678 - Create Heart Candy
    internal class spell_love_is_in_the_air_create_heart_candy : SpellScript, IHasSpellEffects
    {
        private readonly uint[] CreateHeartCandySpells =
        {
            SpellIds.CreateHeartCandy1, SpellIds.CreateHeartCandy2, SpellIds.CreateHeartCandy3, SpellIds.CreateHeartCandy4, SpellIds.CreateHeartCandy5, SpellIds.CreateHeartCandy6, SpellIds.CreateHeartCandy7, SpellIds.CreateHeartCandy8
        };

        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(CreateHeartCandySpells);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(int effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Player target = GetHitPlayer();

            target?.CastSpell(target, CreateHeartCandySpells.SelectRandom(), true);
        }
    }

    [Script] // 70192 - Fragrant Air Analysis
    internal class spell_love_is_in_the_air_fragrant_air_analysis : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(int effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell((uint)GetEffectValue());
        }
    }

    [Script] // 71507 - Heavily Perfumed
    internal class spell_love_is_in_the_air_heavily_perfumed : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), (uint)GetEffectInfo(0).CalcValue());
        }
    }

    [Script] // 71508 - Recently Analyzed
    internal class spell_love_is_in_the_air_recently_analyzed : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HeavilyPerfumed);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.HeavilyPerfumed);
        }
    }

    [Script] // 69438 - Sample Satisfaction
    internal class spell_love_is_in_the_air_sample_satisfaction : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void OnPeriodic(AuraEffect aurEff)
        {
            if (RandomHelper.randChance(30))
                Remove();
        }
    }

    [Script] // 71450 - Crown Parcel Service Uniform
    internal class spell_love_is_in_the_air_service_uniform : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
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

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell((uint)GetEffectInfo(0).CalcValue());
        }
    }

    // 71522 - Crown Chemical Co. Supplies
    [Script] // 71539 - Crown Chemical Co. Supplies
    internal class spell_love_is_in_the_air_cancel_service_uniform : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ServiceUniform);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(int effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell(SpellIds.ServiceUniform);
        }
    }

    // 68529 - Perfume Immune
    [Script] // 68530 - Cologne Immune
    internal class spell_love_is_in_the_air_perfume_cologne_immune : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue(), (uint)spellInfo.GetEffect(1).CalcValue());
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
            SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
        }

        private void HandleScript(int effIndex)
        {
            GetCaster().RemoveAurasDueToSpell((uint)GetEffectValue());
        }
    }
}