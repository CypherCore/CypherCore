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
using Game.Entities;

namespace Game.Network.Packets
{
    public class GameObjUse : ClientPacket
    {
        public GameObjUse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
    }

    public class GameObjReportUse : ClientPacket
    {
        public GameObjReportUse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
    }

    class GameObjectDespawn : ServerPacket
    {
        public GameObjectDespawn() : base(ServerOpcodes.GameObjectDespawn) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ObjectGUID);
        }

        public ObjectGuid ObjectGUID;
    }

    class PageTextPkt : ServerPacket
    {
        public PageTextPkt() : base(ServerOpcodes.PageText) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GameObjectGUID);
        }

        public ObjectGuid GameObjectGUID;
    }

    class GameObjectActivateAnimKit : ServerPacket
    {
        public GameObjectActivateAnimKit() : base(ServerOpcodes.GameObjectActivateAnimKit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ObjectGUID);
            _worldPacket.WriteUInt32(AnimKitID);
            _worldPacket.WriteBit(Maintain);
            _worldPacket.FlushBits();
        }

        public ObjectGuid ObjectGUID;
        public int AnimKitID;
        public bool Maintain;
    }

    class DestructibleBuildingDamage : ServerPacket
    {
        public DestructibleBuildingDamage() : base(ServerOpcodes.DestructibleBuildingDamage, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WritePackedGuid(Owner);
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WriteInt32(Damage);
            _worldPacket.WriteUInt32(SpellID);
        }

        public ObjectGuid Target;
        public ObjectGuid Caster;
        public ObjectGuid Owner;
        public int Damage;
        public uint SpellID;
    }

    class FishNotHooked : ServerPacket
    {
        public FishNotHooked() : base(ServerOpcodes.FishNotHooked) { }

        public override void Write() { }
    }

    class FishEscaped : ServerPacket
    {
        public FishEscaped() : base(ServerOpcodes.FishEscaped) { }

        public override void Write() { }
    }

    class GameObjectCustomAnim : ServerPacket
    {
        public GameObjectCustomAnim() : base(ServerOpcodes.GameObjectCustomAnim, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ObjectGUID);
            _worldPacket.WriteUInt32(CustomAnim);
            _worldPacket.WriteBit(PlayAsDespawn);
            _worldPacket.FlushBits();
        }

        public ObjectGuid ObjectGUID;
        public uint CustomAnim;
        public bool PlayAsDespawn;
    }

    class GameObjectUIAction : ServerPacket
    {
        public GameObjectUIAction() : base(ServerOpcodes.GameObjectUiAction, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ObjectGUID);
            _worldPacket.WriteInt32(UILink);
        }

        public ObjectGuid ObjectGUID;
        public int UILink;
    }
}
