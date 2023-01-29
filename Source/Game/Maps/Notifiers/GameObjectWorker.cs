// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class GameObjectWorker : Notifier
    {
        private readonly IDoWork<GameObject> _do;
        private readonly PhaseShift _phaseShift;

        public GameObjectWorker(WorldObject searcher, IDoWork<GameObject> @do)
        {
            _phaseShift = searcher.GetPhaseShift();
            _do = @do;
        }

        public override void Visit(IList<GameObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];

                if (gameObject.InSamePhase(_phaseShift))
                    _do.Invoke(gameObject);
            }
        }
    }
}