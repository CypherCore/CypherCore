// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        void HandleBattlenetRequest(BattlenetRequest request)
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
        void HandleBattlenetChangeRealmTicket(ChangeRealmTicket changeRealmTicket)
        {
            SetRealmListSecret(changeRealmTicket.Secret);

            ChangeRealmTicketResponse realmListTicket = new();
            realmListTicket.Token = changeRealmTicket.Token;
            realmListTicket.Allow = true;
            realmListTicket.Ticket = new Framework.IO.ByteBuffer();
            realmListTicket.Ticket.WriteCString("WorldserverRealmListTicket");

            SendPacket(realmListTicket);
        }        

        public void SendBattlenetResponse(uint serviceHash, uint methodId, uint token, IMessage response)
        {
            Response bnetResponse = new();
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
            Response bnetResponse = new();
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
            Notification notification = new();
            notification.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
            notification.Method.ObjectId = 1;
            notification.Method.Token = _battlenetRequestToken++;

            if (request.CalculateSize() != 0)
                notification.Data.WriteBytes(request.ToByteArray());

            SendPacket(notification);
        }
    }
}
