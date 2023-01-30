// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Spells
{
    public class SkillStatusData
    {
        public byte Pos { get; set; }
        public SkillState State { get; set; }

        public SkillStatusData(uint _pos, SkillState state)
        {
            Pos = (byte)_pos;
            State = state;
        }
    }
}