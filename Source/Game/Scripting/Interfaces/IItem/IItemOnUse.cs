// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Scripting.Interfaces;
using Game.Spells;

namespace Game.Scripting.Interfaces.IItem
{
    public interface IItemOnUse : IScriptObject
    {
        bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId);
    }
}