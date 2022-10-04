/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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
