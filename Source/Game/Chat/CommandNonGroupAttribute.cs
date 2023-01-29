// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;

namespace Game.Chat
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandNonGroupAttribute : CommandAttribute
    {
        public CommandNonGroupAttribute(string command, CypherStrings help, RBACPermissions rbac, bool allowConsole = false) : base(command, help, rbac, allowConsole)
        {
        }

        public CommandNonGroupAttribute(string command, RBACPermissions rbac, bool allowConsole = false) : base(command, rbac, allowConsole)
        {
        }
    }
}