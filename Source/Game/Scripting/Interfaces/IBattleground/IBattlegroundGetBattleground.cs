// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.BattleGrounds;

namespace Game.Scripting.Interfaces.IBattleground
{
    public interface IBattlegroundGetBattleground : IScriptObject
    {
        Battleground GetBattleground();
    }
}