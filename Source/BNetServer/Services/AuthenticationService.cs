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
using Bgs.Protocol.Authentication.V1;
using BNetServer.Services;
using Framework.Constants;
using Google.Protobuf;

namespace BNetServer.Networking
{
    class AuthenticationService : ServiceBase
    {
        public AuthenticationService(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        LogonRequest request = new LogonRequest();
                        request.MergeFrom(stream);                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleLogon(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationService.Logon(bgs.protocol.authentication.v1.LogonRequest: {1}) returned bgs.protocol.NoData: {2} status {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        ModuleNotification request = new ModuleNotification();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleModuleNotify(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationService.ModuleNotify(bgs.protocol.authentication.v1.ModuleNotification: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 3:
                    {
                        ModuleMessageRequest request = new ModuleMessageRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleModuleMessage(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationService.ModuleMessage(bgs.protocol.authentication.v1.ModuleMessageRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        EntityId request = new EntityId();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleSelectGameAccount_DEPRECATED(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationService.SelectGameAccount_DEPRECATED(bgs.protocol.EntityId: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 5:
                    {
                        GenerateSSOTokenRequest request = new GenerateSSOTokenRequest();
                        request.MergeFrom(stream);
                        

                        GenerateSSOTokenResponse response = new GenerateSSOTokenResponse();
                        BattlenetRpcErrorCode status = HandleGenerateSSOToken(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationService.GenerateSSOToken(bgs.protocol.authentication.v1.GenerateSSOTokenRequest: {1}) returned bgs.protocol.authentication.v1.GenerateSSOTokenResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 6:
                    {
                        SelectGameAccountRequest request = new SelectGameAccountRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleSelectGameAccount(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationService.SelectGameAccount(bgs.protocol.authentication.v1.SelectGameAccountRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 7:
                    {
                        VerifyWebCredentialsRequest request = new VerifyWebCredentialsRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleVerifyWebCredentials(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationService.VerifyWebCredentials(bgs.protocol.authentication.v1.VerifyWebCredentialsRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 8:
                    {
                        GenerateWebCredentialsRequest request = new GenerateWebCredentialsRequest();
                        request.MergeFrom(stream);
                        

                        GenerateWebCredentialsResponse response = new GenerateWebCredentialsResponse();
                        BattlenetRpcErrorCode status = HandleGenerateWebCredentials(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationService.GenerateWebCredentials(bgs.protocol.authentication.v1.GenerateWebCredentialsRequest: {1}) returned bgs.protocol.authentication.v1.GenerateWebCredentialsResponse: {2} status: {3}.",
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

        BattlenetRpcErrorCode HandleLogon(LogonRequest request, NoData response)
        {
            return _session.HandleLogon(request);
        }

        BattlenetRpcErrorCode HandleModuleNotify(ModuleNotification request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationService.ModuleNotify: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleModuleMessage(ModuleMessageRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationService.ModuleMessage: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleSelectGameAccount_DEPRECATED(EntityId request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationService.SelectGameAccount_DEPRECATED: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGenerateSSOToken(GenerateSSOTokenRequest request, GenerateSSOTokenResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationService.GenerateSSOToken: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleSelectGameAccount(SelectGameAccountRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationService.SelectGameAccount: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleVerifyWebCredentials(VerifyWebCredentialsRequest request, NoData response)
        {
            return _session.HandleVerifyWebCredentials(request);
        }

        BattlenetRpcErrorCode HandleGenerateWebCredentials(GenerateWebCredentialsRequest request, GenerateWebCredentialsResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationService.GenerateWebCredentials: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }

    class AuthenticationListener : ServiceBase
    {
        public AuthenticationListener(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        ModuleLoadRequest request = new ModuleLoadRequest();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnModuleLoad(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationListener.OnModuleLoad(bgs.protocol.authentication.v1.ModuleLoadRequest: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        ModuleMessageRequest request = new ModuleMessageRequest();
                        request.MergeFrom(stream);
                        

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleOnModuleMessage(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationListener.OnModuleMessage(bgs.protocol.authentication.v1.ModuleMessageRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        ServerStateChangeRequest request = new ServerStateChangeRequest();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnServerStateChange(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationListener.OnServerStateChange(bgs.protocol.authentication.v1.ServerStateChangeRequest: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 5:
                    {
                        LogonResult request = new LogonResult();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnLogonComplete(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationListener.OnLogonComplete(bgs.protocol.authentication.v1.LogonResult: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 6:
                    {
                        MemModuleLoadRequest request = new MemModuleLoadRequest();
                        request.MergeFrom(stream);
                        

                        MemModuleLoadResponse response = new MemModuleLoadResponse();
                        BattlenetRpcErrorCode status = HandleOnMemModuleLoad(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationListener.OnMemModuleLoad(bgs.protocol.authentication.v1.MemModuleLoadRequest: {1}) returned bgs.protocol.authentication.v1.MemModuleLoadResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 10:
                    {
                        LogonUpdateRequest request = new LogonUpdateRequest();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnLogonUpdate(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationListener.OnLogonUpdate(bgs.protocol.authentication.v1.LogonUpdateRequest: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 11:
                    {
                        VersionInfoNotification request = new VersionInfoNotification();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnVersionInfoUpdated(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationListener.OnVersionInfoUpdated(bgs.protocol.authentication.v1.VersionInfoNotification: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 12:
                    {
                        LogonQueueUpdateRequest request = new LogonQueueUpdateRequest();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnLogonQueueUpdate(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationListener.OnLogonQueueUpdate(bgs.protocol.authentication.v1.LogonQueueUpdateRequest: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 13:
                    {
                        NoData request = new NoData();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnLogonQueueEnd(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationListener.OnLogonQueueEnd(bgs.protocol.NoData: {1}) status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 14:
                    {
                        GameAccountSelectedRequest request = new GameAccountSelectedRequest();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnGameAccountSelected(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AuthenticationListener.OnGameAccountSelected(bgs.protocol.authentication.v1.GameAccountSelectedRequest: {1}) status: {2}.",
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

        BattlenetRpcErrorCode HandleOnModuleLoad(ModuleLoadRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationListener.OnModuleLoad: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnModuleMessage(ModuleMessageRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationListener.OnModuleMessage: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnServerStateChange(ServerStateChangeRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationListener.OnServerStateChange: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnLogonComplete(LogonResult request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationListener.OnLogonComplete: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnMemModuleLoad(MemModuleLoadRequest request, MemModuleLoadResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationListener.OnMemModuleLoad: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnLogonUpdate(LogonUpdateRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationListener.OnLogonUpdate: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnVersionInfoUpdated(VersionInfoNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationListener.OnVersionInfoUpdated: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnLogonQueueUpdate(LogonQueueUpdateRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationListener.OnLogonQueueUpdate: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnLogonQueueEnd(NoData request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationListener.OnLogonQueueEnd: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnGameAccountSelected(GameAccountSelectedRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AuthenticationListener.OnGameAccountSelected: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }
}
