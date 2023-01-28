using System.Numerics;

namespace Game.Misc;

public class PointOfInterest
{
    public uint Flags { get; set; }
    public uint Icon { get; set; }
    public uint Id { get; set; }
    public uint Importance { get; set; }
    public string Name { get; set; }
    public Vector3 Pos;
    public uint WMOGroupID { get; set; }
}