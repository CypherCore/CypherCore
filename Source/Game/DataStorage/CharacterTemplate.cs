// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.DataStorage
{
    public class CharacterTemplate
    {
        public List<CharacterTemplateClass> Classes { get; set; }
        public string Description { get; set; }
        public byte Level { get; set; }
        public string Name { get; set; }
        public uint TemplateSetId { get; set; }
    }
}