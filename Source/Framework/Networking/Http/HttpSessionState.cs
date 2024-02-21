// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Net;

namespace Framework.Networking.Http
{
    public class SessionState
    {
        public Guid Id;
        public IPAddress RemoteAddress;
        public DateTime InactiveTimestamp = DateTime.MaxValue;
    }
}