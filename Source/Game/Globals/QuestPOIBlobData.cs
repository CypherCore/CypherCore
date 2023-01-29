// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game
{
    public class QuestPOIBlobData
    {
        public bool AlwaysAllowMergingBlobs { get; set; }
        public int BlobIndex { get; set; }
        public int Flags { get; set; }
        public int MapID { get; set; }
        public int NavigationPlayerCoditionID { get; set; }
        public int ObjectiveIndex { get; set; }
        public int PlayerConditionID { get; set; }
        public List<QuestPOIBlobPoint> Points { get; set; }
        public int Priority { get; set; }
        public int QuestObjectID { get; set; }
        public int QuestObjectiveID { get; set; }
        public int SpawnTrackingID { get; set; }
        public int UiMapID { get; set; }
        public int WorldEffectID { get; set; }

        public QuestPOIBlobData(int blobIndex, int objectiveIndex, int questObjectiveID, int questObjectID, int mapID, int uiMapID, int priority, int flags,
                                int worldEffectID, int playerConditionID, int navigationPlayerConditionID, int spawnTrackingID, List<QuestPOIBlobPoint> points, bool alwaysAllowMergingBlobs)
        {
            BlobIndex = blobIndex;
            ObjectiveIndex = objectiveIndex;
            QuestObjectiveID = questObjectiveID;
            QuestObjectID = questObjectID;
            MapID = mapID;
            UiMapID = uiMapID;
            Priority = priority;
            Flags = flags;
            WorldEffectID = worldEffectID;
            PlayerConditionID = playerConditionID;
            NavigationPlayerConditionID = navigationPlayerConditionID;
            SpawnTrackingID = spawnTrackingID;
            Points = points;
            AlwaysAllowMergingBlobs = alwaysAllowMergingBlobs;
        }
    }
}