using System;
using System.Collections.Generic;
using Game.AI;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Activators
{
    public class AreaTriggerActivator : IScriptActivator
    {
        public List<string> ScriptBaseTypes => new()
                                               {
                                                   nameof(AreaTriggerAI)
                                               };

        public IScriptObject Activate(Type type, string name, ScriptAttribute attribute)
        {
            return (IScriptObject)Activator.CreateInstance(typeof(GenericAreaTriggerScript<>).MakeGenericType(type), name, attribute.Args);
        }
    }
}