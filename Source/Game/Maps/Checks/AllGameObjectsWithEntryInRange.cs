// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class AllGameObjectsWithEntryInRange : ICheck<GameObject>
    {
        private readonly float _fRange;

        private readonly WorldObject _pObject;
        private readonly uint _uiEntry;

        public AllGameObjectsWithEntryInRange(WorldObject obj, uint entry, float maxRange)
        {
            _pObject = obj;
            _uiEntry = entry;
            _fRange = maxRange;
        }

        public bool Invoke(GameObject go)
        {
            if (_uiEntry == 0 ||
                go.GetEntry() == _uiEntry && _pObject.IsWithinDist(go, _fRange, false))
                return true;

            return false;
        }
    }
}