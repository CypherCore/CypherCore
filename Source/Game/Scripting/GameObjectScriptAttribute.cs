using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GameObjectScriptAttribute : ScriptAttribute
    {
        public GameObjectScriptAttribute(string name = "", params object[] args) : base(name, args)
        {
        }

        public GameObjectScriptAttribute(uint gameObjectId, string name = "", params object[] args) : base(name, args)
        {
            GameObjectIds = new[]
                       {
                           gameObjectId
                       };
        }

        public GameObjectScriptAttribute(uint[] gameObjectIds, string name = "", params object[] args) : base(name, args)
        {
            GameObjectIds = gameObjectIds;
        }

        public uint[] GameObjectIds { get; private set; }
    }
}
