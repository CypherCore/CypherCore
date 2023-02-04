using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Registers
{
    public class CreatureScriptRegister : IScriptRegister
    {
        public Type AttributeType => typeof(CreatureScriptAttribute);

        public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
        {
            if (attribute is CreatureScriptAttribute creatureScript && creatureScript.CreatureIds != null)
                foreach (var id in creatureScript.CreatureIds)
                {
                    var creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(id);

                    if (creatureTemplate == null)
                    {
                        Log.outError(LogFilter.Scripts, $"CreatureScriptAttribute: Unknown creature id {id} for script name {scriptName}");
                        continue;
                    }
                    
                    if (creatureTemplate.ScriptID == 0) // dont override database
                        creatureTemplate.ScriptID = Global.ObjectMgr.GetScriptId(scriptName);

                    if (script != null)
                        Global.ScriptMgr.AddScript(script);
                }
        }

    }
}
