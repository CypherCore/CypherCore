/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Game.Services
{
    class ServiceManager
    {
        public static void Initialize()
        {
            Assembly currentAsm = Assembly.GetExecutingAssembly();
            foreach (var type in currentAsm.GetTypes())
            {
                foreach (var methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    foreach (var msgAttr in methodInfo.GetCustomAttributes<BnetServiceAttribute>())
                    {
                        if (msgAttr == null)
                            continue;

                        if (_clientPacketTable.ContainsKey(msgAttr.Hash) && _clientPacketTable[msgAttr.Hash].ContainsKey(msgAttr.MethodId))
                        {
                            //Log.outError(LogFilter.Network, "Tried to override OpcodeHandler of {0} with {1} (Opcode {2})", _clientPacketTable[msgAttr.Opcode].ToString(), methodInfo.Name, msgAttr.Opcode);
                            continue;
                        }

                        var parameters = methodInfo.GetParameters();
                        if (parameters.Length == 0)
                        {
                            Log.outError(LogFilter.Network, "Method: {0} Has no paramters", methodInfo.Name);
                            continue;
                        }

                        if (!_clientPacketTable.ContainsKey(msgAttr.Hash))
                            _clientPacketTable[msgAttr.Hash] = new Dictionary<uint, ServiceHandler>();

                        _clientPacketTable[msgAttr.Hash][msgAttr.MethodId] = new ServiceHandler(methodInfo, parameters[0].ParameterType);
                    }
                }
            }
        }

        public static ServiceHandler GetHandler(NameHash hash, uint methodId)
        {
            if (!_clientPacketTable.ContainsKey(hash))
                return null;

            return _clientPacketTable[hash].LookupByKey(methodId);
        }

        static ConcurrentDictionary<NameHash, Dictionary<uint, ServiceHandler>> _clientPacketTable = new ConcurrentDictionary<NameHash, Dictionary<uint, ServiceHandler>>();
    }

    public class ServiceHandler
    {
        public ServiceHandler(MethodInfo info, Type type)
        {
            methodCaller = (Func<WorldSession, IMessage, BattlenetRpcErrorCode>)GetType().GetMethod("CreateDelegate", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type).Invoke(null, new object[] { info });
            packetType = type;
        }

        public BattlenetRpcErrorCode Invoke(WorldSession session, CodedInputStream stream)
        {
            var request = (IMessage)Activator.CreateInstance(packetType);
            request.MergeFrom(stream);

            return methodCaller(session, request);
        }

        static Func<WorldSession, IMessage, BattlenetRpcErrorCode> CreateDelegate<P1>(MethodInfo method) where P1 : IMessage
        {
            // create first delegate. It is not fine because its 
            // signature contains unknown types T and P1
            Func<WorldSession, P1, BattlenetRpcErrorCode> d = (Func<WorldSession, P1, BattlenetRpcErrorCode>)method.CreateDelegate(typeof(Func<WorldSession, P1, BattlenetRpcErrorCode>));
            // create another delegate having necessary signature. 
            // It encapsulates first delegate with a closure
            return delegate (WorldSession target, IMessage p) { return d(target, (P1)p); };
        }

        Func<WorldSession, IMessage, BattlenetRpcErrorCode> methodCaller;
        Type packetType;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class BnetServiceAttribute : Attribute
    {
        public BnetServiceAttribute(NameHash hash, uint methodId)
        {
            Hash = hash;
            MethodId = methodId;
        }

        public NameHash Hash { get; }
        public uint MethodId { get; }
    }
}
