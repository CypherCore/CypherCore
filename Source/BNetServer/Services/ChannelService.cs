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
using Bgs.Protocol.Channel.V1;
using BNetServer.Services;
using Framework.Constants;
using Google.Protobuf;

namespace BNetServer.Networking
{
    class ChannelService : ServiceBase
    {
        public ChannelService(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 2:
                    {
                        RemoveMemberRequest request = new RemoveMemberRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleRemoveMember(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelService.RemoveMember(bgs.protocol.channel.v1.RemoveMemberRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 3:
                    {
                        SendMessageRequest request = new SendMessageRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleSendMessage(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelService.SendMessage(bgs.protocol.channel.v1.SendMessageRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        UpdateChannelStateRequest request = new UpdateChannelStateRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleUpdateChannelState(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelService.UpdateChannelState(bgs.protocol.channel.v1.UpdateChannelStateRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 5:
                    {
                        UpdateMemberStateRequest request = new UpdateMemberStateRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleUpdateMemberState(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelService.UpdateMemberState(bgs.protocol.channel.v1.UpdateMemberStateRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 6:
                    {
                        DissolveRequest request = new DissolveRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleDissolve(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelService.Dissolve(bgs.protocol.channel.v1.DissolveRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
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

        BattlenetRpcErrorCode HandleRemoveMember(RemoveMemberRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelService.RemoveMember: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleSendMessage(SendMessageRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelService.SendMessage: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleUpdateChannelState(UpdateChannelStateRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelService.UpdateChannelState: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleUpdateMemberState(UpdateMemberStateRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelService.UpdateMemberState: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleDissolve(DissolveRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelService.Dissolve: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }

    class ChannelListener : ServiceBase
    {
        public ChannelListener(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        JoinNotification request = new JoinNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnJoin(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelListener.OnJoin(bgs.protocol.channel.v1.JoinNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        MemberAddedNotification request = new MemberAddedNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnMemberAdded(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelListener.OnMemberAdded(bgs.protocol.channel.v1.MemberAddedNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 3:
                    {
                        LeaveNotification request = new LeaveNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnLeave(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelListener.OnLeave(bgs.protocol.channel.v1.LeaveNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        MemberRemovedNotification request = new MemberRemovedNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnMemberRemoved(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelListener.OnMemberRemoved(bgs.protocol.channel.v1.MemberRemovedNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 5:
                    {
                        SendMessageNotification request = new SendMessageNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnSendMessage(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelListener.OnSendMessage(bgs.protocol.channel.v1.SendMessageNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 6:
                    {
                        UpdateChannelStateNotification request = new UpdateChannelStateNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnUpdateChannelState(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelListener.OnUpdateChannelState(bgs.protocol.channel.v1.UpdateChannelStateNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 7:
                    {
                        UpdateMemberStateNotification request = new UpdateMemberStateNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnUpdateMemberState(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChannelListener.OnUpdateMemberState(bgs.protocol.channel.v1.UpdateMemberStateNotification: {1}) status: {2}.",
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

        BattlenetRpcErrorCode HandleOnJoin(JoinNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelListener.OnJoin: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnMemberAdded(MemberAddedNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelListener.OnMemberAdded: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnLeave(LeaveNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelListener.OnLeave: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnMemberRemoved(MemberRemovedNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelListener.OnMemberRemoved: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnSendMessage(SendMessageNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelListener.OnSendMessage: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnUpdateChannelState(UpdateChannelStateNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelListener.OnUpdateChannelState: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnUpdateMemberState(UpdateMemberStateNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChannelListener.OnUpdateMemberState: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }
}
