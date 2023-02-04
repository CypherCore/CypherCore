using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Registers
{
    public class SpellScriptRegister : IScriptRegister
    {
        public Type AttributeType => typeof(SpellScriptAttribute);

        public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
        {
            if (attribute is SpellScriptAttribute spellScript && spellScript.SpellIds != null)
                foreach (var id in spellScript.SpellIds)
                {
                    Global.ObjectMgr.RegisterSpellScript(id, scriptName, spellScript.AllRanks);

                    if (script != null)
                        Global.ScriptMgr.AddScript(script);
                }
        }

    }
}
