// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellObjectAreaTargetSelect : ITargetHookHandler
    {
        void FilterTargets(List<WorldObject> targets);
    }

    public class ObjectAreaTargetSelectHandler : TargetHookHandler, ISpellObjectAreaTargetSelect
    {
        public delegate void SpellObjectAreaTargetSelectFnType(List<WorldObject> targets);

        private readonly SpellObjectAreaTargetSelectFnType _func;


        public ObjectAreaTargetSelectHandler(SpellObjectAreaTargetSelectFnType func, uint effectIndex, Targets targetType, SpellScriptHookType hookType = SpellScriptHookType.ObjectAreaTargetSelect) : base(effectIndex, targetType, true, hookType)
        {
            _func = func;
        }

        public void FilterTargets(List<WorldObject> targets)
        {
            _func(targets);
        }
    }
}