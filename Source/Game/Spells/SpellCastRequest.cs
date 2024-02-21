// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Networking.Packets;

namespace Game.Spells
{
    public class SpellCastRequestItemData
    {
        public byte PackSlot;
        public byte Slot;
        public ObjectGuid CastItem;

        public SpellCastRequestItemData(byte packSlot, byte slot, ObjectGuid castItem)
        {
            PackSlot = packSlot;
            Slot = slot;
            CastItem = castItem;
        }
    }

    public class SpellCastRequest
    {
        public SpellCastRequestPkt CastRequest;
        public ObjectGuid CastingUnitGUID;
        public SpellCastRequestItemData ItemData;

        public SpellCastRequest(SpellCastRequestPkt castRequest, ObjectGuid castingUnitGUID, SpellCastRequestItemData itemData = null)
        {
            CastRequest = castRequest;
            CastingUnitGUID = castingUnitGUID;
            ItemData = itemData;
        }
    }

}
