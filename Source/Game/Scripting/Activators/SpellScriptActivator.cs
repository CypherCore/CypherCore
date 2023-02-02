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