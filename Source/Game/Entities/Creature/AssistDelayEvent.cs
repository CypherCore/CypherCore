// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Dynamic;

namespace Game.Entities
{
    public class AssistDelayEvent : BasicEvent
	{
		private List<ObjectGuid> _assistants = new();
		private Unit _owner;


		private ObjectGuid _victim;

		private AssistDelayEvent()
		{
		}

		public AssistDelayEvent(ObjectGuid victim, Unit owner)
		{
			_victim = victim;
			_owner  = owner;
		}

		public override bool Execute(ulong e_time, uint p_time)
		{
			Unit victim = Global.ObjAccessor.GetUnit(_owner, _victim);

			if (victim != null)
				while (!_assistants.Empty())
				{
					Creature assistant = _owner.GetMap().GetCreature(_assistants[0]);
					_assistants.RemoveAt(0);

					if (assistant != null &&
					    assistant.CanAssistTo(_owner, victim))
					{
						assistant.SetNoCallAssistance(true);
						assistant.EngageWithTarget(victim);
					}
				}

			return true;
		}

		public void AddAssistant(ObjectGuid guid)
		{
			_assistants.Add(guid);
		}
	}
}