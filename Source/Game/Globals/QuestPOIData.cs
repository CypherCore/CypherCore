// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.IO;

namespace Game
{
    public class QuestPOIData
    {
        public QuestPOIData(uint questId)
        {
            QuestID = questId;
            Blobs = new List<QuestPOIBlobData>();
            QueryDataBuffer = new ByteBuffer();
        }

        public List<QuestPOIBlobData> Blobs { get; set; }
        public ByteBuffer QueryDataBuffer { get; set; }
        public uint QuestID { get; set; }

        public void InitializeQueryData()
        {
            Write(QueryDataBuffer);
        }

        public void Write(ByteBuffer data)
        {
            data.WriteUInt32(QuestID);
            data.WriteInt32(Blobs.Count);

            foreach (QuestPOIBlobData questPOIBlobData in Blobs)
            {
                data.WriteInt32(questPOIBlobData.BlobIndex);
                data.WriteInt32(questPOIBlobData.ObjectiveIndex);
                data.WriteInt32(questPOIBlobData.QuestObjectiveID);
                data.WriteInt32(questPOIBlobData.QuestObjectID);
                data.WriteInt32(questPOIBlobData.MapID);
                data.WriteInt32(questPOIBlobData.UiMapID);
                data.WriteInt32(questPOIBlobData.Priority);
                data.WriteInt32(questPOIBlobData.Flags);
                data.WriteInt32(questPOIBlobData.WorldEffectID);
                data.WriteInt32(questPOIBlobData.PlayerConditionID);
                data.WriteInt32(questPOIBlobData.NavigationPlayerConditionID);
                data.WriteInt32(questPOIBlobData.SpawnTrackingID);
                data.WriteInt32(questPOIBlobData.Points.Count);

                foreach (QuestPOIBlobPoint questPOIBlobPoint in questPOIBlobData.Points)
                {
                    data.WriteInt16((short)questPOIBlobPoint.X);
                    data.WriteInt16((short)questPOIBlobPoint.Y);
                    data.WriteInt16((short)questPOIBlobPoint.Z);
                }

                data.WriteBit(questPOIBlobData.AlwaysAllowMergingBlobs);
                data.FlushBits();
            }
        }
    }
}