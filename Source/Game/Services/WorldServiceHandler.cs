/*
 * Copyright (C) 2012-2016 CypherCore <http://github.com/CypherCore>
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

using System;
using System.Linq.Expressions;
using System.Reflection;
using Framework.Constants;
using Game.Networking.Packets;
using Google.Protobuf;

namespace Game.Services
{
    public class WorldServiceHandler
    {
        private readonly Delegate _methodCaller;
        private readonly Type _requestType;
        private readonly Type _responseType;

        public WorldServiceHandler(MethodInfo info, ParameterInfo[] parameters)
        {
            _requestType = parameters[0].ParameterType;

            if (parameters.Length > 1)
                _responseType = parameters[1].ParameterType;

            if (_responseType != null)
                _methodCaller = info.CreateDelegate(Expression.GetDelegateType(new[]
                                                                              {
                                                                                  typeof(WorldSession), _requestType, _responseType, info.ReturnType
                                                                              }));
            else
                _methodCaller = info.CreateDelegate(Expression.GetDelegateType(new[]
                                                                              {
                                                                                  typeof(WorldSession), _requestType, info.ReturnType
                                                                              }));
        }

        public void Invoke(WorldSession session, MethodCall methodCall, CodedInputStream stream)
        {
            var request = (IMessage)Activator.CreateInstance(_requestType);
            request.MergeFrom(stream);

            BattlenetRpcErrorCode status;

            if (_responseType != null)
            {
                var response = (IMessage)Activator.CreateInstance(_responseType);
                status = (BattlenetRpcErrorCode)_methodCaller.DynamicInvoke(session, request, response);
                Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server Method: {1}) Returned: {2} Status: {3}.", session.GetRemoteAddress(), request, response, status);

                if (status == 0)
                    session.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, response);
                else
                    session.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, status);
            }
            else
            {
                status = (BattlenetRpcErrorCode)_methodCaller.DynamicInvoke(session, request);
                Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server Method: {1}) Status: {2}.", session.GetRemoteAddress(), request, status);

                if (status != 0)
                    session.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, status);
            }
        }
    }
}