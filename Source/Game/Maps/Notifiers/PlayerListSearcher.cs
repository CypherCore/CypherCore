// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class PlayerListSearcher : Notifier
    {
        private readonly ICheck<Player> _check;
        private readonly List<Unit> _objects;
        private readonly PhaseShift _phaseShift;

        public PlayerListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Player> check)
        {
            _phaseShift = searcher.GetPhaseShift();
            _objects = objects;
            _check = check;
        }

        public PlayerListSearcher(PhaseShift phaseShift, List<Unit> objects, ICheck<Player> check)
        {
            _phaseShift = phaseShift;
            _objects = objects;
            _check = check;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                if (player.InSamePhase(_phaseShift))
                    if (_check.Invoke(player))
                        _objects.Add(player);
            }
        }
    }
}