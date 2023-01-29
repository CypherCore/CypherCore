// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Movement
{
    internal class MovementGeneratorComparator : IComparer<MovementGenerator>
    {
        public int Compare(MovementGenerator a, MovementGenerator b)
        {
            if (a.Equals(b))
                return 0;

            if (a.Mode > b.Mode)
                return 1;
            else if (a.Mode == b.Mode)
                return a.Priority.CompareTo(b.Priority);

            return -1;
        }
    }
}