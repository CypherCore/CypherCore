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
using Bgs.Protocol.Connection.V1;
using BNetServer.Services;
using Framework.Constants;
using Google.Protobuf;
using System.Diagnostics;

namespace BNetServer.Networking
{
    class ConnectionService : ServiceBase
    {
        public ConnectionService(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        ConnectRequest request = ConnectRequest.Parser.ParseFrom(stream);
                        ConnectResponse response = new ConnectResponse();

                        BattlenetRpcErrorCode status = HandleConnect(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ConnectionService.Connect(bgs.protocol.connection.v1.ConnectRequest: {1}) returned bgs.protocol.connection.v1.ConnectResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        BindRequest request = new BindRequest();
                        request.MergeFrom(stream);

                        BindResponse response = new BindResponse();
                        BattlenetRpcErrorCode status = HandleBind(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ConnectionService.Bind(bgs.protocol.connection.v1.BindRequest: {1}) returned bgs.protocol.connection.v1.BindResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 3:
                    {
                        EchoRequest request = new EchoRequest();
                        request.MergeFrom(stream);

                        EchoResponse response = new EchoResponse();
                        BattlenetRpcErrorCode status = HandleEcho(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ConnectionService.Echo(bgs.protocol.connection.v1.EchoRequest: {1}) returned bgs.protocol.connection.v1.EchoResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        DisconnectNotification request = DisconnectNotification.Parser.ParseFrom(stream);

                        BattlenetRpcErrorCode status = HandleForceDisconnect(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ConnectionService.ForceDisconnect(bgs.protocol.connection.v1.DisconnectNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 5:
                    {
                        NoData request = NoData.Parser.ParseFrom(stream);

                        BattlenetRpcErrorCode status = HandleKeepAlive(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ConnectionService.KeepAlive(bgs.protocol.NoData: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 6:
                    {
                        EncryptRequest request = EncryptRequest.Parser.ParseFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleEncrypt(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ConnectionService.Encrypt(bgs.protocol.connection.v1.EncryptRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 7:
                    {
                        DisconnectRequest request = DisconnectRequest.Parser.ParseFrom(stream);

                        BattlenetRpcErrorCode status = HandleRequestDisconnect(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ConnectionService.RequestDisconnect(bgs.protocol.connection.v1.DisconnectRequest: {1}) status: {2}.",
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

        BattlenetRpcErrorCode HandleConnect(ConnectRequest request, ConnectResponse response)
        {
            if (request.ClientId != null)
                response.ClientId.MergeFrom(request.ClientId);

            response.ServerId = new ProcessId();
            response.ServerId.Label = (uint)Process.GetCurrentProcess().Id;
            response.ServerId.Epoch = (uint)Time.UnixTime;
            response.ServerTime = (ulong)Time.UnixTimeMilliseconds;

            response.UseBindlessRpc = request.UseBindlessRpc;

            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode HandleBind(BindRequest request, BindResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ConnectionService.Bind: {1}", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleEcho(EchoRequest request, EchoResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ConnectionService.Echo: {1}", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleForceDisconnect(DisconnectNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ConnectionService.ForceDisconnect: {1}", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleKeepAlive(NoData request)
        {
            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode HandleEncrypt(EncryptRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ConnectionService.Encrypt: {1}", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleRequestDisconnect(DisconnectRequest request)
        {
            var disconnectNotification = new DisconnectNotification();
            disconnectNotification.ErrorCode = request.ErrorCode;
            SendRequest(4, disconnectNotification);

            _session.CloseSocket();
            return BattlenetRpcErrorCode.Ok;
        }
    }
}
