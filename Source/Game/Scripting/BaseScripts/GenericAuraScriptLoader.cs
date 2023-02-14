// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Game.Scripting.BaseScripts
{
    public class GenericAuraScriptLoader<A> : AuraScriptLoader where A : AuraScript
    {
        private readonly object[] _args;

        public GenericAuraScriptLoader(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public override AuraScript GetAuraScript()
        {
            return (A)Activator.CreateInstance(typeof(A), _args);
        }
    }
}