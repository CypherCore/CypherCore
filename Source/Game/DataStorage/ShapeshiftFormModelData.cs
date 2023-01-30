// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.DataStorage
{
    public class ShapeshiftFormModelData
    {
        public List<ChrCustomizationChoiceRecord> Choices { get; set; } = new();
        public List<ChrCustomizationDisplayInfoRecord> Displays { get; set; } = new();
        public uint OptionID { get; set; }
    }
}