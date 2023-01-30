// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class GameObjectListSearcher : Notifier
    {
        private readonly ICheck<GameObject> _check;
        private readonly List<GameObject> _objects;
        private readonly PhaseShift _phaseShift;

        public GameObjectListSearcher(WorldObject searcher, List<GameObject> objects, ICheck<GameObject> check)
        {
            _phaseShift = searcher.GetPhaseShift();
            _objects = objects;
            _check = check;
        }

        public override void Visit(IList<GameObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];

                if (gameObject.InSamePhase(_phaseShift))
                    if (_check.Invoke(gameObject))
                        _objects.Add(gameObject);
            }
        }
    }
}