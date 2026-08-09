// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using BNetServer.Networking;
using Framework.Configuration;
using Framework.Constants;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace BNetServer
{
    public class LoginServiceManager : Singleton<LoginServiceManager>
    {
        ConcurrentDictionary<(uint ServiceHash, uint MethodId), BnetServiceHandler> serviceHandlers = new();
        X509Certificate2 certificate;

        LoginServiceManager() { }

        public void Initialize()
        {
            certificate = X509CertificateLoader.LoadPkcs12FromFile(ConfigMgr.GetDefaultValue("CertificatesFile", "./BNetServer.pfx"), null);

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

                        serviceHandlers[key] = new BnetServiceHandler(methodInfo, parameters);
                    }
                }
            }
        }

        public BnetServiceHandler GetHandler(uint serviceHash, uint methodId)
        {
            return serviceHandlers.LookupByKey((serviceHash, methodId));
        }

        public X509Certificate2 GetCertificate()
        {
            return certificate;
        }

        public bool UsesDevWildcardCertificate()
        {
            return certificate.Subject.Contains("*.*");
        }
    }

    public class BnetServiceHandler
    {
        Delegate methodCaller;
        Type requestType;
        Type responseType;

        public BnetServiceHandler(MethodInfo info, ParameterInfo[] parameters)
        {
            requestType = parameters[0].ParameterType;
            responseType = parameters[1].ParameterType;

            methodCaller = info.CreateDelegate(Expression.GetDelegateType([typeof(Session), requestType, responseType, typeof(Action<Session, BattlenetRpcErrorCode, IMessage>), info.ReturnType]));
        }

        public void Invoke(Session session, uint token, CodedInputStream stream)
        {
            var request = (IMessage)Activator.CreateInstance(requestType);
            request.MergeFrom(stream);

            IMessage response = (IMessage)Activator.CreateInstance(responseType);
            Action<Session, BattlenetRpcErrorCode, IMessage> continuation = CreateServerContinuation(token, nameof(request), response.Descriptor);
            BattlenetRpcErrorCode status = (BattlenetRpcErrorCode)methodCaller.DynamicInvoke(session, request, response, continuation);

            if (continuation != null)
                continuation(session, status, response);
        }

        Action<Session, BattlenetRpcErrorCode, IMessage> CreateServerContinuation(uint token, string methodName, Google.Protobuf.Reflection.MessageDescriptor outputDescriptor)
        {
            return (service, status, response) =>
             {
                 Cypher.Assert(response.Descriptor == outputDescriptor);
                 Log.outDebug(LogFilter.ServiceProtobuf, $"{service.GetClientInfo()} Client called server method {methodName}() {outputDescriptor.FullName}{{ {response} }} status {status}.");
                 if (status == 0)
                     service.SendResponse(token, response);
                 else
                     service.SendResponse(token, status);
             };
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ServiceAttribute : Attribute
    {
        public uint ServiceHash { get; set; }
        public uint MethodId { get; set; }
        public Action<Session, BattlenetRpcErrorCode, IMessage> SendResponse { get; set; }

        public ServiceAttribute(OriginalHash serviceHash, uint methodId)
        {
            ServiceHash = (uint)serviceHash;
            MethodId = methodId;
        }
    }
}