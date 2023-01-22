// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Interfaces.ICreature
{
    public interface ICreatureGetAI : IScriptObject
    {
        CreatureAI GetAI(Creature creature);
    }
}