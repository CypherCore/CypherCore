// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Miscellaneous
{
    /// Only returns true for the given attacker's current victim, if any
    public class IsVictimOf : ICheck<WorldObject>
    {
        WorldObject _victim;

        public IsVictimOf(Unit attacker)
        {
            _victim = attacker?.GetVictim();
        }

        public bool Invoke(WorldObject obj)
        {
            return obj != null && (_victim == obj);
        }
    }

    public class PowerPctOrderPred : IComparer<WorldObject>
    {
        public PowerPctOrderPred(PowerType power, bool ascending = true)
        {
            m_power = power;
            m_ascending = ascending;
        }

        public int Compare(WorldObject objA, WorldObject objB)
        {
            Unit a = objA.ToUnit();
            Unit b = objB.ToUnit();
            float rA = a != null ? a.GetPowerPct(m_power) : 0.0f;
            float rB = b != null ? b.GetPowerPct(m_power) : 0.0f;
            return Convert.ToInt32(m_ascending ? rA < rB : rA > rB);
        }

        PowerType m_power;
        bool m_ascending;
    }

    public class HealthPctOrderPred : IComparer<WorldObject>
    {
        public HealthPctOrderPred(bool ascending = true)
        {
            m_ascending = ascending;
        }

        public int Compare(WorldObject objA, WorldObject objB)
        {
            Unit a = objA.ToUnit();
            Unit b = objB.ToUnit();
            float rA = a.GetMaxHealth() != 0 ? a.GetHealth() / (float)a.GetMaxHealth() : 0.0f;
            float rB = b.GetMaxHealth() != 0 ? b.GetHealth() / (float)b.GetMaxHealth() : 0.0f;
            return Convert.ToInt32(m_ascending ? rA < rB : rA > rB);
        }

        bool m_ascending;
    }

    public class ObjectDistanceOrderPred : IComparer<WorldObject>
    {
        public ObjectDistanceOrderPred(WorldObject pRefObj, bool ascending = true)
        {
            m_refObj = pRefObj;
            m_ascending = ascending;
        }

        public int Compare(WorldObject pLeft, WorldObject pRight)
        {
            return (m_ascending ? m_refObj.GetDistanceOrder(pLeft, pRight) : !m_refObj.GetDistanceOrder(pLeft, pRight)) ? 1 : 0;
        }

        WorldObject m_refObj;
        bool m_ascending;
    }
}
