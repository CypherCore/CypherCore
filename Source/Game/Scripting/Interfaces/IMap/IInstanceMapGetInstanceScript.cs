// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Maps;

namespace Game.Scripting.Interfaces.IMap
{
	public interface IInstanceMapGetInstanceScript : IScriptObject
	{
		InstanceScript GetInstanceScript(InstanceMap map);
	}
}