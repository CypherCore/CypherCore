// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Networking.Packets
{
    public class VignetteDataPkt
    {
        public ObjectGuid ObjGUID;
        public Vector3 Position;
        public int VignetteID;
        public uint ZoneID;
        public uint WMOGroupID;
        public uint WMODoodadPlacementID;
        public float HealthPercent = 1.0f;
        public ushort RecommendedGroupSizeMin;
        public ushort RecommendedGroupSizeMax;

        public void Write(WorldPacket data)
        {
            data.WriteVector3(Position);
            data.WritePackedGuid(ObjGUID);
            data.WriteInt32(VignetteID);
            data.WriteUInt32(ZoneID);
            data.WriteUInt32(WMOGroupID);
            data.WriteUInt32(WMODoodadPlacementID);
            data.WriteFloat(HealthPercent);
            data.WriteUInt16(RecommendedGroupSizeMin);
            data.WriteUInt16(RecommendedGroupSizeMax);
        }
    }

    public class VignetteDataSet
    {
        public List<ObjectGuid> IDs = new();
        public List<VignetteDataPkt> Data = new();

        public void Write(WorldPacket data)
        {
            data.WriteInt32(IDs.Count);
            data.WriteInt32(Data.Count);
            foreach (ObjectGuid id in IDs)
                data.WritePackedGuid(id);

            foreach (VignetteDataPkt vignetteData in Data)
                vignetteData.Write(data);
        }
    }

    class VignetteUpdate : ServerPacket
    {
        public VignetteDataSet Added = new();
        public VignetteDataSet Updated = new();
        public List<ObjectGuid> Removed = new();
        public bool ForceUpdate;
        public bool InFogOfWar;

        public VignetteUpdate() : base(ServerOpcodes.VignetteUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(ForceUpdate);
            _worldPacket.WriteBit(InFogOfWar);
            _worldPacket.WriteInt32(Removed.Count);
            Added.Write(_worldPacket);
            Updated.Write(_worldPacket);
            foreach (ObjectGuid removed in Removed)
                _worldPacket.WritePackedGuid(removed);
        }
    }
}
