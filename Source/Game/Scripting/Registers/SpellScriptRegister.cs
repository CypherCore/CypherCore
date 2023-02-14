// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
                }
        }

    }
}
