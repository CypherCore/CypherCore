// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game
{
    [Flags]
    public enum CleaningFlags
    {
        AchievementProgress = 0x1,
        Skills = 0x2,
        Spells = 0x4,
        Talents = 0x8,
        Queststatus = 0x10
    }
}