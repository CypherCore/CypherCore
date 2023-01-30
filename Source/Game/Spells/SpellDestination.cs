// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Spells
{
    public class SpellDestination
    {
        public WorldLocation Position { get; set; }
        public ObjectGuid TransportGUID;
        public Position TransportOffset { get; set; }

        public SpellDestination()
        {
            Position = new WorldLocation();
            TransportGUID = ObjectGuid.Empty;
            TransportOffset = new Position();
        }

        public SpellDestination(float x, float y, float z, float orientation = 0.0f, uint mapId = 0xFFFFFFFF) : this()
        {
            Position.Relocate(x, y, z, orientation);
            TransportGUID = ObjectGuid.Empty;
            Position.SetMapId(mapId);
        }

        public SpellDestination(Position pos) : this()
        {
            Position.Relocate(pos);
            TransportGUID = ObjectGuid.Empty;
        }

        public SpellDestination(WorldLocation loc) : this()
        {
            Position.WorldRelocate(loc);
            TransportGUID.Clear();
            TransportOffset.Relocate(0, 0, 0, 0);
        }

        public SpellDestination(WorldObject wObj) : this()
        {
            TransportGUID = wObj.GetTransGUID();
            TransportOffset.Relocate(wObj.GetTransOffsetX(), wObj.GetTransOffsetY(), wObj.GetTransOffsetZ(), wObj.GetTransOffsetO());
            Position.Relocate(wObj.GetPosition());
        }

        public void Relocate(Position pos)
        {
            if (!TransportGUID.IsEmpty())
            {
                Position offset;
                Position.GetPositionOffsetTo(pos, out offset);
                TransportOffset.RelocateOffset(offset);
            }

            Position.Relocate(pos);
        }

        public void RelocateOffset(Position offset)
        {
            if (!TransportGUID.IsEmpty())
                TransportOffset.RelocateOffset(offset);

            Position.RelocateOffset(offset);
        }
    }
}