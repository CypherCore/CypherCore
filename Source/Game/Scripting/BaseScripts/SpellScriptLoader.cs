// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Scripting.Interfaces.ISpell;

namespace Game.Scripting.BaseScripts
{
    public class SpellScriptLoader : ScriptObject, ISpellScriptLoaderGetSpellScript
    {
        public SpellScriptLoader(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Should return a fully valid SpellScript.
        public virtual SpellScript GetSpellScript() { return null; }
    }

}
