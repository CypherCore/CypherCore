// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class PlayerWorker : Notifier
    {
        private readonly Action<Player> _action;
        private readonly PhaseShift _phaseShift;

        public PlayerWorker(WorldObject searcher, Action<Player> _action)
        {
            _phaseShift = searcher.GetPhaseShift();
            this._action = _action;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                if (player.InSamePhase(_phaseShift))
                    _action.Invoke(player);
            }
        }
    }
}