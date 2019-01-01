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
using Bgs.Protocol.UserManager.V1;
using BNetServer.Services;
using Framework.Constants;
using Google.Protobuf;

namespace BNetServer.Networking
{
    class UserManagerService : ServiceBase
    {
        public UserManagerService(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        SubscribeRequest request = new SubscribeRequest();
                        request.MergeFrom(stream);

                        SubscribeResponse response = new SubscribeResponse();
                        BattlenetRpcErrorCode status = HandleSubscribe(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerService.Subscribe(bgs.protocol.user_manager.v1.SubscribeRequest: {1}) returned bgs.protocol.user_manager.v1.SubscribeResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 10:
                    {
                        AddRecentPlayersRequest request = new AddRecentPlayersRequest();
                        request.MergeFrom(stream);

                        AddRecentPlayersResponse response = new AddRecentPlayersResponse();
                        BattlenetRpcErrorCode status = HandleAddRecentPlayers(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerService.AddRecentPlayers(bgs.protocol.user_manager.v1.AddRecentPlayersRequest: {1}) returned bgs.protocol.user_manager.v1.AddRecentPlayersResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 11:
                    {
                        ClearRecentPlayersRequest request = new ClearRecentPlayersRequest();
                        request.MergeFrom(stream);
                        

                        ClearRecentPlayersResponse response = new ClearRecentPlayersResponse();
                        BattlenetRpcErrorCode status = HandleClearRecentPlayers(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerService.ClearRecentPlayers(bgs.protocol.user_manager.v1.ClearRecentPlayersRequest: {1}) returned bgs.protocol.user_manager.v1.ClearRecentPlayersResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 20:
                    {
                        BlockPlayerRequest request = new BlockPlayerRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleBlockPlayer(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerService.BlockPlayer(bgs.protocol.user_manager.v1.BlockPlayerRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 21:
                    {
                        UnblockPlayerRequest request = new UnblockPlayerRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleUnblockPlayer(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerService.UnblockPlayer(bgs.protocol.user_manager.v1.UnblockPlayerRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 40:
                    {
                        BlockPlayerRequest request = new BlockPlayerRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleBlockPlayerForSession(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerService.BlockPlayerForSession(bgs.protocol.user_manager.v1.BlockPlayerRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 50:
                    {
                        EntityId request = new EntityId();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleLoadBlockList(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerService.LoadBlockList(bgs.protocol.EntityId: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 51:
                    {
                        UnsubscribeRequest request = new UnsubscribeRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleUnsubscribe(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerService.Unsubscribe(bgs.protocol.user_manager.v1.UnsubscribeRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                default:
                    Log.outError(LogFilter.ServiceProtobuf, "Bad method id {0}.", methodId);
                    SendResponse(token, BattlenetRpcErrorCode.RpcInvalidMethod);
                    break;
            }
        }

        BattlenetRpcErrorCode HandleSubscribe(SubscribeRequest request, SubscribeResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerService.Subscribe: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleAddRecentPlayers(AddRecentPlayersRequest request, AddRecentPlayersResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerService.AddRecentPlayers: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleClearRecentPlayers(ClearRecentPlayersRequest request, ClearRecentPlayersResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerService.ClearRecentPlayers: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleBlockPlayer(BlockPlayerRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerService.BlockPlayer: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleUnblockPlayer(UnblockPlayerRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerService.UnblockPlayer: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleBlockPlayerForSession(BlockPlayerRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerService.BlockPlayerForSession: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleLoadBlockList(EntityId request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerService.LoadBlockList: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleUnsubscribe(UnsubscribeRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerService.Unsubscribe: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }

    class UserManagerListener : ServiceBase
    {
        public UserManagerListener(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        BlockedPlayerAddedNotification request = new BlockedPlayerAddedNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnBlockedPlayerAdded(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerListener.OnBlockedPlayerAdded(bgs.protocol.user_manager.v1.BlockedPlayerAddedNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        BlockedPlayerRemovedNotification request = new BlockedPlayerRemovedNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnBlockedPlayerRemoved(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerListener.OnBlockedPlayerRemoved(bgs.protocol.user_manager.v1.BlockedPlayerRemovedNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 11:
                    {
                        RecentPlayersAddedNotification request = new RecentPlayersAddedNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnRecentPlayersAdded(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerListener.OnRecentPlayersAdded(bgs.protocol.user_manager.v1.RecentPlayersAddedNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 12:
                    {
                        RecentPlayersRemovedNotification request = new RecentPlayersRemovedNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnRecentPlayersRemoved(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method UserManagerListener.OnRecentPlayersRemoved(bgs.protocol.user_manager.v1.RecentPlayersRemovedNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                default:
                    Log.outError(LogFilter.ServiceProtobuf, "Bad method id {0}.", methodId);
                    SendResponse(token, BattlenetRpcErrorCode.RpcInvalidMethod);
                    break;
            }
        }

        BattlenetRpcErrorCode HandleOnBlockedPlayerAdded(BlockedPlayerAddedNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerListener.OnBlockedPlayerAdded: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnBlockedPlayerRemoved(BlockedPlayerRemovedNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerListener.OnBlockedPlayerRemoved: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnRecentPlayersAdded(RecentPlayersAddedNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerListener.OnRecentPlayersAdded: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnRecentPlayersRemoved(RecentPlayersRemovedNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method UserManagerListener.OnRecentPlayersRemoved: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }
}
