// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting.Interfaces.ISpell;

namespace Game.Scripting.BaseScripts
{
    public class SpellScriptLoader : ScriptObject, ISpellScriptLoaderGetSpellScript
    {
        public SpellScriptLoader(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound()
        {
            return true;
        }

        // Should return a fully valid SpellScript.
        public virtual SpellScript GetSpellScript()
        {
            return null;
        }
    }
}