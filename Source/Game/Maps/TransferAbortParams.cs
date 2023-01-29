// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Maps
{
    public class TransferAbortParams
    {
        public byte Arg { get; set; }
        public uint MapDifficultyXConditionId { get; set; }
        public TransferAbortReason Reason { get; set; }

        public TransferAbortParams(TransferAbortReason reason = TransferAbortReason.None, byte arg = 0, uint mapDifficultyXConditionId = 0)
        {
            Reason = reason;
            Arg = arg;
            MapDifficultyXConditionId = mapDifficultyXConditionId;
        }
    }
}