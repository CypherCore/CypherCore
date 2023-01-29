// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.AI
{
    public class EntryCheckPredicate : ICheck<ObjectGuid>
    {
        private readonly uint _entry;

        public EntryCheckPredicate(uint entry)
        {
            _entry = entry;
        }

        public bool Invoke(ObjectGuid guid)
        {
            return guid.GetEntry() == _entry;
        }
    }
}