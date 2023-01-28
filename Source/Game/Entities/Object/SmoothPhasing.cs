// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Entities
{
    public class SmoothPhasing
	{
		private SmoothPhasingInfo _smoothPhasingInfoSingle;
		private Dictionary<ObjectGuid, SmoothPhasingInfo> _smoothPhasingInfoViewerDependent = new();

		public void SetViewerDependentInfo(ObjectGuid seer, SmoothPhasingInfo info)
		{
			_smoothPhasingInfoViewerDependent[seer] = info;
		}

		public void ClearViewerDependentInfo(ObjectGuid seer)
		{
			_smoothPhasingInfoViewerDependent.Remove(seer);
		}

		public void SetSingleInfo(SmoothPhasingInfo info)
		{
			_smoothPhasingInfoSingle = info;
		}

		public bool IsReplacing(ObjectGuid guid)
		{
			return _smoothPhasingInfoSingle != null && _smoothPhasingInfoSingle.ReplaceObject == guid;
		}

		public bool IsBeingReplacedForSeer(ObjectGuid seer)
		{
			SmoothPhasingInfo smoothPhasingInfo = _smoothPhasingInfoViewerDependent.LookupByKey(seer);

			if (smoothPhasingInfo != null)
				return !smoothPhasingInfo.Disabled;

			return false;
		}

		public SmoothPhasingInfo GetInfoForSeer(ObjectGuid seer)
		{
			if (_smoothPhasingInfoViewerDependent.TryGetValue(seer, out SmoothPhasingInfo value))
				return value;

			return _smoothPhasingInfoSingle;
		}

		public void DisableReplacementForSeer(ObjectGuid seer)
		{
			SmoothPhasingInfo smoothPhasingInfo = _smoothPhasingInfoViewerDependent.LookupByKey(seer);

			if (smoothPhasingInfo != null)
				smoothPhasingInfo.Disabled = true;
		}
	}
}