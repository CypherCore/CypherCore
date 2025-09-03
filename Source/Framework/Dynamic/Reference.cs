// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Dynamic
{
    public class Reference<TO, FROM> where TO : class where FROM : class
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
            _RefTo = null;
            _RefFrom = null;
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
            _RefTo = null;
            _RefFrom = null;
        }

        // Link is invalid due to destruction of referenced target object. Call comes from the refTo object
        // Tell our refFrom object, that the link is cut
        public void Invalidate()                                   // the iRefFrom MUST remain!!
        {
            SourceObjectDestroyLink();
            _RefTo = null;
        }

        public bool IsValid()                                // Only check the iRefTo
        {
            return _RefTo != null;
        }

        public TO GetTarget() { return _RefTo; }

        public FROM GetSource() { return _RefFrom; }
    }

    public class RefManager<ReferenceType> : IEnumerable where ReferenceType : class, new()
    {
        LinkedList<ReferenceType> linkedList = new LinkedList<ReferenceType>();

        ~RefManager() { ClearReferences(); }

        public void InsertFirst(ReferenceType pElem)
        {
            linkedList.AddFirst(pElem);
        }

        public void InsertLast(ReferenceType pElem)
        {
            linkedList.AddLast(pElem);
        }

        public ReferenceType Find(Func<ReferenceType, bool> func)
        {
            return linkedList.FirstOrDefault(func);
        }

        public void Remove(Func<ReferenceType, bool> func)
        {
            linkedList.Remove(Find(func));
        }

        public int GetSize()
        {
            return linkedList.Count;
        }

        public ReferenceType GetFirst() { return linkedList.First?.Value; }
        public ReferenceType GetLast() { return linkedList.Last?.Value; }

        public IEnumerator<ReferenceType> GetEnumerator()
        {
            return linkedList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void ClearReferences()
        {
            ReferenceType refe;
            while ((refe = GetFirst()) != null)
                linkedList.Remove(refe);
        }
    }
}
