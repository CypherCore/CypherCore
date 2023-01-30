using System;
using Framework.Constants;
using Game.Entities;
using Game.Spells.Auras.EffectHandlers;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraEffectProcHandler : IAuraEffectHandler
    {
        void HandleProc(AuraEffect aura, ProcEventInfo info);
    }

    public class EffectProcHandler : AuraEffectHandler, IAuraEffectProcHandler
    {
        public delegate void AuraEffectProcDelegate(AuraEffect aura, ProcEventInfo info);

        private readonly AuraEffectProcDelegate _fn;

        public EffectProcHandler(AuraEffectProcDelegate fn, uint effectIndex, AuraType auraType, AuraScriptHookType hookType) : base(effectIndex, auraType, hookType)
        {
            _fn = fn;

            if (hookType != AuraScriptHookType.EffectProc &&
                hookType != AuraScriptHookType.EffectAfterProc)
                throw new Exception($"Hook Type {hookType} is not valid for {nameof(EffectProcHandler)}. Use {AuraScriptHookType.EffectProc} or {AuraScriptHookType.EffectAfterProc}");
        }

        public void HandleProc(AuraEffect aura, ProcEventInfo info)
        {
            _fn(aura, info);
        }
    }
}