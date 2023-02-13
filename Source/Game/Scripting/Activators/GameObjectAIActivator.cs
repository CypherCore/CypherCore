﻿using System;
using System.Collections.Generic;
using Game.AI;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Activators
{
    public class GameObjectAIActivator : IScriptActivator
    {
        public List<string> ScriptBaseTypes => new()
                                               {
                                                   nameof(GameObjectAI)
                                               };

        public IScriptObject Activate(Type type, string name, ScriptAttribute attribute)
        {
            return (IScriptObject)Activator.CreateInstance(typeof(GenericGameObjectScript<>).MakeGenericType(type), name, attribute.Args);
        }
    }
}