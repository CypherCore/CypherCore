// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Scripting.Interfaces.IQuest
{
    public interface IQuestOnAckAutoAccept : IScriptObject
    {
        void OnAcknowledgeAutoAccept(Player player, Quest quest);
    }
}