﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class AzeriteEmpoweredData
    {
        public int[] SelectedAzeritePowers { get; set; } = new int[SharedConst.MaxAzeriteEmpoweredTier];
    }
}