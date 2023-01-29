// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;

namespace Game.Conditions
{
    public class ConditionSourceInfo
    {
        public ConditionSourceInfo(WorldObject target0, WorldObject target1 = null, WorldObject target2 = null)
        {
            ConditionTargets[0] = target0;
            ConditionTargets[1] = target1;
            ConditionTargets[2] = target2;
            ConditionMap = target0?.GetMap();
            LastFailedCondition = null;
        }

        public ConditionSourceInfo(Map map)
        {
            ConditionMap = map;
            LastFailedCondition = null;
        }

        public Map ConditionMap { get; set; }

        public WorldObject[] ConditionTargets { get; set; } = new WorldObject[SharedConst.MaxConditionTargets]; // an array of targets available for conditions
        public Condition LastFailedCondition { get; set; }
    }
}