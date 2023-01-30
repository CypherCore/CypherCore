using Game.AI;
using Game.Scripting.BaseScripts;
using System;
using System.Collections.Generic;

namespace Game.Scripting.Activators
{
    internal class CreatureAIActivator : IScriptActivator
    {
        public List<string> ScriptBaseTypes => new List<string>()
        {
            nameof(ScriptedAI)
        };

        public void Activate(Type type, string name, ScriptAttribute attribute)
        {
            Activator.CreateInstance(typeof(GenericCreatureScript<>).MakeGenericType(type), name, attribute.Args);
        }
    }
}
