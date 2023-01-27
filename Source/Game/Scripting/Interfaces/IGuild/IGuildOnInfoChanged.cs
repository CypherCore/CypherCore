// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Guilds;

namespace Game.Scripting.Interfaces.IGuild
{
	public interface IGuildOnInfoChanged : IScriptObject
	{
		void OnInfoChanged(Guild guild, string newInfo);
	}
}