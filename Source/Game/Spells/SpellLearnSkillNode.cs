// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Spells
{
    public class SpellLearnSkillNode
    {
        public ushort Maxvalue { get; set; } // 0  - max skill value for player level
        public SkillType Skill { get; set; }
        public ushort Step { get; set; }
        public ushort Value { get; set; } // 0  - max skill value for player level
    }
}