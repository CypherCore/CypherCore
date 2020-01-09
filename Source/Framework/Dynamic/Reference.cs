/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

using Framework.Collections;

namespace Framework.Dynamic
{
    public class Reference<TO, FROM> : LinkedListElement where TO : class where FROM : class
    {
        TO _RefTo;
        FROM _RefFrom;

        // Tell our refTo (target) object that we have a link
        public virtual void TargetObjectBuildLink() { }

        // Tell our refTo (taget) object, that the link is cut
        public virtual void TargetObjectDestroyLink() { }

        // Tell our refFrom (source) object, that the link is cut (Target destroyed)
        public virtual void SourceObjectDestroyLink() { }

        public Reference()
        {
            _RefTo = null; _RefFrom = null;
        }

        // Create new link
        public void Link(TO toObj, FROM fromObj)
        {
            Cypher.Assert(fromObj != null);                                // fromObj MUST not be NULL
            if (IsValid())
                Unlink();
            if (toObj != null)
            {
                _RefTo = toObj;
                _RefFrom = fromObj;
                TargetObjectBuildLink();
            }
        }

        // We don't need the reference anymore. Call comes from the refFrom object
        // Tell our refTo object, that the link is cut
        public void Unlink()
        {
            TargetObjectDestroyLink();
            Delink();
            _RefTo = null;
            _RefFrom = null;
        }

        // Link is invalid due to destruction of referenced target object. Call comes from the refTo object
        // Tell our refFrom object, that the link is cut
        public void Invalidate()                                   // the iRefFrom MUST remain!!
        {
            SourceObjectDestroyLink();
            Delink();
            _RefTo = null;
        }

        public bool IsValid()                                // Only check the iRefTo
        {
            return _RefTo != null;
        }

        public Reference<TO, FROM> Next() { return ((Reference<TO, FROM>)GetNextElement()); }
        public Reference<TO, FROM> Prev() { return ((Reference<TO, FROM>)GetPrevElement()); }

        public TO GetTarget() { return _RefTo; }

        public FROM GetSource() { return _RefFrom; }
    }

    public class RefManager<TO, FROM> : LinkedListHead where TO : class where FROM : class
    {
        ~RefManager() { ClearReferences(); }

        public Reference<TO, FROM> GetFirst() { return (Reference<TO, FROM>)base.GetFirstElement(); }
        public Reference<TO, FROM> GetLast() { return (Reference<TO, FROM>)base.GetLastElement(); }

        public void ClearReferences()
        {
            Reference<TO, FROM> refe;
            while ((refe = GetFirst()) != null)
                refe.Invalidate();
        }
    }
}
