using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CreatureScriptAttribute : ScriptAttribute
    {
        public CreatureScriptAttribute(string name = "", params object[] args) : base(name, args)
        {
        }

        public CreatureScriptAttribute(uint creatureId, string name = "", params object[] args) : base(name, args)
        {
            CreatureIds = new[]
                       {
                           creatureId
                       };
        }

        public CreatureScriptAttribute(uint[] creatureIds, string name = "", params object[] args) : base(name, args)
        {
            CreatureIds = creatureIds;
        }

        public uint[] CreatureIds { get; private set; }
    }
}
