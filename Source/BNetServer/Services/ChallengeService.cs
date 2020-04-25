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
using Bgs.Protocol.Challenge.V1;
using BNetServer.Services;
using Framework.Constants;
using Google.Protobuf;

namespace BNetServer.Networking
{
    class ChallengeListener : ServiceBase
    {
        public ChallengeListener(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
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
