// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using BNetServer;

public static class Global
{
    public static RealmManager RealmMgr { get { return RealmManager.Instance; } }
    public static LoginServiceManager LoginServiceMgr { get { return LoginServiceManager.Instance; } }
}