// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Guilds;

namespace Game.Scripting.Interfaces.IGuild
{
    public interface IGuildOnEvent : IScriptObject
    {
        void OnEvent(Guild guild, byte eventType, ulong playerGuid1, ulong playerGuid2, byte newRank);
    }
}