// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.BattleFields;
using Game.Maps;

namespace Game.Scripting.Interfaces.IBattlefield
{
    public interface IBattlefieldGetBattlefield : IScriptObject
    {
        BattleField GetBattlefield(Map map);
    }
}