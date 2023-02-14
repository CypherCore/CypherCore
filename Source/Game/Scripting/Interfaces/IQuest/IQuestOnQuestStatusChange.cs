// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.IQuest
{
    public interface IQuestOnQuestStatusChange : IScriptObject
    {
        void OnQuestStatusChange(Player player, Quest quest, QuestStatus oldStatus, QuestStatus newStatus);
    }
}