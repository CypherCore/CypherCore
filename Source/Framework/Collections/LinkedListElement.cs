/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

namespace Framework.Collections
{
    public class LinkedListElement
    {
        internal LinkedListElement iNext;
        internal LinkedListElement iPrev;

        public LinkedListElement()
        {
            iNext = iPrev = null;
        }

        ~LinkedListElement() { delink(); }

        bool hasNext() { return (iNext != null && iNext.iNext != null); }
        bool hasPrev() { return (iPrev != null && iPrev.iPrev != null); }
        public bool isInList() { return (iNext != null && iPrev != null); }

        public LinkedListElement GetNextElement() { return hasNext() ? iNext : null; }
        public LinkedListElement GetPrevElement() { return hasPrev() ? iPrev : null; }

        public void delink()
        {
            if (!isInList())
                return;

            iNext.iPrev = iPrev;
            iPrev.iNext = iNext;
            iNext = null;
            iPrev = null;
        }

        public void insertBefore(LinkedListElement pElem)
        {
            pElem.iNext = this;
            pElem.iPrev = iPrev;
            iPrev.iNext = pElem;
            iPrev = pElem;
        }

        public void insertAfter(LinkedListElement pElem)
        {
            pElem.iPrev = this;
            pElem.iNext = iNext;
            iNext.iPrev = pElem;
            iNext = pElem;
        }
    }

    public class LinkedListHead
    {
        LinkedListElement iFirst = new LinkedListElement();
        LinkedListElement iLast = new LinkedListElement();
        uint iSize;

        public LinkedListHead()
        {
            iSize = 0;
            // create empty list

            iFirst.iNext = iLast;
            iLast.iPrev = iFirst;
        }

        public bool isEmpty() { return (!iFirst.iNext.isInList()); }

        public LinkedListElement GetFirstElement() { return (isEmpty() ? null : iFirst.iNext); }
        public LinkedListElement GetLastElement() { return (isEmpty() ? null : iLast.iPrev); }

        public void insertFirst(LinkedListElement pElem)
        {
            iFirst.insertAfter(pElem);
        }

        public void insertLast(LinkedListElement pElem)
        {
            iLast.insertBefore(pElem);
        }

        public uint getSize()
        {
            if (iSize == 0)
            {
                uint result = 0;
                LinkedListElement e = GetFirstElement();
                while (e != null)
                {
                    ++result;
                    e = e.GetNextElement();
                }
                return result;
            }
            else
                return iSize;
        }

        public void incSize() { ++iSize; }
        public void decSize() { --iSize; }
    }
}
