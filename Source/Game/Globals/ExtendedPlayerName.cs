// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public struct ExtendedPlayerName
    {
        public ExtendedPlayerName(string name, string realmName)
        {
            Name = name;
            Realm = realmName;
        }

        public string Name { get; set; }
        public string Realm { get; set; }
    }
}