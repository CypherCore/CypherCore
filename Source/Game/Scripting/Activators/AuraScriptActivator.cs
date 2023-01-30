using System;
using System.Collections.Generic;
using Game.Scripting.BaseScripts;

namespace Game.Scripting.Activators
{
    internal class AuraScriptActivator : IScriptActivator
    {
        public List<string> ScriptBaseTypes => new()
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