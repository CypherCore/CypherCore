// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.AI
{
    public class PassiveAI : CreatureAI
    {
        public PassiveAI(Creature creature) : base(creature)
        {
            creature.SetReactState(ReactStates.Passive);
        }

        public override void UpdateAI(uint diff)
        {
            if (me.IsEngaged() &&
                !me.IsInCombat())
                EnterEvadeMode(EvadeReason.NoHostiles);
        }

        public override void AttackStart(Unit victim)
        {
        }

        public override void MoveInLineOfSight(Unit who)
        {
        }
    }
}