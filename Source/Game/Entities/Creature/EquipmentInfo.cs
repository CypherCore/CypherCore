using Framework.Constants;

namespace Game.Entities;

public class EquipmentInfo
{
    public EquipmentItem[] Items { get; set; } = new EquipmentItem[SharedConst.MaxEquipmentItems];
}