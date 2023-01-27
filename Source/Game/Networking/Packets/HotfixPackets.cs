// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.IO;
using Game.DataStorage;

namespace Game.Networking.Packets
{
	internal class DBQueryBulk : ClientPacket
	{
		public List<DBQueryRecord> Queries = new();

		public uint TableHash;

		public DBQueryBulk(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			TableHash = _worldPacket.ReadUInt32();

			uint count = _worldPacket.ReadBits<uint>(13);

			for (uint i = 0; i < count; ++i)
				Queries.Add(new DBQueryRecord(_worldPacket.ReadUInt32()));
		}

		public struct DBQueryRecord
		{
			public DBQueryRecord(uint recordId)
			{
				RecordID = recordId;
			}

			public uint RecordID;
		}
	}

	public class DBReply : ServerPacket
	{
		public ByteBuffer Data = new();
		public uint RecordID;
		public HotfixRecord.Status Status = HotfixRecord.Status.Invalid;

		public uint TableHash;
		public uint Timestamp;

		public DBReply() : base(ServerOpcodes.DbReply)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(TableHash);
			_worldPacket.WriteUInt32(RecordID);
			_worldPacket.WriteUInt32(Timestamp);
			_worldPacket.WriteBits((byte)Status, 3);
			_worldPacket.WriteUInt32(Data.GetSize());
			_worldPacket.WriteBytes(Data.GetData());
		}
	}

	internal class AvailableHotfixes : ServerPacket
	{
		public MultiMap<int, HotfixRecord> Hotfixes;

		public uint VirtualRealmAddress;

		public AvailableHotfixes(uint virtualRealmAddress, MultiMap<int, HotfixRecord> hotfixes) : base(ServerOpcodes.AvailableHotfixes)
		{
			VirtualRealmAddress = virtualRealmAddress;
			Hotfixes            = hotfixes;
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(VirtualRealmAddress);
			_worldPacket.WriteInt32(Hotfixes.Keys.Count);

			foreach (var key in Hotfixes.Keys)
				Hotfixes[key][0].ID.Write(_worldPacket);
		}
	}

	internal class HotfixRequest : ClientPacket
	{
		public uint ClientBuild;
		public uint DataBuild;
		public List<int> Hotfixes = new();

		public HotfixRequest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			ClientBuild = _worldPacket.ReadUInt32();
			DataBuild   = _worldPacket.ReadUInt32();

			uint hotfixCount = _worldPacket.ReadUInt32();

			for (var i = 0; i < hotfixCount; ++i)
				Hotfixes.Add(_worldPacket.ReadInt32());
		}
	}

	internal class HotfixConnect : ServerPacket
	{
		public ByteBuffer HotfixContent = new();

		public List<HotfixData> Hotfixes = new();

		public HotfixConnect() : base(ServerOpcodes.HotfixConnect)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(Hotfixes.Count);

			foreach (HotfixData hotfix in Hotfixes)
				hotfix.Write(_worldPacket);

			_worldPacket.WriteUInt32(HotfixContent.GetSize());
			_worldPacket.WriteBytes(HotfixContent);
		}

		public class HotfixData
		{
			public HotfixRecord Record = new();
			public uint Size;

			public void Write(WorldPacket data)
			{
				Record.Write(data);
				data.WriteUInt32(Size);
				data.WriteBits((byte)Record.HotfixStatus, 3);
				data.FlushBits();
			}
		}
	}
}