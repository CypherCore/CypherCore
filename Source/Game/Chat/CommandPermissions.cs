// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Chat
{
    public struct CommandPermissions
    {
        public RBACPermissions RequiredPermission;
        public bool AllowConsole;

        public CommandPermissions(RBACPermissions perm, bool allowConsole)
        {
            RequiredPermission = perm;
            AllowConsole = allowConsole;
        }
    }
}