using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities;

public class VendorItemData
{
    private List<VendorItem> _items = new();

    public VendorItem GetItem(uint slot)
    {
        if (slot >= _items.Count)
            return null;

        return _items[(int)slot];
    }

    public bool Empty()
    {
        return _items.Count == 0;
    }

    public int GetItemCount()
    {
        return _items.Count;
    }

    public void AddItem(VendorItem vItem)
    {
        _items.Add(vItem);
    }

    public bool RemoveItem(uint item_id, ItemVendorType type)
    {
        int i = _items.RemoveAll(p => p.Item == item_id && p.Type == type);

        if (i == 0)
            return false;
        else
            return true;
    }

    public VendorItem FindItemCostPair(uint item_id, uint extendedCost, ItemVendorType type)
    {
        return _items.Find(p => p.Item == item_id && p.ExtendedCost == extendedCost && p.Type == type);
    }

    public void Clear()
    {
        _items.Clear();
    }
}