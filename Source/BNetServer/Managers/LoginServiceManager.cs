// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using BNetServer.Networking;
using Framework.Configuration;
using Framework.Constants;
using Framework.Web;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace BNetServer
{
    public class LoginServiceManager : Singleton<LoginServiceManager>
    {
        ConcurrentDictionary<(uint ServiceHash, uint MethodId), BnetServiceHandler> serviceHandlers;
        FormInputs formInputs;
        IPEndPoint externalAddress;
        IPEndPoint localAddress;
        X509Certificate2 certificate;

        LoginServiceManager() 
        {
            serviceHandlers = new ConcurrentDictionary<(uint ServiceHash, uint MethodId), BnetServiceHandler>();
            formInputs = new FormInputs();
        }

        public void Initialize()
        {
            int port = ConfigMgr.GetDefaultValue("LoginREST.Port", 8081);
            if (port < 0 || port > 0xFFFF)
            {
                Log.outError(LogFilter.Network, $"Specified login service port ({port}) out of allowed range (1-65535), defaulting to 8081");
                port = 8081;
            }

            string configuredAddress = ConfigMgr.GetDefaultValue("LoginREST.ExternalAddress", "127.0.0.1");
            IPAddress address;
            if (!IPAddress.TryParse(configuredAddress, out address))
            {
                Log.outError(LogFilter.Network, $"Could not resolve LoginREST.ExternalAddress {configuredAddress}");
                return;
            }
            externalAddress = new IPEndPoint(address, port);

            configuredAddress = ConfigMgr.GetDefaultValue("LoginREST.LocalAddress", "127.0.0.1");
            if (!IPAddress.TryParse(configuredAddress, out address))
            {
                Log.outError(LogFilter.Network, $"Could not resolve LoginREST.ExternalAddress {configuredAddress}");
                return;
            }

            localAddress = new IPEndPoint(address, port);

            // set up form inputs 
            formInputs.Type = "LOGIN_FORM";

            var input = new FormInput();
            input.Id = "account_name";
            input.Type = "text";
            input.Label = "E-mail";
            input.MaxLength = 320;
            formInputs.Inputs.Add(input);

            input = new FormInput();
            input.Id = "password";
            input.Type = "password";
            input.Label = "Password";
            input.MaxLength = 16;
            formInputs.Inputs.Add(input);

            input = new FormInput();
            input.Id = "log_in_submit";
            input.Type = "submit";
            input.Label = "Log In";
            formInputs.Inputs.Add(input);

            certificate = new X509Certificate2("BNetServer.pfx");

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

        public IPEndPoint GetAddressForClient(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
                return localAddress;

            return externalAddress;
        }

        public FormInputs GetFormInput()
        {
            return formInputs;
        }

        public X509Certificate2 GetCertificate()
        {
            return certificate;
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
            if (parameters.Length > 1)
                responseType = parameters[1].ParameterType;

            if (responseType != null)
                methodCaller = info.CreateDelegate(Expression.GetDelegateType(new[] { typeof(Session), requestType, responseType, info.ReturnType }));
            else
                methodCaller = info.CreateDelegate(Expression.GetDelegateType(new[] { typeof(Session), requestType, info.ReturnType }));
        }

        public void Invoke(Session session, uint token, CodedInputStream stream)
        {
            var request = (IMessage)Activator.CreateInstance(requestType);
            request.MergeFrom(stream);

            BattlenetRpcErrorCode status;
            if (responseType != null)
            {
                var response = (IMessage)Activator.CreateInstance(responseType);
                status = (BattlenetRpcErrorCode)methodCaller.DynamicInvoke(session, request, response);
                Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server Method: {1}) Returned: {2} Status: {3}.", session.GetClientInfo(), request, response, status);
                if (status == 0)
                    session.SendResponse(token, response);
                else
                    session.SendResponse(token, status);
            }
            else
            {
                status = (BattlenetRpcErrorCode)methodCaller.DynamicInvoke(session, request);
                Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server Method: {1}) Status: {2}.", session.GetClientInfo(), request, status);
                if (status != 0)
                    session.SendResponse(token, status);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ServiceAttribute : System.Attribute
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