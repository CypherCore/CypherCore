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

namespace Framework.Collections
{
    public class LinkedListElement
    {
        internal LinkedListElement INext;
        internal LinkedListElement IPrev;

        public LinkedListElement()
        {
            INext = IPrev = null;
        }

        ~LinkedListElement() { Delink(); }

        bool HasNext() { return (INext != null && INext.INext != null); }
        bool HasPrev() { return (IPrev != null && IPrev.IPrev != null); }
        public bool IsInList() { return (INext != null && IPrev != null); }

        public LinkedListElement GetNextElement() { return HasNext() ? INext : null; }
        public LinkedListElement GetPrevElement() { return HasPrev() ? IPrev : null; }

        public void Delink()
        {
            if (!IsInList())
                return;

            INext.IPrev = IPrev;
            IPrev.INext = INext;
            INext = null;
            IPrev = null;
        }

        public void InsertBefore(LinkedListElement pElem)
        {
            pElem.INext = this;
            pElem.IPrev = IPrev;
            IPrev.INext = pElem;
            IPrev = pElem;
        }

        public void InsertAfter(LinkedListElement pElem)
        {
            pElem.IPrev = this;
            pElem.INext = INext;
            INext.IPrev = pElem;
            INext = pElem;
        }
    }

    public class LinkedListHead
    {
        LinkedListElement _iFirst = new LinkedListElement();
        LinkedListElement _iLast = new LinkedListElement();
        uint _iSize;

        public LinkedListHead()
        {
            _iSize = 0;
            // create empty list

            _iFirst.INext = _iLast;
            _iLast.IPrev = _iFirst;
        }

        public bool IsEmpty() { return (!_iFirst.INext.IsInList()); }

        public LinkedListElement GetFirstElement() { return (IsEmpty() ? null : _iFirst.INext); }
        public LinkedListElement GetLastElement() { return (IsEmpty() ? null : _iLast.IPrev); }

        public void InsertFirst(LinkedListElement pElem)
        {
            _iFirst.InsertAfter(pElem);
        }

        public void InsertLast(LinkedListElement pElem)
        {
            _iLast.InsertBefore(pElem);
        }

        public uint GetSize()
        {
            if (_iSize == 0)
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
                return _iSize;
        }

        public void IncSize() { ++_iSize; }
        public void DecSize() { --_iSize; }
    }
}
