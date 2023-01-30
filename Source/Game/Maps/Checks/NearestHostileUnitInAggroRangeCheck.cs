// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class NearestHostileUnitInAggroRangeCheck : ICheck<Unit>
    {
        private readonly bool _ignoreCivilians;

        private readonly Creature _me;
        private readonly bool _useLOS;

        public NearestHostileUnitInAggroRangeCheck(Creature creature, bool useLOS = false, bool ignoreCivilians = false)
        {
            _me = creature;
            _useLOS = useLOS;
            _ignoreCivilians = ignoreCivilians;
        }

        public bool Invoke(Unit u)
        {
            if (!u.IsHostileTo(_me))
                return false;

            if (!u.IsWithinDist(_me, _me.GetAggroRange(u)))
                return false;

            if (!_me.IsValidAttackTarget(u))
                return false;

            if (_useLOS && !u.IsWithinLOSInMap(_me))
                return false;

            // pets in aggressive do not attack civilians
            if (_ignoreCivilians)
            {
                Creature c = u.ToCreature();

                if (c != null)
                    if (c.IsCivilian())
                        return false;
            }

            return true;
        }
    }
}