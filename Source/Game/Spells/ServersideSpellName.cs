// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Entities
{
    internal struct ServersideSpellName
    {
        public SpellNameRecord Name;

        public ServersideSpellName(uint id, string name)
        {
            Name = new SpellNameRecord();
            Name.Name = new LocalizedString();

            Name.Id = id;

            for (Locale i = 0; i < Locale.Total; ++i)
                Name.Name[i] = name;
        }
    }
}