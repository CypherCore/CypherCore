// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraEffectProcHandler : IAuraEffectHandler
    {
        void HandleProc(AuraEffect aura, ProcEventInfo info);
    }

    public class AuraEffectProcHandler : AuraEffectHandler, IAuraEffectProcHandler
    {
        public delegate void AuraEffectProcDelegate(AuraEffect aura, ProcEventInfo info);

        private readonly AuraEffectProcDelegate _fn;

        public AuraEffectProcHandler(AuraEffectProcDelegate fn, int effectIndex, AuraType auraType, AuraScriptHookType hookType) : base(effectIndex, auraType, hookType)
        {
            _fn = fn;

            if (hookType != AuraScriptHookType.EffectProc &&
                hookType != AuraScriptHookType.EffectAfterProc)
                throw new Exception($"Hook Type {hookType} is not valid for {nameof(AuraEffectProcHandler)}. Use {AuraScriptHookType.EffectProc} or {AuraScriptHookType.EffectAfterProc}");
        }

        public void HandleProc(AuraEffect aura, ProcEventInfo info)
        {
            _fn(aura, info);
        }
    }
}