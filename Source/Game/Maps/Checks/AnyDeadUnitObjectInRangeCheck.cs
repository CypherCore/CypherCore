// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class AnyDeadUnitObjectInRangeCheck<T> : ICheck<T> where T : WorldObject
    {
        private readonly float _range;

        private readonly WorldObject _searchObj;

        public AnyDeadUnitObjectInRangeCheck(WorldObject searchObj, float range)
        {
            _searchObj = searchObj;
            _range = range;
        }

        public virtual bool Invoke(T obj)
        {
            Player player = obj.ToPlayer();

            if (player)
                return !player.IsAlive() && !player.HasAuraType(AuraType.Ghost) && _searchObj.IsWithinDistInMap(player, _range);

            Creature creature = obj.ToCreature();

            if (creature)
                return !creature.IsAlive() && _searchObj.IsWithinDistInMap(creature, _range);

            Corpse corpse = obj.ToCorpse();

            if (corpse)
                return corpse.GetCorpseType() != CorpseType.Bones && _searchObj.IsWithinDistInMap(corpse, _range);

            return false;
        }
    }
}