// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
        OnPrecast,
        CalcCastTime,
        CalcMultiplier
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
        EffectAbsorbHeal,
        EffectAfterAbsorbHeal,
    }
}
