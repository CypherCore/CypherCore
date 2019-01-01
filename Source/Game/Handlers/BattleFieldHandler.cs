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
using Game.BattleFields;
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        /// <summary>
        /// This send to player windows for invite player to join the war.
        /// </summary>
        /// <param name="queueId">The queue id of Bf</param>
        /// <param name="zoneId">The zone where the battle is (4197 for wg)</param>
        /// <param name="acceptTime">Time in second that the player have for accept</param>
        public void SendBfInvitePlayerToWar(ulong queueId, uint zoneId, uint acceptTime)
        {
            BFMgrEntryInvite bfMgrEntryInvite = new BFMgrEntryInvite();
            bfMgrEntryInvite.QueueID = queueId;
            bfMgrEntryInvite.AreaID = (int)zoneId;
            bfMgrEntryInvite.ExpireTime = Time.UnixTime + acceptTime;
            SendPacket(bfMgrEntryInvite);
        }

        /// <summary>
        /// This send invitation to player to join the queue.
        /// </summary>
        /// <param name="queueId">The queue id of Bf</param>
        /// <param name="battleState">Battlefield State</param>
        public void SendBfInvitePlayerToQueue(ulong queueId, BattlefieldState battleState)
        {
            BFMgrQueueInvite bfMgrQueueInvite = new BFMgrQueueInvite();
            bfMgrQueueInvite.QueueID = queueId;
            bfMgrQueueInvite.BattleState = battleState;
            SendPacket(bfMgrQueueInvite);
        }

        /// <summary>
        /// This send packet for inform player that he join queue.
        /// </summary>
        /// <param name="queueId">The queue id of Bf</param>
        /// <param name="zoneId">The zone where the battle is (4197 for wg)</param>
        /// <param name="battleStatus">Battlefield status</param>
        /// <param name="canQueue">if able to queue</param>
        /// <param name="loggingIn">on log in send queue status</param>
        public void SendBfQueueInviteResponse(ulong queueId, uint zoneId, BattlefieldState battleStatus, bool canQueue = true, bool loggingIn = false)
        {
            BFMgrQueueRequestResponse bfMgrQueueRequestResponse = new BFMgrQueueRequestResponse();
            bfMgrQueueRequestResponse.QueueID = queueId;
            bfMgrQueueRequestResponse.AreaID = (int)zoneId;
            bfMgrQueueRequestResponse.Result = (sbyte)(canQueue ? 1 : 0);
            bfMgrQueueRequestResponse.BattleState = battleStatus;
            bfMgrQueueRequestResponse.LoggingIn = loggingIn;
            SendPacket(bfMgrQueueRequestResponse);
        }

        /// <summary>
        /// This is call when player accept to join war.
        /// </summary>
        /// <param name="queueId">The queue id of Bf</param>
        /// <param name="relocated">Whether player is added to Bf on the spot or teleported from queue</param>
        /// <param name="onOffense">Whether player belongs to attacking team or not</param>
        public void SendBfEntered(ulong queueId, bool relocated, bool onOffense)
        {
            BFMgrEntering bfMgrEntering = new BFMgrEntering();
            bfMgrEntering.ClearedAFK = _player.isAFK();
            bfMgrEntering.Relocated = relocated;
            bfMgrEntering.OnOffense = onOffense;
            bfMgrEntering.QueueID = queueId;
            SendPacket(bfMgrEntering);
        }

        /// <summary>
        /// This is call when player leave battlefield zone.
        /// </summary>
        /// <param name="queueId">The queue id of Bf</param>
        /// <param name="battleState">Battlefield status</param>
        /// <param name="relocated">Whether player is added to Bf on the spot or teleported from queue</param>
        /// <param name="reason">Reason why player left battlefield</param>
        public void SendBfLeaveMessage(ulong queueId, BattlefieldState battleState, bool relocated, BFLeaveReason reason = BFLeaveReason.Exited)
        {
            BFMgrEjected bfMgrEjected = new BFMgrEjected();
            bfMgrEjected.QueueID = queueId;
            bfMgrEjected.Reason = reason;
            bfMgrEjected.BattleState = battleState;
            bfMgrEjected.Relocated = relocated;
            SendPacket(bfMgrEjected);
        }

        /// <summary>
        /// Send by client on clicking in accept or refuse of invitation windows for join game.
        /// </summary>
        //[WorldPacketHandler(ClientOpcodes.BfMgrEntryInviteResponse)]
        void HandleBfEntryInviteResponse(BFMgrEntryInviteResponse bfMgrEntryInviteResponse)
        {
            BattleField bf = Global.BattleFieldMgr.GetBattlefieldByQueueId(bfMgrEntryInviteResponse.QueueID);
            if (bf == null)
                return;

            // If player accept invitation
            if (bfMgrEntryInviteResponse.AcceptedInvite)
            {
                bf.PlayerAcceptInviteToWar(GetPlayer());
            }
            else
            {
                if (GetPlayer().GetZoneId() == bf.GetZoneId())
                    bf.KickPlayerFromBattlefield(GetPlayer().GetGUID());
            }
        }

        /// <summary>
        /// Send by client when he click on accept for queue.
        /// </summary>
        //[WorldPacketHandler(ClientOpcodes.BfMgrQueueInviteResponse)]
        void HandleBfQueueInviteResponse(BFMgrQueueInviteResponse bfMgrQueueInviteResponse)
        {
            BattleField bf = Global.BattleFieldMgr.GetBattlefieldByQueueId(bfMgrQueueInviteResponse.QueueID);
            if (bf == null)
                return;

            if (bfMgrQueueInviteResponse.AcceptedInvite)
                bf.PlayerAcceptInviteToQueue(GetPlayer());
        }

        /// <summary>
        /// Send by client when exited battlefield
        /// </summary>
        //[WorldPacketHandler(ClientOpcodes.BfMgrQueueExitRequest)]
        void HandleBfExitRequest(BFMgrQueueExitRequest bfMgrQueueExitRequest)
        {
            BattleField bf = Global.BattleFieldMgr.GetBattlefieldByQueueId(bfMgrQueueExitRequest.QueueID);
            if (bf == null)
                return;

            bf.AskToLeaveQueue(GetPlayer());
        }
    }
}
