using Game.AI;
using Game.Entities;
using Game.Scripting.Interfaces.IGameObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting.BaseScripts
{
    public class GenericGameObjectScript<AI> : ScriptObjectAutoAddDBBound, IGameObjectGetAI where AI : GameObjectAI
    {
        public GenericGameObjectScript(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public GameObjectAI GetAI(GameObject me)
        {
            if (me.GetInstanceScript() != null)
                return GetInstanceAI<AI>(me);
            else
                return (AI)Activator.CreateInstance(typeof(AI), new object[] { me }.Combine(_args));
        }

        object[] _args;
    }
}
