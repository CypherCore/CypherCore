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
