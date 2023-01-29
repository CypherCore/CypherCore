// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class GameObjectSearcher : Notifier
    {
        private readonly ICheck<GameObject> _check;
        private readonly PhaseShift _phaseShift;
        private GameObject _object;

        public GameObjectSearcher(WorldObject searcher, ICheck<GameObject> check)
        {
            _phaseShift = searcher.GetPhaseShift();
            _check = check;
        }

        public override void Visit(IList<GameObject> objs)
        {
            // already found
            if (_object)
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];

                if (!gameObject.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(gameObject))
                {
                    _object = gameObject;

                    return;
                }
            }
        }

        public GameObject GetTarget()
        {
            return _object;
        }
    }
}