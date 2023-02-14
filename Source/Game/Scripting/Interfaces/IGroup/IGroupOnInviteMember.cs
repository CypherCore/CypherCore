// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Groups;

namespace Game.Scripting.Interfaces.IGroup
{
    public interface IGroupOnInviteMember : IScriptObject
    {
        void OnInviteMember(Group group, ObjectGuid guid);
    }
}