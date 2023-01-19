// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
