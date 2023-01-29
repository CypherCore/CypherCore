// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Scripting.Interfaces.IAura;

namespace Game.Scripting.BaseScripts
{
    public class AuraScriptLoader : ScriptObject, IAuraScriptLoaderGetAuraScript
    {
        public AuraScriptLoader(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound()
        {
            return true;
        }

        // Should return a fully valid AuraScript.
        public virtual AuraScript GetAuraScript()
        {
            return null;
        }
    }
}