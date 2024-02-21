// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.Web;
using System;
using System.Net.Sockets;

namespace Framework.Networking.Http
{
    public interface IAbstractSocket : ISocket
    {
        public void SendResponse(RequestContext context);

        public void QueueQuery(QueryCallback queryCallback);

        public string GetClientInfo();

        public Guid? GetSessionId();
    }

    public abstract class BaseSocket<Derived> : SSLSocket, IAbstractSocket
    {
        public BaseSocket(Socket socket) : base(socket) { }

        public async override void ReadHandler(byte[] data, int receivedLength)
        {
            if (!IsOpen())
                return;

            if (!HttpHelper.ParseRequest(data, receivedLength, out var httpRequest))
            {
                CloseSocket();
                return;
            }

            if (!HandleMessage(httpRequest))
            {
                CloseSocket();
                return;
            }

            await AsyncRead();
        }

        bool HandleMessage(HttpHeader httpRequest)
        {
            RequestContext context = new() { request = httpRequest };

            if (_state == null)
                _state = ObtainSessionState(context);

            RequestHandlerResult status = RequestHandler(context);

            if (status != RequestHandlerResult.Async)
                SendResponse(context);

            return status != RequestHandlerResult.Error;
        }

        public virtual RequestHandlerResult RequestHandler(RequestContext context) { return 0; }

        public async void SendResponse(RequestContext context)
        {
            Log.outDebug(LogFilter.Http, $"{GetClientInfo()} Request {context.request.Method} {context.request.Path} done, status {context.response.Status}");

            {
                bool canLogRequestContent = context.handler == null || !context.handler.Flags.HasFlag(RequestHandlerFlag.DoNotLogRequestContent);
                bool canLogResponseContent = context.handler == null || !context.handler.Flags.HasFlag(RequestHandlerFlag.DoNotLogResponseContent);
                Log.outTrace(LogFilter.Http, $"{GetClientInfo()} Request: {(canLogRequestContent ? context.request.Content : "<REDACTED>")}");
                Log.outTrace(LogFilter.Http, $"{GetClientInfo()} Response: {(canLogResponseContent ? context.response.Content : "<REDACTED>")}");
            }

            await AsyncWrite(HttpHelper.CreateResponse(context));

            if (!context.response.KeepAlive)
                CloseSocket();
        }

        public void QueueQuery(QueryCallback queryCallback)
        {
            _queryProcessor.AddCallback(queryCallback);
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            this._queryProcessor.ProcessReadyCallbacks();
            return true;
        }

        public string GetClientInfo()
        {
            string info = GetRemoteIpAddress().ToString();
            if (_state != null)
                info += $", Session Id: {_state.Id}";

            info += "]";
            return info;
        }

        public Guid? GetSessionId()
        {
            if (_state != null)
                return _state.Id;

            return null;
        }

        public virtual SessionState ObtainSessionState(RequestContext context) { return null; }

        protected AsyncCallbackProcessor<QueryCallback> _queryProcessor = new();
        protected SessionState _state;
    }
}