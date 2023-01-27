// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Game.Entities;
using Game.Maps;
using Game.Scripting.Interfaces;

namespace Game.Scripting
{
	public abstract class ScriptObject : IScriptObject
	{
		private string _name;

		public ScriptObject(string name)
		{
			_name = name;
		}

		public string GetName()
		{
			return _name;
		}

		// Do not override this in scripts; it should be overridden by the various script type classes. It indicates
		// whether or not this script type must be assigned in the database.
		public virtual bool IsDatabaseBound()
		{
			return false;
		}

		public static T GetInstanceAI<T>(WorldObject obj) where T : class
		{
			InstanceMap instance = obj.GetMap().ToInstanceMap();

			if (instance != null &&
			    instance.GetInstanceScript() != null)
				return (T)Activator.CreateInstance(typeof(T),
				                                   new object[]
				                                   {
					                                   obj
				                                   });

			return null;
		}
	}

	public abstract class ScriptObjectAutoAdd : ScriptObject
	{
		protected ScriptObjectAutoAdd(string name) : base(name)
		{
			Global.ScriptMgr.AddScript(this);
		}
	}

	public abstract class ScriptObjectAutoAddDBBound : ScriptObject
	{
		protected ScriptObjectAutoAddDBBound(string name) : base(name)
		{
			Global.ScriptMgr.AddScript(this);
		}

		public override bool IsDatabaseBound()
		{
			return true;
		}
	}
}