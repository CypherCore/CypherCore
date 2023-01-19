// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
