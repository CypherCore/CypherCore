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
using Game.Network;
using Game.Network.Packets;
using Google.Protobuf;
using System;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.BattlenetRequest, Status = SessionStatus.Authed)]
        void HandleBattlenetRequest(BattlenetRequest request)
        {
            Global.ServiceMgr.Dispatch(this, request.Method.GetServiceHash(), request.Method.Token, request.Method.GetMethodId(), new CodedInputStream(request.Data));
        }

        [WorldPacketHandler(ClientOpcodes.BattlenetRequestRealmListTicket, Status = SessionStatus.Authed)]
        void HandleBattlenetRequestRealmListTicket(RequestRealmListTicket requestRealmListTicket)
        {
            SetRealmListSecret(requestRealmListTicket.Secret);

            RealmListTicket realmListTicket = new RealmListTicket();
            realmListTicket.Token = requestRealmListTicket.Token;
            realmListTicket.Allow = true;
            realmListTicket.Ticket = new Framework.IO.ByteBuffer();
            realmListTicket.Ticket.WriteCString("WorldserverRealmListTicket");

            SendPacket(realmListTicket);
        }        

        public void SendBattlenetResponse(uint serviceHash, uint methodId, uint token, IMessage response)
        {
            Response bnetResponse = new Response();
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
            Response bnetResponse = new Response();
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
            Notification notification = new Notification();
            notification.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
            notification.Method.ObjectId = 1;
            notification.Method.Token = _battlenetRequestToken++;

            if (request.CalculateSize() != 0)
                notification.Data.WriteBytes(request.ToByteArray());

            SendPacket(notification);
        }
    }
}
