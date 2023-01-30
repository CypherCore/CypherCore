// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
    internal class BoundaryUnionBoundary : AreaBoundary
    {
        private readonly AreaBoundary _b1;
        private readonly AreaBoundary _b2;

        public BoundaryUnionBoundary(AreaBoundary b1, AreaBoundary b2, bool isInverted = false) : base(isInverted)
        {
            _b1 = b1;
            _b2 = b2;
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            return _b1.IsWithinBoundary(pos) || _b2.IsWithinBoundary(pos);
        }
    }
}