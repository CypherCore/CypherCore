// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class UnitAuraCheck<T> : ICheck<T> where T : WorldObject
    {
        private readonly bool _present;
        private readonly uint _spellId;
        private ObjectGuid _casterGUID;

        public UnitAuraCheck(bool present, uint spellId, ObjectGuid casterGUID = default)
        {
            _present = present;
            _spellId = spellId;
            _casterGUID = casterGUID;
        }

        public bool Invoke(T obj)
        {
            return obj.ToUnit() && obj.ToUnit().HasAura(_spellId, _casterGUID) == _present;
        }

        public static implicit operator Predicate<T>(UnitAuraCheck<T> unit)
        {
            return unit.Invoke;
        }
    }
}