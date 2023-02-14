// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;
using Game.Maps;

namespace Game.Groups
{
    public class GroupInstanceReference : Reference<Group, InstanceMap>
    {
        ~GroupInstanceReference() { Unlink(); }

        public new GroupInstanceReference Next() { return (GroupInstanceReference)base.Next(); }

        public override void TargetObjectBuildLink()
        {
            GetTarget().LinkOwnedInstance(this);
        }
    }

    class GroupInstanceRefManager : RefManager<Group, InstanceMap>
    {
        public new GroupInstanceReference GetFirst() { return (GroupInstanceReference)base.GetFirst(); }
    }
}
