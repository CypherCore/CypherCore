// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Guilds;

namespace Game.Scripting.Interfaces.IGuild
{
    public interface IGuildOnBankEvent : IScriptObject
    {
        void OnBankEvent(Guild guild, byte eventType, byte tabId, ulong playerGuid, uint itemOrMoney, ushort itemStackCount, byte destTabId);
    }
}