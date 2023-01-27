// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Guilds;

namespace Game.Scripting.Interfaces.IGuild
{
	public interface IGuildOnDisband : IScriptObject
	{
		void OnDisband(Guild guild);
	}
}