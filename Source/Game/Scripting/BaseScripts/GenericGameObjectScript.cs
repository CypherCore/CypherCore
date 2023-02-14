// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting.Interfaces.IGameObject;

namespace Game.Scripting.BaseScripts
{
    public class GenericGameObjectScript<AI> : ScriptObjectAutoAddDBBound, IGameObjectGetAI where AI : GameObjectAI
    {
        private readonly object[] _args;

        public GenericGameObjectScript(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public GameObjectAI GetAI(GameObject me)
        {
            if (me.GetInstanceScript() != null)
                return GetInstanceAI<AI>(me);
            else
                return (AI)Activator.CreateInstance(typeof(AI),
                                                    new object[]
                                                    {
                                                        me
                                                    }.Combine(_args));
        }
    }
}