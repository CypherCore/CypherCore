using System;
using System.Collections.Generic;

namespace Game.Scripting.Activators
{
    public interface IScriptActivator
    {
        List<string> ScriptBaseTypes { get; }
        void Activate(Type type, string name, ScriptAttribute attribute);
    }
}