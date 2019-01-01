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
 */﻿

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
        OnHit,
        AfterHit,
        ObjectAreaTargetSelect,
        ObjectTargetSelect,
        DestinationTargetSelect,
        CheckCast,
        BeforeCast,
        OnCast,
        AfterCast
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
        EffectAbsorb,
        EffectAfterAbsorb,
        EffectManaShield,
        EffectAfterManaShield,
        EffectSplit,
        CheckAreaTarget,
        Dispel,
        AfterDispel,
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
}
