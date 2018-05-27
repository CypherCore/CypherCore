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
using Google.Protobuf;
using System;
using System.Collections.Generic;

namespace Game.Services
{
    public class ServiceDispatcher : Singleton<ServiceDispatcher>
    {
        ServiceDispatcher()
        {
            AddService<GameUtilitiesService>(NameHash.GameUtilitiesService);
        }

        public void Dispatch(WorldSession session, uint serviceHash, uint token, uint methodId, CodedInputStream stream)
        {
            var action = _dispatchers.LookupByKey(serviceHash);
            if (action != null)
                action(session, serviceHash, token, methodId, stream);
            else
                Log.outDebug(LogFilter.SessionRpc, "{0} tried to call invalid service {1}", session.GetPlayerInfo(), serviceHash);
        }

        void AddService<Service>(NameHash OriginalHash) where Service : ServiceBase
        {
            _dispatchers[(uint)OriginalHash] = Dispatch<Service>;
        }

        void Dispatch<Service>(WorldSession session, uint serviceHash, uint token, uint methodId, CodedInputStream stream) where Service : ServiceBase
        {
            var obj = (Service)Activator.CreateInstance(typeof(Service), session, serviceHash);
            obj.CallServerMethod(token, methodId, stream);
        }

        Dictionary<uint, DispatcherHandler> _dispatchers = new Dictionary<uint, DispatcherHandler>();

        delegate void DispatcherHandler(WorldSession session, uint serviceHash, uint token, uint methodId, CodedInputStream stream);
    }

    abstract class ServiceBase
    {
        protected ServiceBase(WorldSession session, uint serviceHash)
        {
            _session = session;
            _serviceHash = serviceHash;
        }

        public abstract void CallServerMethod(uint token, uint methodId, CodedInputStream stream);

        public void SendRequest(uint methodId, IMessage request, Action<CodedInputStream> callback) { _session.SendBattlenetRequest(_serviceHash, methodId, request, callback); }
        public void SendRequest(uint methodId, IMessage request) { _session.SendBattlenetRequest(_serviceHash, methodId, request); }
        public void SendResponse(uint methodId, uint token, BattlenetRpcErrorCode status) { _session.SendBattlenetResponse(_serviceHash, methodId, token, status); }
        public void SendResponse(uint methodId, uint token, IMessage response) { _session.SendBattlenetResponse(_serviceHash, methodId, token, response); }

        public string GetCallerInfo()
        {
            return _session.GetPlayerInfo();
        }

        protected WorldSession _session;
        protected uint _serviceHash;
    }
}
