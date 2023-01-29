// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class ObjectGUIDCheck : ICheck<WorldObject>
    {
        private ObjectGuid _GUID;

        public ObjectGUIDCheck(ObjectGuid GUID)
        {
            _GUID = GUID;
        }

        public bool Invoke(WorldObject obj)
        {
            return obj.GetGUID() == _GUID;
        }

        public static implicit operator Predicate<WorldObject>(ObjectGUIDCheck check)
        {
            return check.Invoke;
        }
    }
}