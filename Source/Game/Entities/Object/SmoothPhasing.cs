// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Entities
{
    public class SmoothPhasing
    {
        SmoothPhasingInfo smoothPhasingInfoSingle;
        Dictionary<ObjectGuid, SmoothPhasingInfo> smoothPhasingInfoViewerDependent = new();

        public void SetViewerDependentInfo(ObjectGuid seer, SmoothPhasingInfo info)
        {
            smoothPhasingInfoViewerDependent[seer] = info;
        }

        public void ClearViewerDependentInfo(ObjectGuid seer)
        {
            smoothPhasingInfoViewerDependent.Remove(seer);
        }

        public void SetSingleInfo(SmoothPhasingInfo info)
        {
            smoothPhasingInfoSingle = info;
        }

        public bool IsReplacing(ObjectGuid guid)
        {
            return smoothPhasingInfoSingle != null && smoothPhasingInfoSingle.ReplaceObject == guid;
        }

        public bool IsBeingReplacedForSeer(ObjectGuid seer)
        {
            SmoothPhasingInfo smoothPhasingInfo = smoothPhasingInfoViewerDependent.LookupByKey(seer);
            if (smoothPhasingInfo != null)
                return !smoothPhasingInfo.Disabled;

            return false;
        }

        public SmoothPhasingInfo GetInfoForSeer(ObjectGuid seer)
        {
            if (smoothPhasingInfoViewerDependent.TryGetValue(seer, out SmoothPhasingInfo value))
                return value;

            return smoothPhasingInfoSingle;
        }

        public void DisableReplacementForSeer(ObjectGuid seer)
        {
            SmoothPhasingInfo smoothPhasingInfo = smoothPhasingInfoViewerDependent.LookupByKey(seer);
            if (smoothPhasingInfo != null)
                smoothPhasingInfo.Disabled = true;
        }
    }

    public class SmoothPhasingInfo
    {
        // Fields visible on client
        public ObjectGuid? ReplaceObject;
        public bool ReplaceActive = true;
        public bool StopAnimKits = true;

        // Serverside fields
        public bool Disabled = false;

        public SmoothPhasingInfo(ObjectGuid replaceObject, bool replaceActive, bool stopAnimKits)
        {
            ReplaceObject = replaceObject;
            ReplaceActive = replaceActive;
            StopAnimKits = stopAnimKits;
        }
    }
}
