// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Game.Scripting.BaseScripts
{
    public class GenericSpellScriptLoader<S> : SpellScriptLoader where S : SpellScript
    {
        private readonly object[] _args;

        public GenericSpellScriptLoader(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public override SpellScript GetSpellScript()
        {
            return Activator.CreateInstance(typeof(S), _args) as S;
        }
    }
}