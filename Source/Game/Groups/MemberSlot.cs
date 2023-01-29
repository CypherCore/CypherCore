// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Groups
{
    public class MemberSlot
    {
        public byte Class { get; set; }
        public GroupMemberFlags Flags { get; set; }
        public byte Group { get; set; }
        public ObjectGuid Guid { get; set; }
        public string Name { get; set; }
        public Race Race { get; set; }
        public bool ReadyChecked { get; set; }
        public LfgRoles Roles { get; set; }
    }
}