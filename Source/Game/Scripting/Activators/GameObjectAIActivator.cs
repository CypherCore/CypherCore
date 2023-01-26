using Game.AI;
using Game.Scripting.BaseScripts;
using System;
using System.Collections.Generic;

namespace Game.Scripting.Activators
{
    internal class GameObjectAIActivator : IScriptActivator
    {
        public List<string> ScriptBaseTypes => new List<string>()
        {
            nameof(GameObjectAI)
        };

        public void Activate(Type type, string name, ScriptAttribute attribute)
        {
            Activator.CreateInstance(typeof(GenericGameObjectScript<>).MakeGenericType(type), name, attribute.Args);
        }
    }
}
