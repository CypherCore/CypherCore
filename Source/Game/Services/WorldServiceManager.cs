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

using Framework.Constants;
using Game.Networking.Packets;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Game.Services
{
    public class WorldServiceManager : Singleton<WorldServiceManager>
    {
        ConcurrentDictionary<(uint ServiceHash, uint MethodId), WorldServiceHandler> serviceHandlers;

        WorldServiceManager()
        {
            serviceHandlers = new ConcurrentDictionary<(uint ServiceHash, uint MethodId), WorldServiceHandler>();

            Assembly currentAsm = Assembly.GetExecutingAssembly();
            foreach (var type in currentAsm.GetTypes())
            {
                foreach (var methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    foreach (var serviceAttr in methodInfo.GetCustomAttributes<ServiceAttribute>())
                    {
                        if (serviceAttr == null)
                            continue;

                        var key = (serviceAttr.ServiceHash, serviceAttr.MethodId);
                        if (serviceHandlers.ContainsKey(key))
                        {
                            Log.outError(LogFilter.Network, $"Tried to override ServiceHandler: {serviceHandlers[key]} with {methodInfo.Name} (ServiceHash: {serviceAttr.ServiceHash} MethodId: {serviceAttr.MethodId})");
                            continue;
                        }

                        var parameters = methodInfo.GetParameters();
                        if (parameters.Length == 0)
                        {
                            Log.outError(LogFilter.Network, $"Method: {methodInfo.Name} needs atleast one paramter");
                            continue;
                        }

                        serviceHandlers[key] = new WorldServiceHandler(methodInfo, parameters);
                    }
                }
            }
        }

        public WorldServiceHandler GetHandler(uint serviceHash, uint methodId)
        {
            return serviceHandlers.LookupByKey((serviceHash, methodId));
        }
    }

    public class WorldServiceHandler
    {
        Delegate methodCaller;
        Type requestType;
        Type responseType;

        public WorldServiceHandler(MethodInfo info, ParameterInfo[] parameters)
        {
            requestType = parameters[0].ParameterType;
            responseType = parameters[1].ParameterType;

            methodCaller = info.CreateDelegate(Expression.GetDelegateType([typeof(WorldSession), requestType, responseType, typeof(Action<WorldSession, BattlenetRpcErrorCode, IMessage>), info.ReturnType]));
        }

        public void Invoke(WorldSession session, MethodCall methodCall, CodedInputStream stream)
        {
            var request = (IMessage)Activator.CreateInstance(requestType);
            request.MergeFrom(stream);

            IMessage response = (IMessage)Activator.CreateInstance(responseType);
            Action<WorldSession, BattlenetRpcErrorCode, IMessage> continuation = CreateServerContinuation(methodCall, nameof(request), response.Descriptor);
            BattlenetRpcErrorCode status = (BattlenetRpcErrorCode)methodCaller.DynamicInvoke(session, request, response, continuation);

            if (continuation != null)
                continuation(session, status, response);
        }

        Action<WorldSession, BattlenetRpcErrorCode, IMessage> CreateServerContinuation(MethodCall methodCall, string methodName, Google.Protobuf.Reflection.MessageDescriptor outputDescriptor)
        {
            return (service, status, response) =>
            {
                Cypher.Assert(response.Descriptor == outputDescriptor);
                Log.outDebug(LogFilter.ServiceProtobuf, $"{service.GetPlayerInfo()} Client called server method {methodName}() {outputDescriptor.FullName}{{ {response} }} status {status}.");
                if (status == 0)
                    service.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, response);
                else
                    service.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, status);
            };
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ServiceAttribute : Attribute
    {
        public uint ServiceHash { get; set; }
        public uint MethodId { get; set; }

        public ServiceAttribute(OriginalHash serviceHash, uint methodId)
        {
            ServiceHash = (uint)serviceHash;
            MethodId = methodId;
        }
    }
}
