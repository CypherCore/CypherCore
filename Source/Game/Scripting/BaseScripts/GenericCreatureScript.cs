// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting.Interfaces.ICreature;

namespace Game.Scripting.BaseScripts
{
    public class GenericCreatureScript<AI> : ScriptObjectAutoAddDBBound, ICreatureGetAI where AI : CreatureAI
    {
        private readonly object[] _args;

        public GenericCreatureScript(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public virtual CreatureAI GetAI(Creature me)
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