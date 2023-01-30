using Game.Scripting.BaseScripts;
using System;
using System.Collections.Generic;

namespace Game.Scripting.Activators
{
    internal class AuraScriptActivator : IScriptActivator
    {
        public List<string> ScriptBaseTypes => new List<string>()
        {
            nameof(AuraScript)
        };

        public void Activate(Type type, string name, ScriptAttribute attribute)
        {
            name = name.Replace("_AuraScript", "");
            Activator.CreateInstance(typeof(GenericAuraScriptLoader<>).MakeGenericType(type), name, attribute.Args);
        }
    }
}
