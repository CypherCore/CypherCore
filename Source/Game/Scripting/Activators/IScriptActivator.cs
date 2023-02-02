using System;
using System.Collections.Generic;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Activators
{
    public interface IScriptActivator
    {
        List<string> ScriptBaseTypes { get; }
        IScriptObject Activate(Type type, string name, ScriptAttribute attribute);
    }
}