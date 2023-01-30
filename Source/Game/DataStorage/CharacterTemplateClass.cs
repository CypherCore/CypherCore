// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public struct CharacterTemplateClass
    {
        public CharacterTemplateClass(FactionMasks factionGroup, byte classID)
        {
            FactionGroup = factionGroup;
            ClassID = classID;
        }

        public FactionMasks FactionGroup;
        public byte ClassID;
    }
}