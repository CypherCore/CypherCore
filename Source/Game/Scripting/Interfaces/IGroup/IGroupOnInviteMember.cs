// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Groups;

namespace Game.Scripting.Interfaces.IGroup
{
	public interface IGroupOnInviteMember : IScriptObject
	{
		void OnInviteMember(Group group, ObjectGuid guid);
	}
}