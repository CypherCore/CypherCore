// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
