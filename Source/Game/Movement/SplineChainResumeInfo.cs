// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Movement
{
    public class SplineChainResumeInfo
    {
        public List<SplineChainLink> Chain { get; set; } = new();
        public bool IsWalkMode { get; set; }

        public uint PointID { get; set; }
        public byte PointIndex { get; set; }
        public byte SplineIndex { get; set; }
        public uint TimeToNext { get; set; }

        public SplineChainResumeInfo()
        {
        }

        public SplineChainResumeInfo(uint id, List<SplineChainLink> chain, bool walk, byte splineIndex, byte wpIndex, uint msToNext)
        {
            PointID = id;
            Chain = chain;
            IsWalkMode = walk;
            SplineIndex = splineIndex;
            PointIndex = wpIndex;
            TimeToNext = msToNext;
        }

        public bool Empty()
        {
            return Chain.Empty();
        }

        public void Clear()
        {
            Chain.Clear();
        }
    }
}