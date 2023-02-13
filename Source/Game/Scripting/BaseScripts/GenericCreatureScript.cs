﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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