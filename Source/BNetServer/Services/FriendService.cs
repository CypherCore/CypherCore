/*
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

using Bgs.Protocol;
using Bgs.Protocol.Friends.V1;
using BNetServer.Services;
using Framework.Constants;
using Google.Protobuf;

namespace BNetServer.Networking
{
    class FriendsService : ServiceBase
    {
        public FriendsService(Session session, uint serviceHash) : base(session, serviceHash) { }

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
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.Subscribe(bgs.protocol.friends.v1.SubscribeRequest: {1}) returned bgs.protocol.friends.v1.SubscribeResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        SendInvitationRequest request = new SendInvitationRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleSendInvitation(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.SendInvitation(bgs.protocol.SendInvitationRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 3:
                    {
                        AcceptInvitationRequest request = new AcceptInvitationRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleAcceptInvitation(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.AcceptInvitation(bgs.protocol.GenericInvitationRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        RevokeInvitationRequest request = new RevokeInvitationRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleRevokeInvitation(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.RevokeInvitation(bgs.protocol.GenericInvitationRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 5:
                    {
                        DeclineInvitationRequest request = new DeclineInvitationRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleDeclineInvitation(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.DeclineInvitation(bgs.protocol.GenericInvitationRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 6:
                    {
                        IgnoreInvitationRequest request = new IgnoreInvitationRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleIgnoreInvitation(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.IgnoreInvitation(bgs.protocol.GenericInvitationRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 8:
                    {
                        RemoveFriendRequest request = new RemoveFriendRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleRemoveFriend(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.RemoveFriend(bgs.protocol.friends.v1.GenericFriendRequest: {1}) returned bgs.protocol.friends.v1.GenericFriendResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 9:
                    {
                        ViewFriendsRequest request = new ViewFriendsRequest();
                        request.MergeFrom(stream);

                        ViewFriendsResponse response = new ViewFriendsResponse();
                        BattlenetRpcErrorCode status = HandleViewFriends(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.ViewFriends(bgs.protocol.friends.v1.ViewFriendsRequest: {1}) returned bgs.protocol.friends.v1.ViewFriendsResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 10:
                    {
                        UpdateFriendStateRequest request = new UpdateFriendStateRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleUpdateFriendState(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.UpdateFriendState(bgs.protocol.friends.v1.UpdateFriendStateRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 11:
                    {
                        UnsubscribeRequest request = new UnsubscribeRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleUnsubscribe(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.Unsubscribe(bgs.protocol.friends.v1.UnsubscribeRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 12:
                    {
                        RevokeAllInvitationsRequest request = new RevokeAllInvitationsRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleRevokeAllInvitations(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsService.RevokeAllInvitations(bgs.protocol.friends.v1.GenericFriendRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
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
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.Subscribe: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleSendInvitation(SendInvitationRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.SendInvitation: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleAcceptInvitation(AcceptInvitationRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.AcceptInvitation: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleRevokeInvitation(RevokeInvitationRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.RevokeInvitation: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleDeclineInvitation(DeclineInvitationRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.DeclineInvitation: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleIgnoreInvitation(IgnoreInvitationRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.IgnoreInvitation: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleRemoveFriend(RemoveFriendRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.RemoveFriend: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleViewFriends(ViewFriendsRequest request, ViewFriendsResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.ViewFriends: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleUpdateFriendState(UpdateFriendStateRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.UpdateFriendState: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleUnsubscribe(UnsubscribeRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.Unsubscribe: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleRevokeAllInvitations(RevokeAllInvitationsRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsService.RevokeAllInvitations: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }

    class FriendsListener : ServiceBase
    {
        public FriendsListener(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        FriendNotification request = new FriendNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnFriendAdded(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsListener.OnFriendAdded(bgs.protocol.friends.v1.FriendNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        FriendNotification request = new FriendNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnFriendRemoved(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsListener.OnFriendRemoved(bgs.protocol.friends.v1.FriendNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 3:
                    {
                        InvitationNotification request = new InvitationNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnReceivedInvitationAdded(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsListener.OnReceivedInvitationAdded(bgs.protocol.friends.v1.InvitationNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        InvitationNotification request = new InvitationNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnReceivedInvitationRemoved(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsListener.OnReceivedInvitationRemoved(bgs.protocol.friends.v1.InvitationNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 5:
                    {
                        InvitationNotification request = new InvitationNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnSentInvitationAdded(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsListener.OnSentInvitationAdded(bgs.protocol.friends.v1.InvitationNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 6:
                    {
                        InvitationNotification request = new InvitationNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnSentInvitationRemoved(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsListener.OnSentInvitationRemoved(bgs.protocol.friends.v1.InvitationNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 7:
                    {
                        UpdateFriendStateNotification request = new UpdateFriendStateNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnUpdateFriendState(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method FriendsListener.OnUpdateFriendState(bgs.protocol.friends.v1.UpdateFriendStateNotification: {1}) status: {2}.",
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

        BattlenetRpcErrorCode HandleOnFriendAdded(FriendNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsListener.OnFriendAdded: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnFriendRemoved(FriendNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsListener.OnFriendRemoved: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnReceivedInvitationAdded(InvitationNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsListener.OnReceivedInvitationAdded: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnReceivedInvitationRemoved(InvitationNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsListener.OnReceivedInvitationRemoved: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnSentInvitationAdded(InvitationNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsListener.OnSentInvitationAdded: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnSentInvitationRemoved(InvitationNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsListener.OnSentInvitationRemoved: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnUpdateFriendState(UpdateFriendStateNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method FriendsListener.OnUpdateFriendState: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }
}
