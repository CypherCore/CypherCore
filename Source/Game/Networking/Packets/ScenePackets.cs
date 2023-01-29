// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    internal class PlayScene : ServerPacket
    {
        public bool Encrypted;
        public Position Location;
        public uint PlaybackFlags;

        public uint SceneID;
        public uint SceneInstanceID;
        public uint SceneScriptPackageID;
        public ObjectGuid TransportGUID;

        public PlayScene() : base(ServerOpcodes.PlayScene, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SceneID);
            _worldPacket.WriteUInt32(PlaybackFlags);
            _worldPacket.WriteUInt32(SceneInstanceID);
            _worldPacket.WriteUInt32(SceneScriptPackageID);
            _worldPacket.WritePackedGuid(TransportGUID);
            _worldPacket.WriteXYZO(Location);
            _worldPacket.WriteBit(Encrypted);
            _worldPacket.FlushBits();
        }
    }

    internal class CancelScene : ServerPacket
    {
        public uint SceneInstanceID;

        public CancelScene() : base(ServerOpcodes.CancelScene, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SceneInstanceID);
        }
    }

    internal class SceneTriggerEvent : ClientPacket
    {
        public string _Event;

        public uint SceneInstanceID;

        public SceneTriggerEvent(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(6);
            SceneInstanceID = _worldPacket.ReadUInt32();
            _Event = _worldPacket.ReadString(len);
        }
    }

    internal class ScenePlaybackComplete : ClientPacket
    {
        public uint SceneInstanceID;

        public ScenePlaybackComplete(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            SceneInstanceID = _worldPacket.ReadUInt32();
        }
    }

    internal class ScenePlaybackCanceled : ClientPacket
    {
        public uint SceneInstanceID;

        public ScenePlaybackCanceled(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            SceneInstanceID = _worldPacket.ReadUInt32();
        }
    }
}