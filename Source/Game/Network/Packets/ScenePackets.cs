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
    class PlayScene : ServerPacket
    {
        public PlayScene() : base(ServerOpcodes.PlayScene, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SceneID);
            _worldPacket.WriteUInt32(PlaybackFlags);
            _worldPacket.WriteUInt32(SceneInstanceID);
            _worldPacket.WriteUInt32(SceneScriptPackageID);
            _worldPacket.WritePackedGuid(TransportGUID);
            _worldPacket.WriteXYZO(Location);
        }

        public uint SceneID;
        public uint PlaybackFlags;
        public uint SceneInstanceID;
        public uint SceneScriptPackageID;
        public ObjectGuid TransportGUID;
        public Position Location;
    }

    class CancelScene : ServerPacket
    {
        public CancelScene() : base(ServerOpcodes.CancelScene, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SceneInstanceID);
        }

        public uint SceneInstanceID;
    }

    class SceneTriggerEvent : ClientPacket
    {
        public SceneTriggerEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(6);
            SceneInstanceID = _worldPacket.ReadUInt32();
            _Event = _worldPacket.ReadString(len);
        }

        public uint SceneInstanceID;
        public string _Event;
    }

    class ScenePlaybackComplete : ClientPacket
    {
        public ScenePlaybackComplete(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SceneInstanceID = _worldPacket.ReadUInt32();
        }

        public uint SceneInstanceID;
    }

    class ScenePlaybackCanceled : ClientPacket
    {
        public ScenePlaybackCanceled(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SceneInstanceID = _worldPacket.ReadUInt32();
        }

        public uint SceneInstanceID;
    }
}
