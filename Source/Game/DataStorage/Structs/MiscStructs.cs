// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage
{
    public struct WMOAreaTableTripple
    {
        public WMOAreaTableTripple(int r, int a, int g)
        {
            groupId = g;
            rootId = r;
            adtId = a;
        }

        // ordered by entropy; that way memcmp will have a minimal medium runtime
        int groupId;
        int rootId;
        int adtId;
    }

    public class TaxiPathBySourceAndDestination
    {
        public TaxiPathBySourceAndDestination(uint _id, uint _price)
        {
            Id = _id;
            price = _price;
        }

        public uint Id;
        public uint price;
    }
}
