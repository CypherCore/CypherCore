// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellDestinationTargetSelectHandler : ITargetHookHandler
    {
        void SetDest(ref SpellDestination dest);
    }

    public class DestinationTargetSelectHandler : TargetHookHandler, ISpellDestinationTargetSelectHandler
    {
        public delegate void SpellDestinationTargetSelectFnType(ref SpellDestination dest);

        private readonly SpellDestinationTargetSelectFnType _func;


        public DestinationTargetSelectHandler(SpellDestinationTargetSelectFnType func, uint effectIndex, Targets targetType, SpellScriptHookType hookType = SpellScriptHookType.DestinationTargetSelect) : base(effectIndex, targetType, false, hookType, true)
        {
            _func = func;
        }

        public void SetDest(ref SpellDestination dest)
        {
            _func(ref dest);
        }
    }
}