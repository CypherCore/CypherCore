// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IItem
{
    public interface IItemOnUse : IScriptObject
    {
        bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId);
    }
}