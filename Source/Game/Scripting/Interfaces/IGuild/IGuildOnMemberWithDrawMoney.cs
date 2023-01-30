// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Guilds;

namespace Game.Scripting.Interfaces.IGuild
{
    public interface IGuildOnMemberWithDrawMoney : IScriptObject
    {
        void OnMemberWitdrawMoney(Guild guild, Player player, ulong amount, bool isRepair);
    }
}