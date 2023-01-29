// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Collision
{
    public class AreaInfo
    {
        public int AdtId { get; set; }
        public uint Flags { get; set; }
        public float Ground_Z { get; set; }
        public int GroupId { get; set; }

        public bool Result { get; set; }
        public int RootId { get; set; }

        public AreaInfo()
        {
            Ground_Z = float.NegativeInfinity;
        }
    }
}