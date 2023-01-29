// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Collision
{
    public class LocationInfo
    {
        public float Ground_Z { get; set; }
        public ModelInstance HitInstance { get; set; }
        public GroupModel HitModel { get; set; }

        public int RootId { get; set; }

        public LocationInfo()
        {
            Ground_Z = float.NegativeInfinity;
        }
    }
}