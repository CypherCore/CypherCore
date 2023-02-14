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
    public class GameObjectScriptRegister : IScriptRegister
    {
        public Type AttributeType => typeof(GameObjectScriptAttribute);

        public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
        {
            if (attribute is GameObjectScriptAttribute gameObjectScript && gameObjectScript.GameObjectIds != null)
                foreach (var id in gameObjectScript.GameObjectIds)
                {
                    var gameObject = Global.ObjectMgr.GetGameObjectTemplate(id);

                    if (gameObject == null)
                    {
                        Log.outError(LogFilter.Scripts, $"GameObjectScriptAttribute: Unknown game object id {id} for script name {scriptName}");
                        continue;
                    }

                    if (gameObject.ScriptId == 0) // dont override database
                        gameObject.ScriptId = Global.ObjectMgr.GetScriptId(scriptName);
                }
        }

    }
}
