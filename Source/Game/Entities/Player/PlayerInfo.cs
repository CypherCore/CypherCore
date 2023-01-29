// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;

namespace Game.Entities
{
    public class PlayerInfo
    {
        public List<PlayerCreateInfoAction> Action { get; set; } = new();
        public List<uint>[] CastSpells { get; set; } = new List<uint>[(int)PlayerCreateMode.Max];
        public CreatePosition CreatePositionInfo;
        public CreatePosition? CreatePositionNPE;
        public List<uint> CustomSpells { get; set; } = new();

        public uint? IntroMovieId { get; set; }
        public uint? IntroSceneId { get; set; }
        public uint? IntroSceneIdNPE { get; set; }
        public List<PlayerCreateInfoItem> Item { get; set; } = new();

        public ItemContext ItemContext { get; set; }

        public PlayerLevelInfo[] LevelInfo { get; set; } = new PlayerLevelInfo[WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel)];
        public List<SkillRaceClassInfoRecord> Skills { get; set; } = new();

        public PlayerInfo()
        {
            for (var i = 0; i < CastSpells.Length; ++i)
                CastSpells[i] = new List<uint>();
        }

        public struct CreatePosition
        {
            public WorldLocation Loc { get; set; }
            public ulong? TransportGuid { get; set; }
        }
    }
}