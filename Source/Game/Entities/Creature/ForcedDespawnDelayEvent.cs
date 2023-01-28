// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Dynamic;

namespace Game.Entities
{
    public class ForcedDespawnDelayEvent : BasicEvent
	{
		private Creature _owner;
		private TimeSpan _respawnTimer;

		public ForcedDespawnDelayEvent(Creature owner, TimeSpan respawnTimer = default)
		{
			_owner        = owner;
			_respawnTimer = respawnTimer;
		}

		public override bool Execute(ulong e_time, uint p_time)
		{
			_owner.DespawnOrUnsummon(TimeSpan.Zero, _respawnTimer); // since we are here, we are not TempSummon as object Type cannot change during runtime

			return true;
		}
	}
}