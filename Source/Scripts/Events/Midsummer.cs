// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Events.Midsummer
{
    struct SpellIds
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

    struct QuestIds
    {
        //JugglingTorch
        public const uint TorchCatchingA = 11657;
        public const uint TorchCatchingH = 11923;
        public const uint MoreTorchCatchingA = 11924;
        public const uint MoreTorchCatchingH = 11925;
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

    [Script] // 45907 - Torch Target Picker
    class spell_midsummer_torch_target_picker : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TargetIndicatorCosmetic, SpellIds.TargetIndicator);
        }

        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            target.CastSpell(target, SpellIds.TargetIndicatorCosmetic, true);
            target.CastSpell(target, SpellIds.TargetIndicator, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 46054 - Torch Toss (land)
    class spell_midsummer_torch_toss_land : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BraziersHit);
        }

        void HandleScript(uint effIndex)
        {
            GetHitUnit().CastSpell(GetCaster(), SpellIds.BraziersHit, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 29705, 29726, 29727 - Test Ribbon Pole Channel
    class spell_midsummer_test_ribbon_pole_channel : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RibbonPolePeriodicVisual, SpellIds.BurningHotPoleDance, SpellIds.HasFullMidsummerSet, SpellIds.RibbonDance);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.RibbonPolePeriodicVisual);
        }

        void PeriodicTick(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.RibbonPolePeriodicVisual, true);

            Aura aur = target.GetAura(SpellIds.RibbonDance);
            if (aur != null)
            {
                aur.SetMaxDuration(Math.Min(3600000, aur.GetMaxDuration() + 180000));
                aur.RefreshDuration();

                if (aur.GetMaxDuration() == 3600000 && target.HasAura(SpellIds.HasFullMidsummerSet))
                    target.CastSpell(target, SpellIds.BurningHotPoleDance, true);
            }
            else
                target.CastSpell(target, SpellIds.RibbonDance, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 1, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script] // 45406 - Holiday - Midsummer, Ribbon Pole Periodic Visual
    class spell_midsummer_ribbon_pole_periodic_visual : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TestRibbonPole1, SpellIds.TestRibbonPole2, SpellIds.TestRibbonPole3);
        }

        void PeriodicTick(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            if (!target.HasAura(SpellIds.TestRibbonPole1) && !target.HasAura(SpellIds.TestRibbonPole2) && !target.HasAura(SpellIds.TestRibbonPole3))
                Remove();
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 45819 - Throw Torch
    class spell_midsummer_juggle_torch : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.JuggleTorchSlow, SpellIds.JuggleTorchMedium, SpellIds.JuggleTorchFast, SpellIds.JuggleTorchSelf,
                SpellIds.JuggleTorchShadowSlow, SpellIds.JuggleTorchShadowMedium, SpellIds.JuggleTorchShadowFast, SpellIds.JuggleTorchShadowSelf);
        }

        void HandleDummy(uint effIndex)
        {
            if (GetExplTargetDest() == null)
                return;

            Position spellDest = GetExplTargetDest();
            float distance = GetCaster().GetExactDist2d(spellDest.GetPositionX(), spellDest.GetPositionY());

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

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 45644 - Juggle Torch (Catch)
    class spell_midsummer_torch_catch : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GiveTorch);
        }

        void HandleDummy(uint effIndex)
        {
            Player player = GetHitPlayer();
            if (!player)
                return;

            if (player.GetQuestStatus(QuestIds.TorchCatchingA) == QuestStatus.Rewarded || player.GetQuestStatus(QuestIds.TorchCatchingH) == QuestStatus.Rewarded)
                player.CastSpell(player, SpellIds.GiveTorch);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 46747 - Fling torch
    class spell_midsummer_fling_torch : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlingTorchTriggered, SpellIds.FlingTorchShadow);
        }

        void HandleDummy(uint effIndex)
        {
            Position dest = GetCaster().GetFirstCollisionPosition(30.0f, (float)RandomHelper.NextDouble() * (2 * MathF.PI));
            GetCaster().CastSpell(dest, SpellIds.FlingTorchTriggered, new CastSpellExtraArgs(true));
            GetCaster().CastSpell(dest, SpellIds.FlingTorchShadow, new CastSpellExtraArgs(false));
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 45669 - Fling Torch
    class spell_midsummer_fling_torch_triggered : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.JuggleTorchMissed);
        }

        void HandleTriggerMissile(uint effIndex)
        {
            Position pos = GetHitDest();
            if (pos != null)
        {
                if (GetCaster().GetExactDist2d(pos) > 3.0f)
                {
                    PreventHitEffect(effIndex);
                    GetCaster().CastSpell(GetExplTargetDest(), SpellIds.JuggleTorchMissed, new CastSpellExtraArgs(false));
                    GetCaster().RemoveAura(SpellIds.TorchesCaught);
                }
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleTriggerMissile, 0, SpellEffectName.TriggerMissile));
        }
    }

    [Script] // 45671 - Juggle Torch (Catch, Quest)
    class spell_midsummer_fling_torch_catch : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlingTorchTriggered, SpellIds.TorchCatchingSuccessAlliance, SpellIds.TorchCatchingSuccessHorde, SpellIds.TorchCatchingRemoveTorches, SpellIds.FlingTorchShadow);
        }

        void HandleScriptEffect(uint effIndex)
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
            if (player.GetQuestStatus(QuestIds.TorchCatchingA) == QuestStatus.Incomplete || player.GetQuestStatus(QuestIds.TorchCatchingH) == QuestStatus.Incomplete)
                requiredCatches = 3;
            else if (player.GetQuestStatus(QuestIds.MoreTorchCatchingA) == QuestStatus.Incomplete || player.GetQuestStatus(QuestIds.MoreTorchCatchingH) == QuestStatus.Incomplete)
                requiredCatches = 9;

            // Used quest item without being on quest - do nothing
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

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 45676 - Juggle Torch (Quest, Missed)
    class spell_midsummer_fling_torch_missed : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            // This spell only hits the caster
            targets.RemoveAll(obj => obj.GetGUID() != GetCaster().GetGUID());
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEntry));
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 2, Targets.UnitDestAreaEntry));
        }
    }
}
