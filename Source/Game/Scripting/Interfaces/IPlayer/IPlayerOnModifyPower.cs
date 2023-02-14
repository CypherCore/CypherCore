// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    public interface IPlayerOnModifyPower : IScriptObject
    {
        void OnModifyPower(Player player, PowerType power, int oldValue, ref int newValue, bool regen);
    }
}
