// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game
{
    [Flags]
    public enum DisableFlags
    {
        SpellPlayer = 0x01,
        SpellCreature = 0x02,
        SpellPet = 0x04,
        SpellDeprecatedSpell = 0x08,
        SpellMap = 0x10,
        SpellArea = 0x20,
        SpellLOS = 0x40,
        SpellGameobject = 0x80,
        SpellArenas = 0x100,
        SpellBattleGrounds = 0x200,
        MaxSpell = SpellPlayer | SpellCreature | SpellPet | SpellDeprecatedSpell | SpellMap | SpellArea | SpellLOS | SpellGameobject | SpellArenas | SpellBattleGrounds,

        VmapAreaFlag = 0x01,
        VmapHeight = 0x02,
        VmapLOS = 0x04,
        VmapLiquidStatus = 0x08,

        MMapPathFinding = 0x00,

        DungeonStatusNormal = 0x01,
        DungeonStatusHeroic = 0x02,

        DungeonStatusNormal10Man = 0x01,
        DungeonStatusNormal25Man = 0x02,
        DungeonStatusHeroic10Man = 0x04,
        DungeonStatusHeroic25Man = 0x08
    }
}