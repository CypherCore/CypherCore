// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Framework.GameMath;

namespace Game.Collision
{
    public class GameobjectModelData
    {
        public AxisAlignedBox Bound;

        public GameobjectModelData(string name_, Vector3 lowBound, Vector3 highBound, bool isWmo_)
        {
            Bound = new AxisAlignedBox(lowBound, highBound);
            Name = name_;
            IsWmo = isWmo_;
        }

        public bool IsWmo { get; set; }
        public string Name { get; set; }
    }
}