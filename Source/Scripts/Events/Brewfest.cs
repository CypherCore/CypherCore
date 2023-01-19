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

        //Brewfestmounttransformation
        public const uint MountRam100 = 43900;
        public const uint MountRam60 = 43899;
        public const uint MountKodo100 = 49379;
        public const uint MountKodo60 = 49378;
        public const uint BrewfestMountTransform = 49357;
        public const uint BrewfestMountTransformReverse = 52845;
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
                {
                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.AddSpellMod(SpellValueMod.AuraStack, 1);
                    target.CastSpell(target, SpellIds.RamFatigue, args);
                    if (aurEff.GetTickNumber() == 8)
                        target.CastSpell(target, QuestIds.BrewfestSpeedBunnyYellow, true);
                    break;
                }
                case SpellIds.RamGallop:
                {
                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.AddSpellMod(SpellValueMod.AuraStack, target.HasAura(SpellIds.RamFatigue) ? 4 : 5 /*Hack*/);
                    target.CastSpell(target, SpellIds.RamFatigue, args);
                    if (aurEff.GetTickNumber() == 8)
                        target.CastSpell(target, QuestIds.BrewfestSpeedBunnyRed, true);
                    break;
                }
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
            GetHitUnit().CastSpell((Unit)null, GetEffectInfo().TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask & ~TriggerCastFlags.IgnorePowerAndReagentCost));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleForceCast, 0, SpellEffectName.ForceCast));
        }
    }

    [Script] // 43755 - Brewfest - Daily - Relay Race - Player - Increase Mount Duration - DND
    class spell_brewfest_relay_race_turn_in : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Aura aura = GetHitUnit().GetAura(SpellIds.SwiftWorkRam);
            if (aura != null)
            {
                aura.SetDuration(aura.GetDuration() + 30 * Time.InMilliseconds);
                GetCaster().CastSpell(GetHitUnit(), SpellIds.RelayRaceTurnIn, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
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

    [Script]
    class spell_item_brewfest_mount_transformation : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.MountRam100, SpellIds.MountRam60, SpellIds.MountKodo100, SpellIds.MountKodo60);
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
                    case SpellIds.BrewfestMountTransform:
                        if (caster.GetSpeedRate(UnitMoveType.Run) >= 2.0f)
                            spell_id = caster.GetTeam() == Team.Alliance ? SpellIds.MountRam100 : SpellIds.MountKodo100;
                        else
                            spell_id = caster.GetTeam() == Team.Alliance ? SpellIds.MountRam60 : SpellIds.MountKodo60;
                        break;
                    case SpellIds.BrewfestMountTransformReverse:
                        if (caster.GetSpeedRate(UnitMoveType.Run) >= 2.0f)
                            spell_id = caster.GetTeam() == Team.Horde ? SpellIds.MountRam100 : SpellIds.MountKodo100;
                        else
                            spell_id = caster.GetTeam() == Team.Horde ? SpellIds.MountRam60 : SpellIds.MountKodo60;
                        break;
                    default:
                        return;
                }
                caster.CastSpell(caster, spell_id, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }
}
