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
using Game;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Holiday
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

        //Pilgrims Bounty
        public const uint WellFedApTrigger = 65414;
        public const uint WellFedZmTrigger = 65412;
        public const uint WellFedHitTrigger = 65416;
        public const uint WellFedHasteTrigger = 65410;
        public const uint WellFedSpiritTrigger = 65415;

        //Mistletoe
        public const uint CreateMistletoe = 26206;
        public const uint CreateHolly = 26207;
        public const uint CreateSnowflakes = 45036;

        //Winter Wondervolt
        public const uint Px238WinterWondervoltTransform1 = 26157;
        public const uint Px238WinterWondervoltTransform2 = 26272;
        public const uint Px238WinterWondervoltTransform3 = 26273;
        public const uint Px238WinterWondervoltTransform4 = 26274;

        //Ramblabla
        public const uint Giddyup = 42924;
        public const uint RentalRacingRam = 43883;
        public const uint SwiftWorkRam = 43880;
        public const uint RentalRacingRamAura = 42146;
        public const uint RamLevelNeutral = 43310;
        public const uint RamTrot = 42992;
        public const uint RamCanter = 42993;
        public const uint RamGallop = 42994;
        public const uint RamFatigue = 43052;
        public const uint ExhaustedRam = 43332;
        public const uint RelayRaceTurnIn = 44501;

        //Brazierhit
        public const uint TorchTossingTraining = 45716;
        public const uint TorchTossingPractice = 46630;
        public const uint TorchTossingTrainingSuccessAlliance = 45719;
        public const uint TorchTossingTrainingSuccessHorde = 46651;
        public const uint BraziersHit = 45724;

        //Ribbonpoledata
        public const uint HasFullMidsummerSet = 58933;
        public const uint BurningHotPoleDance = 58934;
        public const uint RibbonDanceCosmetic = 29726;
        public const uint RibbonDance = 29175;
    }

    struct QuestIds
    {
        //Ramblabla
        public const uint BrewfestSpeedBunnyGreen = 43345;
        public const uint BrewfestSpeedBunnyYellow = 43346;
        public const uint BrewfestSpeedBunnyRed = 43347;

        //Barkerbunny
        // Horde
        public const uint BarkForDrohnsDistillery = 11407;
        public const uint BarkForTchalisVoodooBrewery = 11408;

        // Alliance
        public const uint BarkBarleybrew = 11293;
        public const uint BarkForThunderbrews = 11294;
    }

    struct TextIds
    {
        // Bark For Drohn'S Distillery!
        public const uint DrohnDistillery1 = 23520;
        public const uint DrohnDistillery2 = 23521;
        public const uint DrohnDistillery3 = 23522;
        public const uint DrohnDistillery4 = 23523;

        // Bark For T'Chali'S Voodoo Brewery!
        public const uint TChalisVoodoo1 = 23524;
        public const uint TChalisVoodoo2 = 23525;
        public const uint TChalisVoodoo3 = 23526;
        public const uint TChalisVoodoo4 = 23527;

        // Bark For The Barleybrews!
        public const uint Barleybrew1 = 23464;
        public const uint Barleybrew2 = 23465;
        public const uint Barleybrew3 = 23466;
        public const uint Barleybrew4 = 22941;

        // Bark For The Thunderbrews!
        public const uint Thunderbrews1 = 23467;
        public const uint Thunderbrews2 = 23468;
        public const uint Thunderbrews3 = 23469;
        public const uint Thunderbrews4 = 22942;
    }

    struct GameobjectIds
    {
        public const uint RibbonPole = 181605;
    }



    [Script] // 45102 Romantic Picnic
    class spell_love_is_in_the_air_romantic_picnic : AuraScript
    {
        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetStandState(UnitStandStateType.Sit);
            target.CastSpell(target, SpellIds.MealPeriodic, false);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            // Every 5 seconds
            Unit target = GetTarget();
            Unit caster = GetCaster();

            // If our player is no longer sit, remove all auras
            if (target.GetStandState() != UnitStandStateType.Sit)
            {
                target.RemoveAura(SpellIds.RomanticPicnicAchiev);
                target.RemoveAura(GetAura());
                return;
            }

            target.CastSpell(target, SpellIds.BasketCheck, false); // unknown use, it targets Romantic Basket
            target.CastSpell(target, RandomHelper.RAND(SpellIds.MealEatVisual, SpellIds.DrinkVisual), false);

            bool foundSomeone = false;
            // For nearby players, check if they have the same aura. If so, cast Romantic Picnic (45123)
            // required by achievement and "hearts" visual
            List<Player> playerList = new List<Player>();
            AnyPlayerInObjectRangeCheck checker = new AnyPlayerInObjectRangeCheck(target, SharedConst.InteractionDistance * 2);
            var searcher = new PlayerListSearcher(target, playerList, checker);
            Cell.VisitWorldObjects(target, searcher, SharedConst.InteractionDistance * 2);
            foreach (var player in playerList)
            {
                if (player != target && player.HasAura(GetId())) // && player.GetStandState() == UNIT_STAND_STATE_SIT)
                {
                    if (caster)
                    {
                        caster.CastSpell(player, SpellIds.RomanticPicnicAchiev, true);
                        caster.CastSpell(target, SpellIds.RomanticPicnicAchiev, true);
                    }
                    foundSomeone = true;
                    // break;
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
                Gender gender = target.GetGender();
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

    [Script]
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

    [Script]
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

            uint spellId = 0;
            bool female = target.GetGender() == Gender.Female;

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

    [Script("spell_gen_slow_roasted_turkey", SpellIds.WellFedApTrigger)]
    [Script("spell_gen_cranberry_chutney", SpellIds.WellFedZmTrigger)]
    [Script("spell_gen_spice_bread_stuffing", SpellIds.WellFedHitTrigger)]
    [Script("spell_gen_pumpkin_pie", SpellIds.WellFedSpiritTrigger)]
    [Script("spell_gen_candied_sweet_potato", SpellIds.WellFedHasteTrigger)]
    class spell_pilgrims_bounty_buff_food : AuraScript
    {
        public spell_pilgrims_bounty_buff_food(uint triggeredSpellId)
        {
            _triggeredSpellId = triggeredSpellId;
            _handled = false;
        }

        void HandleTriggerSpell(AuraEffect aurEff)
        {
            PreventDefaultAction();
            if (_handled)
                return;

            _handled = true;
            GetTarget().CastSpell(GetTarget(), _triggeredSpellId, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleTriggerSpell, 2, AuraType.PeriodicTriggerSpell));
        }

        uint _triggeredSpellId;

        bool _handled;
    }

    [Script]
    class spell_winter_veil_mistletoe : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.CreateMistletoe, SpellIds.CreateHolly, SpellIds.CreateSnowflakes);
        }

        void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();
            if (target)
            {
                uint spellId = RandomHelper.RAND(SpellIds.CreateHolly, SpellIds.CreateMistletoe, SpellIds.CreateSnowflakes);
                GetCaster().CastSpell(target, spellId, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 26275 - PX-238 Winter Wondervolt TRAP
    class spell_winter_veil_px_238_winter_wondervolt : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Px238WinterWondervoltTransform1, SpellIds.Px238WinterWondervoltTransform2,
                SpellIds.Px238WinterWondervoltTransform3, SpellIds.Px238WinterWondervoltTransform4);
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            uint[] spells =
            {
                    SpellIds.Px238WinterWondervoltTransform1,
                    SpellIds.Px238WinterWondervoltTransform2,
                    SpellIds.Px238WinterWondervoltTransform3,
                    SpellIds.Px238WinterWondervoltTransform4
                };

            Unit target = GetHitUnit();
            if (target)
            {
                for (byte i = 0; i < 4; ++i)
                    if (target.HasAura(spells[i]))
                        return;

                target.CastSpell(target, spells[RandomHelper.URand(0, 3)], true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 42924 - Giddyup!
    class spell_brewfest_giddyup : AuraScript
    {
        void OnChange(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            if (!target.HasAura(SpellIds.RentalRacingRam) && !target.HasAura(SpellIds.SwiftWorkRam))
            {
                target.RemoveAura(GetId());
                return;
            }

            if (target.HasAura(SpellIds.ExhaustedRam))
                return;

            switch (GetStackAmount())
            {
                case 1: // green
                    target.RemoveAura(SpellIds.RamLevelNeutral);
                    target.RemoveAura(SpellIds.RamCanter);
                    target.CastSpell(target, SpellIds.RamTrot, true);
                    break;
                case 6: // yellow
                    target.RemoveAura(SpellIds.RamTrot);
                    target.RemoveAura(SpellIds.RamGallop);
                    target.CastSpell(target, SpellIds.RamCanter, true);
                    break;
                case 11: // red
                    target.RemoveAura(SpellIds.RamCanter);
                    target.CastSpell(target, SpellIds.RamGallop, true);
                    break;
                default:
                    break;
            }

            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Default)
            {
                target.RemoveAura(SpellIds.RamTrot);
                target.CastSpell(target, SpellIds.RamLevelNeutral, true);
            }
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            GetTarget().RemoveAuraFromStack(GetId());
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(OnChange, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.ChangeAmountMask));
            OnEffectRemove.Add(new EffectApplyHandler(OnChange, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.ChangeAmountMask));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    // 43310 - Ram Level - Neutral
    // 42992 - Ram - Trot
    // 42993 - Ram - Canter
    // 42994 - Ram - Gallop
    [Script]
    class spell_brewfest_ram : AuraScript
    {
        void OnPeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            if (target.HasAura(SpellIds.ExhaustedRam))
                return;

            switch (GetId())
            {
                case SpellIds.RamLevelNeutral:
                    {
                        Aura aura = target.GetAura(SpellIds.RamFatigue);
                        if (aura != null)
                            aura.ModStackAmount(-4);
                    }
                    break;
                case SpellIds.RamTrot: // green
                    {
                        Aura aura = target.GetAura(SpellIds.RamFatigue);
                        if (aura != null)
                            aura.ModStackAmount(-2);
                        if (aurEff.GetTickNumber() == 4)
                            target.CastSpell(target, QuestIds.BrewfestSpeedBunnyGreen, true);
                    }
                    break;
                case SpellIds.RamCanter:
                    target.CastCustomSpell(SpellIds.RamFatigue, SpellValueMod.AuraStack, 1, target, TriggerCastFlags.FullMask);
                    if (aurEff.GetTickNumber() == 8)
                        target.CastSpell(target, QuestIds.BrewfestSpeedBunnyYellow, true);
                    break;
                case SpellIds.RamGallop:
                    target.CastCustomSpell(SpellIds.RamFatigue, SpellValueMod.AuraStack, target.HasAura(SpellIds.RamFatigue) ? 4 : 5 /*Hack*/, target, TriggerCastFlags.FullMask);
                    if (aurEff.GetTickNumber() == 8)
                        target.CastSpell(target, QuestIds.BrewfestSpeedBunnyRed, true);
                    break;
                default:
                    break;
            }

        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 1, AuraType.PeriodicDummy));
        }
    }

    [Script] // 43052 - Ram Fatigue
    class spell_brewfest_ram_fatigue : AuraScript
    {
        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();

            if (GetStackAmount() == 101)
            {
                target.RemoveAura(SpellIds.RamLevelNeutral);
                target.RemoveAura(SpellIds.RamTrot);
                target.RemoveAura(SpellIds.RamCanter);
                target.RemoveAura(SpellIds.RamGallop);
                target.RemoveAura(SpellIds.Giddyup);

                target.CastSpell(target, SpellIds.ExhaustedRam, true);
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script] // 43450 - Brewfest - apple trap - friendly DND
    class spell_brewfest_apple_trap : AuraScript
    {
        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.RamFatigue);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.ForceReaction, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 43332 - Exhausted Ram
    class spell_brewfest_exhausted_ram : AuraScript
    {
        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.RamLevelNeutral, true);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 43714 - Brewfest - Relay Race - Intro - Force - Player to throw- DND
    class spell_brewfest_relay_race_intro_force_player_to_throw : SpellScript
    {
        void HandleForceCast(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            // All this spells trigger a spell that requires reagents; if the
            // triggered spell is cast as "triggered", reagents are not consumed
            GetHitUnit().CastSpell(null, GetEffectInfo().TriggerSpell, TriggerCastFlags.FullMask & ~TriggerCastFlags.IgnorePowerAndReagentCost);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleForceCast, 0, SpellEffectName.ForceCast));
        }
    }

    [Script]
    class spell_brewfest_relay_race_turn_in : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Aura aura = GetHitUnit().GetAura(SpellIds.SwiftWorkRam);
            if (aura != null)
            {
                aura.SetDuration(aura.GetDuration() + 30 * Time.InMilliseconds);
                GetCaster().CastSpell(GetHitUnit(), SpellIds.RelayRaceTurnIn, TriggerCastFlags.FullMask);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 43876 - Dismount Ram
    class spell_brewfest_dismount_ram : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            GetCaster().RemoveAura(SpellIds.RentalRacingRam);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 43259 Brewfest  - Barker Bunny 1
    // 43260 Brewfest  - Barker Bunny 2
    // 43261 Brewfest  - Barker Bunny 3
    // 43262 Brewfest  - Barker Bunny 4
    [Script]
    class spell_brewfest_barker_bunny : AuraScript
    {
        public override bool Load()
        {
            return GetUnitOwner().IsTypeId(TypeId.Player);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player target = GetTarget().ToPlayer();

            uint BroadcastTextId = 0;

            if (target.GetQuestStatus(QuestIds.BarkForDrohnsDistillery) == QuestStatus.Incomplete ||
                target.GetQuestStatus(QuestIds.BarkForDrohnsDistillery) == QuestStatus.Complete)
                BroadcastTextId = RandomHelper.RAND(TextIds.DrohnDistillery1, TextIds.DrohnDistillery2, TextIds.DrohnDistillery3, TextIds.DrohnDistillery4);

            if (target.GetQuestStatus(QuestIds.BarkForTchalisVoodooBrewery) == QuestStatus.Incomplete ||
                target.GetQuestStatus(QuestIds.BarkForTchalisVoodooBrewery) == QuestStatus.Complete)
                BroadcastTextId = RandomHelper.RAND(TextIds.TChalisVoodoo1, TextIds.TChalisVoodoo2, TextIds.TChalisVoodoo3, TextIds.TChalisVoodoo4);

            if (target.GetQuestStatus(QuestIds.BarkBarleybrew) == QuestStatus.Incomplete ||
                target.GetQuestStatus(QuestIds.BarkBarleybrew) == QuestStatus.Complete)
                BroadcastTextId = RandomHelper.RAND(TextIds.Barleybrew1, TextIds.Barleybrew2, TextIds.Barleybrew3, TextIds.Barleybrew4);

            if (target.GetQuestStatus(QuestIds.BarkForThunderbrews) == QuestStatus.Incomplete ||
                target.GetQuestStatus(QuestIds.BarkForThunderbrews) == QuestStatus.Complete)
                BroadcastTextId = RandomHelper.RAND(TextIds.Thunderbrews1, TextIds.Thunderbrews2, TextIds.Thunderbrews3, TextIds.Thunderbrews4);

            if (BroadcastTextId != 0)
                target.Talk(BroadcastTextId, ChatMsg.Say, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), target);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 45724 - Braziers Hit!
    class spell_midsummer_braziers_hit : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TorchTossingTraining, SpellIds.TorchTossingPractice);
        }

        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetTarget().ToPlayer();
            if (!player)
                return;

            if ((player.HasAura(SpellIds.TorchTossingTraining) && GetStackAmount() == 8) || (player.HasAura(SpellIds.TorchTossingPractice) && GetStackAmount() == 20))
            {
                if (player.GetTeam() == Team.Alliance)
                    player.CastSpell(player, SpellIds.TorchTossingTrainingSuccessAlliance, true);
                else if (player.GetTeam() == Team.Horde)
                    player.CastSpell(player, SpellIds.TorchTossingTrainingSuccessHorde, true);
                Remove();
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Reapply));
        }
    }

    [Script]
    class spell_gen_ribbon_pole_dancer_check : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HasFullMidsummerSet, SpellIds.RibbonDance, SpellIds.BurningHotPoleDance);
        }

        void PeriodicTick(AuraEffect aurEff)
        {
            Unit target = GetTarget();

            // check if aura needs to be removed
            if (!target.FindNearestGameObject(GameobjectIds.RibbonPole, 8.0f) || !target.HasUnitState(UnitState.Casting))
            {
                target.InterruptNonMeleeSpells(false);
                target.RemoveAurasDueToSpell(GetId());
                target.RemoveAura(SpellIds.RibbonDanceCosmetic);
                return;
            }

            // set xp buff duration
            Aura aur = target.GetAura(SpellIds.RibbonDance);
            if (aur != null)
            {
                aur.SetMaxDuration(Math.Min(3600000, aur.GetMaxDuration() + 180000));
                aur.RefreshDuration();

                // reward achievement criteria
                if (aur.GetMaxDuration() == 3600000 && target.HasAura(SpellIds.HasFullMidsummerSet))
                    target.CastSpell(target, SpellIds.BurningHotPoleDance, true);
            }
            else
                target.AddAura(SpellIds.RibbonDance, target);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDummy));
        }
    }
}