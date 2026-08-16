// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol;
using Bgs.Protocol.Connection.V1;
using Framework.Constants;
using Google.Protobuf;
using System;

namespace BNetServer.Networking
{
    public partial class Session
    {
        [Service(OriginalHash.ConnectionService, 1)]
        BattlenetRpcErrorCode HandleConnect(ConnectRequest request, ConnectResponse response, Action<Session, BattlenetRpcErrorCode, IMessage> continuation)
        {
            var now = DateTime.Now - DateTime.UnixEpoch;
            response.ServerId = new ProcessId();
            response.ServerId.Label = (uint)Environment.ProcessId;
            response.ServerId.Epoch = (uint)(now - TimeSpan.FromMilliseconds(Time.GetMSTime())).TotalSeconds;
            if (request.ClientId == null)
            {
                response.ClientId = new ProcessId();
                response.ClientId.Label = (uint)GetSessionId();
                response.ClientId.Epoch = (uint)(GetCreationTime() - DateTime.UnixEpoch).TotalSeconds;
            }
            else
                response.ClientId.MergeFrom(request.ClientId);

            response.ServerTime = (ulong)Time.UnixTimeMilliseconds;

            response.UseBindlessRpc = request.UseBindlessRpc;

            response.Ciid = $"{response.ServerId.Label:X08}{response.ServerId.Epoch:X08}-{response.ClientId.Label:X08}{response.ClientId.Epoch:X08}";

            SetClientInstanceId(response.Ciid);

            return BattlenetRpcErrorCode.Ok;
        }

        [Service(OriginalHash.ConnectionService, 5)]
        BattlenetRpcErrorCode HandleKeepAlive(NoData request)
        {
            return BattlenetRpcErrorCode.Ok;
        }

        [Service(OriginalHash.ConnectionService, 7)]
        BattlenetRpcErrorCode HandleRequestDisconnect(DisconnectRequest request)
        {
            var disconnectNotification = new DisconnectNotification();
            disconnectNotification.ErrorCode = request.ErrorCode;
            SendRequest((uint)OriginalHash.ConnectionService, 4, disconnectNotification);

            CloseSocket();
            return BattlenetRpcErrorCode.Ok;
        }
    }
}