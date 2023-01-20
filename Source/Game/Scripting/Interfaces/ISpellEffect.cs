using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;

namespace Game.Scripting.Interfaces
{
    public interface ISpellEffect
    {
        uint EffectIndex { get; }

        SpellScriptHookType HookType { get; }
    }

    public class SpellEffect : ISpellEffect
    {
        public SpellEffect(uint effectIndex, SpellScriptHookType hookType)
        {
            EffectIndex = effectIndex;
            HookType = hookType;
        }

        public uint EffectIndex { get; private set;}

        public SpellScriptHookType HookType { get; private set; }


    }
}
