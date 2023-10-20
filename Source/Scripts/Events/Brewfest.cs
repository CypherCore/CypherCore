// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Events.Brewfest
{
    struct SpellIds
    {
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

        // Quest
        public const uint BrewfestQuestSpeedBunnyGreen = 43345;
        public const uint BrewfestQuestSpeedBunnyYellow = 43346;
        public const uint BrewfestQuestSpeedBunnyRed = 43347;
    }

    struct QuestIds
    {
        // Horde
        public const uint BarkForDrohnsDistillery = 11407;
        public const uint BarkForTchalisVoodooBrewery = 11408;

        // Alliance
        public const uint BarkBarleybrew = 11293;
        public const uint BarkForThunderbrews = 11294;
    }
    struct TextIds
    {
        // Bark for Drohn's Distillery!
        public const uint SayDrohnDistillery1 = 23520;
        public const uint SayDrohnDistillery2 = 23521;
        public const uint SayDrohnDistillery3 = 23522;
        public const uint SayDrohnDistillery4 = 23523;

        // Bark for T'chali's Voodoo Brewery!
        public const uint SayTchalisVoodoo1 = 23524;
        public const uint SayTchalisVoodoo2 = 23525;
        public const uint SayTchalisVoodoo3 = 23526;
        public const uint SayTchalisVoodoo4 = 23527;

        // Bark for the Barleybrews!
        public const uint SayBarleybrew1 = 23464;
        public const uint SayBarleybrew2 = 23465;
        public const uint SayBarleybrew3 = 23466;
        public const uint SayBarleybrew4 = 22941;

        // Bark for the Thunderbrews!
        public const uint SayThunderbrews1 = 23467;
        public const uint SayThunderbrews2 = 23468;
        public const uint SayThunderbrews3 = 23469;
        public const uint SayThunderbrews4 = 22942;
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
            AfterEffectApply.Add(new(OnChange, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.ChangeAmountMask));
            OnEffectRemove.Add(new(OnChange, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.ChangeAmountMask));
            OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    // 43310 - Ram Level - Neutral
    // 42992 - Ram - Trot
    // 42993 - Ram - Canter
    [Script] // 42994 - Ram - Gallop
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
                    break;
                }
                case SpellIds.RamTrot: // green
                {
                    Aura aura = target.GetAura(SpellIds.RamFatigue);
                    if (aura != null)
                        aura.ModStackAmount(-2);
                    if (aurEff.GetTickNumber() == 4)
                        target.CastSpell(target, SpellIds.BrewfestQuestSpeedBunnyGreen, true);
                    break;
                }
                case SpellIds.RamCanter:
                {
                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.AddSpellMod(SpellValueMod.AuraStack, 1);
                    target.CastSpell(target, SpellIds.RamFatigue, args);
                    if (aurEff.GetTickNumber() == 8)
                        target.CastSpell(target, SpellIds.BrewfestQuestSpeedBunnyYellow, true);
                    break;
                }
                case SpellIds.RamGallop:
                {
                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.AddSpellMod(SpellValueMod.AuraStack, target.HasAura(SpellIds.RamFatigue) ? 4 : 5);
                    target.CastSpell(target, SpellIds.RamFatigue, args);
                    if (aurEff.GetTickNumber() == 8)
                        target.CastSpell(target, SpellIds.BrewfestQuestSpeedBunnyRed, true);
                    break;
                }
                default:
                    break;
            }

        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(OnPeriodic, 1, AuraType.PeriodicDummy));
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
            AfterEffectApply.Add(new(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script] // 43450 - Brewfest - apple trap - friendly Dnd
    class spell_brewfest_apple_trap : AuraScript
    {
        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.RamFatigue);
        }

        public override void Register()
        {
            OnEffectApply.Add(new(OnApply, 0, AuraType.ForceReaction, AuraEffectHandleModes.Real));
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
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 43714 - Brewfest - Relay Race - Intro - Force - Player to throw- Dnd
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
            OnEffectHitTarget.Add(new(HandleForceCast, 0, SpellEffectName.ForceCast));
        }
    }

    [Script] // 43755 - Brewfest - Daily - Relay Race - Player - Increase Mount Duration - Dnd
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
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 43259 Brewfest  - Barker Bunny 1
    // 43260 Brewfest  - Barker Bunny 2
    // 43261 Brewfest  - Barker Bunny 3
    [Script] // 43262 Brewfest  - Barker Bunny 4
    class spell_brewfest_barker_bunny : AuraScript
    {
        public override bool Load()
        {
            return GetUnitOwner().IsPlayer();
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player target = GetTarget().ToPlayer();

            uint BroadcastTextId = 0;

            if (target.GetQuestStatus(QuestIds.BarkForDrohnsDistillery) == QuestStatus.Incomplete ||
                target.GetQuestStatus(QuestIds.BarkForDrohnsDistillery) == QuestStatus.Complete)
                BroadcastTextId = RandomHelper.RAND(TextIds.SayDrohnDistillery1, TextIds.SayDrohnDistillery2, TextIds.SayDrohnDistillery3, TextIds.SayDrohnDistillery4);

            if (target.GetQuestStatus(QuestIds.BarkForTchalisVoodooBrewery) == QuestStatus.Incomplete ||
                target.GetQuestStatus(QuestIds.BarkForTchalisVoodooBrewery) == QuestStatus.Complete)
                BroadcastTextId = RandomHelper.RAND(TextIds.SayTchalisVoodoo1, TextIds.SayTchalisVoodoo2, TextIds.SayTchalisVoodoo3, TextIds.SayTchalisVoodoo4);

            if (target.GetQuestStatus(QuestIds.BarkBarleybrew) == QuestStatus.Incomplete ||
                target.GetQuestStatus(QuestIds.BarkBarleybrew) == QuestStatus.Complete)
                BroadcastTextId = RandomHelper.RAND(TextIds.SayBarleybrew1, TextIds.SayBarleybrew2, TextIds.SayBarleybrew3, TextIds.SayBarleybrew4);

            if (target.GetQuestStatus(QuestIds.BarkForThunderbrews) == QuestStatus.Incomplete ||
                target.GetQuestStatus(QuestIds.BarkForThunderbrews) == QuestStatus.Complete)
                BroadcastTextId = RandomHelper.RAND(TextIds.SayThunderbrews1, TextIds.SayThunderbrews2, TextIds.SayThunderbrews3, TextIds.SayThunderbrews4);

            if (BroadcastTextId != 0)
                target.Talk(BroadcastTextId, ChatMsg.Say, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), target);
        }

        public override void Register()
        {
            OnEffectApply.Add(new(OnApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 49357 - Brewfest Mount Transformation
    [Script] // 52845 - Brewfest Mount Transformation (Faction Swap)
    class spell_brewfest_mount_transformation : SpellScript
    {
        const uint SpellMountRam100 = 43900;
        const uint SpellMountRam60 = 43899;
        const uint SpellMountKodo100 = 49379;
        const uint SpellMountKodo60 = 49378;
        const uint SpellBrewfestMountTransform = 49357;
        const uint SpellBrewfestMountTransformReverse = 52845;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellMountRam100, SpellMountRam60, SpellMountKodo100, SpellMountKodo60);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            if (caster.HasAuraType(AuraType.Mounted))
            {
                caster.RemoveAurasByType(AuraType.Mounted);
                uint spell_id;

                switch (GetSpellInfo().Id)
                {
                    case SpellBrewfestMountTransform:
                        if (caster.GetSpeedRate(UnitMoveType.Run) >= 2.0f)
                            spell_id = caster.GetTeam() == Team.Alliance ? SpellMountRam100 : SpellMountKodo100;
                        else
                            spell_id = caster.GetTeam() == Team.Alliance ? SpellMountRam60 : SpellMountKodo60;
                        break;
                    case SpellBrewfestMountTransformReverse:
                        if (caster.GetSpeedRate(UnitMoveType.Run) >= 2.0f)
                            spell_id = caster.GetTeam() == Team.Horde ? SpellMountRam100 : SpellMountKodo100;
                        else
                            spell_id = caster.GetTeam() == Team.Horde ? SpellMountRam60 : SpellMountKodo60;
                        break;
                    default:
                        return;
                }
                caster.CastSpell(caster, spell_id, true);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 50098 - The Beast Within
    class spell_brewfest_botm_the_beast_within : AuraScript
    {
        const uint SpellBotmUnleashTheBeast = 50099;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBotmUnleashTheBeast);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellBotmUnleashTheBeast);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 49864 - Gassy
    class spell_brewfest_botm_gassy : AuraScript
    {
        const uint SpellBotmBelchBrewBelchVisual = 49860;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBotmBelchBrewBelchVisual);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellBotmBelchBrewBelchVisual, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 49822 - Bloated
    class spell_brewfest_botm_bloated : AuraScript
    {
        const uint SpellBotmBubbleBrewTriggerMissile = 50072;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBotmBubbleBrewTriggerMissile);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellBotmBubbleBrewTriggerMissile, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 49738 - Internal Combustion
    class spell_brewfest_botm_internal_combustion : AuraScript
    {
        const uint SpellBotmBelchFireVisual = 49737;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBotmBelchFireVisual);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellBotmBelchFireVisual, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 49962 - Jungle Madness!
    class spell_brewfest_botm_jungle_madness : SpellScript
    {
        const uint SpellBotmJungleBrewVisionEffect = 50010;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBotmJungleBrewVisionEffect);
        }

        void HandleAfterCast()
        {
            GetCaster().CastSpell(GetCaster(), SpellBotmJungleBrewVisionEffect, true);
        }

        public override void Register()
        {
            AfterCast.Add(new(HandleAfterCast));
        }
    }

    [Script] // 50243 - Teach Language
    class spell_brewfest_botm_teach_language : SpellScript
    {
        const uint SpellLearnGnomishBinary = 50242;
        const uint SpellLearnGoblinBinary = 50246;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellLearnGnomishBinary, SpellLearnGoblinBinary);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            if (caster != null)
                caster.CastSpell(caster, caster.GetTeam() == Team.Alliance ? SpellLearnGnomishBinary : SpellLearnGoblinBinary, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 42254, 42255, 42256, 42257, 42258, 42259, 42260, 42261, 42263, 42264, 43959, 43961 - Weak Alcohol
    class spell_brewfest_botm_weak_alcohol : SpellScript
    {
        const uint SpellBotmCreateEmptyBrewBottle = 51655;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBotmCreateEmptyBrewBottle);
        }

        void HandleAfterCast()
        {
            GetCaster().CastSpell(GetCaster(), SpellBotmCreateEmptyBrewBottle, true);
        }

        public override void Register()
        {
            AfterCast.Add(new(HandleAfterCast));
        }
    }

    [Script] // 51694 - Botm - Empty Bottle Throw - Resolve
    class spell_brewfest_botm_empty_bottle_throw_resolve : SpellScript
    {
        const uint SpellBotmEmptyBottleThrowImpactCreature = 51695;   // Just unit, not creature
        const uint SpellBotmEmptyBottleThrowImpactGround = 51697;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBotmEmptyBottleThrowImpactCreature, SpellBotmEmptyBottleThrowImpactGround);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();

            Unit target = GetHitUnit();
            if (target != null)
                caster.CastSpell(target, SpellBotmEmptyBottleThrowImpactCreature, true);
            else
                caster.CastSpell(GetHitDest().GetPosition(), SpellBotmEmptyBottleThrowImpactGround, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }
}