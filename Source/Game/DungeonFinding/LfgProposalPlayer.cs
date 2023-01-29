// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.DungeonFinding
{
    public class LfgProposalPlayer
    {
        public ObjectGuid Group;

        public LfgProposalPlayer()
        {
            Role = 0;
            Accept = LfgAnswer.Pending;
            Group = ObjectGuid.Empty;
        }

        public LfgAnswer Accept { get; set; }

        public LfgRoles Role { get; set; }
    }
}