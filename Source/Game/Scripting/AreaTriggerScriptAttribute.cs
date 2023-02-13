using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AreaTriggerScriptAttribute : ScriptAttribute
    {
        public AreaTriggerScriptAttribute(string name = "", params object[] args) : base(name, args)
        {
        }

        public AreaTriggerScriptAttribute(uint areaTriggerId, string name = "", params object[] args) : base(name, args)
        {
            AreaTriggerIds = new[]
                       {
                           areaTriggerId
                       };
        }

        public AreaTriggerScriptAttribute(uint[] areaTriggerIds, string name = "", params object[] args) : base(name, args)
        {
            AreaTriggerIds = areaTriggerIds;
        }

        public uint[] AreaTriggerIds { get; private set; }
    }
}
