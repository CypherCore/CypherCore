// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.BattleGrounds
{
    public class BattlegroundTemplate
    {
        public BattlemasterListRecord BattlemasterEntry { get; set; }
        public BattlegroundTypeId Id { get; set; }
        public float MaxStartDistSq { get; set; }
        public uint ScriptId { get; set; }
        public WorldSafeLocsEntry[] StartLocation { get; set; } = new WorldSafeLocsEntry[SharedConst.PvpTeamsCount];
        public byte Weight { get; set; }

        public bool IsArena()
        {
            return BattlemasterEntry.InstanceType == (uint)MapTypes.Arena;
        }

        public ushort GetMinPlayersPerTeam()
        {
            return (ushort)BattlemasterEntry.MinPlayers;
        }

        public ushort GetMaxPlayersPerTeam()
        {
            return (ushort)BattlemasterEntry.MaxPlayers;
        }

        public byte GetMinLevel()
        {
            return BattlemasterEntry.MinLevel;
        }

        public byte GetMaxLevel()
        {
            return BattlemasterEntry.MaxLevel;
        }
    }
}