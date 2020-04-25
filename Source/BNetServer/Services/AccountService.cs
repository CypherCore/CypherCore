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
using Bgs.Protocol.Account.V1;
using BNetServer.Services;
using Framework.Constants;
using Google.Protobuf;

namespace BNetServer.Networking
{
    class AccountService : ServiceBase
    {
        public AccountService(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 13:
                    {
                        ResolveAccountRequest request = new ResolveAccountRequest();
                        request.MergeFrom(stream);

                        ResolveAccountResponse response = new ResolveAccountResponse();
                        BattlenetRpcErrorCode status = HandleResolveAccount(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.GetAccount(GetAccountRequest: {1}) returned GetAccountResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 15:
                    {
                        IsIgrAddressRequest request = new IsIgrAddressRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleIsIgrAddress(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.IsIgrAddress(IsIgrAddressRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 25:
                    {
                        SubscriptionUpdateRequest request = new SubscriptionUpdateRequest();
                        request.MergeFrom(stream);

                        SubscriptionUpdateResponse response = new SubscriptionUpdateResponse();
                        BattlenetRpcErrorCode status = HandleSubscribe(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.Subscribe(SubscriptionUpdateRequest: {1}) returned SubscriptionUpdateResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 26:
                    {
                        SubscriptionUpdateRequest request = new SubscriptionUpdateRequest();
                        request.MergeFrom(stream);

                        NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleUnsubscribe(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.Unsubscribe(SubscriptionUpdateRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 30:
                    {
                        GetAccountStateRequest request = new GetAccountStateRequest();
                        request.MergeFrom(stream);

                        GetAccountStateResponse response = new GetAccountStateResponse();
                        BattlenetRpcErrorCode status = HandleGetAccountState(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.GetAccountState(GetAccountStateRequest: {1}) returned GetAccountStateResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 31:
                    {
                        GetGameAccountStateRequest request = new GetGameAccountStateRequest();
                        request.MergeFrom(stream);

                        GetGameAccountStateResponse response = new GetGameAccountStateResponse();
                        BattlenetRpcErrorCode status = HandleGetGameAccountState(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.GetGameAccountState(GetGameAccountStateRequest: {1}) returned GetGameAccountStateResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 32:
                    {
                        GetLicensesRequest request = new GetLicensesRequest();
                        request.MergeFrom(stream);

                        GetLicensesResponse response = new GetLicensesResponse();
                        BattlenetRpcErrorCode status = HandleGetLicenses(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.GetLicenses(GetLicensesRequest: {1}) returned GetLicensesResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 33:
                    {
                        GetGameTimeRemainingInfoRequest request = new GetGameTimeRemainingInfoRequest();
                        request.MergeFrom(stream);

                        GetGameTimeRemainingInfoResponse response = new GetGameTimeRemainingInfoResponse();
                        BattlenetRpcErrorCode status = HandleGetGameTimeRemainingInfo(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.GetGameTimeRemainingInfo(GetGameTimeRemainingInfoRequest: {1}) returned GetGameTimeRemainingInfoResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 34:
                    {
                        GetGameSessionInfoRequest request = new GetGameSessionInfoRequest();
                        request.MergeFrom(stream);

                        GetGameSessionInfoResponse response = new GetGameSessionInfoResponse();
                        BattlenetRpcErrorCode status = HandleGetGameSessionInfo(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.GetGameSessionInfo(GetGameSessionInfoRequest: {1}) returned GetGameSessionInfoResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 35:
                    {
                        GetCAISInfoRequest request = new GetCAISInfoRequest();
                        request.MergeFrom(stream);

                        GetCAISInfoResponse response = new GetCAISInfoResponse();
                        BattlenetRpcErrorCode status = HandleGetCAISInfo(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.GetCAISInfo(GetCAISInfoRequest: {1}) returned GetCAISInfoResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 37:
                    {
                        GetAuthorizedDataRequest request = new GetAuthorizedDataRequest();
                        request.MergeFrom(stream);

                        GetAuthorizedDataResponse response = new GetAuthorizedDataResponse();
                        BattlenetRpcErrorCode status = HandleGetAuthorizedData(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountService.GetAuthorizedData(GetAuthorizedDataRequest: {1}) returned GetAuthorizedDataResponse: {2} status: {3}.",
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

        BattlenetRpcErrorCode HandleResolveAccount(ResolveAccountRequest request, ResolveAccountResponse response)
        {
            Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountService.ResolveAccount({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleIsIgrAddress(IsIgrAddressRequest request, NoData response)
        {
            Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountService.IsIgrAddress({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleSubscribe(SubscriptionUpdateRequest request, SubscriptionUpdateResponse response)
        {
            Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountService.Subscribe({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleUnsubscribe(SubscriptionUpdateRequest request, NoData response)
        {
            Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountService.Unsubscribe({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGetAccountState(GetAccountStateRequest request, GetAccountStateResponse response)
        {
            return _session.HandleGetAccountState(request, response);
        }

        BattlenetRpcErrorCode HandleGetGameAccountState(GetGameAccountStateRequest request, GetGameAccountStateResponse response)
        {
            return _session.HandleGetGameAccountState(request, response);
        }

        BattlenetRpcErrorCode HandleGetLicenses(GetLicensesRequest request, GetLicensesResponse response)
        {
            Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountService.GetLicenses({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGetGameTimeRemainingInfo(GetGameTimeRemainingInfoRequest request, GetGameTimeRemainingInfoResponse response)
        {
            Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountService.GetGameTimeRemainingInfo({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGetGameSessionInfo(GetGameSessionInfoRequest request, GetGameSessionInfoResponse response)
        {
            Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountService.GetGameSessionInfo({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGetCAISInfo(GetCAISInfoRequest request, GetCAISInfoResponse response)
        {
            Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountService.GetCAISInfo({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGetAuthorizedData(GetAuthorizedDataRequest request, GetAuthorizedDataResponse response)
        {
            Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountService.GetAuthorizedData({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleGameAccountFlagUpdate(GameAccountFlagUpdateRequest request)
        {
            Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountService.GameAccountFlagUpdate({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }

    class AccountListener : ServiceBase
    {
        public AccountListener(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        AccountStateNotification request = new AccountStateNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnAccountStateUpdated(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountListener.OnAccountStateUpdated(AccountStateNotification: {1} status: {2}.", GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        GameAccountStateNotification request = new GameAccountStateNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnGameAccountStateUpdated(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountListener.OnGameAccountStateUpdated(GameAccountStateNotification: {1} status: {2}.", GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 3:
                    {
                        GameAccountNotification request = new GameAccountNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnGameAccountsUpdated(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountListener.OnGameAccountsUpdated(GameAccountNotification: {1} status: {2}.", GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        GameAccountSessionNotification request = new GameAccountSessionNotification();
                        request.MergeFrom(stream);

                        BattlenetRpcErrorCode status = HandleOnGameSessionUpdated(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method AccountListener.OnGameSessionUpdated(GameAccountSessionNotification: {1} status: {2}.", GetCallerInfo(), request.ToString(), status);
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

        BattlenetRpcErrorCode HandleOnAccountStateUpdated(AccountStateNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountListener.OnAccountStateUpdated: {1}", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnGameAccountStateUpdated(GameAccountStateNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountListener.OnGameAccountStateUpdated: {1}", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnGameAccountsUpdated(GameAccountNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountListener.OnGameAccountsUpdated: {1}", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnGameSessionUpdated(GameAccountSessionNotification request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method AccountListener.OnGameSessionUpdated: {1}", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }
}
