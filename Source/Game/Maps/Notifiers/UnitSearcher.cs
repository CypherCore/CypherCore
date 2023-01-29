// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class UnitSearcher : Notifier
    {
        private readonly ICheck<Unit> _check;
        private readonly PhaseShift _phaseShift;
        private Unit _object;

        public UnitSearcher(WorldObject searcher, ICheck<Unit> check)
        {
            _phaseShift = searcher.GetPhaseShift();
            _check = check;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                if (!player.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(player))
                {
                    _object = player;

                    return;
                }
            }
        }

        public override void Visit(IList<Creature> objs)
        {
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

        public Unit GetTarget()
        {
            return _object;
        }
    }
}