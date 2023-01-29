// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class AllWorldObjectsInRange : ICheck<WorldObject>
    {
        private readonly float _fRange;

        private readonly WorldObject _pObject;

        public AllWorldObjectsInRange(WorldObject obj, float maxRange)
        {
            _pObject = obj;
            _fRange = maxRange;
        }

        public bool Invoke(WorldObject go)
        {
            return _pObject.IsWithinDist(go, _fRange, false) && _pObject.InSamePhase(go);
        }
    }
}