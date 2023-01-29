// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Scenarios
{
    public class ScenarioPOI
    {
        public int BlobIndex { get; set; }
        public int Flags { get; set; }
        public int MapID { get; set; }
        public int NavigationPlayerConditionID { get; set; }
        public int PlayerConditionID { get; set; }
        public List<ScenarioPOIPoint> Points { get; set; } = new();
        public int Priority { get; set; }
        public int UiMapID { get; set; }
        public int WorldEffectID { get; set; }

        public ScenarioPOI(int blobIndex, int mapID, int uiMapID, int priority, int flags, int worldEffectID, int playerConditionID, int navigationPlayerConditionID, List<ScenarioPOIPoint> points)
        {
            BlobIndex = blobIndex;
            MapID = mapID;
            UiMapID = uiMapID;
            Priority = priority;
            Flags = flags;
            WorldEffectID = worldEffectID;
            PlayerConditionID = playerConditionID;
            NavigationPlayerConditionID = navigationPlayerConditionID;
            Points = points;
        }
    }
}