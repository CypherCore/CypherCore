// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
            if (parameters.Length > 1)
                responseType = parameters[1].ParameterType;

            if (responseType != null)
                methodCaller = info.CreateDelegate(Expression.GetDelegateType(new[] { typeof(WorldSession), requestType, responseType, info.ReturnType }));
            else
                methodCaller = info.CreateDelegate(Expression.GetDelegateType(new[] { typeof(WorldSession), requestType, info.ReturnType }));
        }

        public void Invoke(WorldSession session, MethodCall methodCall, CodedInputStream stream)
        {
            var request = (IMessage)Activator.CreateInstance(requestType);
            request.MergeFrom(stream);

            BattlenetRpcErrorCode status;
            if (responseType != null)
            {
                var response = (IMessage)Activator.CreateInstance(responseType);
                status = (BattlenetRpcErrorCode)methodCaller.DynamicInvoke(session, request, response);
                Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server Method: {1}) Returned: {2} Status: {3}.", session.GetRemoteAddress(), request, response, status);
                if (status == 0)
                    session.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, response);
                else
                    session.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, status);
            }
            else
            {
                status = (BattlenetRpcErrorCode)methodCaller.DynamicInvoke(session, request);
                Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server Method: {1}) Status: {2}.", session.GetRemoteAddress(), request, status);
                if (status != 0)
                    session.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, status);
            }
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
