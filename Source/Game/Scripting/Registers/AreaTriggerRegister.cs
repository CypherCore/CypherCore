using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Registers
{
    public class AreaTriggerRegister : IScriptRegister
    {
        public Type AttributeType => typeof(AreaTriggerScriptAttribute);

        public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
        {
            if (attribute is AreaTriggerScriptAttribute spellScript && spellScript.AreaTriggerIds != null)
                foreach (var id in spellScript.AreaTriggerIds)
                {
                    var atcp = Global.AreaTriggerDataStorage.GetAreaTriggerCreateProperties(id);

                    if (atcp != null && atcp.ScriptId != 0)
                        atcp.ScriptId = Global.ObjectMgr.GetScriptId(scriptName);
                }
        }

    }
}
