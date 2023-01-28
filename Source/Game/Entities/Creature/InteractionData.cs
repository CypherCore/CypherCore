using Game.Entities;

namespace Game.Misc;

public class InteractionData
{
    public uint PlayerChoiceId { get; set; }

    public ObjectGuid SourceGuid;
    public uint TrainerId { get; set; }

    public void Reset()
    {
        SourceGuid.Clear();
        TrainerId = 0;
    }
}