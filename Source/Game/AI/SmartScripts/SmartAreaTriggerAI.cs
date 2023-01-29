// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.AI
{
    public class SmartAreaTriggerAI : AreaTriggerAI
	{
		private readonly SmartScript _script = new();

		public SmartAreaTriggerAI(AreaTrigger areaTrigger) : base(areaTrigger)
		{
		}

		public override void OnInitialize()
		{
			GetScript().OnInitialize(at);
		}

		public override void OnUpdate(uint diff)
		{
			GetScript().OnUpdate(diff);
		}

		public override void OnUnitEnter(Unit unit)
		{
			GetScript().ProcessEventsFor(SmartEvents.AreatriggerOntrigger, unit);
		}

		public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker)
		{
			GetScript().SetTimedActionList(e, entry, invoker);
		}

		public SmartScript GetScript()
		{
			return _script;
		}
	}
}