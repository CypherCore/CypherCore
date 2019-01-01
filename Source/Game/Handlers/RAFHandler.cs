/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.Entities;
using Game.Network;
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.GrantLevel)]
        void HandleGrantLevel(GrantLevel grantLevel)
        {
            Player target = Global.ObjAccessor.GetPlayer(GetPlayer(), grantLevel.Target);

            // check cheating
            byte levels = GetPlayer().GetGrantableLevels();
            ReferAFriendError error = 0;
            if (!target)
                error = ReferAFriendError.NoTarget;
            if (levels == 0)
                error = ReferAFriendError.InsufficientGrantableLevels;
            else if (GetRecruiterId() != target.GetSession().GetAccountId())
                error = ReferAFriendError.NotReferredBy;
            else if (target.GetTeamId() != GetPlayer().GetTeamId())
                error = ReferAFriendError.DifferentFaction;
            else if (target.getLevel() >= GetPlayer().getLevel())
                error = ReferAFriendError.TargetTooHigh;
            else if (target.getLevel() >= WorldConfig.GetIntValue(WorldCfg.MaxRecruitAFriendBonusPlayerLevel))
                error = ReferAFriendError.GrantLevelMaxI;
            else if (target.GetGroup() != GetPlayer().GetGroup())
                error = ReferAFriendError.NotInGroup;
            else if (target.getLevel() >= Global.ObjectMgr.GetMaxLevelForExpansion(target.GetSession().GetExpansion()))
                error = ReferAFriendError.InsufExpanLvl;

            if (error != 0)
            {
                ReferAFriendFailure failure = new ReferAFriendFailure();
                failure.Reason = error;
                if (error == ReferAFriendError.NotInGroup)
                    failure.Str = target.GetName();

                SendPacket(failure);
                return;
            }

            ProposeLevelGrant proposeLevelGrant = new ProposeLevelGrant();
            proposeLevelGrant.Sender = GetPlayer().GetGUID();
            target.SendPacket(proposeLevelGrant);
        }

        [WorldPacketHandler(ClientOpcodes.AcceptLevelGrant)]
        void HandleAcceptGrantLevel(AcceptLevelGrant acceptLevelGrant)
        {
            Player other = Global.ObjAccessor.GetPlayer(GetPlayer(), acceptLevelGrant.Granter);
            if (!(other && other.GetSession() != null))
                return;

            if (GetAccountId() != other.GetSession().GetRecruiterId())
                return;

            if (other.GetGrantableLevels() != 0)
                other.SetGrantableLevels(other.GetGrantableLevels() - 1);
            else
                return;

            GetPlayer().GiveLevel(GetPlayer().getLevel() + 1);
        }
    }
}
