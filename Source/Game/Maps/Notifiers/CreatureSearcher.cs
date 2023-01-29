// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class CreatureSearcher : Notifier
    {
        private readonly ICheck<Creature> _check;
        private readonly PhaseShift _phaseShift;
        private Creature _object;

        public CreatureSearcher(WorldObject searcher, ICheck<Creature> check)
        {
            _phaseShift = searcher.GetPhaseShift();
            _check = check;
        }

        public override void Visit(IList<Creature> objs)
        {
            // already found
            if (_object)
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                if (!creature.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(creature))
                {
                    _object = creature;

                    return;
                }
            }
        }

        public Creature GetTarget()
        {
            return _object;
        }
    }
}