using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities;

public class VendorItem
{
    public VendorItem()
    {
    }

    public VendorItem(uint _item, int _maxcount, uint _incrtime, uint _ExtendedCost, ItemVendorType _Type)
    {
        Item = _item;
        Maxcount = (uint)_maxcount;
        Incrtime = _incrtime;
        ExtendedCost = _ExtendedCost;
        Type = _Type;
    }

    public List<uint> BonusListIDs { get; set; } = new();
    public uint ExtendedCost { get; set; }
    public bool IgnoreFiltering { get; set; }
    public uint Incrtime { get; set; } // Time for restore items amount if Maxcount != 0

    public uint Item { get; set; }
    public uint Maxcount { get; set; } // 0 for infinity Item amount
    public uint PlayerConditionId { get; set; }
    public ItemVendorType Type { get; set; }
}