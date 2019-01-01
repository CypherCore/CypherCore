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

using System;

namespace Game.Combat
{
    public class UnitBaseEvent
    {
        public UnitBaseEvent(UnitEventTypes pType)
        {
            iType = pType;
        }
        public UnitEventTypes getType()
        {
            return iType;
        }
        bool matchesTypeMask(uint pMask)
        {
            return Convert.ToBoolean((uint)iType & pMask);
        }

        void setType(UnitEventTypes pType)
        {
            iType = pType;
        }

        UnitEventTypes iType;
    }

    public class ThreatRefStatusChangeEvent : UnitBaseEvent
    {
        public ThreatRefStatusChangeEvent(UnitEventTypes pType)
            : base(pType)
        {
            iHostileReference = null;
        }
        public ThreatRefStatusChangeEvent(UnitEventTypes pType, HostileReference pHostileReference)
            : base(pType)
        {
            iHostileReference = pHostileReference;
        }
        public ThreatRefStatusChangeEvent(UnitEventTypes pType, HostileReference pHostileReference, float pValue)
            : base(pType)
        {
            iHostileReference = pHostileReference; 
            iFValue = pValue;
        }
        public ThreatRefStatusChangeEvent(UnitEventTypes pType, HostileReference pHostileReference, bool pValue)
            : base(pType)
        {
            iHostileReference = pHostileReference; 
            iBValue = pValue;
        }

        public float getFValue()
        {
            return iFValue;
        }

        bool getBValue()
        {
            return iBValue;
        }

        void setBValue(bool pValue)
        {
            iBValue = pValue;
        }

        public HostileReference getReference()
        {
            return iHostileReference;
        }

        public void setThreatManager(ThreatManager pThreatManager)
        {
            iThreatManager = pThreatManager;
        }

        ThreatManager getThreatManager()
        {
            return iThreatManager;
        }

        float iFValue;
        bool iBValue;
        HostileReference iHostileReference;
        ThreatManager iThreatManager;
    }

    public enum UnitEventTypes
    {
        // Player/Pet Changed On/Offline Status
        ThreatRefOnlineStatus = 1 << 0,

        // Threat For Player/Pet Changed
        ThreatRefThreatChange = 1 << 1,

        // Player/Pet Will Be Removed From List (Dead) [For Internal Use]
        ThreatRefRemoveFromList = 1 << 2,

        // Player/Pet Entered/Left  Water Or Some Other Place Where It Is/Was Not Accessible For The Creature
        ThreatRefAccessibleStatus = 1 << 3,

        // Threat List Is Going To Be Sorted (If Dirty Flag Is Set)
        ThreatSortList = 1 << 4,

        // New Target Should Be Fetched, Could Tbe The Current Target As Well
        ThreatSetNextTarget = 1 << 5,

        // A New Victim (Target) Was Set. Could Be Null
        ThreatVictimChanged = 1 << 6

        // Future Use
        //Unit_Killed                   = 1<<7,

        //Future Use
        //Unit_Health_Change            = 1<<8,
    }
}
