// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	internal class TotemDestroyed : ClientPacket
	{
		public byte Slot;

		public ObjectGuid TotemGUID;

		public TotemDestroyed(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Slot      = _worldPacket.ReadUInt8();
			TotemGUID = _worldPacket.ReadPackedGuid();
		}
	}

	internal class TotemCreated : ServerPacket
	{
		public bool CannotDismiss;
		public uint Duration;
		public byte Slot;
		public uint SpellID;
		public float TimeMod = 1.0f;

		public ObjectGuid Totem;

		public TotemCreated() : base(ServerOpcodes.TotemCreated)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt8(Slot);
			_worldPacket.WritePackedGuid(Totem);
			_worldPacket.WriteUInt32(Duration);
			_worldPacket.WriteUInt32(SpellID);
			_worldPacket.WriteFloat(TimeMod);
			_worldPacket.WriteBit(CannotDismiss);
			_worldPacket.FlushBits();
		}
	}

	internal class TotemMoved : ServerPacket
	{
		public byte NewSlot;
		public byte Slot;

		public ObjectGuid Totem;

		public TotemMoved() : base(ServerOpcodes.TotemMoved)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt8(Slot);
			_worldPacket.WriteUInt8(NewSlot);
			_worldPacket.WritePackedGuid(Totem);
		}
	}
}