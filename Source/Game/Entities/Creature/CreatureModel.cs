namespace Game.Entities;

public class CreatureModel
{
    public static CreatureModel DefaultInvisibleModel { get; set; } = new(11686, 1.0f, 1.0f);
    public static CreatureModel DefaultVisibleModel { get; set; } = new(17519, 1.0f, 1.0f);

    public uint CreatureDisplayID { get; set; }
    public float DisplayScale { get; set; }
    public float Probability { get; set; }

    public CreatureModel()
    {
    }

    public CreatureModel(uint creatureDisplayID, float displayScale, float probability)
    {
        CreatureDisplayID = creatureDisplayID;
        DisplayScale = displayScale;
        Probability = probability;
    }
}