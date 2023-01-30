using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting.Activators
{
    public interface IScriptActivator
    {
        List<string> ScriptBaseTypes { get; }
        void Activate(Type type, string name, ScriptAttribute attribute);
    }
}
