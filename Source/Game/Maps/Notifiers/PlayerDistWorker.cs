// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class PlayerDistWorker : Notifier
    {
        private readonly IDoWork<Player> _do;
        private readonly float _dist;
        private readonly WorldObject _searcher;

        public PlayerDistWorker(WorldObject searcher, float _dist, IDoWork<Player> @do)
        {
            _searcher = searcher;
            this._dist = _dist;
            _do = @do;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                if (player.InSamePhase(_searcher) &&
                    player.IsWithinDist(_searcher, _dist))
                    _do.Invoke(player);
            }
        }
    }
}