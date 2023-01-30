// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class PlayerOrPetCheck : ICheck<WorldObject>
    {
        public bool Invoke(WorldObject obj)
        {
            if (obj.IsTypeId(TypeId.Player))
                return false;

            Creature creature = obj.ToCreature();

            if (creature)
                return !creature.IsPet();

            return true;
        }
    }
}