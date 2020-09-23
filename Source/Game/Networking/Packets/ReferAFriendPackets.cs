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

using Framework.Constants;
using Game.Entities;
using System;

namespace Game.Networking.Packets
{
    public class RecruitAFriendFailure : ServerPacket
    {
        public RecruitAFriendFailure() : base(ServerOpcodes.RecruitAFriendFailure) { }

        public override void Write()
        {
            _worldPacket .WriteInt32((int)Reason);
            // Client uses this string only if Reason == ERR_REFER_A_FRIEND_NOT_IN_GROUP || Reason == ERR_REFER_A_FRIEND_SUMMON_OFFLINE_S
            // but always reads it from packet
            _worldPacket.WriteBits(Str.GetByteCount(), 6);
            _worldPacket.WriteString(Str);
        }

        public string Str;
        public ReferAFriendError Reason;
    }
}
