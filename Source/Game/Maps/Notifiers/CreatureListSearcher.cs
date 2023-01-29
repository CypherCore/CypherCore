// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class CreatureListSearcher : Notifier
    {
        internal PhaseShift _phaseShift;
        private readonly ICheck<Creature> _check;
        private readonly List<Creature> _objects;

        public CreatureListSearcher(WorldObject searcher, List<Creature> objects, ICheck<Creature> check)
        {
            _phaseShift = searcher.GetPhaseShift();
            _objects = objects;
            _check = check;
        }

        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                if (creature.InSamePhase(_phaseShift))
                    if (_check.Invoke(creature))
                        _objects.Add(creature);
            }
        }
    }
}