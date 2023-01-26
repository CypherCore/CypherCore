using Game.AI;
using Game.Entities;
using Game.Scripting.Interfaces.IAreaTriggerEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting.BaseScripts
{
    public class GenericAreaTriggerScript<AI> : ScriptObjectAutoAddDBBound, IAreaTriggerEntityGetAI where AI : AreaTriggerAI
    {
        public GenericAreaTriggerScript(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public AreaTriggerAI GetAI(AreaTrigger me)
        {
            if (me.GetInstanceScript() != null)
                return GetInstanceAI<AI>(me);
            else
                return (AI)Activator.CreateInstance(typeof(AI), new object[] { me }.Combine(_args));
        }

        object[] _args;
    }

}
