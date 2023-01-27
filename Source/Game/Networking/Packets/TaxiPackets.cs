// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	internal class TaxiNodeStatusQuery : ClientPacket
	{
		public ObjectGuid UnitGUID;

		public TaxiNodeStatusQuery(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			UnitGUID = _worldPacket.ReadPackedGuid();
		}
	}

	internal class TaxiNodeStatusPkt : ServerPacket
	{
		public TaxiNodeStatus Status; // replace with TaxiStatus enum
		public ObjectGuid Unit;

		public TaxiNodeStatusPkt() : base(ServerOpcodes.TaxiNodeStatus)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Unit);
			_worldPacket.WriteBits(Status, 2);
			_worldPacket.FlushBits();
		}
	}

	public class ShowTaxiNodes : ServerPacket
	{
		public byte[] CanLandNodes = null; // Nodes known by player
		public byte[] CanUseNodes = null;  // Nodes available for use - this can temporarily disable a known node

		public ShowTaxiNodesWindowInfo? WindowInfo;

		public ShowTaxiNodes() : base(ServerOpcodes.ShowTaxiNodes)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteBit(WindowInfo.HasValue);
			_worldPacket.FlushBits();

			_worldPacket.WriteInt32(CanLandNodes.Length);
			_worldPacket.WriteInt32(CanUseNodes.Length);

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
	}

	internal class EnableTaxiNode : ClientPacket
	{
		public ObjectGuid Unit;

		public EnableTaxiNode(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Unit = _worldPacket.ReadPackedGuid();
		}
	}

	internal class TaxiQueryAvailableNodes : ClientPacket
	{
		public ObjectGuid Unit;

		public TaxiQueryAvailableNodes(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Unit = _worldPacket.ReadPackedGuid();
		}
	}

	internal class ActivateTaxi : ClientPacket
	{
		public uint FlyingMountID;
		public uint GroundMountID;
		public uint Node;

		public ObjectGuid Vendor;

		public ActivateTaxi(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Vendor        = _worldPacket.ReadPackedGuid();
			Node          = _worldPacket.ReadUInt32();
			GroundMountID = _worldPacket.ReadUInt32();
			FlyingMountID = _worldPacket.ReadUInt32();
		}
	}

	internal class NewTaxiPath : ServerPacket
	{
		public NewTaxiPath() : base(ServerOpcodes.NewTaxiPath)
		{
		}

		public override void Write()
		{
		}
	}

	internal class ActivateTaxiReplyPkt : ServerPacket
	{
		public ActivateTaxiReply Reply;

		public ActivateTaxiReplyPkt() : base(ServerOpcodes.ActivateTaxiReply)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteBits(Reply, 4);
			_worldPacket.FlushBits();
		}
	}

	internal class TaxiRequestEarlyLanding : ClientPacket
	{
		public TaxiRequestEarlyLanding(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	public struct ShowTaxiNodesWindowInfo
	{
		public ObjectGuid UnitGUID;
		public int CurrentNode;
	}
}