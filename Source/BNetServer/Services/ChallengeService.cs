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
using Bgs.Protocol.Challenge.V1;
using BNetServer.Services;
using Framework.Constants;
using Google.Protobuf;

namespace BNetServer.Networking
{
    class ChallengeService : ServiceBase
    {
        public ChallengeService(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        ChallengePickedRequest request = new ChallengePickedRequest();
                        request.MergeFrom(stream);
                        

                        ChallengePickedResponse response = new ChallengePickedResponse();
                        BattlenetRpcErrorCode status = HandleChallengePicked(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChallengeService.ChallengePicked(bgs.protocol.challenge.v1.ChallengePickedRequest: {1}) returned bgs.protocol.challenge.v1.ChallengePickedResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        ChallengeAnsweredRequest request = new ChallengeAnsweredRequest();
                        request.MergeFrom(stream);
                        

                        ChallengeAnsweredResponse response = new ChallengeAnsweredResponse();
                        BattlenetRpcErrorCode status = HandleChallengeAnswered(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChallengeService.ChallengeAnswered(bgs.protocol.challenge.v1.ChallengeAnsweredRequest: {1}) returned bgs.protocol.challenge.v1.ChallengeAnsweredResponse: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 3:
                    {
                        ChallengeCancelledRequest request = new ChallengeCancelledRequest();
                        request.MergeFrom(stream);
                        

      NoData response = new NoData();
                        BattlenetRpcErrorCode status = HandleChallengeCancelled(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChallengeService.ChallengeCancelled(bgs.protocol.challenge.v1.ChallengeCancelledRequest: {1}) returned bgs.protocol.NoData: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        SendChallengeToUserRequest request = new SendChallengeToUserRequest();
                        request.MergeFrom(stream);
                        

                        SendChallengeToUserResponse response = new SendChallengeToUserResponse();
                        BattlenetRpcErrorCode status = HandleSendChallengeToUser(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChallengeService.SendChallengeToUser(bgs.protocol.challenge.v1.SendChallengeToUserRequest: {1}) returned bgs.protocol.challenge.v1.SendChallengeToUserResponse: {2} status: {3}.",
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

        BattlenetRpcErrorCode HandleChallengePicked(ChallengePickedRequest request, ChallengePickedResponse response) {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChallengeService.ChallengePicked: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleChallengeAnswered(ChallengeAnsweredRequest request, ChallengeAnsweredResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChallengeService.ChallengeAnswered: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleChallengeCancelled(ChallengeCancelledRequest request, NoData response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChallengeService.ChallengeCancelled: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleSendChallengeToUser(SendChallengeToUserRequest request, SendChallengeToUserResponse response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChallengeService.SendChallengeToUser: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

    }

    class ChallengeListener : ServiceBase
    {
        public ChallengeListener(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                case 1:
                    {
                        ChallengeUserRequest request = new ChallengeUserRequest();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnChallengeUser(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChallengeListener.OnChallengeUser(bgs.protocol.challenge.v1.ChallengeUserRequest: {1} status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 2:
                    {
                        ChallengeResultRequest request = new ChallengeResultRequest();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnChallengeResult(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChallengeListener.OnChallengeResult(bgs.protocol.challenge.v1.ChallengeResultRequest: {1} status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 3:
                    {
                        ChallengeExternalRequest request = new ChallengeExternalRequest();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnExternalChallenge(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChallengeListener.OnExternalChallenge(bgs.protocol.challenge.v1.ChallengeExternalRequest: {1} status: {2}.",
                          GetCallerInfo(), request.ToString(), status);
                        if (status != 0)
                            SendResponse(token, status);
                        break;
                    }
                case 4:
                    {
                        ChallengeExternalResult request = new ChallengeExternalResult();
                        request.MergeFrom(stream);
                        

                        BattlenetRpcErrorCode status = HandleOnExternalChallengeResult(request);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ChallengeListener.OnExternalChallengeResult(bgs.protocol.challenge.v1.ChallengeExternalResult: {1} status: {2}.",
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

        BattlenetRpcErrorCode HandleOnChallengeUser(ChallengeUserRequest request) {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChallengeListener.OnChallengeUser: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnChallengeResult(ChallengeResultRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChallengeListener.OnChallengeResult: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnExternalChallenge(ChallengeExternalRequest request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChallengeListener.OnExternalChallenge: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        BattlenetRpcErrorCode HandleOnExternalChallengeResult(ChallengeExternalResult request)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ChallengeListener.OnExternalChallengeResult: {1}",
              GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }
    }
}
