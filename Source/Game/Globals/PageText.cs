﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class PageText
    {
        public byte Flags { get; set; }
        public uint NextPageID { get; set; }
        public int PlayerConditionID { get; set; }
        public string Text { get; set; }
    }
}