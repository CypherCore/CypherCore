// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	public class GameObjUse : ClientPacket
	{
		public ObjectGuid Guid;
		public bool IsSoftInteract;

		public GameObjUse(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Guid           = _worldPacket.ReadPackedGuid();
			IsSoftInteract = _worldPacket.HasBit();
		}
	}

	public class GameObjReportUse : ClientPacket
	{
		public ObjectGuid Guid;
		public bool IsSoftInteract;

		public GameObjReportUse(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Guid           = _worldPacket.ReadPackedGuid();
			IsSoftInteract = _worldPacket.HasBit();
		}
	}

	internal class GameObjectDespawn : ServerPacket
	{
		public ObjectGuid ObjectGUID;

		public GameObjectDespawn() : base(ServerOpcodes.GameObjectDespawn)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(ObjectGUID);
		}
	}

	internal class PageTextPkt : ServerPacket
	{
		public ObjectGuid GameObjectGUID;

		public PageTextPkt() : base(ServerOpcodes.PageText)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(GameObjectGUID);
		}
	}

	internal class GameObjectActivateAnimKit : ServerPacket
	{
		public int AnimKitID;
		public bool Maintain;

		public ObjectGuid ObjectGUID;

		public GameObjectActivateAnimKit() : base(ServerOpcodes.GameObjectActivateAnimKit, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(ObjectGUID);
			_worldPacket.WriteInt32(AnimKitID);
			_worldPacket.WriteBit(Maintain);
			_worldPacket.FlushBits();
		}
	}

	internal class DestructibleBuildingDamage : ServerPacket
	{
		public ObjectGuid Caster;
		public int Damage;
		public ObjectGuid Owner;
		public uint SpellID;

		public ObjectGuid Target;

		public DestructibleBuildingDamage() : base(ServerOpcodes.DestructibleBuildingDamage, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Target);
			_worldPacket.WritePackedGuid(Owner);
			_worldPacket.WritePackedGuid(Caster);
			_worldPacket.WriteInt32(Damage);
			_worldPacket.WriteUInt32(SpellID);
		}
	}

	internal class FishNotHooked : ServerPacket
	{
		public FishNotHooked() : base(ServerOpcodes.FishNotHooked)
		{
		}

		public override void Write()
		{
		}
	}

	internal class FishEscaped : ServerPacket
	{
		public FishEscaped() : base(ServerOpcodes.FishEscaped)
		{
		}

		public override void Write()
		{
		}
	}

	internal class GameObjectCustomAnim : ServerPacket
	{
		public uint CustomAnim;

		public ObjectGuid ObjectGUID;
		public bool PlayAsDespawn;

		public GameObjectCustomAnim() : base(ServerOpcodes.GameObjectCustomAnim, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(ObjectGUID);
			_worldPacket.WriteUInt32(CustomAnim);
			_worldPacket.WriteBit(PlayAsDespawn);
			_worldPacket.FlushBits();
		}
	}

	internal class GameObjectPlaySpellVisual : ServerPacket
	{
		public ObjectGuid ActivatorGUID;

		public ObjectGuid ObjectGUID;
		public uint SpellVisualID;

		public GameObjectPlaySpellVisual() : base(ServerOpcodes.GameObjectPlaySpellVisual)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(ObjectGUID);
			_worldPacket.WritePackedGuid(ActivatorGUID);
			_worldPacket.WriteUInt32(SpellVisualID);
		}
	}

	internal class GameObjectSetStateLocal : ServerPacket
	{
		public ObjectGuid ObjectGUID;
		public byte State;

		public GameObjectSetStateLocal() : base(ServerOpcodes.GameObjectSetStateLocal, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(ObjectGUID);
			_worldPacket.WriteUInt8(State);
		}
	}

	internal class GameObjectInteraction : ServerPacket
	{
		public PlayerInteractionType InteractionType;
		public ObjectGuid ObjectGUID;

		public GameObjectInteraction() : base(ServerOpcodes.GameObjectInteraction)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(ObjectGUID);
			_worldPacket.WriteInt32((int)InteractionType);
		}
	}

	internal class GameObjectCloseInteraction : ServerPacket
	{
		public PlayerInteractionType InteractionType;

		public GameObjectCloseInteraction() : base(ServerOpcodes.GameObjectCloseInteraction)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32((int)InteractionType);
		}
	}
}