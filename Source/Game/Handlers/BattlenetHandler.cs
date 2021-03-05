﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Networking;
using Game.Networking.Packets;
using Google.Protobuf;
using System;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.BattlenetRequest, Status = SessionStatus.Authed)]
        private void HandleBattlenetRequest(BattlenetRequest request)
        {
            var handler = Global.ServiceMgr.GetHandler(request.Method.GetServiceHash(), request.Method.GetMethodId());
            if (handler != null)
                handler.Invoke(this, request.Method, new CodedInputStream(request.Data));
            else
            {
                SendBattlenetResponse(request.Method.GetServiceHash(), request.Method.GetMethodId(), request.Method.Token, BattlenetRpcErrorCode.RpcNotImplemented);
                Log.outDebug(LogFilter.SessionRpc, "{0} tried to call invalid service {1}", GetPlayerInfo(), request.Method.GetServiceHash());
            }
        }

        [WorldPacketHandler(ClientOpcodes.ChangeRealmTicket, Status = SessionStatus.Authed)]
        private void HandleBattlenetChangeRealmTicket(ChangeRealmTicket changeRealmTicket)
        {
            SetRealmListSecret(changeRealmTicket.Secret);

            var realmListTicket = new ChangeRealmTicketResponse();
            realmListTicket.Token = changeRealmTicket.Token;
            realmListTicket.Allow = true;
            realmListTicket.Ticket = new Framework.IO.ByteBuffer();
            realmListTicket.Ticket.WriteCString("WorldserverRealmListTicket");

            SendPacket(realmListTicket);
        }        

        public void SendBattlenetResponse(uint serviceHash, uint methodId, uint token, IMessage response)
        {
            var bnetResponse = new Response();
            bnetResponse.BnetStatus = BattlenetRpcErrorCode.Ok;
            bnetResponse.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
            bnetResponse.Method.ObjectId = 1;
            bnetResponse.Method.Token = token;

            if (response.CalculateSize() != 0)
                bnetResponse.Data.WriteBytes(response.ToByteArray());

            SendPacket(bnetResponse);
        }

        public void SendBattlenetResponse(uint serviceHash, uint methodId, uint token,  BattlenetRpcErrorCode status)
        {
            var bnetResponse = new Response();
            bnetResponse.BnetStatus = status;
            bnetResponse.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
            bnetResponse.Method.ObjectId = 1;
            bnetResponse.Method.Token = token;

            SendPacket(bnetResponse);
        }

        public void SendBattlenetRequest(uint serviceHash, uint methodId, IMessage request, Action<CodedInputStream> callback)
        {
            _battlenetResponseCallbacks[_battlenetRequestToken] = callback;
            SendBattlenetRequest(serviceHash, methodId, request);
        }

        public void SendBattlenetRequest(uint serviceHash, uint methodId, IMessage request)
        {
            var notification = new Notification();
            notification.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
            notification.Method.ObjectId = 1;
            notification.Method.Token = _battlenetRequestToken++;

            if (request.CalculateSize() != 0)
                notification.Data.WriteBytes(request.ToByteArray());

            SendPacket(notification);
        }
    }
}
