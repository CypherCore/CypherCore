/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Game.Entities;

namespace Game.Groups
{
    public class GroupReference : Reference<Group, Player>
    {
        public GroupReference()
        {
            iSubGroup = 0;
        }

        ~GroupReference() { unlink(); }

        public override void targetObjectBuildLink()
        {
            getTarget().LinkMember(this);
        }

        public new GroupReference next() { return (GroupReference)base.next(); }

        public byte getSubGroup() { return iSubGroup; }

        public void setSubGroup(byte pSubGroup) { iSubGroup = pSubGroup; }

        byte iSubGroup;
    }

    public class GroupRefManager : RefManager<Group, Player>
    {
        public new GroupReference getFirst() { return (GroupReference)base.getFirst(); }
    }
}
