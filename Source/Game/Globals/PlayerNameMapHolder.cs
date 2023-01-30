// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game;
using Game.Entities;

internal class PlayerNameMapHolder
{
    private static readonly Dictionary<string, Player> _playerNameMap = new();

    public static void Insert(Player p)
    {
        _playerNameMap[p.GetName()] = p;
    }

    public static void Remove(Player p)
    {
        _playerNameMap.Remove(p.GetName());
    }

    public static Player Find(string name)
    {
        if (!ObjectManager.NormalizePlayerName(ref name))
            return null;

        return _playerNameMap.LookupByKey(name);
    }
}