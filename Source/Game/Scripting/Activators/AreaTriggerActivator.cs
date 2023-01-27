using System;
using System.Collections.Generic;
using Game.AI;
using Game.Scripting.BaseScripts;

namespace Game.Scripting.Activators
{
	internal class AreaTriggerActivator : IScriptActivator
	{
		public List<string> ScriptBaseTypes => new()
		                                       {
			                                       nameof(AreaTriggerAI)
		                                       };

		public void Activate(Type type, string name, ScriptAttribute attribute)
		{
			Activator.CreateInstance(typeof(GenericAreaTriggerScript<>).MakeGenericType(type), name, attribute.Args);
		}
	}
}