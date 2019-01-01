/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;

namespace Game.Network.Packets
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

            _worldPacket.WriteUInt32(CanLandNodes.Length);
            _worldPacket.WriteUInt32(CanUseNodes.Length);

            if (WindowInfo.HasValue)
            {
                _worldPacket.WritePackedGuid(WindowInfo.Value.UnitGUID);
                _worldPacket.WriteUInt32(WindowInfo.Value.CurrentNode);
            }

            foreach (var node in CanLandNodes)
                _worldPacket.WriteUInt8(node);

            foreach (var node in CanUseNodes)
                _worldPacket.WriteUInt8(node);
        }

        public Optional<ShowTaxiNodesWindowInfo> WindowInfo;
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
