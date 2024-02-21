// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using BNetServer;
using BNetServer.Networking;
using BNetServer.REST;

public static class Global
{
    public static RealmManager RealmMgr { get { return RealmManager.Instance; } }
    public static SessionManager SessionMgr { get { return SessionManager.Instance; } }
    public static LoginRESTService LoginService { get { return LoginRESTService.Instance; } }
    public static LoginServiceManager LoginServiceMgr { get { return LoginServiceManager.Instance; } }
}