// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class ResurrectionData
	{
		public uint Aura { get; set; }
        public ObjectGuid GUID;
		public uint Health { get; set; }
        public WorldLocation Location { get; set; } = new();
		public uint Mana { get; set; }
    }
}