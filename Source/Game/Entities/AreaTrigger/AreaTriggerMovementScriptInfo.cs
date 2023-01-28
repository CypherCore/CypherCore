using System.Numerics;
using Game.Networking;

namespace Game.Entities;

public struct AreaTriggerMovementScriptInfo
{
    public uint SpellScriptID;
    public Vector3 Center;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(SpellScriptID);
        data.WriteVector3(Center);
    }
}