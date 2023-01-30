using Game.Scripting.BaseScripts;
using System;
using System.Collections.Generic;

namespace Game.Scripting.Activators
{
    internal class SpellScriptActivator : IScriptActivator
    {
        public List<string> ScriptBaseTypes => new List<string>()
        {
            nameof(SpellScript)
        };

        public void Activate(Type type, string name, ScriptAttribute attribute)
        {
            name = name.Replace("_SpellScript", "");
            Activator.CreateInstance(typeof(GenericSpellScriptLoader<>).MakeGenericType(type), name, attribute.Args);
        }
    }
}
