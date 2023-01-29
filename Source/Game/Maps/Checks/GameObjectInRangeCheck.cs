// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class GameObjectInRangeCheck : ICheck<GameObject>
    {
        private readonly uint _entry;

        private readonly float _x, _y, _z, _range;

        public GameObjectInRangeCheck(float _x, float _y, float _z, float _range, uint _entry = 0)
        {
            this._x = _x;
            this._y = _y;
            this._z = _z;
            this._range = _range;
            this._entry = _entry;
        }

        public bool Invoke(GameObject go)
        {
            if (_entry == 0 ||
                go.GetGoInfo() != null && go.GetGoInfo().entry == _entry)
                return go.IsInRange(_x, _y, _z, _range);
            else return false;
        }
    }
}