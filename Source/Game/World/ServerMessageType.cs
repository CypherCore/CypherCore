// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public enum ServerMessageType
    {
        ShutdownTime = 1,
        RestartTime = 2,
        String = 3,
        ShutdownCancelled = 4,
        RestartCancelled = 5,
        BgShutdownTime = 6,
        BgRestartTime = 7,
        InstanceShutdownTime = 8,
        InstanceRestartTime = 9,
        ContentReady = 10,
        TicketServicedSoon = 11,
        WaitTimeUnavailable = 12,
        TicketWaitTime = 13
    }
}