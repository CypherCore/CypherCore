// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.DataStorage;

namespace Game.DungeonFinding
{
    public class LFGDungeonData
    {
        public float X, Y, Z, O;

        public LFGDungeonData(LFGDungeonsRecord dbc)
        {
            Id = dbc.Id;
            Name = dbc.Name[Global.WorldMgr.GetDefaultDbcLocale()];
            Map = (uint)dbc.MapID;
            Type = dbc.TypeID;
            Expansion = dbc.ExpansionLevel;
            Group = dbc.GroupID;
            ContentTuningId = dbc.ContentTuningID;
            Difficulty = dbc.DifficultyID;
            Seasonal = dbc.Flags[0].HasAnyFlag(LfgFlags.Seasonal);
        }

        public uint ContentTuningId { get; set; }
        public Difficulty Difficulty { get; set; }
        public uint Expansion { get; set; }
        public uint Group { get; set; }

        public uint Id { get; set; }
        public uint Map { get; set; }
        public string Name { get; set; }
        public ushort RequiredItemLevel { get; set; }
        public bool Seasonal { get; set; }
        public LfgType Type { get; set; }

        // Helpers
        public uint Entry()
        {
            return (uint)(Id + ((int)Type << 24));
        }
    }
}