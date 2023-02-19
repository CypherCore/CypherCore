// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellEffect
    {
        int EffectIndex { get; }

        SpellScriptHookType HookType { get; }
    }

    public class SpellEffect : ISpellEffect
    {
        public SpellEffect(int effectIndex, SpellScriptHookType hookType)
        {
            EffectIndex = effectIndex;
            HookType = hookType;
        }

        public int EffectIndex { get; private set; }

        public SpellScriptHookType HookType { get; private set; }
    }
}