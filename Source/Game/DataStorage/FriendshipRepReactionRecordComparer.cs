// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.DataStorage
{
    internal class FriendshipRepReactionRecordComparer : IComparer<FriendshipRepReactionRecord>
    {
        public int Compare(FriendshipRepReactionRecord left, FriendshipRepReactionRecord right)
        {
            return left.ReactionThreshold.CompareTo(right.ReactionThreshold);
        }
    }
}