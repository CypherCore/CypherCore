// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class HeightDifferenceCheck : ICheck<WorldObject>
    {
        private readonly WorldObject _baseObject;
        private readonly float _difference;
        private readonly bool _reverse;

        public HeightDifferenceCheck(WorldObject go, float diff, bool reverse)
        {
            _baseObject = go;
            _difference = diff;
            _reverse = reverse;
        }

        public bool Invoke(WorldObject unit)
        {
            return unit.GetPositionZ() - _baseObject.GetPositionZ() > _difference != _reverse;
        }
    }
}