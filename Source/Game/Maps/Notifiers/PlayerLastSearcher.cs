// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class PlayerLastSearcher : Notifier
    {
        private readonly ICheck<Player> _check;
        private readonly PhaseShift _phaseShift;
        private Player _object;

        public PlayerLastSearcher(WorldObject searcher, ICheck<Player> check)
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
                    _object = player;
            }
        }

        public Player GetTarget()
        {
            return _object;
        }
    }
}