// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
