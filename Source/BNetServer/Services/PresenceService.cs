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

using Bgs.Protocol;
using Bgs.Protocol.Presence.V1;
using BNetServer.Services;
using Framework.Constants;
using Google.Protobuf;

namespace BNetServer.Networking
{
    class PresenceService : ServiceBase
    {
        public PresenceService(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        SubscribeRequest request = new SubscribeRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleSubscribe(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method PresenceService.Subscribe(bgs.protocol.presence.v1.SubscribeRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        UnsubscribeRequest request = new UnsubscribeRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleUnsubscribe(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method PresenceService.Unsubscribe(bgs.protocol.presence.v1.UnsubscribeRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 3:
                    {
                        UpdateRequest request = new UpdateRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleUpdate(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method PresenceService.Update(bgs.protocol.presence.v1.UpdateRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        QueryRequest request = new QueryRequest();
                        request.MergeFrom(stream);
                        

                        QueryResponse response = new QueryResponse();
                        BattlenetRpcErrorCode status = HandleQuery(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method PresenceService.Query(bgs.protocol.presence.v1.QueryRequest: {1}) returned bgs.protocol.presence.v1.QueryResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 5:
                    {
                        OwnershipRequest request = new OwnershipRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleOwnership(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method PresenceService.Ownership(bgs.protocol.presence.v1.OwnershipRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 7:
                    {
                        SubscribeNotificationRequest request = new SubscribeNotificationRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleSubscribeNotification(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method PresenceService.SubscribeNotification(bgs.protocol.presence.v1.SubscribeNotificationRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                /*case 8:
                    {
                        MigrateOlympusCustomMessageRequest request = new MigrateOlympusCustomMessageRequest();
                        request.MergeFrom(stream);
                        

                        MigrateOlympusCustomMessageResponse response = new MigrateOlympusCustomMessageResponse();
                        BattlenetRpcErrorCode status = HandleMigrateOlympusCustomMessage(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method PresenceService.MigrateOlympusCustomMessage(bgs.protocol.presence.v1.MigrateOlympusCustomMessageRequest: {1}) returned bgs.protocol.presence.v1.MigrateOlympusCustomMessageResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }*/
                default:
                    Log.outError(LogFilter.ServiceProtobuf, "Bad method id {0}.", methodId);
                    SendResponse(token, BattlenetRpcErrorCode.RpcInvalidMethod);
                    break;
            }
        }

        BattlenetRpcErrorCode HandleSubscribe(SubscribeRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method PresenceService.Subscribe: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleUnsubscribe(UnsubscribeRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method PresenceService.Unsubscribe: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleUpdate(UpdateRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method PresenceService.Update: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleQuery(QueryRequest request, QueryResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method PresenceService.Query: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOwnership(OwnershipRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method PresenceService.Ownership: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleSubscribeNotification(SubscribeNotificationRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method PresenceService.SubscribeNotification: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        /*BattlenetRpcErrorCode HandleMigrateOlympusCustomMessage(MigrateOlympusCustomMessageRequest request, MigrateOlympusCustomMessageResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method PresenceService.MigrateOlympusCustomMessage: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
        */
    }
}
