// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    internal class StoredAuraTeleportLocation
	{
		public enum State
		{
			Unchanged,
			Changed,
			Deleted
		}

		public State CurrentState { get; set; }
        public WorldLocation Loc { get; set; }
    }
}