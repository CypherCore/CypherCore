// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Maps;

namespace Game
{
    public class CellObjectGuids
    {
        public SortedSet<ulong> Creatures { get; set; } = new();
        public SortedSet<ulong> Gameobjects { get; set; } = new();

        public void AddSpawn(SpawnData data)
        {
            switch (data.type)
            {
                case SpawnObjectType.Creature:
                    Creatures.Add(data.SpawnId);

                    break;
                case SpawnObjectType.GameObject:
                    Gameobjects.Add(data.SpawnId);

                    break;
            }
        }

        public void RemoveSpawn(SpawnData data)
        {
            switch (data.type)
            {
                case SpawnObjectType.Creature:
                    Creatures.Remove(data.SpawnId);

                    break;
                case SpawnObjectType.GameObject:
                    Gameobjects.Remove(data.SpawnId);

                    break;
            }
        }
    }
}