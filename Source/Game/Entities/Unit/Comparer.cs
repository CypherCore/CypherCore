// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities
{
    public class PowerPctOrderPred : IComparer<WorldObject>
    {
        private readonly bool _ascending;

        private readonly PowerType _power;

        public PowerPctOrderPred(PowerType power, bool ascending = true)
        {
            _power = power;
            _ascending = ascending;
        }

        public int Compare(WorldObject objA, WorldObject objB)
        {
            Unit a = objA.ToUnit();
            Unit b = objB.ToUnit();
            float rA = a != null ? a.GetPowerPct(_power) : 0.0f;
            float rB = b != null ? b.GetPowerPct(_power) : 0.0f;

            return Convert.ToInt32(_ascending ? rA < rB : rA > rB);
        }
    }

    public class HealthPctOrderPred : IComparer<WorldObject>
    {
        private readonly bool _ascending;

        public HealthPctOrderPred(bool ascending = true)
        {
            _ascending = ascending;
        }

        public int Compare(WorldObject objA, WorldObject objB)
        {
            Unit a = objA.ToUnit();
            Unit b = objB.ToUnit();
            float rA = a.GetMaxHealth() != 0 ? a.GetHealth() / (float)a.GetMaxHealth() : 0.0f;
            float rB = b.GetMaxHealth() != 0 ? b.GetHealth() / (float)b.GetMaxHealth() : 0.0f;

            return Convert.ToInt32(_ascending ? rA < rB : rA > rB);
        }
    }

    public class ObjectDistanceOrderPred : IComparer<WorldObject>
    {
        private readonly bool _ascending;

        private readonly WorldObject _refObj;

        public ObjectDistanceOrderPred(WorldObject pRefObj, bool ascending = true)
        {
            _refObj = pRefObj;
            _ascending = ascending;
        }

        public int Compare(WorldObject pLeft, WorldObject pRight)
        {
            return (_ascending ? _refObj.GetDistanceOrder(pLeft, pRight) : !_refObj.GetDistanceOrder(pLeft, pRight)) ? 1 : 0;
        }
    }
}