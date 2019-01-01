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

using BNetServer.Services;
using Framework.Constants;
using Google.Protobuf;

namespace BNetServer.Networking
{
    class ResourcesService : ServiceBase
    {
        public ResourcesService(Session session, uint serviceHash) : base(session, serviceHash) { }

        public override void CallServerMethod(uint token, uint methodId, CodedInputStream stream)
        {
            switch (methodId)
            {
                /*case 1:
                    {
                        ContentHandleRequest request = new ContentHandleRequest();
                        request.MergeFrom(stream);

                        ContentHandle response = new ContentHandle();
                        BattlenetRpcErrorCode status = HandleGetContentHandle(request, response);
                        Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server method ResourcesService.GetContentHandle(bgs.protocol.resources.v1.ContentHandleRequest: {1}) returned bgs.protocol.ContentHandle: {2} status: {3}.",
                          GetCallerInfo(), request.ToString(), response.ToString(), status);
                        if (status == 0)
                            SendResponse(token, response);
                        else
                            SendResponse(token, status);
                        break;
                    }*/
                default:
                    Log.outError(LogFilter.ServiceProtobuf, "Bad method id {0}.", methodId);
                    SendResponse(token, BattlenetRpcErrorCode.RpcInvalidMethod);
                    break;
            }
        }

        /*BattlenetRpcErrorCode HandleGetContentHandle(ContentHandleRequest request, ContentHandle response)
        {
            Log.outError(LogFilter.ServiceProtobuf, "{0} Client tried to call not implemented method ResourcesService.GetContentHandle({1})", GetCallerInfo(), request.ToString());
            return BattlenetRpcErrorCode.RpcNotImplemented;
        }*/
    }
}
