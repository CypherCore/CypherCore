// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Activators
{
    public class SpellScriptActivator : IScriptActivator
    {
        public List<string> ScriptBaseTypes => new()
                                               {
                                                   nameof(SpellScript)
                                               };

        public IScriptObject Activate(Type type, string name, ScriptAttribute attribute)
        {
            name = name.Replace("_SpellScript", "");
            return (IScriptObject)Activator.CreateInstance(typeof(GenericSpellScriptLoader<>).MakeGenericType(type), name, attribute.Args);
        }
    }
}