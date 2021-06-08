// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol;
using Bgs.Protocol.Connection.V1;
using Framework.Constants;
using System;

namespace BNetServer.Networking
{
    public partial class Session
    {
        [Service(OriginalHash.ConnectionService, 1)]
        BattlenetRpcErrorCode HandleConnect(ConnectRequest request, ConnectResponse response)
        {
            if (request.ClientId != null)
                response.ClientId.MergeFrom(request.ClientId);

            response.ServerId = new ProcessId();
            response.ServerId.Label = (uint)Environment.ProcessId;
            response.ServerId.Epoch = (uint)Time.UnixTime;
            response.ServerTime = (ulong)Time.UnixTimeMilliseconds;

            response.UseBindlessRpc = request.UseBindlessRpc;

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