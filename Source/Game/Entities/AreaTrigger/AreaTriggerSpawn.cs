using Framework.Constants;
using Game.Maps;

namespace Game.Entities;

public class AreaTriggerSpawn : SpawnData
{
    public AreaTriggerId TriggerId;

    public AreaTriggerSpawn() : base(SpawnObjectType.AreaTrigger)
    {
    }

    public AreaTriggerShapeInfo Shape { get; set; } = new();
}