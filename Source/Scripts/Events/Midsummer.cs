// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells;

namespace Scripts.m_Events.Midsummer
{
    internal struct SpellIds
    {
        //Brazierhit
        public const uint TorchTossingTraining = 45716;
        public const uint TorchTossingPractice = 46630;
        public const uint TorchTossingTrainingSuccessAlliance = 45719;
        public const uint TorchTossingTrainingSuccessHorde = 46651;
        public const uint TargetIndicatorCosmetic = 46901;
        public const uint TargetIndicator = 45723;
        public const uint BraziersHit = 45724;

        //RibbonPoleData
        public const uint HasFullMidsummerSet = 58933;
        public const uint BurningHotPoleDance = 58934;
        public const uint RibbonPolePeriodicVisual = 45406;
        public const uint RibbonDance = 29175;
        public const uint TestRibbonPole1 = 29705;
        public const uint TestRibbonPole2 = 29726;
        public const uint TestRibbonPole3 = 29727;

        //Jugglingtorch
        public const uint JuggleTorchSlow = 45792;
        public const uint JuggleTorchMedium = 45806;
        public const uint JuggleTorchFast = 45816;
        public const uint JuggleTorchSelf = 45638;
        public const uint JuggleTorchShadowSlow = 46120;
        public const uint JuggleTorchShadowMedium = 46118;
        public const uint JuggleTorchShadowFast = 46117;
        public const uint JuggleTorchShadowSelf = 46121;
        public const uint GiveTorch = 45280;

        //Flingtorch
        public const uint FlingTorchTriggered = 45669;
        public const uint FlingTorchShadow = 46105;
        public const uint JuggleTorchMissed = 45676;
        public const uint TorchesCaught = 45693;
        public const uint TorchCatchingSuccessAlliance = 46081;
        public const uint TorchCatchingSuccessHorde = 46654;
        public const uint TorchCatchingRemoveTorches = 46084;
    }

    internal struct QuestIds
    {
        //JugglingTorch
        public const uint TorchCatchingA = 11657;
        public const uint TorchCatchingH = 11923;
        public const uint MoreTorchCatchingA = 11924;
        public const uint MoreTorchCatchingH = 11925;
    }

    [Script] // 45724 - Braziers Hit!
    internal class spell_midsummer_braziers_hit : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TorchTossingTraining, SpellIds.TorchTossingPractice);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Reapply, AuraScriptHookType.EffectAfterApply));
        }

        private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetTarget().ToPlayer();

            if (!player)
                return;

            if ((player.HasAura(SpellIds.TorchTossingTraining) && GetStackAmount() == 8) ||
                (player.HasAura(SpellIds.TorchTossingPractice) && GetStackAmount() == 20))
            {
                if (player.GetTeam() == Team.Alliance)
                    player.CastSpell(player, SpellIds.TorchTossingTrainingSuccessAlliance, true);
                else if (player.GetTeam() == Team.Horde)
                    player.CastSpell(player, SpellIds.TorchTossingTrainingSuccessHorde, true);

                Remove();
            }
        }
    }

    [Script] // 45907 - Torch Target Picker
    internal class spell_midsummer_torch_target_picker : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TargetIndicatorCosmetic, SpellIds.TargetIndicator);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(int effIndex)
        {
            Unit target = GetHitUnit();
            target.CastSpell(target, SpellIds.TargetIndicatorCosmetic, true);
            target.CastSpell(target, SpellIds.TargetIndicator, true);
        }
    }

    [Script] // 46054 - Torch Toss (land)
    internal class spell_midsummer_torch_toss_land : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BraziersHit);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(int effIndex)
        {
            GetHitUnit().CastSpell(GetCaster(), SpellIds.BraziersHit, true);
        }
    }

    [Script] // 29705, 29726, 29727 - Test Ribbon Pole Channel
    internal class spell_midsummer_test_ribbon_pole_channel : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RibbonPolePeriodicVisual, SpellIds.BurningHotPoleDance, SpellIds.HasFullMidsummerSet, SpellIds.RibbonDance);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
            AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
        }

        private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.RibbonPolePeriodicVisual);
        }

        private void PeriodicTick(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.RibbonPolePeriodicVisual, true);

            Aura aur = target.GetAura(SpellIds.RibbonDance);

            if (aur != null)
            {
                aur.SetMaxDuration(Math.Min(3600000, aur.GetMaxDuration() + 180000));
                aur.RefreshDuration();

                if (aur.GetMaxDuration() == 3600000 &&
                    target.HasAura(SpellIds.HasFullMidsummerSet))
                    target.CastSpell(target, SpellIds.BurningHotPoleDance, true);
            }
            else
            {
                target.CastSpell(target, SpellIds.RibbonDance, true);
            }
        }
    }

    [Script] // 45406 - Holiday - Midsummer, Ribbon Pole Periodic Visual
    internal class spell_midsummer_ribbon_pole_periodic_visual : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TestRibbonPole1, SpellIds.TestRibbonPole2, SpellIds.TestRibbonPole3);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDummy));
        }

        private void PeriodicTick(AuraEffect aurEff)
        {
            Unit target = GetTarget();

            if (!target.HasAura(SpellIds.TestRibbonPole1) &&
                !target.HasAura(SpellIds.TestRibbonPole2) &&
                !target.HasAura(SpellIds.TestRibbonPole3))
                Remove();
        }
    }

    [Script] // 45819 - Throw Torch
    internal class spell_midsummer_juggle_torch : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.JuggleTorchSlow,
                                     SpellIds.JuggleTorchMedium,
                                     SpellIds.JuggleTorchFast,
                                     SpellIds.JuggleTorchSelf,
                                     SpellIds.JuggleTorchShadowSlow,
                                     SpellIds.JuggleTorchShadowMedium,
                                     SpellIds.JuggleTorchShadowFast,
                                     SpellIds.JuggleTorchShadowSelf);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        private void HandleDummy(int effIndex)
        {
            if (GetExplTargetDest() == null)
                return;

            Position spellDest = GetExplTargetDest();
            double distance = GetCaster().GetExactDist2d(spellDest.GetPositionX(), spellDest.GetPositionY());

            uint torchSpellID = 0;
            uint torchShadowSpellID = 0;

            if (distance <= 1.5f)
            {
                torchSpellID = SpellIds.JuggleTorchSelf;
                torchShadowSpellID = SpellIds.JuggleTorchShadowSelf;
                spellDest = GetCaster().GetPosition();
            }
            else if (distance <= 10.0f)
            {
                torchSpellID = SpellIds.JuggleTorchSlow;
                torchShadowSpellID = SpellIds.JuggleTorchShadowSlow;
            }
            else if (distance <= 20.0f)
            {
                torchSpellID = SpellIds.JuggleTorchMedium;
                torchShadowSpellID = SpellIds.JuggleTorchShadowMedium;
            }
            else
            {
                torchSpellID = SpellIds.JuggleTorchFast;
                torchShadowSpellID = SpellIds.JuggleTorchShadowFast;
            }

            GetCaster().CastSpell(spellDest, torchSpellID, new CastSpellExtraArgs(false));
            GetCaster().CastSpell(spellDest, torchShadowSpellID, new CastSpellExtraArgs(false));
        }
    }

    [Script] // 45644 - Juggle Torch (Catch)
    internal class spell_midsummer_torch_catch : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GiveTorch);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(int effIndex)
        {
            Player player = GetHitPlayer();

            if (!player)
                return;

            if (player.GetQuestStatus(QuestIds.TorchCatchingA) == QuestStatus.Rewarded ||
                player.GetQuestStatus(QuestIds.TorchCatchingH) == QuestStatus.Rewarded)
                player.CastSpell(player, SpellIds.GiveTorch);
        }
    }

    [Script] // 46747 - Fling torch
    internal class spell_midsummer_fling_torch : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlingTorchTriggered, SpellIds.FlingTorchShadow);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        private void HandleDummy(int effIndex)
        {
            Position dest = GetCaster().GetFirstCollisionPosition(30.0f, (float)RandomHelper.NextDouble() * (2 * MathF.PI));
            GetCaster().CastSpell(dest, SpellIds.FlingTorchTriggered, new CastSpellExtraArgs(true));
            GetCaster().CastSpell(dest, SpellIds.FlingTorchShadow, new CastSpellExtraArgs(false));
        }
    }

    [Script] // 45669 - Fling Torch
    internal class spell_midsummer_fling_torch_triggered : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.JuggleTorchMissed);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleTriggerMissile, 0, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHit));
        }

        private void HandleTriggerMissile(int effIndex)
        {
            Position pos = GetHitDest();

            if (pos != null)
                if (GetCaster().GetExactDist2d(pos) > 3.0f)
                {
                    PreventHitEffect(effIndex);
                    GetCaster().CastSpell(GetExplTargetDest(), SpellIds.JuggleTorchMissed, new CastSpellExtraArgs(false));
                    GetCaster().RemoveAura(SpellIds.TorchesCaught);
                }
        }
    }

    [Script] // 45671 - Juggle Torch (Catch, Quest)
    internal class spell_midsummer_fling_torch_catch : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlingTorchTriggered, SpellIds.TorchCatchingSuccessAlliance, SpellIds.TorchCatchingSuccessHorde, SpellIds.TorchCatchingRemoveTorches, SpellIds.FlingTorchShadow);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(int effIndex)
        {
            Player player = GetHitPlayer();

            if (!player)
                return;

            if (GetExplTargetDest() == null)
                return;

            // Only the caster can catch the torch
            if (player.GetGUID() != GetCaster().GetGUID())
                return;

            byte requiredCatches = 0;

            // Number of required catches depends on quest - 4 for the normal quest, 10 for the daily version
            if (player.GetQuestStatus(QuestIds.TorchCatchingA) == QuestStatus.Incomplete ||
                player.GetQuestStatus(QuestIds.TorchCatchingH) == QuestStatus.Incomplete)
                requiredCatches = 3;
            else if (player.GetQuestStatus(QuestIds.MoreTorchCatchingA) == QuestStatus.Incomplete ||
                     player.GetQuestStatus(QuestIds.MoreTorchCatchingH) == QuestStatus.Incomplete)
                requiredCatches = 9;

            // Used quest Item without being on quest - do nothing
            if (requiredCatches == 0)
                return;

            if (player.GetAuraCount(SpellIds.TorchesCaught) >= requiredCatches)
            {
                player.CastSpell(player, (player.GetTeam() == Team.Alliance) ? SpellIds.TorchCatchingSuccessAlliance : SpellIds.TorchCatchingSuccessHorde);
                player.CastSpell(player, SpellIds.TorchCatchingRemoveTorches);
                player.RemoveAura(SpellIds.TorchesCaught);
            }
            else
            {
                Position dest = player.GetFirstCollisionPosition(15.0f, (float)RandomHelper.NextDouble() * (2 * MathF.PI));
                player.CastSpell(player, SpellIds.TorchesCaught);
                player.CastSpell(dest, SpellIds.FlingTorchTriggered, new CastSpellExtraArgs(true));
                player.CastSpell(dest, SpellIds.FlingTorchShadow, new CastSpellExtraArgs(false));
            }
        }
    }

    [Script] // 45676 - Juggle Torch (Quest, Missed)
    internal class spell_midsummer_fling_torch_missed : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEntry));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 2, Targets.UnitDestAreaEntry));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            // This spell only hits the caster
            targets.RemoveAll(obj => obj.GetGUID() != GetCaster().GetGUID());
        }
    }
}