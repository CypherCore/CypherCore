// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    public struct ObjectData
    {
        public ObjectData(uint _entry, uint _type)
        {
            Entry = _entry;
            Type = _type;
        }

        public uint Entry;
        public uint Type;
    }
}