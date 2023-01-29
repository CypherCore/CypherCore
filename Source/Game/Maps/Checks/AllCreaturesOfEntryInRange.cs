// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{

    public class AllCreaturesOfEntryInRange : ICheck<Creature>
    {
        private readonly float _fRange;

        private readonly WorldObject _pObject;
        private readonly uint _uiEntry;

        public AllCreaturesOfEntryInRange(WorldObject obj, uint entry, float maxRange = 0f)
        {
            _pObject = obj;
            _uiEntry = entry;
            _fRange = maxRange;
        }

        public bool Invoke(Creature creature)
        {
            if (_uiEntry != 0)
                if (creature.GetEntry() != _uiEntry)
                    return false;

            if (_fRange != 0f)
            {
                if (_fRange > 0.0f &&
                    !_pObject.IsWithinDist(creature, _fRange, false))
                    return false;

                if (_fRange < 0.0f &&
                    _pObject.IsWithinDist(creature, _fRange, false))
                    return false;
            }

            return true;
        }
    }
}