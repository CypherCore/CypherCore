// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Runtime.CompilerServices;

public class Cypher
{
    public static void Assert(bool value, string message = "", [CallerMemberName]string memberName = "")
    {
        if (!value)
        {
            if (!message.IsEmpty())
                Log.outFatal(LogFilter.Server, message);

            throw new Exception(memberName);
        }
    }
}
