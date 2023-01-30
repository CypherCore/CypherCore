// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game.Scripting.BaseScripts
{
    public class GenericAuraScriptLoader<A> : AuraScriptLoader where A : AuraScript
    {
        public GenericAuraScriptLoader(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public override AuraScript GetAuraScript() { return (A)Activator.CreateInstance(typeof(A), _args); }

        object[] _args;
    }

}
