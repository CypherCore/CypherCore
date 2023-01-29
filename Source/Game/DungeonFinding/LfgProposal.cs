// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.DungeonFinding
{
    public class LfgProposal
    {
        public long CancelTime { get; set; }
        public uint DungeonId { get; set; }
        public uint Encounters { get; set; }
        public ObjectGuid Group;

        public uint Id { get; set; }
        public bool IsNew { get; set; }
        public ObjectGuid Leader;
        public Dictionary<ObjectGuid, LfgProposalPlayer> Players { get; set; } = new(); // Players _data
        public List<ObjectGuid> Queues { get; set; } = new();
        public List<ulong> Showorder { get; set; } = new();
        public LfgProposalState State { get; set; }

        public LfgProposal(uint dungeon = 0)
        {
            Id = 0;
            DungeonId = dungeon;
            State = LfgProposalState.Initiating;
            Group = ObjectGuid.Empty;
            Leader = ObjectGuid.Empty;
            CancelTime = 0;
            Encounters = 0;
            IsNew = true;
        }
    }
}