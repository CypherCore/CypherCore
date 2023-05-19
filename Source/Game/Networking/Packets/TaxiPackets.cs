// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;

namespace Game.Networking.Packets
{
    class TaxiNodeStatusQuery : ClientPacket
    {
        public TaxiNodeStatusQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            UnitGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid UnitGUID;
    }

    class TaxiNodeStatusPkt : ServerPacket
    {
        public TaxiNodeStatusPkt() : base(ServerOpcodes.TaxiNodeStatus) { }

        public override void Write()
        {
            _worldPacket .WritePackedGuid( Unit);
            _worldPacket.WriteBits(Status, 2);
            _worldPacket.FlushBits();
        }

        public TaxiNodeStatus Status; // replace with TaxiStatus enum
        public ObjectGuid Unit;
    }

    public class ShowTaxiNodes : ServerPacket
    {
        public ShowTaxiNodes() : base(ServerOpcodes.ShowTaxiNodes) { }

        public override void Write()
        {
            _worldPacket.WriteBit(WindowInfo.HasValue);
            _worldPacket.FlushBits();

            _worldPacket.WriteInt32(CanLandNodes.Length / 8);  // client reads this in uint64 blocks, size is ensured to be divisible by 8 in TaxiMask constructor
            _worldPacket.WriteInt32(CanUseNodes.Length / 8);  // client reads this in uint64 blocks, size is ensured to be divisible by 8 in TaxiMask constructor

            if (WindowInfo.HasValue)
            {
                _worldPacket.WritePackedGuid(WindowInfo.Value.UnitGUID);
                _worldPacket.WriteInt32(WindowInfo.Value.CurrentNode);
            }

            foreach (var node in CanLandNodes)
                _worldPacket.WriteUInt8(node);

            foreach (var node in CanUseNodes)
                _worldPacket.WriteUInt8(node);
        }

        public ShowTaxiNodesWindowInfo? WindowInfo;
        public byte[] CanLandNodes = null; // Nodes known by player
        public byte[] CanUseNodes = null; // Nodes available for use - this can temporarily disable a known node
    }

    class EnableTaxiNode : ClientPacket
    {
        public EnableTaxiNode(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Unit = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Unit;
    }

    class TaxiQueryAvailableNodes : ClientPacket
    {
        public TaxiQueryAvailableNodes(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Unit = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Unit;
    }

    class ActivateTaxi : ClientPacket
    {
        public ActivateTaxi(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Vendor = _worldPacket.ReadPackedGuid();
            Node = _worldPacket.ReadUInt32();
            GroundMountID = _worldPacket.ReadUInt32();
            FlyingMountID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Vendor;
        public uint Node;
        public uint GroundMountID;
        public uint FlyingMountID;
    }

    class NewTaxiPath : ServerPacket
    {
        public NewTaxiPath() : base(ServerOpcodes.NewTaxiPath) { }

        public override void Write() { }
    }

    class ActivateTaxiReplyPkt : ServerPacket
    {
        public ActivateTaxiReplyPkt() : base(ServerOpcodes.ActivateTaxiReply) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Reply, 4);
            _worldPacket.FlushBits();
        }

        public ActivateTaxiReply Reply;
    }

    class TaxiRequestEarlyLanding : ClientPacket
    {
        public TaxiRequestEarlyLanding(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public struct ShowTaxiNodesWindowInfo
    {
        public ObjectGuid UnitGUID;
        public int CurrentNode;
    }
}
