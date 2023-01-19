// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Database
{
    public static class DB
    {
        public static LoginDatabase Login = new();
        public static CharacterDatabase Characters = new();
        public static WorldDatabase World = new();
        public static HotfixDatabase Hotfix = new();
    }
}
