using System;
using Game.AI;
using Game.Entities;
using Game.Scripting.Interfaces.IAreaTriggerEntity;

namespace Game.Scripting.BaseScripts
{
	public class GenericAreaTriggerScript<AI> : ScriptObjectAutoAddDBBound, IAreaTriggerEntityGetAI where AI : AreaTriggerAI
	{
		private object[] _args;

		public GenericAreaTriggerScript(string name, object[] args) : base(name)
		{
			_args = args;
		}

		public AreaTriggerAI GetAI(AreaTrigger me)
		{
			if (me.GetInstanceScript() != null)
				return GetInstanceAI<AI>(me);
			else
				return (AI)Activator.CreateInstance(typeof(AI),
				                                    new object[]
				                                    {
					                                    me
				                                    }.Combine(_args));
		}
	}
}