using System.Numerics;
using Game.Networking;

namespace Game.Entities;

public class AreaTriggerOrbitInfo
{
    public Vector3? Center;

    public ObjectGuid? PathTarget;
    public float BlendFromRadius { get; set; }
    public bool CanLoop { get; set; }
    public bool CounterClockwise { get; set; }
    public int ElapsedTimeForMovement { get; set; }
    public float InitialAngle { get; set; }
    public float Radius { get; set; }
    public uint StartDelay { get; set; }
    public uint TimeToTarget { get; set; }
    public float ZOffset { get; set; }

    public void Write(WorldPacket data)
    {
        data.WriteBit(PathTarget.HasValue);
        data.WriteBit(Center.HasValue);
        data.WriteBit(CounterClockwise);
        data.WriteBit(CanLoop);

        data.WriteUInt32(TimeToTarget);
        data.WriteInt32(ElapsedTimeForMovement);
        data.WriteUInt32(StartDelay);
        data.WriteFloat(Radius);
        data.WriteFloat(BlendFromRadius);
        data.WriteFloat(InitialAngle);
        data.WriteFloat(ZOffset);

        if (PathTarget.HasValue)
            data.WritePackedGuid(PathTarget.Value);

        if (Center.HasValue)
            data.WriteVector3(Center.Value);
    }
}