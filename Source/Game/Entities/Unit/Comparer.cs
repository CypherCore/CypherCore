/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
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
