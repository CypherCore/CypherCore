using System;
using System.Collections.Generic;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Activators
{
    public class AuraScriptActivator : IScriptActivator
    {
        public List<string> ScriptBaseTypes => new()
                                               {
                                                   nameof(AuraScript)
                                               };

        public IScriptObject Activate(Type type, string name, ScriptAttribute attribute)
        {
            name = name.Replace("_AuraScript", "");
            return (IScriptObject)Activator.CreateInstance(typeof(GenericAuraScriptLoader<>).MakeGenericType(type), name, attribute.Args);
        }
    }
}