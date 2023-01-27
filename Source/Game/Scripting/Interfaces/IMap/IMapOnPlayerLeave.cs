// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Maps;

namespace Game.Scripting.Interfaces.IMap
{
	public interface IMapOnPlayerLeave<T> : IScriptObject where T : Map
	{
		void OnPlayerLeave(T map, Player player);
	}
}