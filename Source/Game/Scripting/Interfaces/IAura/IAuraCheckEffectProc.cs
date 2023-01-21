using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.Aura
{
    public interface IAuraCheckEffectProc : IAuraEffectHandler
    {
        bool CheckProc(AuraEffect aura, ProcEventInfo info);
    }

    public class CheckEffectProcHandler : AuraEffectHandler, IAuraCheckEffectProc
    {
        public delegate bool AuraCheckEffectProcDelegate(AuraEffect aura, ProcEventInfo info);
        AuraCheckEffectProcDelegate _fn;

        public CheckEffectProcHandler(AuraCheckEffectProcDelegate fn, uint effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.CheckEffectProc)
        {
            _fn = fn;
        }

        public bool CheckProc(AuraEffect aura, ProcEventInfo info)
        {
            return _fn(aura, info);
        }
    }
}
