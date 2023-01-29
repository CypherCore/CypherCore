// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.DungeonFinding
{
    public class LfgUpdateData
    {
        public LfgUpdateData(LfgUpdateType _type = LfgUpdateType.Default)
        {
            UpdateType = _type;
            State = LfgState.None;
        }

        public LfgUpdateData(LfgUpdateType _type, List<uint> _dungeons)
        {
            UpdateType = _type;
            State = LfgState.None;
            Dungeons = _dungeons;
        }

        public LfgUpdateData(LfgUpdateType _type, LfgState _state, List<uint> _dungeons)
        {
            UpdateType = _type;
            State = _state;
            Dungeons = _dungeons;
        }

        public List<uint> Dungeons { get; set; } = new();
        public LfgState State { get; set; }

        public LfgUpdateType UpdateType { get; set; }
    }
}