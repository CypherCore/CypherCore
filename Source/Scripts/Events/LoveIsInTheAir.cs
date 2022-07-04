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
    class spell_item_create_heart_candy : SpellScript
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
}
