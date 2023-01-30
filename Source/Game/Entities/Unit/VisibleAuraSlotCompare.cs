using System.Collections.Generic;
using Game.Spells;

namespace Game.Entities;

internal class VisibleAuraSlotCompare : IComparer<AuraApplication>
{
    public int Compare(AuraApplication x, AuraApplication y)
    {
        return x.GetSlot().CompareTo(y.GetSlot());
    }
}