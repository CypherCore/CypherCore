namespace Game.Entities;

public class FormationInfo
{
    public float FollowAngle { get; set; }
    public float FollowDist { get; set; }
    public uint GroupAI { get; set; }
    public ulong LeaderSpawnId { get; set; }
    public uint[] LeaderWaypointIDs { get; set; } = new uint[2];
}