// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
            return (S)Activator.CreateInstance(typeof(S), _args);
        }
    }
}