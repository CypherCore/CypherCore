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

        void CallEffect(uint effIndex);
    }

    public class SpellEffect : ISpellEffect
    {
        public delegate void SpellEffectFn(uint index);

        public SpellEffect(SpellEffectFn callEffect, uint effectIndex)
        {
            EffectIndex = effectIndex;
            _callEffect = callEffect;
        }

        public uint EffectIndex { get; private set;}

        private readonly SpellEffectFn _callEffect;

        public void CallEffect(uint effIndex)
        {
            _callEffect(EffectIndex);
        }
    }
}
