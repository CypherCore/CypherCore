// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;
using Game.Entities;

namespace Game.Groups
{
    public class GroupReference : Reference<Group, Player>
    {
        public GroupReference()
        {
            iSubGroup = 0;
        }

        ~GroupReference() { Unlink(); }

        public override void TargetObjectBuildLink()
        {
            GetTarget().LinkMember(this);
        }

        public new GroupReference Next() { return (GroupReference)base.Next(); }

        public byte GetSubGroup() { return iSubGroup; }

        public void SetSubGroup(byte pSubGroup) { iSubGroup = pSubGroup; }

        byte iSubGroup;
    }

    public class GroupRefManager : RefManager<Group, Player>
    {
        public new GroupReference GetFirst() { return (GroupReference)base.GetFirst(); }
    }
}
