namespace Game.Entities;

public class CreatureModelInfo
{
    public float BoundingRadius { get; set; }
    public float CombatReach { get; set; }
    public uint DisplayIdOtherGender { get; set; }
    public sbyte Gender { get; set; }
    public bool IsTrigger { get; set; }
}