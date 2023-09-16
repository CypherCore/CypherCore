// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.﻿

namespace Framework.Constants
{
    public enum SpellScriptState
    {
        None = 0,
        Registration,
        Loading,
        Unloading,
    }

    // SpellScript interface - enum used for runtime checks of script function calls
    public enum SpellScriptHookType
    {
        Launch = SpellScriptState.Unloading + 1,
        LaunchTarget,
        EffectHit,
        EffectHitTarget,
        EffectSuccessfulDispel,
        BeforeHit,
        Hit,
        AfterHit,
        ObjectAreaTargetSelect,
        ObjectTargetSelect,
        DestinationTargetSelect,
        CheckCast,
        BeforeCast,
        OnCast,
        OnResistAbsorbCalculation,
        AfterCast,
        CalcCritChance,
        CalcDamage,
        CalcHealing,
        OnPrecast,
        CalcCastTime
    }

    // AuraScript interface - enum used for runtime checks of script function calls
    public enum AuraScriptHookType
    {
        EffectApply = SpellScriptState.Unloading + 1,
        EffectAfterApply,
        EffectRemove,
        EffectAfterRemove,
        EffectPeriodic,
        EffectUpdatePeriodic,
        EffectCalcAmount,
        EffectCalcPeriodic,
        EffectCalcSpellmod,
        EffectCalcCritChance,
        EffectCalcDamageAndHealing,
        EffectAbsorb,
        EffectAfterAbsorb,
        EffectManaShield,
        EffectAfterManaShield,
        EffectSplit,
        CheckAreaTarget,
        Dispel,
        AfterDispel,
        EnterLeaveCombat,
        // Spell Proc Hooks
        CheckProc,
        CheckEffectProc,
        PrepareProc,
        Proc,
        EffectProc,
        EffectAfterProc,
        AfterProc,
        //Apply,
        //Remove
    }

    public enum EncounterType
    {
        DungeonEncounter,
        Battleground,
        MythicPlusRun
    }
}
