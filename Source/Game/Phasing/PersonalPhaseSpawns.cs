// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game
{
    internal class PersonalPhaseSpawns
    {
        public static TimeSpan DELETE_TIME_DEFAULT = TimeSpan.FromMinutes(1);
        public TimeSpan? DurationRemaining;
        public List<ushort> Grids { get; set; } = new();

        public List<WorldObject> Objects { get; set; } = new();

        public bool IsEmpty()
        {
            return Objects.Empty() && Grids.Empty();
        }
    }
}