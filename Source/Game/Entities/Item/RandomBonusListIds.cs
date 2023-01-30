// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Entities
{
    public class RandomBonusListIds
    {
        public List<uint> BonusListIDs { get; set; } = new();
        public List<double> Chances { get; set; } = new();
    }
}