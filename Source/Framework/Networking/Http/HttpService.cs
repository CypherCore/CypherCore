// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Framework.Networking.Http
{
    public class DispatcherService
    {
        public DispatcherService(LogFilter logFilter)
        {
            _logger = logFilter;
        }

        public RequestHandlerResult HandleRequest(IAbstractSocket session, RequestContext context)
        {
            Log.outDebug(_logger, $"{session.GetClientInfo()} Starting request {context.request.Method} {context.request.Path}");

            string path = new Func<string>(() =>
            {
                string path = context.request.Path;
                int queryIndex = path.IndexOf('?');
                if (queryIndex != -1)
                    path = path.Substring(0, queryIndex);
                return path;
            })();

            context.handler = new Func<RequestHandler>(() =>
            {
                switch (context.request.Method)
                {
                    case "GET":
                    case "HEAD":
                        return _getHandlers.LookupByKey(path);
                    case "POST":
                        return _postHandlers.LookupByKey(path);
                    default:
                        return null;
                }
            })();

            DateTime responseDate = DateTime.Now;
            //context.response.Date = responseDate - Timezone.GetSystemZoneOffsetAt(responseDate);
            //context.response.Server.Add(BOOST_BEAST_VERSION_STRING);
            context.response.KeepAlive = context.request.KeepAlive;

            if (context.handler == null)
                return HandlePathNotFound(session, context);

            return context.handler.Func(session, context);
        }

        RequestHandlerResult HandleBadRequest(IAbstractSocket session, RequestContext context)
        {
            context.response.Status = HttpStatusCode.BadRequest;
            return RequestHandlerResult.Handled;
        }

        public RequestHandlerResult HandleUnauthorized(IAbstractSocket session, RequestContext context)
        {
            context.response.Status = HttpStatusCode.Unauthorized;
            return RequestHandlerResult.Handled;
        }

        RequestHandlerResult HandlePathNotFound(IAbstractSocket session, RequestContext context)
        {
            context.response.Status = HttpStatusCode.NotFound;
            return RequestHandlerResult.Handled;
        }

        public void RegisterHandler(HttpMethod method, string path, Func<IAbstractSocket, RequestContext, RequestHandlerResult> handler, RequestHandlerFlag flags = RequestHandlerFlag.None)
        {
            var handlerMap = new Func<Dictionary<string, RequestHandler>>(() =>
            {
                switch (method.Method)
                {
                    case "GET":
                        return _getHandlers;
                    case "POST":
                        return _postHandlers;
                    default:
                        //ABORT_MSG($"Tried to register a handler for unsupported HTTP method {method}");
                        return null;
                }
            })();

            handlerMap[path] = new RequestHandler() { Func = handler, Flags = flags };
            Log.outInfo(_logger, $"Registered new handler for {method} {path}");
        }

        Dictionary<string, RequestHandler> _getHandlers = new();
        Dictionary<string, RequestHandler> _postHandlers = new();

        LogFilter _logger;
    }

    public class SessionService
    {
        public SessionService(LogFilter logFilter)
        {
            _logger = logFilter;
        }

        public void InitAndStoreSessionState(SessionState state, IPAddress address)
        {
            state.RemoteAddress = address;

            // Generate session id
            lock (_sessionsMutex)
            {
                while (state.Id == Guid.Empty || _sessions.ContainsKey(state.Id))
                    state.Id = new(new byte[0].GenerateRandomKey(16));

                Log.outDebug(_logger, $"Client at {address} created new session {state.Id}");
                _sessions[state.Id] = state;
            }
        }

        public void Start()
        {
            _inactiveSessionsKillTimer = new System.Timers.Timer(TimeSpan.FromMinutes(1));
            _inactiveSessionsKillTimer.Elapsed += (_, _) => KillInactiveSessions();
            _inactiveSessionsKillTimer.Start();
        }

        public void Stop()
        {
            _inactiveSessionsKillTimer = null;

            lock (_sessionsMutex)
                _sessions.Clear();

            lock (_inactiveSessionsMutex)
                _inactiveSessions.Clear();
        }

        public SessionState FindAndRefreshSessionState(string id, IPAddress address)
        {
            SessionState state;

            lock (_sessionsMutex)
            {
                if (!_sessions.TryGetValue(Guid.Parse(id), out state))
                {
                    Log.outDebug(_logger, $"Client at {address} attempted to use a session {id} that was expired");
                    return null; // no session
                }
            }

            if (!state.RemoteAddress.Equals(address))
            {
                Log.outError(_logger, $"Client at {address} attempted to use a session {id} that was last accessed from {state.RemoteAddress}, denied access");
                return null;
            }

            lock (_inactiveSessionsMutex)
            {
                _inactiveSessions.Remove(state.Id);
            }

            return state;
        }

        public void MarkSessionInactive(Guid id)
        {
            bool wasActive = true;
            lock (_inactiveSessionsMutex)
            {
                wasActive = !_inactiveSessions.Contains(id);
                if (wasActive)
                    _inactiveSessions.Add(id);
            }

            if (wasActive)
            {
                lock (_sessionsMutex)
                {
                    var itr = _sessions.LookupByKey(id);
                    if (itr != null)
                    {
                        itr.InactiveTimestamp = DateTime.Now + TimeSpan.FromMinutes(5);
                        Log.outTrace(_logger, $"Session {id} marked as inactive");
                    }
                }
            }
        }

        void KillInactiveSessions()
        {
            lock (_inactiveSessionsMutex)
            {
                List<Guid> inactiveSessions = new(_inactiveSessions);
                _inactiveSessions.Clear();


                DateTime now = DateTime.Now;
                int inactiveSessionsCount = inactiveSessions.Count;

                lock (_sessionsMutex)
                {
                    foreach (var guid in inactiveSessions.ToList())
                    {
                        if (!_sessions.TryGetValue(guid, out var sessionState) || sessionState.InactiveTimestamp < now)
                        {
                            _sessions.Remove(guid);
                            inactiveSessions.Remove(guid);
                        }
                    }
                }

                Log.outDebug(_logger, $"Killed {inactiveSessionsCount - inactiveSessions.Count} inactive sessions");

                // restore sessions not killed to inactive queue
                foreach (var guid in inactiveSessions.ToList())
                {
                    _inactiveSessions.Add(guid);
                    inactiveSessions.Remove(guid);
                }
            }
        }

        object _sessionsMutex = new();
        Dictionary<Guid, SessionState> _sessions = new();

        object _inactiveSessionsMutex = new();
        List<Guid> _inactiveSessions = new();
        System.Timers.Timer _inactiveSessionsKillTimer;

        LogFilter _logger;
    }

    public class HttpService<SessionImpl> : SocketManager<SessionImpl> where SessionImpl : IAbstractSocket
    {
        protected DispatcherService dispatcherService;
        protected SessionService sessionService;

        public HttpService(LogFilter logFilter) : base()
        {
            dispatcherService = new(logFilter);
            sessionService = new(logFilter);
            _logger = logFilter;
        }

        public override bool StartNetwork(string bindIp, int port, int threadCount = 1)
        {
            if (!base.StartNetwork(bindIp, port, threadCount))
                return false;

            sessionService.Start();
            return true;
        }

        public override void StopNetwork()
        {
            sessionService.Stop();
            base.StopNetwork();
        }

        // http handling
        public delegate RequestHandlerResult HttpRequestHandler(SessionImpl session, RequestContext context);
        public void RegisterHandler(HttpMethod method, string path, HttpRequestHandler handler, RequestHandlerFlag flags = RequestHandlerFlag.None)
        {
            dispatcherService.RegisterHandler(method, path, (session, context) =>
            {
                return handler((SessionImpl)session, context);
            }, flags);
        }

        // session tracking
        public virtual SessionState CreateNewSessionState(IPAddress address)
        {
            var state = new SessionState();
            sessionService.InitAndStoreSessionState(state, address);
            return state;
        }

        class Thread : NetworkThread<SessionImpl>
        {
            protected override void SocketRemoved(SessionImpl session)
            {
                var id = session.GetSessionId();
                if (id.HasValue)
                    _service.MarkSessionInactive(id.Value);
            }

            public SessionService _service;
        }

        public NetworkThread<SessionImpl>[] CreateThreads()
        {
            Thread[] threads = new Thread[GetNetworkThreadCount()];
            for (int i = 0; i < GetNetworkThreadCount(); ++i)
                threads[i]._service = sessionService;
            return threads;
        }

        public DispatcherService GetDispatcherService() { return dispatcherService; }
        public SessionService GetSessionService() { return sessionService; }

        protected LogFilter _logger;
    }

    public class RequestHandler
    {
        public Func<IAbstractSocket, RequestContext, RequestHandlerResult> Func;
        public RequestHandlerFlag Flags;
    }

    public class RequestContext
    {
        public HttpHeader request = new();
        public HttpHeader response = new();
        public RequestHandler handler;
    }
}