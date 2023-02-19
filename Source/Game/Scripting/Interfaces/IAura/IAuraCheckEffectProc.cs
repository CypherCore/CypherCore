// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCheckEffectProc : IAuraEffectHandler
    {
        bool CheckProc(AuraEffect aura, ProcEventInfo info);
    }

    public class AuraCheckEffectProcHandler : AuraEffectHandler, IAuraCheckEffectProc
    {
        public delegate bool AuraCheckEffectProcDelegate(AuraEffect aura, ProcEventInfo info);

        private readonly AuraCheckEffectProcDelegate _fn;

        public AuraCheckEffectProcHandler(AuraCheckEffectProcDelegate fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.CheckEffectProc)
        {
            _fn = fn;
        }

        public bool CheckProc(AuraEffect aura, ProcEventInfo info)
        {
            return _fn(aura, info);
        }
    }
}